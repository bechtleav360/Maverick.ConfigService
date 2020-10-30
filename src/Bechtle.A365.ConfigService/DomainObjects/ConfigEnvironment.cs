using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.Interfaces.Stores;
using Microsoft.Extensions.Caching.Memory;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    /// <summary>
    ///     Domain-Object representing a Config-Environment which stores a set of Keys from which to build Configurations
    /// </summary>
    public class ConfigEnvironment : DomainObject
    {
        /// <inheritdoc />
        public ConfigEnvironment(EnvironmentIdentifier identifier)
        {
            if (identifier is null)
                throw new ArgumentNullException(nameof(identifier));

            if (string.IsNullOrWhiteSpace(identifier.Category))
                throw new ArgumentNullException(nameof(identifier), $"{nameof(identifier.Category)} is null or empty");

            if (string.IsNullOrWhiteSpace(identifier.Name))
                throw new ArgumentNullException(nameof(identifier), $"{nameof(identifier.Name)} is null or empty");

            Created = false;
            Deleted = false;
            Identifier = new EnvironmentIdentifier(identifier.Category, identifier.Name);
            IsDefault = false;
        }

        /// <summary>
        ///     Flag indicating if this Environment has been created or not
        /// </summary>
        public bool Created { get; protected set; }

        /// <summary>
        ///     Flag indicating if this Environment has been deleted or not
        /// </summary>
        public bool Deleted { get; protected set; }

        /// <inheritdoc cref="EnvironmentIdentifier" />
        public EnvironmentIdentifier Identifier { get; protected set; }

        /// <summary>
        ///     Flag indicating if this is the Default-Environment of its Category
        /// </summary>
        public bool IsDefault { get; protected set; }

        /// <summary>
        ///     ordered layers used to represent this Environment
        /// </summary>
        public List<LayerIdentifier> Layers { get; protected set; }

        /// <summary>
        ///     assign the given Layers in the given order to this Environment
        /// </summary>
        /// <param name="layers"></param>
        /// <returns></returns>
        public IResult AssignLayers(IEnumerable<LayerIdentifier> layers)
        {
            if (!Created)
                return Result.Error("can't assign layers to non-existent environment", ErrorCode.NotFound);

            Layers = layers.ToList();
            CapturedDomainEvents.Add(new EnvironmentLayersModified(Identifier, Layers));

            return Result.Success();
        }

        // 10 for identifier, 5 for rest, each Key, each Path (recursively)
        /// <inheritdoc />
        public override long CalculateCacheSize() => Identifier.Category.Length + Identifier.Name.Length;

        /// <summary>
        ///     flag this environment as existing, and create the appropriate events for that
        /// </summary>
        /// <returns></returns>
        public IResult Create(bool isDefault = false)
        {
            if (Created)
                return Result.Success();

            Created = true;
            Deleted = false;
            IsDefault = isDefault;

            if (IsDefault)
                CapturedDomainEvents.Add(new DefaultEnvironmentCreated(Identifier));
            else
                CapturedDomainEvents.Add(new EnvironmentCreated(Identifier));

            return Result.Success();
        }

        /// <summary>
        ///     flag this environment as deleted, and create the appropriate events for that
        /// </summary>
        /// <returns></returns>
        public IResult Delete()
        {
            if (Deleted)
                return Result.Success();

            Created = false;
            Deleted = true;
            CapturedDomainEvents.Add(new EnvironmentDeleted(Identifier));

            return Result.Success();
        }

        /// <inheritdoc />
        public override CacheItemPriority GetCacheItemPriority() => CacheItemPriority.Normal;

        /// <summary>
        ///     resolve all layers and collect their Keys/Values
        /// </summary>
        /// <param name="objectStore"></param>
        /// <returns></returns>
        public async Task<IResult<Dictionary<LayerIdentifier, List<ConfigEnvironmentKey>>>> GetKeys(IDomainObjectStore objectStore)
        {
            if (!Created)
                return Result.Error<Dictionary<LayerIdentifier, List<ConfigEnvironmentKey>>>("environment does not exist", ErrorCode.NotFound);

            var layerData = Layers.ToDictionary(layer => layer, _ => new List<ConfigEnvironmentKey>());

            foreach (var (layerId, data) in layerData)
            {
                var result = await objectStore.ReplayObject(new EnvironmentLayer(layerId),
                                                            layerId.ToString(),
                                                            CurrentVersion);

                if (result.IsError)
                    return Result.Error<Dictionary<LayerIdentifier, List<ConfigEnvironmentKey>>>(result.Message, result.Code);

                layerData[layerId] = result.Data.Keys.Values.ToList();
            }

            return Result.Success(layerData);
        }

        /// <summary>
        ///     get the values of <see cref="GetKeys" /> as a flat map.
        ///     layers are collapsed (add or update) in the order indicated by <see cref="Layers" /> (index-0 = base, ascending)
        /// </summary>
        /// <param name="objectStore"></param>
        /// <returns></returns>
        public async Task<IResult<Dictionary<string, string>>> GetKeysAsDictionary(IDomainObjectStore objectStore)
        {
            var layerDataResult = await GetKeys(objectStore);

            if (layerDataResult.IsError)
                return Result.Error<Dictionary<string, string>>(layerDataResult.Message, layerDataResult.Code);

            var layerData = layerDataResult.Data;
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var layer in Layers)
            foreach (var entry in layerData[layer])
                result[entry.Key] = entry.Value;

            return Result.Success(result);
        }

        /// <inheritdoc />
        protected override void ApplySnapshotInternal(DomainObject domainObject)
        {
            if (!(domainObject is ConfigEnvironment other))
                return;

            Created = other.Created;
            Deleted = other.Deleted;
            Identifier = other.Identifier;
            IsDefault = other.IsDefault;
        }

        /// <inheritdoc />
        protected override IDictionary<Type, Func<ReplayedEvent, bool>> GetEventApplicationMapping()
            => new Dictionary<Type, Func<ReplayedEvent, bool>>
            {
                {typeof(DefaultEnvironmentCreated), HandleDefaultEnvironmentCreatedEvent},
                {typeof(EnvironmentCreated), HandleEnvironmentCreatedEvent},
                {typeof(EnvironmentDeleted), HandleEnvironmentDeletedEvent},
                {typeof(EnvironmentLayersModified), HandleEnvironmentLayersModified}
            };

        /// <inheritdoc />
        protected override string GetSnapshotIdentifier() => Identifier.ToString();

        private bool HandleDefaultEnvironmentCreatedEvent(ReplayedEvent replayedEvent)
        {
            if (!(replayedEvent.DomainEvent is DefaultEnvironmentCreated created) || created.Identifier != Identifier)
                return false;

            IsDefault = true;
            Created = true;
            return true;
        }

        private bool HandleEnvironmentCreatedEvent(ReplayedEvent replayedEvent)
        {
            if (!(replayedEvent.DomainEvent is EnvironmentCreated created) || created.Identifier != Identifier)
                return false;

            IsDefault = false;
            Created = true;
            return true;
        }

        private bool HandleEnvironmentDeletedEvent(ReplayedEvent replayedEvent)
        {
            if (!(replayedEvent.DomainEvent is EnvironmentDeleted deleted) || deleted.Identifier != Identifier)
                return false;

            Created = false;
            Deleted = true;
            return true;
        }

        private bool HandleEnvironmentLayersModified(ReplayedEvent replayedEvent)
        {
            if (!(replayedEvent.DomainEvent is EnvironmentLayersModified modified) || modified.Identifier != Identifier)
                return false;

            Layers = modified.Layers;
            return true;
        }
    }
}