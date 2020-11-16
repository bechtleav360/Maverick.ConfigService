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
        /// <summary>
        ///     validate a single <see cref="ConfigKeyAction" />
        /// </summary>
        /// <returns></returns>
        private static IResult ValidateConfigKeyAction(ConfigKeyAction action)
        {
            if (action is null)
                return Result.Error("invalid data: action is null", ErrorCode.ValidationFailed);

            if (string.IsNullOrWhiteSpace(action.Key))
                return Result.Error("invalid data: no key defined", ErrorCode.ValidationFailed);

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
        private static IResult ValidateConfigKeyActions(ConfigKeyAction[] actions)
        {
            if (actions is null || !actions.Any())
                return Result.Error("invalid data: no modified keys", ErrorCode.ValidationFailed);

            var errors = actions.Select(ValidateConfigKeyAction)
                                .Where(r => r.IsError)
                                .ToList();

            if (errors.Any())
                return Result.Error($"invalid data: {errors.Count} {(errors.Count == 1 ? "action" : "actions")} failed validation);\r\n" +
                                    string.Join(";\r\n", errors.Select(e => $"{e.Code} - {e.Message}")),
                                    ErrorCode.ValidationFailed);

            return Result.Success();
        }

        /// <summary>
        ///     validate a dictionary of Key=>Value pairs and aggregate their errors
        /// </summary>
        /// <returns></returns>
        private static IResult ValidateDictionary(IDictionary<string, string> dict)
        {
            var errors = dict.Select(kvp => string.IsNullOrWhiteSpace(kvp.Key)
                                                ? Result.Error("invalid data: key is null / empty", ErrorCode.ValidationFailed)
                                                : Result.Success())
                             .Where(r => r.IsError)
                             .ToList();

            if (errors.Any())
                return Result.Error($"invalid data: {errors.Count} {(errors.Count == 1 ? "key" : "keys")} failed validation);\r\n" +
                                    string.Join(";\r\n", errors.Select(e => $"{e.Code} - {e.Message}")),
                                    ErrorCode.ValidationFailed);

            return Result.Success();
        }

        private static IResult ValidateDomainEvent(ConfigurationBuilt @event)
        {
            var identifierResult = ValidateIdentifier(@event.Identifier);
            if (identifierResult.IsError)
                return identifierResult;

            return Result.Success();
        }

        private static IResult ValidateDomainEvent(DefaultEnvironmentCreated @event)
        {
            var identifierResult = ValidateIdentifier(@event.Identifier);
            if (identifierResult.IsError)
                return identifierResult;

            return Result.Success();
        }

        private static IResult ValidateDomainEvent(EnvironmentCreated @event)
        {
            var identifierResult = ValidateIdentifier(@event.Identifier);
            if (identifierResult.IsError)
                return identifierResult;

            return Result.Success();
        }

        private static IResult ValidateDomainEvent(EnvironmentDeleted @event)
        {
            var identifierResult = ValidateIdentifier(@event.Identifier);
            if (identifierResult.IsError)
                return identifierResult;

            return Result.Success();
        }

        private static IResult ValidateDomainEvent(EnvironmentLayerKeysImported @event)
        {
            var identifierResult = ValidateIdentifier(@event.Identifier);
            if (identifierResult.IsError)
                return identifierResult;

            var keyResult = ValidateConfigKeyActions(@event.ModifiedKeys);
            if (keyResult.IsError)
                return keyResult;

            return Result.Success();
        }

        private static IResult ValidateDomainEvent(EnvironmentLayerKeysModified @event)
        {
            var identifierResult = ValidateIdentifier(@event.Identifier);
            if (identifierResult.IsError)
                return identifierResult;

            var keyResult = ValidateConfigKeyActions(@event.ModifiedKeys);
            if (keyResult.IsError)
                return keyResult;

            return Result.Success();
        }

        private static IResult ValidateDomainEvent(StructureCreated @event)
        {
            var identifierResult = ValidateIdentifier(@event.Identifier);
            if (identifierResult.IsError)
                return identifierResult;

            var keysResult = ValidateDictionary(@event.Keys);
            if (keysResult.IsError)
                return keysResult;

            var variableResult = ValidateDictionary(@event.Variables);
            if (variableResult.IsError)
                return variableResult;

            return Result.Success();
        }

        private static IResult ValidateDomainEvent(StructureDeleted @event)
        {
            if (@event is null)
                return Result.Error("invalid data: null event", ErrorCode.ValidationFailed);

            var identifierResult = ValidateIdentifier(@event.Identifier);
            if (identifierResult.IsError)
                return identifierResult;

            return Result.Success();
        }

        private static IResult ValidateDomainEvent(StructureVariablesModified @event)
        {
            if (@event is null)
                return Result.Error("invalid data: null event", ErrorCode.ValidationFailed);

            var identifierResult = ValidateIdentifier(@event.Identifier);
            if (identifierResult.IsError)
                return identifierResult;

            var keysResult = ValidateConfigKeyActions(@event.ModifiedKeys);
            if (keysResult.IsError)
                return keysResult;

            return Result.Success();
        }

        private static IResult ValidateIdentifier(ConfigurationIdentifier identifier)
        {
            if (identifier is null)
                return Result.Error("invalid identifier (null", ErrorCode.ValidationFailed);

            var environmentResult = ValidateIdentifier(identifier.Environment);
            if (environmentResult.IsError)
                return environmentResult;

            var structureResult = ValidateIdentifier(identifier.Structure);
            if (structureResult.IsError)
                return structureResult;

            return Result.Success();
        }

        private static IResult ValidateIdentifier(EnvironmentIdentifier identifier)
        {
            if (identifier is null)
                return Result.Error("invalid EnvironmentIdentifier (null)", ErrorCode.ValidationFailed);

            if (string.IsNullOrWhiteSpace(identifier.Category))
                return Result.Error("invalid EnvironmentIdentifier.Category (empty / null)", ErrorCode.ValidationFailed);

            if (string.IsNullOrWhiteSpace(identifier.Name))
                return Result.Error("invalid EnvironmentIdentifier.Name (empty / null)", ErrorCode.ValidationFailed);

            return Result.Success();
        }

        private static IResult ValidateIdentifier(StructureIdentifier identifier)
        {
            if (identifier is null)
                return Result.Error("invalid StructureIdentifier (null)", ErrorCode.ValidationFailed);

            if (string.IsNullOrWhiteSpace(identifier.Name))
                return Result.Error("invalid StructureIdentifier.Name (empty / null)", ErrorCode.ValidationFailed);

            if (identifier.Version <= 0)
                return Result.Error("invalid StructureIdentifier.Version (x <= 0)", ErrorCode.ValidationFailed);

            return Result.Success();
        }

        private static IResult ValidateIdentifier(LayerIdentifier identifier)
        {
            if (identifier is null)
                return Result.Error("invalid StructureIdentifier (null)", ErrorCode.ValidationFailed);

            if (string.IsNullOrWhiteSpace(identifier.Name))
                return Result.Error("invalid StructureIdentifier.Name (empty / null)", ErrorCode.ValidationFailed);

            return Result.Success();
        }

        /// <inheritdoc />
        public IResult ValidateDomainEvent(DomainEvent domainEvent)
        {
            if (domainEvent is null)
                return Result.Error("invalid data: null event", ErrorCode.ValidationFailed);

            if (string.IsNullOrWhiteSpace(domainEvent.EventType))
                return Result.Error("event does not contain EventType", ErrorCode.ValidationFailed);

            var result = domainEvent switch
            {
                ConfigurationBuilt @event => ValidateDomainEvent(@event),
                DefaultEnvironmentCreated @event => ValidateDomainEvent(@event),
                EnvironmentCreated @event => ValidateDomainEvent(@event),
                EnvironmentDeleted @event => ValidateDomainEvent(@event),
                EnvironmentLayersModified @event => ValidateDomainEvent(@event),
                EnvironmentLayerCreated @event => ValidateDomainEvent(@event),
                EnvironmentLayerDeleted @event => ValidateDomainEvent(@event),
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

        private IResult ValidateDomainEvent(EnvironmentLayerDeleted @event)
        {
            var identifierResult = ValidateIdentifier(@event.Identifier);
            if (identifierResult.IsError)
                return identifierResult;

            return Result.Success();
        }

        private IResult ValidateDomainEvent(EnvironmentLayerCreated @event)
        {
            var identifierResult = ValidateIdentifier(@event.Identifier);
            if (identifierResult.IsError)
                return identifierResult;

            return Result.Success();
        }

        private IResult ValidateDomainEvent(EnvironmentLayersModified @event)
        {
            var identifierResult = ValidateIdentifier(@event.Identifier);
            if (identifierResult.IsError)
                return identifierResult;

            if (@event.Layers is null)
                return Result.Error("invalid data: layers is null", ErrorCode.ValidationFailed);

            if (@event.Layers.Distinct().Count() != @event.Layers.Count)
                return Result.Error("invalid data: layers-entries are not unique", ErrorCode.ValidationFailed);

            return Result.Success();
        }
    }
}