using System;
using System.Collections.Generic;
using System.Linq;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Interfaces;

namespace Bechtle.A365.ConfigService.Implementations
{
    /// <inheritdoc />
    public class InternalDataCommandValidator : ICommandValidator
    {
        /// <inheritdoc />
        public IResult ValidateDomainEvent(DomainEvent? domainEvent)
        {
            if (domainEvent is null)
            {
                return Result.Error("invalid data: null event", ErrorCode.ValidationFailed);
            }

            if (string.IsNullOrWhiteSpace(domainEvent.EventType))
            {
                return Result.Error("event does not contain EventType", ErrorCode.ValidationFailed);
            }

            IResult result = domainEvent switch
            {
                ConfigurationBuilt @event => ValidateDomainEvent(@event),
                DefaultEnvironmentCreated @event => ValidateDomainEvent(@event),
                EnvironmentCreated @event => ValidateDomainEvent(@event),
                EnvironmentDeleted @event => ValidateDomainEvent(@event),
                EnvironmentLayersModified @event => ValidateDomainEvent(@event),
                EnvironmentLayerCreated @event => ValidateDomainEvent(@event),
                EnvironmentLayerDeleted @event => ValidateDomainEvent(@event),
                EnvironmentLayerCopied @event => ValidateDomainEvent(@event),
                EnvironmentLayerTagsChanged @event => ValidateDomainEvent(@event),
                EnvironmentLayerKeysImported @event => ValidateDomainEvent(@event),
                EnvironmentLayerKeysModified @event => ValidateDomainEvent(@event),
                StructureCreated @event => ValidateDomainEvent(@event),
                StructureDeleted @event => ValidateDomainEvent(@event),
                StructureVariablesModified @event => ValidateDomainEvent(@event),
                _ => Result.Error($"DomainEvent '{domainEvent.GetType().Name}' can't be validated; not supported", ErrorCode.ValidationFailed)
            };

            KnownMetrics.EventsValidated.WithLabels(result.IsError ? "Invalid" : "Valid").Inc();

            return result;
        }

        /// <summary>
        ///     validate a single <see cref="ConfigKeyAction" />
        /// </summary>
        /// <returns></returns>
        private static IResult ValidateConfigKeyAction(ConfigKeyAction? action)
        {
            if (action is null)
            {
                return Result.Error("invalid data: action is null", ErrorCode.ValidationFailed);
            }

            if (string.IsNullOrWhiteSpace(action.Key))
            {
                return Result.Error("invalid data: no key defined", ErrorCode.ValidationFailed);
            }

            switch (action.Type)
            {
                case ConfigKeyActionType.Set:
                case ConfigKeyActionType.Delete:
                    break;

                default:
                    return Result.Error($"invalid data: invalid Type {action.Type:D} / '{action.Type:G}'; key='{action.Key}'", ErrorCode.ValidationFailed);
            }

            return Result.Success();
        }

        /// <summary>
        ///     validate multiple <see cref="ConfigKeyAction" /> and aggregate their errors
        /// </summary>
        /// <returns></returns>
        private static IResult ValidateConfigKeyActions(ConfigKeyAction[]? actions)
        {
            if (actions is null || !actions.Any())
            {
                return Result.Error("invalid data: no modified keys", ErrorCode.ValidationFailed);
            }

            List<IResult> errors = actions.Select(ValidateConfigKeyAction)
                                          .Where(r => r.IsError)
                                          .ToList();

            if (errors.Any())
            {
                return Result.Error(
                    $"invalid data: {errors.Count} {(errors.Count == 1 ? "action" : "actions")} failed validation);\r\n"
                    + string.Join(";\r\n", errors.Select(e => $"{e.Code} - {e.Message}")),
                    ErrorCode.ValidationFailed);
            }

            return Result.Success();
        }

