using System.Collections.Generic;
using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.Cli.Commands
{
    public class LayerComparison
    {
        public LayerIdentifier Source { get; set; }

        public LayerIdentifier Target { get; set; }

        public List<ConfigKeyAction> RequiredActions { get; set; }
    }
}