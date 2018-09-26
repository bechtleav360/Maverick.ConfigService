﻿namespace Bechtle.A365.ConfigService.Dto.DomainEvents
{
    /// <inheritdoc />
    /// <summary>
    ///     an Environment has been created under the given identifier
    /// </summary>
    public class EnvironmentCreated : DomainEvent
    {
        /// <inheritdoc cref="EnvironmentIdentifier" />
        public EnvironmentIdentifier Identifier { get; set; }
    }
}