﻿using Playground.Domain.Events;

namespace Playground.Domain.Persistence.PostgreSQL.PerformanceTests.GuidIdentifier.Model.Events
{
    public class OrderShippingAddressChanged : DomainEvent
    {
        public string NewAddress { get; set; }

        public OrderShippingAddressChanged(string newAddress)
        {
            NewAddress = newAddress;
        }
    }
}