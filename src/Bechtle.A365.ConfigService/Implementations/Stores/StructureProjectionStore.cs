﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Interfaces;
using Bechtle.A365.ConfigService.Interfaces.Stores;
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
        public async Task<IResult<IList<StructureIdentifier>>> GetAvailable(QueryRange range)
        {
            try
            {
                _logger.LogDebug("collecting available structures");

                return await _domainObjectManager.GetStructures(range, CancellationToken.None);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve structures");
                return Result.Error<IList<StructureIdentifier>>("failed to retrieve structures", ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<IList<int>>> GetAvailableVersions(string name, QueryRange range)
        {
            try
            {
                _logger.LogDebug("collecting available versions of structure '{StructureName}'", name);

                IResult<IList<StructureIdentifier>> listResult = await _domainObjectManager.GetStructures(name, range, CancellationToken.None);
                if (listResult.IsError)
                {
                    return Result.Error<IList<int>>(listResult.Message, listResult.Code);
                }

                IList<int> versions = listResult.Data
                                                .Select(id => id.Version)
                                                .ToList();

                return Result.Success(versions);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve structures");
                return Result.Error<IList<int>>("failed to retrieve structures", ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<IDictionary<string, string>>> GetKeys(StructureIdentifier identifier, QueryRange range)
        {
            try
            {
                _logger.LogDebug("retrieving keys of structure '{Identifier}'", identifier);

                IResult<ConfigStructure> structureResult = await _domainObjectManager.GetStructure(identifier, CancellationToken.None);
                if (structureResult.IsError)
                {
                    return Result.Error<IDictionary<string, string>>(structureResult.Message, structureResult.Code);
                }

                ConfigStructure structure = structureResult.Data;

                _logger.LogDebug("got structure at version '{CurrentVersion}'", structure.CurrentVersion);

                ImmutableSortedDictionary<string, string> result = structure.Keys
                                                                            .OrderBy(k => k.Key)
                                                                            .Skip(range.Offset)
                                                                            .Take(range.Length)
                                                                            .ToImmutableSortedDictionary(
                                                                                k => k.Key,
                                                                                k => k.Value,
                                                                                StringComparer.OrdinalIgnoreCase);

                return Result.Success<IDictionary<string, string>>(result);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve keys for structure {Identifier}", identifier);
                return Result.Error<IDictionary<string, string>>($"failed to retrieve keys for structure {identifier}", ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<IDictionary<string, string>>> GetVariables(StructureIdentifier identifier, QueryRange range)
        {
            try
            {
                _logger.LogDebug("retrieving variables of structure '{Identifier}'", identifier);

                IResult<ConfigStructure> structureResult = await _domainObjectManager.GetStructure(identifier, CancellationToken.None);
                if (structureResult.IsError)
                {
                    return Result.Error<IDictionary<string, string>>(structureResult.Message, structureResult.Code);
                }

                ConfigStructure structure = structureResult.Data;

                _logger.LogDebug("got structure at version '{CurrentVersion}'", structure.CurrentVersion);

                ImmutableSortedDictionary<string, string> result = structure.Variables
                                                                            .OrderBy(k => k.Key)
                                                                            .Skip(range.Offset)
                                                                            .Take(range.Length)
                                                                            .ToImmutableSortedDictionary(
                                                                                k => k.Key,
                                                                                k => k.Value,
                                                                                StringComparer.OrdinalIgnoreCase);

                return Result.Success<IDictionary<string, string>>(result);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to retrieve variables for structure {Identifier}", identifier);
                return Result.Error<IDictionary<string, string>>($"failed to retrieve variables for structure {identifier}", ErrorCode.DbQueryError);
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

        /// <inheritdoc />
        public async Task<IResult<ConfigStructureMetadata>> GetMetadata(StructureIdentifier identifier)
        {
            _logger.LogDebug("retrieving metadata for structure {Identifier}", identifier);

            IResult<ConfigStructure> structureResult = await _domainObjectManager.GetStructure(identifier, CancellationToken.None);

            if (structureResult.IsError)
                return Result.Error<ConfigStructureMetadata>(structureResult.Message, structureResult.Code);

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
    }
}
