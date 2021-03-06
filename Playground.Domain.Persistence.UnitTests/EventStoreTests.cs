﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using NUnit.Framework;
using Playground.Core.Serialization;
using Playground.Domain.Events;
using Playground.Domain.Persistence.Events;
using Playground.Domain.Persistence.UnitTests.TestModel;
using Playground.Tests;
using Ploeh.AutoFixture;

namespace Playground.Domain.Persistence.UnitTests
{
    public class EventStoreTests : SimpleTestBase
    {
        [Test]
        public async Task CreateEventStream_WillCreateStream_WhenStreamDoesNotExist()
        {
            // arrange
            var streamId = Guid.NewGuid();

            A.CallTo(() => Faker.Resolve<IEventRepository>()
                .CheckStream(streamId))
                .Returns(false);

            var aggregateTypeName = typeof (TestAggregateRoot).AssemblyQualifiedName;

            Faker.Provide<Func<Guid>>(Guid.NewGuid);

            var sut = Faker.Resolve<EventStore>();

            // act
            await sut
                .CreateEventStream<TestAggregateRoot>(streamId)
                .ConfigureAwait(false);

            // assert
            A.CallTo(() => Faker.Resolve<IEventRepository>()
                .CreateStream(streamId, aggregateTypeName))
                .MustHaveHappened(Repeated.Exactly.Once);
        }

        [Test]
        public void CreateEventStream_ThrowsException_WhenStreamAlreadyExists()
        {
            // arrange
            var streamId = Guid.NewGuid();

            A.CallTo(() => Faker.Resolve<IEventRepository>()
                .CheckStream(streamId))
                .Returns(true);

            Faker.Provide<Func<Guid>>(Guid.NewGuid);

            var sut = Faker.Resolve<EventStore>();

            Func<Task> expcetionThrower = async () => await sut
                .CreateEventStream<TestAggregateRoot>(streamId)
                .ConfigureAwait(false);

            // act & assert
            expcetionThrower
                .ShouldThrow<InvalidOperationException>();
        }

        [Test]
        public async Task StoreEvents_WillStoreMappedEvents_WhenStreamExistsAndIsNotBroken()
        {
            // arrange
            var streamId = Guid.NewGuid();
            var currentStreamVersion = Fixture.Create<long>();

            var event1 = Fixture
                .Build<TestAggregateChanged>()
                .With(e => e.Metadata, new Metadata(streamId, currentStreamVersion + 1, typeof(TestAggregateChanged)))
                .Create();
            var event2 = Fixture
                .Build<TestAggregateChanged>()
                .With(e => e.Metadata, new Metadata(streamId, currentStreamVersion + 2, typeof(TestAggregateChanged)))
                .Create();

            var lastStreamEvent = new StoredEvent(
                typeof (TestAggregateCreated).AssemblyQualifiedName,
                Fixture.Create<DateTime>(),
                Fixture.Create<string>(),
                Guid.NewGuid(),
                currentStreamVersion);

            A.CallTo(() => Faker.Resolve<IEventRepository>()
                .GetLast(streamId))
                .Returns(Task.FromResult(lastStreamEvent));

            var event1Serialiazed = Fixture.Create<string>();
            var event2Serialiazed = Fixture.Create<string>();

            A.CallTo(() => Faker.Resolve<IObjectSerializer>()
                .Serialize(event1))
                .Returns(event1Serialiazed);

            A.CallTo(() => Faker.Resolve<IObjectSerializer>()
                .Serialize(event2))
                .Returns(event2Serialiazed);

            var batchId = Guid.NewGuid();

            Faker.Provide<Func<Guid>>(() => batchId);

            var expectedEventName = typeof (TestAggregateChanged).AssemblyQualifiedName;

            var expectedEvents = new List<StoredEvent>
            {
                new StoredEvent(expectedEventName, event1.Metadata.OccorredOn, event1Serialiazed, batchId, currentStreamVersion + 1),
                new StoredEvent(expectedEventName, event2.Metadata.OccorredOn, event2Serialiazed, batchId, currentStreamVersion + 2)
            };

            var sut = Faker.Resolve<EventStore>();

            // act
            await sut
                .StoreEvents(streamId, currentStreamVersion, new DomainEvent[] { event1, event2 })
                .ConfigureAwait(false);

            // assert
            A.CallTo(() => Faker.Resolve<IEventRepository>()
                .Add(
                    streamId,
                    A<ICollection<StoredEvent>>.That.Matches(evts => expectedEvents.All(evts.Contains))))
                .MustHaveHappened(Repeated.Exactly.Once);
        }