        /// <summary>
        ///     validate a dictionary of Key=>Value pairs and aggregate their errors
        /// </summary>
        /// <returns></returns>
        private static IResult ValidateDictionary(IDictionary<string, string?> dict)
        {
            List<IResult> errors = dict.Select(
                                           kvp => string.IsNullOrWhiteSpace(kvp.Key)
                                                      ? Result.Error("invalid data: key is null / empty", ErrorCode.ValidationFailed)
                                                      : Result.Success())
                                       .Where(r => r.IsError)
                                       .ToList();

            if (errors.Any())
            {
                return Result.Error(
                    $"invalid data: {errors.Count} {(errors.Count == 1 ? "key" : "keys")} failed validation);\r\n"
                    + string.Join(";\r\n", errors.Select(e => $"{e.Code} - {e.Message}")),
                    ErrorCode.ValidationFailed);
            }

            return Result.Success();
        }

        private static IResult ValidateDomainEvent(ConfigurationBuilt @event)
        {
            IResult identifierResult = ValidateIdentifier(@event.Identifier);
            if (identifierResult.IsError)
            {
                return identifierResult;
            }

            return Result.Success();
        }

        private static IResult ValidateDomainEvent(DefaultEnvironmentCreated @event)
        {
            IResult identifierResult = ValidateIdentifier(@event.Identifier);
            if (identifierResult.IsError)
            {
                return identifierResult;
            }

            return Result.Success();
        }

        private static IResult ValidateDomainEvent(EnvironmentCreated @event)
        {
            IResult identifierResult = ValidateIdentifier(@event.Identifier);
            if (identifierResult.IsError)
            {
                return identifierResult;
            }

            return Result.Success();
        }

        private static IResult ValidateDomainEvent(EnvironmentDeleted @event)
        {
            IResult identifierResult = ValidateIdentifier(@event.Identifier);
            if (identifierResult.IsError)
            {
                return identifierResult;
            }

            return Result.Success();
        }

        private static IResult ValidateDomainEvent(EnvironmentLayerKeysImported @event)
        {
            IResult identifierResult = ValidateIdentifier(@event.Identifier);
            if (identifierResult.IsError)
            {
                return identifierResult;
            }

            IResult keyResult = ValidateConfigKeyActions(@event.ModifiedKeys);
            if (keyResult.IsError)
            {
                return keyResult;
            }

            return Result.Success();
        }

        private static IResult ValidateDomainEvent(EnvironmentLayerTagsChanged @event)
        {
            IResult identifierResult = ValidateIdentifier(@event.Identifier);
            if (identifierResult.IsError)
            {
                return identifierResult;
            }

            if (!@event.AddedTags.Any() && !@event.RemovedTags.Any())
            {
                return Result.Error("invalid data: no tags changed", ErrorCode.ValidationFailed);
            }

            if (@event.AddedTags.Distinct(StringComparer.OrdinalIgnoreCase).Count() != @event.AddedTags.Count)
            {
                return Result.Error("invalid data: duplicate tags added", ErrorCode.ValidationFailed);
            }

            if (@event.RemovedTags.Distinct(StringComparer.OrdinalIgnoreCase).Count() != @event.RemovedTags.Count)
            {
                return Result.Error("invalid data: duplicate tags removed", ErrorCode.ValidationFailed);
            }

            return Result.Success();
        }

        private static IResult ValidateDomainEvent(EnvironmentLayerKeysModified @event)
        {
            IResult identifierResult = ValidateIdentifier(@event.Identifier);
            if (identifierResult.IsError)
            {
                return identifierResult;
            }

            IResult keyResult = ValidateConfigKeyActions(@event.ModifiedKeys);
            if (keyResult.IsError)
            {
                return keyResult;
            }

            return Result.Success();
        }

        private static IResult ValidateDomainEvent(StructureCreated @event)
        {
            IResult identifierResult = ValidateIdentifier(@event.Identifier);
            if (identifierResult.IsError)
            {
                return identifierResult;
            }

            IResult keysResult = ValidateDictionary(@event.Keys);
            if (keysResult.IsError)
            {
                return keysResult;
            }

            IResult variableResult = ValidateDictionary(@event.Variables);
            if (variableResult.IsError)
            {
                return variableResult;
            }

            return Result.Success();
        }

