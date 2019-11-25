﻿namespace Bechtle.A365.ConfigService.DomainObjects
{
    /// <summary>
    ///     Generic container holding a Snapshot of a <see cref="DomainObject" /> in a given point-in-time
    /// </summary>
    public class DomainObjectSnapshot
    {
        /// <summary>
        ///     arbitrary Name that designates the Origin of the Snapshot
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        ///     arbitrary identifier to tie this snapshot to its Origin
        /// </summary>
        public string Identifier { get; set; }

        /// <summary>
        ///     Serialized Data, to be evaluated by the Target-<see cref="DomainObject" />
        /// </summary>
        public string JsonData { get; set; }

        /// <summary>
        ///     EventStore-Version of the original <see cref="DomainObject" />
        /// </summary>
        public long Version { get; set; }

        /// <summary>
        ///     EventStore-MetaVersion of the original <see cref="DomainObject"/>
        /// </summary>
        public long MetaVersion { get; set; }
    }
}