using System.Collections.Generic;

namespace Bechtle.A365.ConfigService.Parsing
{
    public class ReferencePart : ConfigValuePart
    {
        public Dictionary<ReferenceCommand, string> Commands { get; }

        public ReferencePart(Dictionary<ReferenceCommand, string> commands)
        {
            Commands = commands;
        }
    }
}