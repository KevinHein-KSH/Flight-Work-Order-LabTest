using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LabTest.Models.Enums;

namespace LabTest.Models.Entities
{
    public class WorkOrder
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public required Guid FlightId { get; set; }

        [ForeignKey("FlightId")]
        public required Flight Flight { get; set; }

        [Required]
        public required string RawCommand { get; set; }

        public int? CheckInMinutes { get; set; }

        public int? BaggageMinutes { get; set; }

        public int? CleaningMinutes { get; set; }

        /// <summary>
        /// Possible values: 0, 90, 180, 270
        /// </summary>
        public PbbAngle? PbbAngle { get; set; }

        public DateTime? UpdatedAt { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        public DateTime? DeletedAt { get; set; }
    }
}