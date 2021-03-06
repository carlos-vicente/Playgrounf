﻿using System;
using Playground.Domain.Events;
using Playground.Domain.Model;

namespace Playground.Domain.Persistence.UnitTests.TestModel
{
    internal class TestAggregateRoot 
        : AggregateRoot<TestAggregateState>,
        IGetAppliedWith<TestAggregateCreated>,
        IGetAppliedWith<TestAggregateChanged>
    {
        public TestAggregateRoot(Guid id) : base(id) { }

        public TestAggregateRoot(Guid id, TestAggregateState hydratedState, long currentVersion)
            : base(id, hydratedState, currentVersion)
        {
            
        }

        void IGetAppliedWith<TestAggregateCreated>.Apply(TestAggregateCreated e)
        {
            throw new NotImplementedException();
        }

        void IGetAppliedWith<TestAggregateChanged>.Apply(TestAggregateChanged e)
        {
            throw new NotImplementedException();
        }
    }
}