using System;
using System.Collections.Generic;
using System.Linq;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    /// <summary>
    ///     Node inside a Tree of Paths to represent Environment-Data
    /// </summary>
    public sealed class EnvironmentLayerKeyPath
    {
        /// <summary>
        ///     List of Children that may come after this
        /// </summary>
        public List<EnvironmentLayerKeyPath> Children { get; }

        /// <summary>
        ///     Full Path including Parents
        /// </summary>
        public string FullPath => (ParentPath != "" ? ParentPath + "/" : "") + Path + (Children.Any() ? "/" : "");

        /// <summary>
        ///     Full path to the parent-node.
        ///     Used instead of parent-reference to make de-/serialisation easier
        /// </summary>
        public string ParentPath { get; set; }

        /// <summary>
        ///     last Path-Component of this Node
        /// </summary>
        public string Path { get; set; }

        /// <inheritdoc cref="EnvironmentLayerKeyPath" />
        public EnvironmentLayerKeyPath(
            string path,
            EnvironmentLayerKeyPath? parent = null,
            IEnumerable<EnvironmentLayerKeyPath>? children = null)
        {
            Path = path;
            ParentPath = parent?.FullPath ?? string.Empty;
            Children = children is null ? new List<EnvironmentLayerKeyPath>() : new List<EnvironmentLayerKeyPath>(children);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is EnvironmentLayerKeyPath other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Children, ParentPath, Path);

        /// <inheritdoc cref="operator ==" />
        public static bool operator ==(EnvironmentLayerKeyPath left, EnvironmentLayerKeyPath right) => Equals(left, right);

        /// <inheritdoc cref="operator !=" />
        public static bool operator !=(EnvironmentLayerKeyPath left, EnvironmentLayerKeyPath right) => !Equals(left, right);

        private bool Equals(EnvironmentLayerKeyPath other) =>
            Children.SequenceEqual(other.Children)
            && ParentPath == other.ParentPath
            && Path == other.Path;
    }
}
