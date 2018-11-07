﻿using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Common.DomainEvents;

namespace Bechtle.A365.ConfigService.Services
{
    /// <summary>
    /// </summary>
    public interface IEventStore
    {
        /// <summary>
        /// </summary>
        /// <param name="domainEvent"></param>
        /// <returns></returns>
        Task WriteEvent<T>(T domainEvent) where T : DomainEvent;
    }
}