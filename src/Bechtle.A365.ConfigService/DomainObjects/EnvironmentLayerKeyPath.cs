using System;
using System.Collections.Generic;
using System.Linq;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    /// <summary>
    ///     Node inside a Tree of Paths to represent Environment-Data
    /// </summary>
    public class EnvironmentLayerKeyPath : IEquatable<EnvironmentLayerKeyPath>
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
        public virtual bool Equals(EnvironmentLayerKeyPath other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return (Equals(Children, other.Children)
                    || !Children.Except(other.Children).Any()
                    && !other.Children.Except(Children).Any())
                   && Equals(Parent, other.Parent)
                   && Path == other.Path;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((EnvironmentLayerKeyPath) obj);
        }

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Children, Parent, Path);

        /// <summary>
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator ==(EnvironmentLayerKeyPath left, EnvironmentLayerKeyPath right) => Equals(left, right);

        /// <summary>
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator !=(EnvironmentLayerKeyPath left, EnvironmentLayerKeyPath right) => !Equals(left, right);
    }
}
