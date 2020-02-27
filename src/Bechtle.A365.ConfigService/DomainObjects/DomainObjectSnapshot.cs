using System;

namespace Bechtle.A365.ConfigService.DomainObjects
{
    /// <summary>
    ///     Generic container holding a Snapshot of a <see cref="DomainObject" /> in a given point-in-time
    /// </summary>
    public class DomainObjectSnapshot : IEquatable<DomainObjectSnapshot>
    {
        /// <inheritdoc cref="DomainObjectSnapshot"/>
        public DomainObjectSnapshot(string dataType, string identifier, string jsonData, long version, long metaVersion)
        {
            DataType = dataType;
            Identifier = identifier;
            JsonData = jsonData;
            Version = version;
            MetaVersion = metaVersion;
        }

        /// <summary>
        ///     arbitrary Name that designates the Origin of the Snapshot
        /// </summary>
        public string DataType { get; }

        /// <summary>
        ///     arbitrary identifier to tie this snapshot to its Origin
        /// </summary>
        public string Identifier { get; }

        /// <summary>
        ///     Serialized Data, to be evaluated by the Target-<see cref="DomainObject" />
        /// </summary>
        public string JsonData { get; }

        /// <summary>
        ///     EventStore-MetaVersion of the original <see cref="DomainObject" />
        /// </summary>
        public long MetaVersion { get; }

        /// <summary>
        ///     EventStore-Version of the original <see cref="DomainObject" />
        /// </summary>
        public long Version { get; }

        /// <summary>
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator ==(DomainObjectSnapshot left, DomainObjectSnapshot right) => Equals(left, right);

        /// <summary>
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator !=(DomainObjectSnapshot left, DomainObjectSnapshot right) => !Equals(left, right);

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((DomainObjectSnapshot) obj);
        }

        /// <inheritdoc />
        public virtual bool Equals(DomainObjectSnapshot other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return DataType == other.DataType && Identifier == other.Identifier && JsonData == other.JsonData && Version == other.Version &&
                   MetaVersion == other.MetaVersion;
        }

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(DataType, Identifier, JsonData, Version, MetaVersion);
    }
}