using System.Text;
using LabTest.Models.DTOs;
using LabTest.Models.Entities;
using LabTest.Repositories.IRepositories;
using LabTest.Services.FlightImport;

namespace LabTest.Services
{
    public class FlightService
    {
        private readonly IFlightRepository _flightRepository;

        public FlightService(IFlightRepository flightRepository)
        {
            _flightRepository = flightRepository;
        }

        public async Task<PagedResult<ResponseFlightModel>> GetFlightsAsync(int pageNumber = 1, int pageSize = 5)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 5;

            var (items, totalItems) = await _flightRepository.GetPagedAsync(pageNumber, pageSize);

            return new PagedResult<ResponseFlightModel>
            {
                TotalCount = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Items = items
            };
        }

        public async Task<BulkUploadResult> BulkUploadAsync(IFormFile? file)
        {
            var result = new BulkUploadResult();

            if (file == null || file.Length == 0)
            {
                result.Errors.Add("No file uploaded.");
                return result;
            }

            var now = DateTime.UtcNow;
            var addedFlights = new List<Flight>();

            // Build composite keys of existing flights to detect exact duplicates (all fields match)
            var existingFlights = await _flightRepository.GetAllExistingAsync();

            var existingFlightNumbers = new HashSet<string>(
                existingFlights.Select(f => FlightRecordValidator.BuildFlightKey(f.FlightNumber, f.ScheduledArrivalTimeUtc, f.OriginAirport, f.DestinationAirport)),
                StringComparer.Ordinal
            );

            var inFileFlightNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: false);

            var rows = file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) ? await FlightCsvReader.ReadAsync(reader)
                        : file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ? await FlightJsonReader.ReadAsync(reader)
                        : null;

            if (rows == null)
            {
                result.Errors.Add("Unsupported file format. Please upload a .csv or .json file.");
                return result;
            }

            foreach (var row in rows)
            {
                if (row.Error != null)
                {
                    result.Errors.Add(row.Label == "" ? row.Error : $"{row.Label}: {row.Error}");
                    continue;
                }

                if (!FlightRecordValidator.TryValidateAndMap(row.Model!, existingFlightNumbers, inFileFlightNumbers, now, out var flight, out var error))
                {
                    result.Errors.Add($"{row.Label}: {error}");
                    continue;
                }

                addedFlights.Add(flight!);
                inFileFlightNumbers.Add(flight!.FlightNumber);
            }


            if (addedFlights.Count == 0)
            {
                return result;
            }

            await _flightRepository.AddRangeAsync(addedFlights);
            await _flightRepository.SaveChangesAsync();

            result.Count = addedFlights.Count;
            return result;
        }


    }
}
