﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.DomainEvents;
using EventStore.ClientAPI;

namespace Bechtle.A365.ConfigService.Services
{
    /// <summary>
    /// </summary>
    public interface IEventStore
    {
        /// <inheritdoc cref="Services.ConnectionState"/>
        ConnectionState ConnectionState { get; }

        /// <summary>
        ///     replay all events and get the written DomainEvents
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<(RecordedEvent, DomainEvent)>> ReplayEvents();

        /// <summary>
        /// </summary>
        /// <param name="domainEvent"></param>
        /// <returns></returns>
        Task WriteEvent<T>(T domainEvent) where T : DomainEvent;
    }
}