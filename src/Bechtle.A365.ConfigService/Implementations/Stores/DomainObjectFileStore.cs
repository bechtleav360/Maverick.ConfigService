using System;
using System.IO;
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
        public async Task<IResult<TObject>> LoadObject<TObject, TIdentifier>(TIdentifier identifier, long version)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier
        {
            var location = new FileInfo(
                Path.Combine(
                    _locationProvider.Directory,
                    Base64Encode(typeof(TObject).Name),
                    Base64Encode(
                        identifier?.ToString()
                        ?? throw new ArgumentNullException(
                            nameof(identifier),
                            "identifier or identifier.ToString is null")),
                    version.ToString("x16")));

            try
            {
                await using FileStream fileStream = location.OpenRead();
                var obj = await JsonSerializer.DeserializeAsync<TObject>(fileStream);

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
        }

        /// <inheritdoc />
        public async Task<IResult> StoreObject<TObject, TIdentifier>(TObject obj)
            where TObject : DomainObject<TIdentifier>
            where TIdentifier : Identifier
        {
            var location = new FileInfo(
                Path.Combine(
                    _locationProvider.Directory,
                    Base64Encode(typeof(TObject).Name),
                    Base64Encode(obj.Id.ToString()),
                    obj.CurrentVersion.ToString("x16")));

            try
            {
                // try to create the directory
                location.Directory?.Create();
                await using FileStream fileStream = location.OpenWrite();
                await JsonSerializer.SerializeAsync(fileStream, obj);

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
        }

        private string Base64Encode(string str) => Convert.ToBase64String(Encoding.UTF8.GetBytes(str));
    }
}
