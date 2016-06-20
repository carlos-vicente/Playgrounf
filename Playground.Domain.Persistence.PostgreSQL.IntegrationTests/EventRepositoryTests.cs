﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Npgsql;
using NUnit.Framework;
using Playground.Domain.Persistence.Events;
using Playground.Domain.Persistence.PostgreSQL.IntegrationTests.Postgresql;
using Playground.Tests;
using Ploeh.AutoFixture;

namespace Playground.Domain.Persistence.PostgreSQL.IntegrationTests
{
    public class EventRepositoryTests : SimpleTestBase
    {
        private EventRepository _sut;

        public override void SetUp()
        {
            base.SetUp();

            DatabaseHelper.CleanEvents();
            DatabaseHelper.CleanEventStreams();

            _sut = new EventRepository(DatabaseHelper.GetConnectionStringBuilder());
        }

        [Test]
        public async Task CreateStream_WillCreateStream_WhenStreamIdIsValid()
        {
            // arrange
            var streamId = Fixture.Create<Guid>();
            var streamName = Fixture.Create<string>();

            var expectedStream = new EventStream
            {
                EventStreamId = streamId,
                EventStreamName = streamName
            };

            // act
            await _sut
                .CreateStream(streamId, streamName)
                .ConfigureAwait(false);

            // assert
            var actualStream = await DatabaseHelper
                .GetLatestStreamCreated()
                .ConfigureAwait(false);

            actualStream
                .ShouldBeEquivalentTo(expectedStream);
        }

        [Test]
        public void CreateStream_WillThrowException_WhenStreamIdIsInvalid()
        {
            // arrange
            var streamId = Guid.Empty;
            var streamName = Fixture.Create<string>();

            Func<Task> exceptionThrower = async () => await _sut
                .CreateStream(streamId, streamName)
                .ConfigureAwait(false);

            // act/assert
            exceptionThrower
                .ShouldThrow<ArgumentException>()
                .And
                .ParamName
                .ShouldBeEquivalentTo("streamId");
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("    ")]
        public void CreateStream_WillThrowException_WhenStreamNameIsInvalid(string invalidStreamName)
        {
            // arrange
            var streamId = Guid.NewGuid();

            Func<Task> exceptionThrower = async () => await _sut
                .CreateStream(streamId, invalidStreamName)
                .ConfigureAwait(false);

            // act/assert
            exceptionThrower
                .ShouldThrow<ArgumentException>()
                .And
                .ParamName
                .ShouldBeEquivalentTo("streamName");
        }

        [Test]
        public async Task CreateStream_WillThrowException_WhenStreamIdAlreadyExists()
        {
            // arrange
            var streamId = Fixture.Create<Guid>();
            var streamName = Fixture.Create<string>();

            await DatabaseHelper
                .CreateEventStream(streamId, streamName)
                .ConfigureAwait(false);

            Func<Task> exceptionThrower = async () => await _sut
                .CreateStream(streamId, streamName)
                .ConfigureAwait(false);

            // act/assert
            exceptionThrower
                .ShouldThrow<PostgresException>();
        }

        [Test]
        public async Task CheckStream_WillReturnTrue_WhenStreamIdAlreadyExists()
        {
            // arrange
            var streamId = Fixture.Create<Guid>();
            var streamName = Fixture.Create<string>();

            await DatabaseHelper
                .CreateEventStream(streamId, streamName)
                .ConfigureAwait(false);

            // act
            var streamExists = await _sut
                .CheckStream(streamId)
                .ConfigureAwait(false);

            // assert
            streamExists.Should().BeTrue();
        }

        [Test]
        public async Task CheckStream_WillReturnFalse_WhenStreamIdDoesNotExist()
        {
            // arrange
            var streamId = Fixture.Create<Guid>();

            // act
            var streamExists = await _sut
                .CheckStream(streamId)
                .ConfigureAwait(false);

            // assert
            streamExists.Should().BeFalse();
        }

        [Test]
        public void CheckStream_WillThrowException_WhenStreamIdIsInvalid()
        {
            // arrange
            var streamId = Guid.Empty;

            Func<Task> exceptionThrower = async () => await _sut
                .CheckStream(streamId)
                .ConfigureAwait(false);

            // act/assert
            exceptionThrower
                .ShouldThrow<ArgumentException>();
        }

