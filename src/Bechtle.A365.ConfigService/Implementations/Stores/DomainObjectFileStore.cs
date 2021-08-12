using System;
using System.IO;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Interfaces;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Implementations.Stores
{
    /// <summary>
    ///     Component that stores Objects in serialized form in a local Directory
    /// </summary>
    public class DomainObjectFileStore : IDomainObjectFileStore
    {
        private readonly IDomainObjectStoreLocationProvider _locationProvider;
        private readonly ILogger<DomainObjectFileStore> _logger;

        /// <summary>
        ///     Create a new instance of <see cref="DomainObjectFileStore" />
        /// </summary>
        /// <param name="logger">logger to send diagnostics to</param>
        /// <param name="locationProvider">determines where the root-directory of this Store should be</param>
        public DomainObjectFileStore(
            ILogger<DomainObjectFileStore> logger,
            IDomainObjectStoreLocationProvider locationProvider)
        {
            _logger = logger;
            _locationProvider = locationProvider;
        }

        /// <inheritdoc />
        public async Task<IResult<TObject>> LoadObject<TObject, TIdentifier>(
            Guid fileId,
            long version)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier
        {
            FileInfo location = MakeFileLocation<TObject, TIdentifier>(fileId, version);

            try
            {
                await using FileStream fileStream = location.OpenRead();
                var obj = await JsonSerializer.DeserializeAsync<TObject>(
                              fileStream,
                              new JsonSerializerOptions
                              {
                                  AllowTrailingCommas = true,
                                  ReadCommentHandling = JsonCommentHandling.Skip,
                                  PropertyNameCaseInsensitive = true
                              });

                return Result.Success(obj);
            }
            catch (IOException e)
            {
                _logger.LogWarning(e, "unable to load stored object from local file {Filepath}", location);
                return Result.Error<TObject>($"unable to load stored object from local file {location}", ErrorCode.IOError);
            }
            catch (JsonException e)
            {
                _logger.LogWarning(e, "unable to deserialize stored object to type {ObjectType}", typeof(TObject).Name);
                return Result.Error<TObject>($"unable to deserialize stored object to type {typeof(TObject).Name}", ErrorCode.SerializationFailed);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "unable to read stored object, unknown error occured");
                return Result.Error<TObject>("unknown error occured while reading object from file-storage", ErrorCode.Undefined);
            }
        }

        private FileInfo MakeFileLocation<TObject, TIdentifier>(
            Guid fileId,
            long version)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier
            => new FileInfo(
                Path.Combine(
                    _locationProvider.Directory,
                    Convert.ToBase64String(Encoding.UTF8.GetBytes(typeof(TObject).Name)),
                    Convert.ToBase64String(fileId.ToByteArray()),
                    version.ToString("x16")));

        /// <inheritdoc />
        public Task<IResult> DeleteObject<TObject, TIdentifier>(
            Guid fileId,
            long version)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier
        {
            FileInfo location = MakeFileLocation<TObject, TIdentifier>(fileId, version);

            try
            {
                location.Delete();
                return Task.FromResult(Result.Success());
            }
            catch (SecurityException e)
            {
                _logger.LogWarning(e, "unable to delete stored object from local file {Filepath}", location);
                return Task.FromResult(Result.Error($"unable to delete stored object from local file {location}", ErrorCode.IOError));
            }
            catch (UnauthorizedAccessException e)
            {
                _logger.LogWarning(e, "unable to delete stored object from local file {Filepath}", location);
                return Task.FromResult(Result.Error($"unable to delete stored object from local file {location}", ErrorCode.IOError));
            }
            catch (IOException e)
            {
                _logger.LogWarning(e, "unable to delete stored object from local file {Filepath}", location);
                return Task.FromResult(Result.Error($"unable to delete stored object from local file {location}", ErrorCode.IOError));
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "unable to delete stored object from local file {Filepath}", location);
                return Task.FromResult(Result.Error($"unable to delete stored object from local file {location}", ErrorCode.Undefined));
            }
        }

        /// <inheritdoc />
        public async Task<IResult> StoreObject<TObject, TIdentifier>(
            TObject obj,
            Guid fileId)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier
        {
            FileInfo location = MakeFileLocation<TObject, TIdentifier>(fileId, obj.CurrentVersion);

            try
            {
                // try to create the directory
                location.Directory?.Create();
                await using FileStream fileStream = location.OpenWrite();
                await JsonSerializer.SerializeAsync(
                    fileStream,
                    obj,
                    new JsonSerializerOptions
                    {
                        WriteIndented = false
                    });

                return Result.Success();
            }
            catch (JsonException e)
            {
                _logger.LogWarning(e, "unable to serialize domainObject");
                return Result.Error("unable to serialize object", ErrorCode.SerializationFailed);
            }
            catch (IOException e)
            {
                _logger.LogWarning(e, "unable to store object in local file {Filepath}", location);
                return Result.Error($"unable to store object in local file {location}", ErrorCode.IOError);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "unable to store object in local file, unknown error occured");
                return Result.Error("unknown error occured while writing object to file-storage", ErrorCode.Undefined);
            }
        }
    }
}
