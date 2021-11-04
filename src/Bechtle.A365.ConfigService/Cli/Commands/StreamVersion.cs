namespace Bechtle.A365.ConfigService.Cli.Commands
{
    /// <summary>
    ///     Named versions of EventStore-Streams
    /// </summary>
    public enum StreamVersion
    {
        /// <summary>
        ///     Version is not defined - default-value to catch errors
        /// </summary>
        Undefined = 0,

        /// <summary>
        ///     First version of DomainEvents that can be migrated.
        ///     This is the Version where Environments were modified directly
        /// </summary>
        Initial = 1,

        /// <summary>
        ///     Second version of DomainEvents.
        ///     This is the version that split Environments into Layers that can be assigned and modified
        /// </summary>
        LayeredEnvironments = 2,

        /// <summary>
        ///     Third version of DomainEvents.
        ///     This is the version that wrapped all DomainEvents in the IDomainEvent shell provided by ServiceBase.
        /// </summary>
        ServiceBased = 3
    }
}
