using System.Collections.Generic;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.Cli.Commands
{
    public class EnvironmentComparison
    {
        public EnvironmentIdentifier Source { get; set; }

        public EnvironmentIdentifier Target { get; set; }

        public List<ConfigKeyAction> RequiredActions { get; set; }
    }
}