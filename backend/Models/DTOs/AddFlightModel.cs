using System;

namespace LabTest.Models.DTOs
{
    public class AddFlightModel
    {
        public string? FlightNumber { get; set; }
        public string? ScheduledArrivalTimeUtc { get; set; }
        public string? OriginAirport { get; set; }
        public string? DestinationAirport { get; set; }
    }
}

