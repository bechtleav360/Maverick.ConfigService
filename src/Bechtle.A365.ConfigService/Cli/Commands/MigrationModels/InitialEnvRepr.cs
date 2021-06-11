using System.Collections.Generic;

namespace Bechtle.A365.ConfigService.Cli.Commands.MigrationModels
{
    /// <summary>
    ///     internal representation of an Environment in its 'Initial' version
    /// </summary>
    public class InitialEnvRepr
    {
        public InitialEnvIdRepr Identifier;

        public bool IsDefault;

        public List<InitialKeyRepr> Keys;
    }
}