﻿using System;

namespace Playground.Domain.Persistence.PostgreSQL.Queries
{
    internal class GetAllEventsQuery
    {
        public Guid streamId { get; set; }
    }
}
