using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.Objects;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Interfaces;
using Polly;

namespace Bechtle.A365.ConfigService.Implementations
{
    /// <inheritdoc />
    public class DataImporter : IDataImporter
    {
        private readonly IDomainObjectManager _domainObjectManager;

        /// <inheritdoc cref="DataImporter" />
        public DataImporter(IDomainObjectManager domainObjectManager)
        {
            _domainObjectManager = domainObjectManager;
        }

        /// <inheritdoc />
        public async Task<IResult> Import(ConfigExport export)
        {
            if (export is null)
            {
                return Result.Error($"{nameof(export)} must not be null", ErrorCode.InvalidData);
            }

            foreach (LayerExport layerExport in export.Layers)
            {
                var identifier = new LayerIdentifier(layerExport.Name);

                IResult importResult = await _domainObjectManager.ImportLayer(
                                           identifier,
                                           layerExport.Keys
                                                      .Select(k => new EnvironmentLayerKey(k.Key, k.Value, k.Type, k.Description, 0))
                                                      .ToList(),
                                           CancellationToken.None);

                if (importResult.IsError)
                {
                    return importResult;
                }
            }

            foreach (EnvironmentExport envExport in export.Environments)
            {
                var identifier = new EnvironmentIdentifier(envExport.Category, envExport.Name);

                IResult<ConfigEnvironment> envResult = await _domainObjectManager.GetEnvironment(identifier, CancellationToken.None);
                if (envResult.IsError && envResult.Code == ErrorCode.NotFound)
                {
                    // previous actions possibly wrote events that need to be projected, now we have to wait for this to be done
                    // each time we attempt to write these events the ES can tell us that our projected- and the stream-version are different
                    // if that happens, we wait a little and try again
                    IResult creationResult =
                        await Policy.HandleResult<IResult>(r => r.IsError)
                                    .WaitAndRetryAsync(5, _ => TimeSpan.FromSeconds(4))
                                    .ExecuteAsync(() => _domainObjectManager.CreateEnvironment(identifier, CancellationToken.None));

                    if (creationResult.IsError)
                    {
                        return creationResult;
                    }
                }

                // same deal as above
                IResult layerAssignResult =
                    await Policy.HandleResult<IResult>(r => r.IsError)
                                .WaitAndRetryAsync(5, _ => TimeSpan.FromSeconds(4))
                                .ExecuteAsync(() => _domainObjectManager.AssignEnvironmentLayers(identifier, envExport.Layers, CancellationToken.None));

                if (layerAssignResult.IsError)
                {
                    return layerAssignResult;
                }
            }

            return Result.Success();
        }
    }
}
