using System.Collections.Generic;

namespace Bechtle.A365.ConfigService.Parsing
{
    /// <summary>
    ///     parse strings for references
    ///     <remarks>
    ///         Structure: {{Keyword: Value}}
    ///         Allowed Keyword:
    ///         - Using
    ///         - make the provided value available under the name provided to 'Alias'
    ///         - must be used in conjunction with 'Alias'
    ///         - Alias
    ///         - Path (default)
    ///         - path to one or more values that should be inserted here
    ///     </remarks>
    /// </summary>
    public interface IConfigurationParser
    {
        List<ConfigValuePart> Parse(string text);
    }
}