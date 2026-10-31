using System;

namespace LabTest.Models.DTOs
{
    public class ResponseFlightModel
    {
        public Guid? Id { get; set; }
        public string? FlightNumber { get; set; }
        public DateTime ScheduledArrivalTimeUtc { get; set; }
        public string? OriginAirport { get; set; }
        public string? DestinationAirport { get; set; }
        public DateTime? CreatedAt { get; set; }

        public DateTime? LastActiveWO { get; set; }
    }
}

