using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Common.Objects;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Interfaces;

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
                if (envResult.IsError)
                {
                    if (envResult.Code == ErrorCode.NotFound)
                    {
                        IResult creationResult = await _domainObjectManager.CreateEnvironment(identifier, CancellationToken.None);
                        if (creationResult.IsError)
                        {
                            return creationResult;
                        }
                    }
                }

                IResult layerAssignResult = await _domainObjectManager.AssignEnvironmentLayers(identifier, envExport.Layers, CancellationToken.None);
                if (layerAssignResult.IsError)
                {
                    return layerAssignResult;
                }
            }

            return Result.Success();
        }
    }
}
