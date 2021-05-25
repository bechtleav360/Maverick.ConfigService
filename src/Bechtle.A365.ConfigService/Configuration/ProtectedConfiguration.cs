using System;
using System.Collections.Generic;

namespace Bechtle.A365.ConfigService.Configuration
{
    /// <summary>
    ///     Service => Certificate association for data-protection
    /// </summary>
    public class ProtectedConfiguration
    {
        private Dictionary<string, string> _regions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     Service to Certificate association for data-protection
        /// </summary>
        public Dictionary<string, string> Regions
        {
            get => _regions;
            set => _regions = new Dictionary<string, string>(value, StringComparer.OrdinalIgnoreCase);
        }
    }
}