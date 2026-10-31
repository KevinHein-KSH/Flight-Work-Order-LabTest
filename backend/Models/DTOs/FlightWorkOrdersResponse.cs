using System;
using System.Collections.Generic;

namespace LabTest.Models.DTOs
{
    public class FlightWorkOrdersResponse
    {
        public Guid? Id { get; set; }
        public string? FlightNumber { get; set; }
        public DateTime ScheduledArrivalUtc { get; set; }
        public string? OriginAirport { get; set; }
        public string? DestinationAirport { get; set; }

        public PagedResult<WorkOrderPreviewModel>? WorkOrdersPreview { get; set; }
        public WorkOrderPreviewModel? ActiveWorkOrder { get; set; }
    }
}
