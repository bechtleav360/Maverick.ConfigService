using System;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.Controllers.V1
{
    /// <summary>
    ///     Base class for Versioned Controllers that inherit functionality from previous versions
    /// </summary>
    public abstract class VersionedController<T> : ControllerBase
    {
        /// <summary>
        ///     instance of this Controllers' previous version
        /// </summary>
        protected T PreviousVersion { get; }

        /// <inheritdoc />
        public VersionedController(IServiceProvider provider, ILogger logger, T previousVersion)
            : base(provider, logger)
        {
            PreviousVersion = previousVersion;
        }
    }
}