        [Test]
        public async Task StoreEvents_WillStoreMappedEvents_WhenStreamExistsAndIsEmpty()
        {
            // arrange
            var streamId = Guid.NewGuid();
            
            var event1 = Fixture
                .Build<TestAggregateChanged>()
                .With(e => e.Metadata, new Metadata(streamId, 1L, typeof(TestAggregateChanged)))
                .Create();
            var event2 = Fixture
                .Build<TestAggregateChanged>()
                .With(e => e.Metadata, new Metadata(streamId, 2L, typeof(TestAggregateChanged)))
                .Create();

            A.CallTo(() => Faker.Resolve<IEventRepository>()
                .GetLast(streamId))
                .Returns(Task.FromResult<StoredEvent>(null));

            var event1Serialiazed = Fixture.Create<string>();
            var event2Serialiazed = Fixture.Create<string>();

            A.CallTo(() => Faker.Resolve<IObjectSerializer>()
                .Serialize(event1))
                .Returns(event1Serialiazed);

            A.CallTo(() => Faker.Resolve<IObjectSerializer>()
                .Serialize(event2))
                .Returns(event2Serialiazed);

            var batchId = Guid.NewGuid();

            Faker.Provide<Func<Guid>>(() => batchId);

            var expectedEventName = typeof(TestAggregateChanged).AssemblyQualifiedName;

            var expectedEvents = new List<StoredEvent>
            {
                new StoredEvent(expectedEventName, event1.Metadata.OccorredOn, event1Serialiazed, batchId, 1),
                new StoredEvent(expectedEventName, event2.Metadata.OccorredOn, event2Serialiazed, batchId, 2)
            };

            var sut = Faker.Resolve<EventStore>();

            // act
            await sut
                .StoreEvents(streamId, 0L, new DomainEvent[] { event1, event2 })
                .ConfigureAwait(false);

            // assert
            A.CallTo(() => Faker.Resolve<IEventRepository>()
                .Add(
                    streamId,
                    A<ICollection<StoredEvent>>.That.Matches(evts => expectedEvents.All(evts.Contains))))
                .MustHaveHappened(Repeated.Exactly.Once);
        }

        [Test]
        public void StoreEvents_WillThrowException_WhenStreamExistsAndIsBroken()
        {
            // arrange
            var streamId = Guid.NewGuid();
            var currentStreamVersion = Fixture.Create<long>();

            var event1 = Fixture
                .Build<TestAggregateChanged>()
                .With(e => e.Metadata, new Metadata(streamId, 1L, typeof(TestAggregateChanged)))
                .Create();
            var event2 = Fixture
                .Build<TestAggregateChanged>()
                .With(e => e.Metadata, new Metadata(streamId, 2L, typeof(TestAggregateChanged)))
                .Create();

            var lastStreamEvent = new StoredEvent(
                typeof(TestAggregateCreated).AssemblyQualifiedName,
                Fixture.Create<DateTime>(),
                Fixture.Create<string>(),
                Guid.NewGuid(),
                currentStreamVersion + 1);

            A.CallTo(() => Faker.Resolve<IEventRepository>()
                .GetLast(streamId))
                .Returns(Task.FromResult(lastStreamEvent));

            Faker.Provide<Func<Guid>>(Guid.NewGuid);
            var sut = Faker.Resolve<EventStore>();

            Func<Task> exceptionThrower = async () => await sut
                .StoreEvents(streamId, currentStreamVersion, new DomainEvent[] { event1, event2 })
                .ConfigureAwait(false);

            // act & assert
            exceptionThrower
                .ShouldThrow<InvalidOperationException>()
                .And
                .Message
                .Should()
                .Contain($"Cant add new events on version {currentStreamVersion} as current storage version is {lastStreamEvent.EventId}");

            A.CallTo(() => Faker.Resolve<IEventRepository>()
                .Add(streamId, A<ICollection<StoredEvent>>._))
                .MustNotHaveHappened();
        }