        [Test]
        public async Task GetAll_WillReturnAllEvents_WhenThereAreEventsForStream()
        {
            // arrange
            var streamId = Fixture.Create<Guid>();
            var streamName = Fixture.Create<string>();

            var now = GetDateTimeToMillisecond(DateTime.UtcNow); 

            await DatabaseHelper
                .CreateEventStream(streamId, streamName)
                .ConfigureAwait(false);

            var event1 = new StoredEvent("some type", now, "{\"prop\":\"value\"}", 1L);
            var event2 = new StoredEvent("some type", now.AddSeconds(1), "{}", 2L);

            await DatabaseHelper
                .CreateEvent(streamId, event1)
                .ConfigureAwait(false);
            await DatabaseHelper
                .CreateEvent(streamId, event2)
                .ConfigureAwait(false);

            var expectedEvents = new[]
            {
                event1,
                event2
            };

            // act
            var events = await _sut
                .GetAll(streamId)
                .ConfigureAwait(false);

            // assert
            events
                .Should()
                .ContainInOrder(expectedEvents);
        }

        [Test]
        public async Task GetAll_WillReturnEmptyList_WhenThereAreNoEventsForStream()
        {
            // arrange
            var streamId = Fixture.Create<Guid>();
            var streamName = Fixture.Create<string>();

            await DatabaseHelper
                .CreateEventStream(streamId, streamName)
                .ConfigureAwait(false);

            // act
            var events = await _sut
                .GetAll(streamId)
                .ConfigureAwait(false);

            // assert
            events.Should().BeEmpty();
        }

        [Test]
        public async Task GetAll_WillReturnEmptyList_WhenStreamDoesNotExist()
        {
            // arrange
            var streamId = Fixture.Create<Guid>();

            // act
            var events = await _sut
                .GetAll(streamId)
                .ConfigureAwait(false);

            // assert
            events.Should().BeEmpty();
        }

        [Test]
        public void GetAll_WillThrowException_WhenStreamIdIsInvalid()
        {
            // arrange
            var streamId = Guid.Empty;

            Func<Task> exceptionThrower = async () => await _sut
                .GetAll(streamId)
                .ConfigureAwait(false);

            // act/assert
            exceptionThrower
                .ShouldThrow<ArgumentException>();
        }

        [Test]
        public async Task GetLast_WillReturnLastEvent_WhenThereAreMultipleEvents()
        {
            // arrange
            var streamId = Fixture.Create<Guid>();
            var streamName = Fixture.Create<string>();

            await DatabaseHelper
                .CreateEventStream(streamId, streamName)
                .ConfigureAwait(false);

            var now = GetDateTimeToMillisecond(DateTime.UtcNow);

            var event1 = new StoredEvent("some type", now, "{\"prop\":\"value\"}", 1L);
            var event2 = new StoredEvent("some type", now, "{}", 2L);

            await DatabaseHelper
                .CreateEvent(streamId, event1)
                .ConfigureAwait(false);
            await DatabaseHelper
                .CreateEvent(streamId, event2)
                .ConfigureAwait(false);

            // act
            var lastEvent = await _sut
                .GetLast(streamId)
                .ConfigureAwait(false);

            // assert
            lastEvent.ShouldBeEquivalentTo(event2);
        }

        [Test]
        public async Task GetLast_WillReturnNull_WhenThereAreNoEvents()
        {
            // arrange
            var streamId = Fixture.Create<Guid>();
            var streamName = Fixture.Create<string>();

            await DatabaseHelper
                .CreateEventStream(streamId, streamName)
                .ConfigureAwait(false);

            // act
            var lastEvent = await _sut
                .GetLast(streamId)
                .ConfigureAwait(false);

            // assert
            lastEvent.Should().BeNull();
        }

        [Test]
        public void GetLast_WillThrowException_WhenStreamIdIsInvalid()
        {
            // arrange
            var streamId = Guid.Empty;

            Func<Task> exceptionThrower = async () => await _sut
                .GetLast(streamId)
                .ConfigureAwait(false);

            // act/assert
            exceptionThrower
                .ShouldThrow<ArgumentException>();
        }

        [Test]
        public async Task Add_WillAddEvents_ToEmptyStream()
        {
            // arrange
            var streamId = Fixture.Create<Guid>();
            var streamName = Fixture.Create<string>();

            await DatabaseHelper
                .CreateEventStream(streamId, streamName)
                .ConfigureAwait(false);

            var now = GetDateTimeToMillisecond(DateTime.UtcNow);

            var evt1 = new StoredEvent("some type", now, "{\"prop\":\"value\"}", 1L);
            var evt2 = new StoredEvent("some type2", now, "{\"prop\":\"value1\"}", 3L);
            var evt3 = new StoredEvent("some type", now.AddMinutes(5), "{}", 4L);

            var events = new[] {evt1, evt2, evt3};

            // act
            await _sut
                .Add(streamId, events)
                .ConfigureAwait(false);

            // assert
            var actualEvents = await DatabaseHelper
                .GetStreamEvents(streamId)
                .ConfigureAwait(false);

            actualEvents
                .Should()
                .ContainInOrder(events);
        }

