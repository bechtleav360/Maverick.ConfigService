using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using Bechtle.A365.ConfigService.DomainObjects;
using Bechtle.A365.ConfigService.Interfaces;
using Bechtle.A365.ConfigService.Interfaces.Stores;

namespace Bechtle.A365.ConfigService.Implementations.EventProjections
{
    /// <summary>
    ///     Base-Functionality for implementations of <see cref="IDomainEventProjection{TDomainEvent}" />
    /// </summary>
    public abstract class EventProjectionBase
    {
        private readonly IDomainObjectStore _objectStore;

        /// <summary>
        ///     Initialize shared components
        /// </summary>
        /// <param name="objectStore">storage for generated configs</param>
        protected EventProjectionBase(IDomainObjectStore objectStore)
        {
            _objectStore = objectStore;
        }

        /// <summary>
        ///     Generate the autocomplete-paths for this layer
        /// </summary>
        /// <param name="keys">map of Key => Object</param>
        /// <returns></returns>
        protected List<EnvironmentLayerKeyPath> GenerateKeyPaths(IDictionary<string, EnvironmentLayerKey> keys)
        {
            var roots = new List<EnvironmentLayerKeyPath>();

            foreach ((string key, EnvironmentLayerKey _) in keys.OrderBy(k => k.Key))
            {
                string[] parts = key.Split('/');

                string rootPart = parts.First();
                EnvironmentLayerKeyPath? root = roots.FirstOrDefault(p => p.Path.Equals(rootPart, StringComparison.InvariantCultureIgnoreCase));

                if (root is null)
                {
                    root = new EnvironmentLayerKeyPath(rootPart);
                    roots.Add(root);
                }

                EnvironmentLayerKeyPath current = root;

                foreach (string part in parts.Skip(1))
                {
                    EnvironmentLayerKeyPath? next = current.Children.FirstOrDefault(p => p.Path.Equals(part, StringComparison.InvariantCultureIgnoreCase));

                    if (next is null)
                    {
                        next = new EnvironmentLayerKeyPath(part, current);
                        current.Children.Add(next);
                    }

                    current = next;
                }
            }

            return roots;
        }

        /// <summary>
        ///     Resolve the keys, and stack them to generate the data for an <see cref="ConfigEnvironment" />
        /// </summary>
        /// <param name="layerIds">collection of <see cref="LayerIdentifier" /> in ascending priority</param>
        /// <returns>result containing resolved env-data</returns>
        protected async Task<IResult<Dictionary<string, EnvironmentLayerKey>>> ResolveEnvironmentKeys(IEnumerable<LayerIdentifier> layerIds)
        {
            var result = new Dictionary<string, EnvironmentLayerKey>(StringComparer.OrdinalIgnoreCase);

            foreach (LayerIdentifier layerId in layerIds)
            {
                IResult<EnvironmentLayer> layerResult = await _objectStore.Load<EnvironmentLayer, LayerIdentifier>(layerId);
                if (layerResult.IsError)
                {
                    return Result.Error<Dictionary<string, EnvironmentLayerKey>>(layerResult.Message, layerResult.Code);
                }

                EnvironmentLayer layer = layerResult.CheckedData;

                foreach ((string _, EnvironmentLayerKey entry) in layer.Keys)
                {
                    // remove existing key if it exists already...
                    if (result.ContainsKey(entry.Key))
                    {
                        result.Remove(entry.Key);
                    }

                    // add or replace key
                    result.Add(entry.Key, entry);
                }
            }

            return Result.Success(result);
        }
    }
}
