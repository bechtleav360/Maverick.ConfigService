using System.Collections.Generic;

namespace Bechtle.A365.ConfigService.Parsing
{
    public class ReferencePart : ConfigValuePart
    {
        public ReferencePart(Dictionary<ReferenceCommand, string> commands)
        {
            Commands = commands;
        }

        public Dictionary<ReferenceCommand, string> Commands { get; }
    }
}