        [Test]
        public async Task LoadAllEvents_WillLoadAndMapAllEvents_WhenThereAreStoredEvents()
        {
            // arrange
            var streamId = Guid.NewGuid();

            var event1Metadata = new Metadata(streamId, 1L, typeof(TestAggregateChanged));
            var event2Metadata = new Metadata(streamId, 2L, typeof(TestAggregateChanged));
            var typeName = typeof(TestAggregateChanged).AssemblyQualifiedName;

            var event1 = Fixture
                .Build<TestAggregateChanged>()
                .With(e => e.Metadata, event1Metadata)
                .Create();
            var event2 = Fixture
                .Build<TestAggregateChanged>()
                .With(e => e.Metadata, event2Metadata)
                .Create();

            var expectedEvents = new List<DomainEvent>
            {
                event1,
                event2
            };

            var event1Serialized = Fixture.Create<string>();
            var event2Serialized = Fixture.Create<string>();

            var batchId = Guid.NewGuid();
            var storedEvents = new List<StoredEvent>
            {
                new StoredEvent(typeName, event1Metadata.OccorredOn, event1Serialized, batchId, event1Metadata.Version),
                new StoredEvent(typeName, event2Metadata.OccorredOn, event2Serialized, batchId, event2Metadata.Version)
            };

            A.CallTo(() => Faker.Resolve<IEventRepository>()
                .CheckStream(streamId))
                .Returns(true);

            A.CallTo(() => Faker.Resolve<IEventRepository>()
                .GetAll(streamId))
                .Returns(storedEvents);

            A.CallTo(() => Faker.Resolve<IObjectSerializer>()
                .Deserialize(event1Serialized, typeof(TestAggregateChanged)))
                .Returns(event1);
            A.CallTo(() => Faker.Resolve<IObjectSerializer>()
                .Deserialize(event2Serialized, typeof(TestAggregateChanged)))
                .Returns(event2);

            Faker.Provide<Func<Guid>>(Guid.NewGuid);
            var sut = Faker.Resolve<EventStore>();

            // act
            var events = await sut
                .LoadAllEvents(streamId)
                .ConfigureAwait(false);

            // assert
            events
                .ShouldAllBeEquivalentTo(expectedEvents);
        }

        [Test]
        public async Task LoadAllEvents_WillReturnEmptyList_WhenThereAreNoStoredEvents()
        {
            // arrange
            var streamId = Guid.NewGuid();

            A.CallTo(() => Faker.Resolve<IEventRepository>()
                .CheckStream(streamId))
                .Returns(true);

            A.CallTo(() => Faker.Resolve<IEventRepository>()
                .GetAll(streamId))
                .Returns(new List<StoredEvent>());

            Faker.Provide<Func<Guid>>(Guid.NewGuid);
            var sut = Faker.Resolve<EventStore>();

            // act
            var events = await sut
                .LoadAllEvents(streamId)
                .ConfigureAwait(false);

            // assert
            events
                .Should()
                .BeEmpty();

            A.CallTo(() => Faker.Resolve<IObjectSerializer>()
                .Deserialize(A<string>._, A<Type>._))
                .MustNotHaveHappened();
        }

        [Test]
        public async Task LoadAllEvents_WillReturnEmptyList_WhenRepositoryReturnsNull()
        {
            // arrange
            var streamId = Guid.NewGuid();

            A.CallTo(() => Faker.Resolve<IEventRepository>()
                .CheckStream(streamId))
                .Returns(true);

            A.CallTo(() => Faker.Resolve<IEventRepository>()
                .GetAll(streamId))
                .Returns(null as List<StoredEvent>);

            Faker.Provide<Func<Guid>>(Guid.NewGuid);
            var sut = Faker.Resolve<EventStore>();

            // act
            var events = await sut
                .LoadAllEvents(streamId)
                .ConfigureAwait(false);

            // assert
            events
                .Should()
                .BeEmpty();

            A.CallTo(() => Faker.Resolve<IObjectSerializer>()
                .Deserialize(A<string>._, A<Type>._))
                .MustNotHaveHappened();
        }

        [Test]
        public async Task LoadAllEvents_WillReturnNull_WhenThereIsNoStream()
        {
            // arrange
            var streamId = Guid.NewGuid();

            A.CallTo(() => Faker.Resolve<IEventRepository>()
                .CheckStream(streamId))
                .Returns(false);

            Faker.Provide<Func<Guid>>(Guid.NewGuid);
            var sut = Faker.Resolve<EventStore>();

            // act
            var events = await sut
                .LoadAllEvents(streamId)
                .ConfigureAwait(false);

            // assert
            events
                .Should()
                .BeNull();
        }
    }
}
