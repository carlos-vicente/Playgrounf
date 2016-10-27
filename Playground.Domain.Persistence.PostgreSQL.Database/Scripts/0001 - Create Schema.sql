﻿CREATE TABLE public."EventStreams"
(
  "EventStreamId" uuid NOT NULL,
  "EventStreamName" text NOT NULL,
  "CreatedOn" timestamp without time zone NOT NULL,
  CONSTRAINT "EventStream_PK" PRIMARY KEY ("EventStreamId")
);


CREATE TABLE public."Events"
(
  "EventStreamId" uuid NOT NULL,
  "EventId" bigint NOT NULL,
  "TypeName" text NOT NULL,
  "OccurredOn" timestamp without time zone NOT NULL,
  "BatchId" uuid NOT NULL,
  "EventBody" json NOT NULL,
  CONSTRAINT "EventPK" PRIMARY KEY ("EventStreamId", "EventId"),
  CONSTRAINT "Events_EventStreams_FK" FOREIGN KEY ("EventStreamId")
      REFERENCES public."EventStreams" ("EventStreamId") MATCH SIMPLE
      ON UPDATE NO ACTION ON DELETE NO ACTION
);


CREATE TABLE public."Snaphots"
(
	"EventStreamId" uuid NOT NULL,
	"Version" bigint,
	"TakenOn" timestamp without time zone NOT NULL,
	"Data" json,
	CONSTRAINT "Snaphots_PK" PRIMARY KEY ("EventStreamId")
);


CREATE TYPE event AS (
	EventId bigint, 
	TypeName text, 
	OccurredOn timestamp without time zone,
	BatchId uuid,
	EventBody json
);