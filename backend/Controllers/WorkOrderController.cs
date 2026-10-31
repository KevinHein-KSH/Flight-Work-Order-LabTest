using System;
using System.Threading.Tasks;
using LabTest.Models.DTOs;
using LabTest.Services;
using Microsoft.AspNetCore.Mvc;

namespace LabTest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WorkOrderController : ControllerBase
    {
        private readonly WorkOrderServices workOrderServices;
        public WorkOrderController(WorkOrderServices workOrderServices)
        {
            this.workOrderServices = workOrderServices;
        }

        // GET api/workorder/by-flight/{flightId}?pageNumber=1&pageSize=5
        [HttpGet("flight/{flightId}")]
        public async Task<IActionResult> GetByFlightId(Guid flightId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 5)
        {
            var result = await workOrderServices.GetByFlightIdAsync(flightId, pageNumber, pageSize);
            if (result == null)
            {
                return NotFound();
            }
            return Ok(result);
        }

        // POST api/workorder/{flightId}
        [HttpPost("{flightId}")]
        public async Task<IActionResult> AddByFlightId(Guid flightId, [FromBody] AddWorkOrderRequest request)
        {
            var (ok, error, created) = await workOrderServices.AddWorkOrderAsync(flightId, request?.Raw);
            if (!ok)
            {
                return BadRequest(error);
            }
            return Ok(created);
        }
    }
}
