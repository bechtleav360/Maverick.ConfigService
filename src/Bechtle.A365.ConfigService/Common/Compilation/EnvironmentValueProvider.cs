using System.Collections.Generic;

namespace Bechtle.A365.ConfigService.Common.Compilation
{
    /// <inheritdoc cref="IConfigValueProvider"/>
    public class EnvironmentValueProvider : DictionaryValueProvider
    {
        /// <inheritdoc />
        public EnvironmentValueProvider(IDictionary<string, string?> repository) : base(repository, "environment")
        {
        }
    }
}
