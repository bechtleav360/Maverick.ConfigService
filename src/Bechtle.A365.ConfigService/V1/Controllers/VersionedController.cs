using System;
using Microsoft.Extensions.Logging;

namespace Bechtle.A365.ConfigService.V1.Controllers
{
    /// <summary>
    ///     Base class for Versioned Controllers that inherit functionality from previous versions (V0)
    /// </summary>
    public abstract class VersionedController<T> : ControllerBase where T : V0.Controllers.ControllerBase
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