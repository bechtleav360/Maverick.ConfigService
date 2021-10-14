using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Implementations.Health;
using Bechtle.A365.ConfigService.Interfaces;
using LiteDB;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Implementations
{
    /// <summary>
    ///     Service that checks if the local projection-cache is compatible with this app-version.
    ///     If not, the cache will be removed.
    ///     No requests should be processed before this check completes, so this will trigger a readiness check.
    /// </summary>
    public class ProjectionCacheCleanupService : BackgroundService
    {
        private readonly ILiteDatabase _database;
        private readonly ProjectionCacheCompatibleCheck _healthCheck;
        private readonly IDomainObjectStoreLocationProvider _locationProvider;
        private readonly ILogger<ProjectionCacheCleanupService> _logger;

        /// <summary>
        ///     Returns the Service-Version (Major.Minor.Patch.Build).
        ///     In Debug-Builds, the Build-Version will always be empty.
        ///     This relies on the fact, that our Build-Pipeline always sets an
        ///     incrementing build-version before releasing the artifact.
        ///     1.2.3 in our .csproj becomes 1.2.3.x, where x is the build# / 1.2.3.12 / 1.3.0.13 / ...
        /// </summary>
        private static string AppVersion => Assembly.GetExecutingAssembly()
                                                    .GetName()
                                                    .Version
                                                    ?.ToString(4);

        /// <inheritdoc />
        public ProjectionCacheCleanupService(
            IDomainObjectStoreLocationProvider locationProvider,
            ProjectionCacheCompatibleCheck healthCheck,
            ILogger<ProjectionCacheCleanupService> logger)
        {
            _locationProvider = locationProvider;
            _healthCheck = healthCheck;
            _logger = logger;
            _database = new LiteDatabase(
                new ConnectionString
                {
                    Connection = ConnectionType.Shared,
                    Filename = locationProvider.FileName
                });
        }

        /// <inheritdoc />
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // make sure we're not ready to begin with
            _healthCheck.SetReady(false);

            if (IsDbVersionMatching())
            {
                _logger.LogInformation("cache-version matches app-version, green-lighting cache");
                _healthCheck.SetReady();
                return Task.CompletedTask;
            }

            _logger.LogInformation("cache-version does not match app-version, removing cache and re-projecting");

            if (RemoveProjectionCache())
            {
                _logger.LogInformation("cache removed, writing current version '{Version}'", AppVersion);
                if (RecordCurrentVersion())
                {
                    _logger.LogInformation("cache marked with current version '{Version}'", AppVersion);
                    _healthCheck.SetReady();
                    return Task.CompletedTask;
                }

                _logger.LogInformation(
                    "unable to mark cache with current version, "
                    + "this is fatal and prevents this service from properly starting. "
                    + "To start this Service, manually delete the cache, "
                    + "and make sure this app has write-access to the directory ({CacheDirectory})",
                    _locationProvider.Directory);
            }

            _logger.LogError(
                "One or more errors encountered while removing incompatible projection-cache, see previous log-entries. "
                + "This Service will not be ready due to this.");

            return Task.CompletedTask;
        }

        /// <summary>
        ///     Check if the underlying DB belongs to the current App-Version, or needs to be recreated
        /// </summary>
        private bool IsDbVersionMatching()
        {
            try
            {
                string activeVersion = AppVersion;

                if (activeVersion is null)
                {
                    throw new InvalidOperationException(
                        "unable to access version of executing assembly - cannot guarantee");
                }

                ILiteCollection<AppCacheVersion> collection = _database.GetCollection<AppCacheVersion>("_storageVersion");
                string storageVersion = collection.Query()
                                                  .FirstOrDefault()
                                                  ?.ApplicationVersion
                                        ?? string.Empty;

                // if nothing is stored (no db, or app-version that doesn't store this data)
                // then we can't assume it's working - nuke it anyway
                // only if the stored version matches exactly what we're currently running as
                // then we're letting the cache be
                return activeVersion.Equals(storageVersion, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "error while checking cache compatibility");
                return false;
            }
        }

        private bool RecordCurrentVersion()
        {
            try
            {
                ILiteCollection<AppCacheVersion> collection = _database.GetCollection<AppCacheVersion>("_storageVersion");
                collection.EnsureIndex(c => c.Id);

                // ensure we only ever write one version
                collection.DeleteAll();
                collection.Insert(
                    new AppCacheVersion
                    {
                        Id = Guid.NewGuid(),
                        ApplicationVersion = AppVersion,
                        CreatedAt = DateTime.UtcNow
                    });

                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "unable to write current version to db for future compatibility-checks");
                return false;
            }
        }

        /// <summary>
        ///     Delete the contents of the directory where our Projection-Cache is located.
        ///     Currently, this is './data/cache' and everything in that folder is volatile.
        /// </summary>
        private bool RemoveProjectionCache()
        {
            var errorEncountered = false;
            var directory = new DirectoryInfo(_locationProvider.Directory);

            if (!directory.Exists)
            {
                _logger.LogInformation(
                    "projection-cache doesn't exist ('{CacheDirectory}'), nothing to clean up",
                    directory.FullName);
                return true;
            }

            _logger.LogInformation(
                "deleting old projection-cache - everything below '{CacheDirectory}'",
                directory.FullName);

            // delete everything in our cache-directory, but leave the actual directory intact.
            // ---
            // this is the important step, delete all cache-files
            foreach (FileInfo file in directory.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                try
                {
                    _logger.LogDebug("deleting cache-file; {File}", file.FullName);
                    file.Delete();
                }
                catch (IOException e)
                {
                    _logger.LogError(e, "unable to delete file '{File}'", file.FullName);
                    errorEncountered = true;
                }
            }

            // this is to be thorough, and not strictly necessary
            foreach (DirectoryInfo subDirectory in directory.EnumerateDirectories("*", SearchOption.AllDirectories))
            {
                try
                {
                    _logger.LogDebug("deleting cache-directory; {Directory}", subDirectory.FullName);
                    subDirectory.Delete(true);
                }
                catch (IOException e)
                {
                    _logger.LogError(e, "unable to delete directory '{Directory}'", subDirectory.FullName);
                    errorEncountered = true;
                }
            }

            _logger.LogInformation("done deleting projection-cache");
            return !errorEncountered;
        }

        /// <summary>
        ///     Class containing Metadata for the compatibility of this Cache
        /// </summary>
        internal class AppCacheVersion
        {
            /// <summary>
            ///     Application-Version with which this cache was built
            /// </summary>
            public string ApplicationVersion { get; set; }

            /// <summary>
            ///     Time at which this record was created
            /// </summary>
            public DateTime CreatedAt { get; set; }

            /// <summary>
            ///     Unique Id of this Entry
            /// </summary>
            public Guid Id { get; set; }
        }
    }
}
