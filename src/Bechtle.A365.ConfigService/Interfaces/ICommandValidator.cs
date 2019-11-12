using Bechtle.A365.ConfigService.Common;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.Interfaces
{
    /// <summary>
    ///     component that validates DomainEvents for validity.
    ///     - command contains valid data
    ///     - command hasn't been issued with same intent (same command at a later date)
    /// </summary>
    public interface ICommandValidator
    {
        /// <summary>
        ///     validate the given <see cref="DomainEvent" /> for its validity
        /// </summary>
        /// <param name="domainEvent"></param>
        /// <returns></returns>
        IResult ValidateDomainEvent(DomainEvent domainEvent);
    }
}