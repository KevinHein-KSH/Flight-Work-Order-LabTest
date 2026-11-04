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
        private readonly WorkOrderService workOrderService;
        public WorkOrderController(WorkOrderService workOrderService)
        {
            this.workOrderService = workOrderService;
        }

        // GET api/workorder/by-flight/{flightId}?pageNumber=1&pageSize=5
        [HttpGet("flight/{flightId}")]
        public async Task<IActionResult> GetByFlightId(Guid flightId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 5)
        {
            var result = await workOrderService.GetByFlightIdAsync(flightId, pageNumber, pageSize);
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
            var (ok, error, created) = await workOrderService.AddWorkOrderAsync(flightId, request?.Raw);
            if (!ok)
            {
                return BadRequest(error);
            }
            return Ok(created);
        }
    }
}
