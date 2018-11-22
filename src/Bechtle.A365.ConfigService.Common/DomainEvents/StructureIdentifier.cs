using Bechtle.A365.ConfigService.Common.DbObjects;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    /// <summary>
    ///     Configuration-Structure, filled out with data from <see cref="StructureIdentifier" /> to create a Configuration
    /// </summary>
    public class StructureIdentifier : Identifier
    {
        /// <inheritdoc />
        public StructureIdentifier()
        {
        }

        /// <inheritdoc />
        public StructureIdentifier(string name, int version)
        {
            Name = name;
            Version = version;
        }

        /// <inheritdoc />
        public StructureIdentifier(Structure structure) : this(structure.Name, structure.Version)
        {
        }

        /// <summary>
        ///     name of this structure, indicates uses the Configuration built from this and <see cref="StructureIdentifier" />
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     unique version of this Structure
        /// </summary>
        public int Version { get; set; }

        public override string ToString() => $"[{nameof(StructureIdentifier)}; {nameof(Name)}: '{Name}'; {nameof(Version)}: '{Version}']";
    }
}