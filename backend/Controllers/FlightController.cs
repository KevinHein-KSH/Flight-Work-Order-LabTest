using LabTest.Models.DTOs;
using LabTest.Services;
using Microsoft.AspNetCore.Mvc;

namespace LabTest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FlightController : ControllerBase
    {
        private readonly FlightService flightService;
        public FlightController(FlightService flightService)
        {
            this.flightService = flightService;
        }
        [HttpGet]
        public async Task<IActionResult> GetAllFlights([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 5)
        {
            var flights = await flightService.GetFlightsAsync(pageNumber, pageSize);
            return Ok(flights);
        }

        [HttpPost("bulk-upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> BulkUploadFlights(IFormFile? file)
        {
            BulkUploadResult result = await flightService.BulkUploadAsync(file);
            if (!result.AnyInserted)
            {
                return BadRequest(result.Errors.Count > 0 ? string.Join("; ", result.Errors) : "No valid flight records found.");
            }
            return Ok(result);
        }
    }
}
