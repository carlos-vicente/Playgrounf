﻿CREATE OR REPLACE FUNCTION get_events_for_stream(streamId UUID)
RETURNS TABLE ("EventId" bigint, "TypeName" text, "OccurredOn" timestamp without time zone, "BatchId" uuid, "EventBody" json) AS $$
BEGIN
	RETURN QUERY SELECT E."EventId", E."TypeName", E."OccurredOn", E."BatchId", E."EventBody"
		FROM public."Events" as E
		WHERE E."EventStreamId" = streamId;
END;
$$ LANGUAGE plpgsql;



CREATE OR REPLACE FUNCTION public.create_event_stream(streamid uuid, streamName text, createdOn timestamp)
  RETURNS void AS $$
BEGIN
	IF(SELECT COUNT(*) 
		FROM public."EventStreams"
		WHERE "EventStreamId" = streamId) > 0 THEN
		RAISE EXCEPTION 'Cannot create an event stream for (%) because it already exists', streamId;
	END IF;

	INSERT INTO public."EventStreams" ("EventStreamId", "EventStreamName", "CreatedOn")
	values (streamId, streamName, createdOn);
END;
$$ LANGUAGE plpgsql;



CREATE OR REPLACE FUNCTION check_stream_exists(streamId UUID)
RETURNS BOOLEAN AS $$
BEGIN
	RETURN (SELECT COUNT(*) 
		FROM public."EventStreams"
		WHERE "EventStreamId" = streamId) > 0;
END;
$$ LANGUAGE plpgsql;


CREATE OR REPLACE FUNCTION get_last_event_for_stream(streamId UUID)
RETURNS TABLE ("EventId" bigint, "TypeName" text, "OccurredOn" timestamp without time zone, "BatchId" uuid, "EventBody" json) AS $$
BEGIN
	RETURN QUERY SELECT E."EventId", E."TypeName", E."OccurredOn", E."BatchId", E."EventBody"
		FROM public."Events" as E
		WHERE E."EventStreamId" = streamId
		ORDER BY E."OccurredOn" DESC, E."EventId" DESC
		LIMIT 1;
END;
$$ LANGUAGE plpgsql;


CREATE OR REPLACE FUNCTION save_events_in_stream(events event[], streamId uuid)
RETURNS void AS $$
DECLARE size int;
BEGIN
	IF(SELECT COUNT(*) 
		FROM public."EventStreams"
		WHERE "EventStreamId" = streamId) = 0 THEN
		RAISE EXCEPTION 'Stream (%) does not exist', streamId;
	END IF;
	
	size := array_length(events, 1);

	FOR index IN 1..size LOOP
		INSERT INTO public."Events" ("EventStreamId", "EventId", "TypeName", "OccurredOn", "BatchId", "EventBody")
		VALUES (streamId, events[index].EventId, events[index].TypeName, events[index].OccurredOn, events[index].BatchId, events[index].EventBody);
	END LOOP;
END;
$$ LANGUAGE plpgsql;



