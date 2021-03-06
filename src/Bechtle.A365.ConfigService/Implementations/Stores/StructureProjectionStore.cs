﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
        private readonly IDomainObjectStore _domainObjectStore;
        private readonly IEventStore _eventStore;
        private readonly ILogger<StructureProjectionStore> _logger;
        private readonly IList<ICommandValidator> _validators;

        /// <inheritdoc cref="StructureProjectionStore" />
        public StructureProjectionStore(ILogger<StructureProjectionStore> logger,
                                        IDomainObjectStore domainObjectStore,
                                        IEventStore eventStore,
                                        IEnumerable<ICommandValidator> validators)
        {
            _logger = logger;
            _domainObjectStore = domainObjectStore;
            _eventStore = eventStore;
            _validators = validators.ToList();
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (_domainObjectStore != null)
                await _domainObjectStore.DisposeAsync();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _domainObjectStore?.Dispose();
        }

        /// <inheritdoc />
        public async Task<IResult> Create(StructureIdentifier identifier,
                                          IDictionary<string, string> keys,
                                          IDictionary<string, string> variables)
        {
            _logger.LogDebug($"attempting to create new structure '{identifier}'");

            var structResult = await _domainObjectStore.ReplayObject(new ConfigStructure(identifier), identifier.ToString());
            if (structResult.IsError)
                return structResult;

            var structure = structResult.Data;

            _logger.LogDebug("creating Structure");
            var result = structure.Create(keys, variables);
            if (result.IsError)
                return result;

            _logger.LogDebug("validating resulting events");
            var errors = structure.Validate(_validators);
            if (errors.Any())
                return Result.Error("failed to validate generated DomainEvents",
                                    ErrorCode.ValidationFailed,
                                    errors.Values
                                          .SelectMany(_ => _)
                                          .ToList());

            _logger.LogDebug("writing generated events to ES");
            return await structure.WriteRecordedEvents(_eventStore);
        }

        /// <inheritdoc />
        public async Task<IResult> DeleteVariables(StructureIdentifier identifier, ICollection<string> variablesToDelete)
        {
            _logger.LogDebug($"attempting to delete variables from structure '{identifier}'");

            var structResult = await _domainObjectStore.ReplayObject(new ConfigStructure(identifier), identifier.ToString());
            if (structResult.IsError)
                return structResult;

            var structure = structResult.Data;

            _logger.LogDebug($"removing '{variablesToDelete.Count}' variables from '{identifier}' " +
                             $"at version '{structure.CurrentVersion}' / {structure.MetaVersion}");

            var updateResult = structure.DeleteVariables(variablesToDelete);
            if (updateResult.IsError)
                return updateResult;

            _logger.LogDebug("validating resulting events");
            var errors = structure.Validate(_validators);
            if (errors.Any())
                return Result.Error("failed to validate generated DomainEvents",
                                    ErrorCode.ValidationFailed,
                                    errors.Values
                                          .SelectMany(_ => _)
                                          .ToList());

            return await structure.WriteRecordedEvents(_eventStore);
        }

        /// <inheritdoc />
        public async Task<IResult<IList<StructureIdentifier>>> GetAvailable(QueryRange range)
        {
            try
            {
                _logger.LogDebug("collecting available structures");

                var listResult = await _domainObjectStore.ReplayObject<ConfigStructureList>();
                if (listResult.IsError)
                    return Result.Success<IList<StructureIdentifier>>(new List<StructureIdentifier>());

                var structures = listResult.Data.GetIdentifiers();

                _logger.LogDebug($"got '{structures.Count}' structures, filtering / ordering now");
                var result = structures.OrderBy(s => s.Name)
                                       .ThenByDescending(s => s.Version)
                                       .Skip(range.Offset)
                                       .Take(range.Length)
                                       .ToList();

                return Result.Success<IList<StructureIdentifier>>(result);
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
                _logger.LogDebug($"collecting available versions of structure '{name}'");

                var listResult = await _domainObjectStore.ReplayObject<ConfigStructureList>();
                if (listResult.IsError)
                    return Result.Success<IList<int>>(new List<int>());

                var structures = listResult.Data.GetIdentifiers();

                _logger.LogDebug($"got '{structures.Count}' structures, filtering / ordering now");

                var result = structures.Where(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                                       .OrderByDescending(s => s.Version)
                                       .Skip(range.Offset)
                                       .Take(range.Length)
                                       .Select(s => s.Version)
                                       .ToList();

                return Result.Success<IList<int>>(result);
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
                _logger.LogDebug($"retrieving keys of structure '{identifier}'");

                var structResult = await _domainObjectStore.ReplayObject(new ConfigStructure(identifier), identifier.ToString());
                if (structResult.IsError)
                    return Result.Error<IDictionary<string, string>>("no structure found with (" +
                                                                     $"{nameof(identifier.Name)}: {identifier.Name}; " +
                                                                     $"{nameof(identifier.Version)}: {identifier.Version}" +
                                                                     ")",
                                                                     ErrorCode.NotFound);

                var structure = structResult.Data;

                _logger.LogDebug($"got structure at version '{structure.CurrentVersion}' / {structure.MetaVersion}");

                var result = structure.Keys
                                      .OrderBy(k => k.Key)
                                      .Skip(range.Offset)
                                      .Take(range.Length)
                                      .ToImmutableSortedDictionary(k => k.Key,
                                                                   k => k.Value,
                                                                   StringComparer.OrdinalIgnoreCase);

                return Result.Success<IDictionary<string, string>>(result);
            }
            catch (Exception e)
            {
                _logger.LogError("failed to retrieve keys for structure " +
                                 $"({nameof(identifier.Name)}: {identifier.Name}; {nameof(identifier.Version)}: {identifier.Version}): {e}");

                return Result.Error<IDictionary<string, string>>(
                    "failed to retrieve keys for structure " +
                    $"({nameof(identifier.Name)}: {identifier.Name}; {nameof(identifier.Version)}: {identifier.Version})",
                    ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult<IDictionary<string, string>>> GetVariables(StructureIdentifier identifier, QueryRange range)
        {
            try
            {
                _logger.LogDebug($"retrieving variables of structure '{identifier}'");

                var structResult = await _domainObjectStore.ReplayObject(new ConfigStructure(identifier), identifier.ToString());
                if (structResult.IsError)
                    return Result.Error<IDictionary<string, string>>("no structure found with (" +
                                                                     $"{nameof(identifier.Name)}: {identifier.Name}; " +
                                                                     $"{nameof(identifier.Version)}: {identifier.Version}" +
                                                                     ")",
                                                                     ErrorCode.NotFound);

                var structure = structResult.Data;

                _logger.LogDebug($"got structure at version '{structure.CurrentVersion}' / {structure.MetaVersion}");

                var result = structure.Variables
                                      .OrderBy(v => v.Key)
                                      .Skip(range.Offset)
                                      .Take(range.Length)
                                      .ToImmutableSortedDictionary(k => k.Key,
                                                                   k => k.Value,
                                                                   StringComparer.OrdinalIgnoreCase);

                return Result.Success<IDictionary<string, string>>(result);
            }
            catch (Exception e)
            {
                _logger.LogError("failed to retrieve variables for structure " +
                                 $"({nameof(identifier.Name)}: {identifier.Name}; {nameof(identifier.Version)}: {identifier.Version}): {e}");

                return Result.Error<IDictionary<string, string>>(
                    "failed to retrieve variables for structure " +
                    $"({nameof(identifier.Name)}: {identifier.Name}; {nameof(identifier.Version)}: {identifier.Version})",
                    ErrorCode.DbQueryError);
            }
        }

        /// <inheritdoc />
        public async Task<IResult> UpdateVariables(StructureIdentifier identifier, IDictionary<string, string> variables)
        {
            _logger.LogDebug($"updating '{variables.Count}' variables of structure '{identifier}'");

            var structResult = await _domainObjectStore.ReplayObject(new ConfigStructure(identifier), identifier.ToString());
            if (structResult.IsError)
                return structResult;

            var structure = structResult.Data;

            _logger.LogDebug($"updating '{variables.Count}' variables in '{identifier}' " +
                             $"at version '{structure.CurrentVersion}' / {structure.MetaVersion}");

            var updateResult = structure.ModifyVariables(variables);
            if (updateResult.IsError)
                return updateResult;

            var errors = structure.Validate(_validators);
            if (errors.Any())
                return Result.Error("failed to validate generated DomainEvents",
                                    ErrorCode.ValidationFailed,
                                    errors.Values
                                          .SelectMany(_ => _)
                                          .ToList());

            return await structure.WriteRecordedEvents(_eventStore);
        }
    }
}