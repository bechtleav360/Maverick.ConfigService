using System.Collections.Generic;

namespace Bechtle.A365.ConfigService.Cli.Commands.MigrationModels
{
    /// <summary>
    ///     internal representation of an Environment in its 'Initial' version
    /// </summary>
    public class InitialEnvRepr
    {
        /// <summary>
        ///     Environment-Identifier
        /// </summary>
        public InitialEnvIdRepr Identifier;

        /// <summary>
        ///     Flag indicating if this is the Default-Environment for the given Category
        /// </summary>
        public bool IsDefault;

        /// <summary>
        ///     List of Keys contained in this Environment
        /// </summary>
        public List<InitialKeyRepr>? Keys;
    }
}
