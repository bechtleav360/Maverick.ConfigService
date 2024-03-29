﻿using System;
using Bechtle.A365.ServiceBase.EventStore.DomainEventBase;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    /// <summary>
    ///     Implementation of <see cref="DomainEvent{TPayload}" /> that uses the *actual* type of the Payload instead of of
    ///     <typeparamref name="TEventBase" />
    /// </summary>
    /// <typeparam name="TEventBase">base-class of the given data, used only as fallback-type</typeparam>
    public class LateBindingDomainEvent<TEventBase> : DomainEvent<TEventBase>
    {
        /// <inheritdoc cref="Type" />
        public override string Type => Payload is null ? typeof(TEventBase).Name : Payload.GetType().Name;

        /// <summary>
        ///     DO NOT USE - this is meant to be used by a serialization-library.
        ///     Create a new instance of <see cref="LateBindingDomainEvent{TEventBase}"/>
        /// </summary>
        public LateBindingDomainEvent()
        {
            EventOwner = string.Empty;
            Payload = default!;
        }

        /// <inheritdoc />
        public LateBindingDomainEvent(string eventOwner, TEventBase payload) : base(eventOwner, payload)
        {
        }

        /// <inheritdoc />
        public LateBindingDomainEvent(DateTime timestamp, string eventOwner, TEventBase payload)
            : base(timestamp, eventOwner, payload)
        {
        }
    }
}