        private static IResult ValidateDomainEvent(StructureDeleted @event)
        {
            IResult identifierResult = ValidateIdentifier(@event.Identifier);
            if (identifierResult.IsError)
            {
                return identifierResult;
            }

            return Result.Success();
        }

        private static IResult ValidateDomainEvent(StructureVariablesModified @event)
        {
            IResult identifierResult = ValidateIdentifier(@event.Identifier);
            if (identifierResult.IsError)
            {
                return identifierResult;
            }

            IResult keysResult = ValidateConfigKeyActions(@event.ModifiedKeys);
            if (keysResult.IsError)
            {
                return keysResult;
            }

            return Result.Success();
        }

        private static IResult ValidateDomainEvent(EnvironmentLayerDeleted @event)
        {
            IResult identifierResult = ValidateIdentifier(@event.Identifier);
            if (identifierResult.IsError)
            {
                return identifierResult;
            }

            return Result.Success();
        }

        private static IResult ValidateDomainEvent(EnvironmentLayerCreated @event)
        {
            IResult identifierResult = ValidateIdentifier(@event.Identifier);
            if (identifierResult.IsError)
            {
                return identifierResult;
            }

            return Result.Success();
        }

        private static IResult ValidateDomainEvent(EnvironmentLayerCopied @event)
        {
            IResult sourceIdResult = ValidateIdentifier(@event.SourceIdentifier);
            if (sourceIdResult.IsError)
            {
                return sourceIdResult;
            }

            IResult targetIdResult = ValidateIdentifier(@event.TargetIdentifier);
            if (targetIdResult.IsError)
            {
                return targetIdResult;
            }

            return Result.Success();
        }

        private static IResult ValidateDomainEvent(EnvironmentLayersModified @event)
        {
            IResult identifierResult = ValidateIdentifier(@event.Identifier);
            if (identifierResult.IsError)
            {
                return identifierResult;
            }

            if (@event.Layers.Distinct().Count() != @event.Layers.Count)
            {
                return Result.Error("invalid data: layers-entries are not unique", ErrorCode.ValidationFailed);
            }

            return Result.Success();
        }

        private static IResult ValidateIdentifier(ConfigurationIdentifier identifier)
        {
            IResult environmentResult = ValidateIdentifier(identifier.Environment);
            if (environmentResult.IsError)
            {
                return environmentResult;
            }

            IResult structureResult = ValidateIdentifier(identifier.Structure);
            if (structureResult.IsError)
            {
                return structureResult;
            }

            return Result.Success();
        }

        private static IResult ValidateIdentifier(EnvironmentIdentifier identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier.Category))
            {
                return Result.Error("invalid EnvironmentIdentifier.Category (empty / null)", ErrorCode.ValidationFailed);
            }

            if (string.IsNullOrWhiteSpace(identifier.Name))
            {
                return Result.Error("invalid EnvironmentIdentifier.Name (empty / null)", ErrorCode.ValidationFailed);
            }

            return Result.Success();
        }

        private static IResult ValidateIdentifier(StructureIdentifier identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier.Name))
            {
                return Result.Error("invalid StructureIdentifier.Name (empty / null)", ErrorCode.ValidationFailed);
            }

            if (identifier.Version <= 0)
            {
                return Result.Error("invalid StructureIdentifier.Version (x <= 0)", ErrorCode.ValidationFailed);
            }

            return Result.Success();
        }

        private static IResult ValidateIdentifier(LayerIdentifier identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier.Name))
            {
                return Result.Error("invalid StructureIdentifier.Name (empty / null)", ErrorCode.ValidationFailed);
            }

            return Result.Success();
        }
    }
}
