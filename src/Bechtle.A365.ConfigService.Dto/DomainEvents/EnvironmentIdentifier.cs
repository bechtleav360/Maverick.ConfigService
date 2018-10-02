using Bechtle.A365.ConfigService.Dto.DbObjects;

namespace Bechtle.A365.ConfigService.Dto.DomainEvents
{
    /// <summary>
    ///     Information to identify an Environment
    /// </summary>
    public class EnvironmentIdentifier : Identifier
    {
        /// <inheritdoc />
        public EnvironmentIdentifier()
        {
        }

        /// <inheritdoc />
        public EnvironmentIdentifier(string category, string name)
        {
            Category = category;
            Name = name;
        }

        /// <inheritdoc />
        public EnvironmentIdentifier(ConfigEnvironment environment) : this(environment.Category, environment.Name)
        {
        }

        // @TODO: maybe use a better name for this? Tenant / Folder / etc?
        /// <summary>
        ///     Category for a group of Environments, think Folder / Tenant and the like
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        ///     Unique name for an Environment within a <see cref="Category" />
        /// </summary>
        public string Name { get; set; }

        /// <inheritdoc />
        public override string ToString() => $"[{nameof(EnvironmentIdentifier)}; {nameof(Category)}: '{Category}'; {nameof(Name)}: '{Name}']";
    }
}