namespace Bechtle.A365.ConfigService.Dto.DomainEvents
{
    /// <summary>
    ///     Information to identify an Environment
    /// </summary>
    public class EnvironmentIdentifier
    {
        // @TODO: maybe use a better name for this? Tenant / Folder / etc?
        /// <summary>
        ///     Category for a group of Environments, think Folder / Tenant and the like
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        ///     Unique name for an Environment within a <see cref="Category" />
        /// </summary>
        public string Name { get; set; }
    }
}