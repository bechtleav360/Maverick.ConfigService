using System;
using System.Collections.Generic;
using System.Linq;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    /// <summary>
    ///     Node inside a Tree of Paths to represent Environment-Data
    /// </summary>
    public class ConfigEnvironmentKeyPath : IEquatable<ConfigEnvironmentKeyPath>
    {
        /// <inheritdoc cref="ConfigEnvironmentKeyPath" />
        public ConfigEnvironmentKeyPath(string path, ConfigEnvironmentKeyPath parent = null, IEnumerable<ConfigEnvironmentKeyPath> children = null)
        {
            Path = path;
            Parent = parent;
            Children = new List<ConfigEnvironmentKeyPath>(children ?? new ConfigEnvironmentKeyPath[0]);
        }

        /// <summary>
        ///     List of Children that may come after this
        /// </summary>
        public List<ConfigEnvironmentKeyPath> Children { get; }

        /// <summary>
        ///     Full Path including Parents
        /// </summary>
        public string FullPath => Parent?.FullPath + Path + (Children?.Any() == true ? "/" : "");

        /// <summary>
        ///     Reference to Parent-Node
        /// </summary>
        public ConfigEnvironmentKeyPath Parent { get; }

        /// <summary>
        ///     last Path-Component of this Node
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator ==(ConfigEnvironmentKeyPath left, ConfigEnvironmentKeyPath right) => Equals(left, right);

        /// <summary>
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator !=(ConfigEnvironmentKeyPath left, ConfigEnvironmentKeyPath right) => !Equals(left, right);

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ConfigEnvironmentKeyPath) obj);
        }

        /// <inheritdoc />
        public bool Equals(ConfigEnvironmentKeyPath other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return (Equals(Children, other.Children)
                    || !Children.Except(other.Children).Any()
                    && !other.Children.Except(Children).Any())
                   && Equals(Parent, other.Parent)
                   && Path == other.Path;
        }

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Children, Parent, Path);
    }
}