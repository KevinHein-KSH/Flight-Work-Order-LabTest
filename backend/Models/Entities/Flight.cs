using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabTest.Models.Entities
{
    public class Flight
    {
        [Key]
        public Guid Id { get; set; }

        [MaxLength(20)]
        public required string FlightNumber { get; set; }

        public required DateTime ScheduledArrivalTimeUtc { get; set; }

        [StringLength(3)]
        public required string OriginAirport { get; set; }

        [StringLength(3)]
        public required string DestinationAirport { get; set; }

        public DateTime? DeletedAt { get; set; }

        public required DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}