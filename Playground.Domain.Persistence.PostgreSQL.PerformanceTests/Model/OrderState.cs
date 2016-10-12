﻿using System;
using Playground.Domain.Events;
using Playground.Domain.Persistence.PostgreSQL.PerformanceTests.Model.Events;

namespace Playground.Domain.Persistence.PostgreSQL.PerformanceTests.Model
{
    public class OrderState
        : IGetAppliedWith<OrderCreated>,
        IGetAppliedWith<OrderChanged>,
        IGetAppliedWith<StartedFulfilment>,
        IGetAppliedWith<ShipOrder>,
        IGetAppliedWith<OrderDelivered>
    {
        public string UserOrdering { get; set; }

        public string ShippingAddress { get; set; }

        public Guid ProductIdToSend { get; set; }

        public OrderStatus Status { get; set; }

        void IGetAppliedWith<OrderCreated>.Apply(OrderCreated e)
        {
            UserOrdering = e.UserOrdering;
            ShippingAddress = e.Address;
            ProductIdToSend = e.ProductId;
            Status = OrderStatus.Created;
        }

        void IGetAppliedWith<OrderChanged>.Apply(OrderChanged e)
        {
            ShippingAddress = e.NewAddress;
        }

        void IGetAppliedWith<StartedFulfilment>.Apply(StartedFulfilment e)
        {
            Status = OrderStatus.BeingFulfilled;
        }

        void IGetAppliedWith<ShipOrder>.Apply(ShipOrder e)
        {
            Status = OrderStatus.Shipped;
        }

        void IGetAppliedWith<OrderDelivered>.Apply(OrderDelivered e)
        {
            Status = OrderStatus.Delivered;
        }
    }
}