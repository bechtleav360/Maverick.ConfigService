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
            Layers = new List<LayerIdentifier>();
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
        ///     get all key-objects in this environment, in their end-result form (override-rules applied)
        /// </summary>
        /// <param name="objectStore"></param>
        /// <returns></returns>
        public async Task<IResult<IList<ConfigEnvironmentKey>>> GetKeys(IDomainObjectStore objectStore)
        {
            if(objectStore is null)
                throw new ArgumentNullException(nameof(objectStore));

            var layerDataResult = await GetLayers(objectStore);

            if (layerDataResult.IsError)
                return Result.Error<IList<ConfigEnvironmentKey>>(layerDataResult.Message, layerDataResult.Code);

            var layerData = layerDataResult.Data;
            var result = new Dictionary<string, ConfigEnvironmentKey>(StringComparer.OrdinalIgnoreCase);

            foreach (var layer in Layers)
            foreach (var entry in layerData.First(l => l.Identifier == layer).Keys)
                result[entry.Key] = entry.Value;

            return Result.Success(result.Values.ToList() as IList<ConfigEnvironmentKey>);
        }

        /// <summary>
        ///     get the values of <see cref="GetKeys" /> as a flat map.
        ///     layers are collapsed (add or update) in the order indicated by <see cref="Layers" /> (index-0 = base, ascending)
        /// </summary>
        /// <param name="objectStore"></param>
        /// <returns></returns>
        public async Task<IResult<IDictionary<string, string>>> GetKeysAsDictionary(IDomainObjectStore objectStore)
        {
            if (objectStore is null)
                throw new ArgumentNullException(nameof(objectStore));

            var layerDataResult = await GetLayers(objectStore);

            if (layerDataResult.IsError)
                return Result.Error<IDictionary<string, string>>(layerDataResult.Message, layerDataResult.Code);

            var layerData = layerDataResult.Data;
            IDictionary<string, string> result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var layer in Layers)
            foreach (var entry in layerData.First(l => l.Identifier == layer).Keys)
                result[entry.Key] = entry.Value.Value;

            return Result.Success(result);
        }

        /// <summary>
        ///     resolve all layers and collect their Keys/Values
        /// </summary>
        /// <param name="objectStore"></param>
        /// <returns></returns>
        public async Task<IResult<List<EnvironmentLayer>>> GetLayers(IDomainObjectStore objectStore)
        {
            if (objectStore is null)
                throw new ArgumentNullException(nameof(objectStore));

            if (!Created)
                return Result.Error<List<EnvironmentLayer>>("environment does not exist", ErrorCode.NotFound);

            var list = new List<EnvironmentLayer>(Layers.Count);

            foreach (var layerId in Layers)
            {
                var result = await objectStore.ReplayObject(new EnvironmentLayer(layerId),
                                                            layerId.ToString(),
                                                            CurrentVersion);

                if (result.IsError)
                    return Result.Error<List<EnvironmentLayer>>(result.Message, result.Code);

                list.Add(result.Data);
            }

            return Result.Success(list);
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
            Layers = other.Layers;
        }

        /// <inheritdoc />
        protected override IDictionary<Type, Func<ReplayedEvent, bool>> GetEventApplicationMapping()
            => new Dictionary<Type, Func<ReplayedEvent, bool>>
            {
                {typeof(DefaultEnvironmentCreated), HandleDefaultEnvironmentCreatedEvent},
                {typeof(EnvironmentCreated), HandleEnvironmentCreatedEvent},
                {typeof(EnvironmentDeleted), HandleEnvironmentDeletedEvent},
                {typeof(EnvironmentLayersModified), HandleEnvironmentLayersModified},
                {typeof(EnvironmentLayerKeysImported), HandleEnvironmentLayerKeysImported},
                {typeof(EnvironmentLayerKeysModified), HandleEnvironmentLayerKeysModified}
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

        /// <summary>
        ///     check if a layer that is currently being used is being updated.
        ///     updates to the used layers need to increase <see cref="DomainObject.CurrentVersion" />, so we see an accurate representation of our Layers
        /// </summary>
        private bool HandleEnvironmentLayerKeysImported(ReplayedEvent replayedEvent)
            => replayedEvent.DomainEvent is EnvironmentLayerKeysImported imported
               && Layers.Contains(imported.Identifier);

        /// <summary>
        ///     check if a layer that is currently being used is being updated.
        ///     updates to the used layers need to increase <see cref="DomainObject.CurrentVersion" />, so we see an accurate representation of our Layers
        /// </summary>
        private bool HandleEnvironmentLayerKeysModified(ReplayedEvent replayedEvent)
            => replayedEvent.DomainEvent is EnvironmentLayerKeysModified modified
               && Layers.Contains(modified.Identifier);

        private bool HandleEnvironmentLayersModified(ReplayedEvent replayedEvent)
        {
            if (!(replayedEvent.DomainEvent is EnvironmentLayersModified modified) || modified.Identifier != Identifier)
                return false;

            Layers = modified.Layers;
            return true;
        }
    }
}