        [Test]
        public async Task Add_WillAddEvents_ToStreamWithEvents()
        {
            // arrange
            var streamId = Fixture.Create<Guid>();
            var streamName = Fixture.Create<string>();

            await DatabaseHelper
                .CreateEventStream(streamId, streamName)
                .ConfigureAwait(false);

            var now = GetDateTimeToMillisecond(DateTime.UtcNow);

            var evt1 = new StoredEvent("some type", now, "{\"prop\":\"value\"}", 1L);
            var evt2 = new StoredEvent("some type2", now, "{\"prop\":\"value1\"}", 2L);

            await DatabaseHelper
                .CreateEvent(streamId, evt1)
                .ConfigureAwait(false);
            await DatabaseHelper
                .CreateEvent(streamId, evt2)
                .ConfigureAwait(false);

            var evtToAdd1 = new StoredEvent("some type", now.AddMinutes(2), "{\"prop\":\"value\"}", 5L);
            var evtToAdd2 = new StoredEvent("some type2", now.AddMinutes(2), "{\"prop\":\"value1\"}", 6L);
            var evtToAdd3 = new StoredEvent("some type", now.AddMinutes(5), "{}", 7L);

            var eventsToAdd = new[] { evtToAdd1, evtToAdd2, evtToAdd3 };
            var events = new[] {evt1, evt2, evtToAdd1, evtToAdd2, evtToAdd3};

            // act
            await _sut
                .Add(streamId, eventsToAdd)
                .ConfigureAwait(false);

            // assert
            var actualEvents = await DatabaseHelper
                .GetStreamEvents(streamId)
                .ConfigureAwait(false);

            actualEvents
                .Should()
                .ContainInOrder(events);
        }

        [Test]
        public void Add_WillThrowException_WhenStreamDoesNotExist()
        {
            // arrange
            var streamId = Fixture.Create<Guid>();

            var now = GetDateTimeToMillisecond(DateTime.UtcNow);

            var evt1 = new StoredEvent("some type", now, "{\"prop\":\"value\"}", 1L);
            var evt2 = new StoredEvent("some type2", now, "{\"prop\":\"value1\"}", 3L);
            
            var events = new[] { evt1, evt2 };

            Func<Task> exceptionThrower = async () => await _sut
                .Add(streamId, events)
                .ConfigureAwait(false);

            // act/assert
            exceptionThrower
                .ShouldThrow<PostgresException>();
        }

        [Test]
        public void Add_WillThrowException_WhenStreamIdIsInvalid()
        {
            // arrange
            var streamId = Guid.Empty;

            Func<Task> exceptionThrower = async () => await _sut
                .Add(streamId, new List<StoredEvent>())
                .ConfigureAwait(false);

            // act/assert
            exceptionThrower
                .ShouldThrow<ArgumentException>()
                .And
                .ParamName
                .ShouldBeEquivalentTo("streamId");
        }

        [Test]
        public void Add_WillThrowException_WhenEventsListIsEmpty()
        {
            // arrange
            var streamId = Guid.NewGuid();

            Func<Task> exceptionThrower = async () => await _sut
                .Add(streamId, new List<StoredEvent>())
                .ConfigureAwait(false);

            // act/assert
            exceptionThrower
                .ShouldThrow<ArgumentException>()
                .And
                .ParamName
                .ShouldBeEquivalentTo("events");
        }

        [Test]
        public void Add_WillThrowException_WhenEventsListIsNull()
        {
            // arrange
            var streamId = Guid.NewGuid();

            Func<Task> exceptionThrower = async () => await _sut
                .Add(streamId, new List<StoredEvent>())
                .ConfigureAwait(false);

            // act/assert
            exceptionThrower
                .ShouldThrow<ArgumentException>()
                .And
                .ParamName
                .ShouldBeEquivalentTo("events");
        }

        private static DateTime GetDateTimeToMillisecond(DateTime current)
        {
            return new DateTime(
                current.Year,
                current.Month,
                current.Day,
                current.Hour,
                current.Minute,
                current.Second,
                current.Millisecond);
        }
    }
}
