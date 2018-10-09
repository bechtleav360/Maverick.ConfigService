using System.Collections.Generic;

namespace Bechtle.A365.ConfigService.Configuration
{
    /// <summary>
    /// </summary>
    public class EndpointConfiguration
    {
        /// <summary>
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// </summary>
        public Dictionary<string, string> Properties { get; set; }

        /// <summary>
        /// </summary>
        public string Protocol { get; set; }

        /// <summary>
        /// </summary>
        public string RootPath { get; set; }
    }
}