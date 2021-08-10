using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Interfaces;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Bechtle.A365.ConfigService.Models.V1;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Implementations.Stores
{
    /// <inheritdoc />
    public sealed class StructureProjectionStore : IStructureProjectionStore
    {
        private readonly IDomainObjectManager _domainObjectManager;
        private readonly ILogger<StructureProjectionStore> _logger;

        /// <inheritdoc cref="StructureProjectionStore" />
        public StructureProjectionStore(
            ILogger<StructureProjectionStore> logger,
            IDomainObjectManager domainObjectManager)
        {
            _logger = logger;
            _domainObjectManager = domainObjectManager;
        }

        /// <inheritdoc />
        public ValueTask DisposeAsync() => new ValueTask(Task.CompletedTask);

        /// <inheritdoc />
        public void Dispose()
        {
        }

        /// <inheritdoc />
        public async Task<IResult> Create(
            StructureIdentifier identifier,
            IDictionary<string, string> keys,
            IDictionary<string, string> variables)
        {
            _logger.LogDebug($"attempting to create new structure '{identifier}'");

            return await _domainObjectManager.CreateStructure(identifier, keys, variables, CancellationToken.None);
        }

        /// <inheritdoc />
        public async Task<IResult> DeleteVariables(StructureIdentifier identifier, ICollection<string> variablesToDelete)
        {
            _logger.LogDebug("attempting to delete variables from structure '{Identifier}'", identifier);

            List<ConfigKeyAction> deleteActions = variablesToDelete.Select(ConfigKeyAction.Delete).ToList();
            _logger.LogDebug(
                "removing {RemovedKeyCount} variables from '{Identifier}'",
                deleteActions.Count,
                identifier);

            return await _domainObjectManager.ModifyStructureVariables(
                       identifier,
                       deleteActions,
                       CancellationToken.None);
        }

        /// <inheritdoc />
        public async Task<IResult<Page<StructureIdentifier>>> GetAvailable(QueryRange range)
        {
            try
            {
                _logger.LogDebug("collecting available structures");

                return await _domainObjectManager.GetStructures(range, CancellationToken.None);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve structures");
                return Result.Error<Page<StructureIdentifier>>("failed to retrieve structures", ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<Page<int>>> GetAvailableVersions(string name, QueryRange range)
        {
            try
            {
                _logger.LogDebug("collecting available versions of structure '{StructureName}'", name);

                IResult<Page<StructureIdentifier>> listResult = await _domainObjectManager.GetStructures(name, range, CancellationToken.None);
                if (listResult.IsError)
                {
                    return Result.Error<Page<int>>(listResult.Message, listResult.Code);
                }

                IList<int> versions = listResult.Data
                                                .Items
                                                .Select(id => id.Version)
                                                .ToList();

                return Result.Success(
                    new Page<int>
                    {
                        Items = versions,
                        Count = listResult.Data.Count,
                        Offset = listResult.Data.Offset,
                        TotalCount = listResult.Data.TotalCount
                    });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve structures");
                return Result.Error<Page<int>>("failed to retrieve structures", ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<Page<KeyValuePair<string, string>>>> GetKeys(StructureIdentifier identifier, QueryRange range)
        {
            try
            {
                _logger.LogDebug("retrieving keys of structure '{Identifier}'", identifier);

                IResult<ConfigStructure> structureResult = await _domainObjectManager.GetStructure(identifier, CancellationToken.None);
                if (structureResult.IsError)
                {
                    return Result.Error<Page<KeyValuePair<string, string>>>(structureResult.Message, structureResult.Code);
                }

                ConfigStructure structure = structureResult.Data;

                _logger.LogDebug("got structure at version '{CurrentVersion}'", structure.CurrentVersion);

                List<KeyValuePair<string, string>> result = structure.Keys
                                                                     .OrderBy(k => k.Key)
                                                                     .Skip(range.Offset)
                                                                     .Take(range.Length)
                                                                     .Select(k => new KeyValuePair<string, string>(k.Key, k.Value))
                                                                     .ToList();

                var page = new Page<KeyValuePair<string, string>>
                {
                    Items = result,
                    Count = result.Count,
                    Offset = range.Offset,
                    TotalCount = structure.Keys.Count
                };

                return Result.Success(page);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve keys for structure {Identifier}", identifier);
                return Result.Error<Page<KeyValuePair<string, string>>>($"failed to retrieve keys for structure {identifier}", ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<ConfigStructureMetadata>> GetMetadata(StructureIdentifier identifier)
        {
            _logger.LogDebug("retrieving metadata for structure {Identifier}", identifier);

            IResult<ConfigStructure> structureResult = await _domainObjectManager.GetStructure(identifier, CancellationToken.None);

            if (structureResult.IsError)
            {
                return Result.Error<ConfigStructureMetadata>(structureResult.Message, structureResult.Code);
            }

            ConfigStructure structure = structureResult.Data;

            var metadata = new ConfigStructureMetadata
            {
                Id = structure.Id,
                ChangedAt = structure.ChangedAt,
                ChangedBy = structure.ChangedBy,
                CreatedAt = structure.CreatedAt,
                CreatedBy = structure.CreatedBy,
                KeyCount = structure.Keys.Count,
                VariablesCount = structure.Variables.Count
            };

            return Result.Success(metadata);
        }

        /// <inheritdoc />
        public async Task<IResult<Page<ConfigStructureMetadata>>> GetMetadata(QueryRange range)
        {
            _logger.LogDebug("retrieving metadata for range: {Range}", range);

            IResult<Page<StructureIdentifier>> ids = await _domainObjectManager.GetStructures(range, CancellationToken.None);
            if (ids.IsError)
            {
                return Result.Error<Page<ConfigStructureMetadata>>(ids.Message, ids.Code);
            }

            var results = new List<ConfigStructureMetadata>();
            foreach (StructureIdentifier layerId in ids.Data.Items)
            {
                IResult<ConfigStructureMetadata> result = await GetMetadata(layerId);
                if (result.IsError)
                {
                    return Result.Error<Page<ConfigStructureMetadata>>(result.Message, result.Code);
                }

                results.Add(result.Data);
            }

            return Result.Success(
                new Page<ConfigStructureMetadata>
                {
                    Items = results,
                    Count = results.Count,
                    Offset = range.Offset,
                    TotalCount = ids.Data.TotalCount
                });
        }

        /// <inheritdoc />
        public async Task<IResult<Page<KeyValuePair<string, string>>>> GetVariables(StructureIdentifier identifier, QueryRange range)
        {
            try
            {
                _logger.LogDebug("retrieving variables of structure '{Identifier}'", identifier);

                IResult<ConfigStructure> structureResult = await _domainObjectManager.GetStructure(identifier, CancellationToken.None);
                if (structureResult.IsError)
                {
                    return Result.Error<Page<KeyValuePair<string, string>>>(structureResult.Message, structureResult.Code);
                }

                ConfigStructure structure = structureResult.Data;

                _logger.LogDebug("got structure at version '{CurrentVersion}'", structure.CurrentVersion);

                List<KeyValuePair<string, string>> result = structure.Variables
                                                                     .OrderBy(k => k.Key)
                                                                     .Skip(range.Offset)
                                                                     .Take(range.Length)
                                                                     .Select(k => new KeyValuePair<string, string>(k.Key, k.Value))
                                                                     .ToList();

                var page = new Page<KeyValuePair<string, string>>
                {
                    Items = result,
                    Count = result.Count,
                    Offset = range.Offset,
                    TotalCount = structure.Variables.Count
                };

                return Result.Success(page);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve variables for structure {Identifier}", identifier);
                return Result.Error<Page<KeyValuePair<string, string>>>($"failed to retrieve variables for structure {identifier}", ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult> UpdateVariables(StructureIdentifier identifier, IDictionary<string, string> variables)
        {
            _logger.LogDebug("updating '{VariableCount}' variables of structure '{Identifier}'", variables.Count, identifier);

            return await _domainObjectManager.ModifyStructureVariables(
                       identifier,
                       variables.Select(kvp => ConfigKeyAction.Set(kvp.Key, kvp.Value))
                                .ToList(),
                       CancellationToken.None);
        }
    }
}
