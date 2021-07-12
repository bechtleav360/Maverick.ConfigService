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
        public string FullPath => Parent?.FullPath + Path + (Children?.Any() == true ? "/" : "");

        /// <summary>
        ///     Reference to Parent-Node
        /// </summary>
        public EnvironmentLayerKeyPath Parent { get; set; }

        /// <summary>
        ///     last Path-Component of this Node
        /// </summary>
        public string Path { get; set; }

        /// <inheritdoc cref="EnvironmentLayerKeyPath" />
        public EnvironmentLayerKeyPath()
        {
            Path = string.Empty;
            Parent = null;
            Children = new List<EnvironmentLayerKeyPath>();
        }

        /// <inheritdoc cref="EnvironmentLayerKeyPath" />
        public EnvironmentLayerKeyPath(string path, EnvironmentLayerKeyPath parent = null, IEnumerable<EnvironmentLayerKeyPath> children = null)
        {
            Path = path;
            Parent = parent;
            Children = new List<EnvironmentLayerKeyPath>(children ?? new EnvironmentLayerKeyPath[0]);
        }

        /// <inheritdoc />
        public override bool Equals(object obj) => ReferenceEquals(this, obj) || obj is EnvironmentLayerKeyPath other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Children, Parent, Path);

        /// <inheritdoc cref="operator ==" />
        public static bool operator ==(EnvironmentLayerKeyPath left, EnvironmentLayerKeyPath right) => Equals(left, right);

        /// <inheritdoc cref="operator !=" />
        public static bool operator !=(EnvironmentLayerKeyPath left, EnvironmentLayerKeyPath right) => !Equals(left, right);

        private bool Equals(EnvironmentLayerKeyPath other) =>
            Children.SequenceEqual(other.Children)
            && Equals(Parent, other.Parent)
            && Path == other.Path;
    }
}
