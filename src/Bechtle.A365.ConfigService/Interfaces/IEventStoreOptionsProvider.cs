using System.Threading;
using System.Threading.Tasks;

namespace Bechtle.A365.ConfigService.Interfaces
{
    /// <summary>
    ///     Provides access to Options / Configurations from the configured EventStore
    /// </summary>
    public interface IEventStoreOptionsProvider
    {
        /// <summary>
        ///     Indicates if events are limited in size (<see cref="MaxEventSizeInBytes" />)
        /// </summary>
        bool EventSizeLimited { get; }

        /// <summary>
        ///     shows how large each individual Event can be before being rejected
        /// </summary>
        long MaxEventSizeInBytes { get; }

        /// <summary>
        ///     Load all possible Options from the configured EventStore
        /// </summary>
        /// <param name="cancellationToken">token to cancel the operation with</param>
        /// <returns></returns>
        Task LoadConfiguration(CancellationToken cancellationToken);
    }
}
