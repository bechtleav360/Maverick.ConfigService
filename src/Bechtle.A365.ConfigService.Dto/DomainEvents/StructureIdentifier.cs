namespace Bechtle.A365.ConfigService.Dto.DomainEvents
{
    /// <summary>
    ///     Configuration-Structure, filled out with data from <see cref="EnvironmentIdentifier" /> to create a Configuration
    /// </summary>
    public class StructureIdentifier
    {
        /// <summary>
        ///     name of this structure, indicates uses the Configuration built from this and <see cref="EnvironmentIdentifier" />
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     unique version of this Structure
        /// </summary>
        public int Version { get; set; }
    }
}