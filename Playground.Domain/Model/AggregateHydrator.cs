﻿using System.Collections.Generic;
using Playground.Domain.Events;

namespace Playground.Domain.Model
{
    public class AggregateHydrator : IAggregateHydrator
    {
        public TAggregateRoot HydrateAggregateWithEvents<TAggregateRoot>(
            TAggregateRoot aggregateRootBase, 
            ICollection<IEvent> domainEvents) 
            where TAggregateRoot : AggregateRoot
        {
            foreach (var domainEvent in domainEvents)
            {
                // the dynamic cast makes sure the right method is called
                Do(aggregateRootBase, (dynamic)domainEvent);
            }

            return aggregateRootBase;
        }

        private void Do<TAggregateRoot, TDomainEvent>(
            TAggregateRoot aggregateRootBase, TDomainEvent domainEvent)
            where TAggregateRoot : AggregateRoot
            where TDomainEvent : IEvent
        {
            ((IEmit<TDomainEvent>)aggregateRootBase).Apply(domainEvent);
        }
    }
}