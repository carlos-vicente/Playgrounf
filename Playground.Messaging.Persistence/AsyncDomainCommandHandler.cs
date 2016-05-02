﻿using System.Threading.Tasks;
using Playground.Domain.Model;
using Playground.Domain.Persistence;
using Playground.Messaging.Commands;

namespace Playground.Messaging.Persistence
{
    public abstract class AsyncDomainCommandHandler<TCommand, TAggregate>
        : IAsyncCommandHandler<TCommand> 
        where TAggregate: AggregateRoot 
        where TCommand : DomainCommand<TAggregate>
    {
        private readonly IAggregateContext _aggregateContext;

        protected AsyncDomainCommandHandler(IAggregateContext aggregateContext)
        {
            _aggregateContext = aggregateContext;
        }

        public async Task Handle(TCommand command)
        {
            var aggregate = await _aggregateContext
                .TryLoad<TAggregate>(command.AggregateRootId)
                .ConfigureAwait(false);

            if (aggregate == null)
            {
                aggregate = await _aggregateContext
                    .Create<TAggregate>(command.AggregateRootId)
                    .ConfigureAwait(false);
            }

            await HandleOnAggregate(command, aggregate)
                .ConfigureAwait(false);

            await _aggregateContext
                .Save(aggregate)
                .ConfigureAwait(false);
        }

        public override string ToString()
        {
            return $"{GetType().Name} => {typeof (TAggregate).Name}";
        }

        protected abstract Task HandleOnAggregate(TCommand command, TAggregate aggregate);
    }
}