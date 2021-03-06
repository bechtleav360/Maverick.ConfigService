﻿namespace Bechtle.A365.ConfigService.Common.Objects
{
    /// <summary>
    ///     a single Environment, exported for later import
    /// </summary>
    public class EnvironmentExport
    {
        /// <summary>
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// </summary>
        public EnvironmentKeyExport[] Keys { get; set; }
    }
}