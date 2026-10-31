using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LabTest.Data;
using LabTest.Models.DTOs;
using LabTest.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace LabTest.Services
{
    public class FlightServices
    {
        private readonly AppDbContext _context;
        private static string BuildFlightKey(string flightNumber, DateTime scheduledUtc, string origin, string destination)
            => $"{flightNumber.ToUpperInvariant()}|{scheduledUtc.ToUniversalTime():o}|{origin.ToUpperInvariant()}|{destination.ToUpperInvariant()}";

        public FlightServices(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<ResponseFlightModel>> GetFlightsAsync(int pageNumber = 1, int pageSize = 5)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 5;

            var query = _context.Flights
                .AsNoTracking() // read only
                .Where(f => f.DeletedAt == null);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(f => f.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new ResponseFlightModel
                {
                    Id = f.Id,
                    FlightNumber = f.FlightNumber,
                    ScheduledArrivalTimeUtc = f.ScheduledArrivalTimeUtc,
                    OriginAirport = f.OriginAirport,
                    DestinationAirport = f.DestinationAirport,
                    CreatedAt = f.CreatedAt,
                    LastActiveWO = _context.WorkOrders
                        .AsNoTracking()
                        .Where(w => w.FlightId == f.Id && w.DeletedAt == null && w.UpdatedAt == null)
                        .OrderByDescending(w => w.CreatedAt)
                        .Select(w => (DateTime?)w.CreatedAt)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return new PagedResult<ResponseFlightModel>
            {
                TotalCount = totalCount,
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
            var existingFlights = await _context.Flights
                .AsNoTracking()
                .Where(f => f.DeletedAt == null)
                .Select(f => new { f.FlightNumber, f.ScheduledArrivalTimeUtc, f.OriginAirport, f.DestinationAirport })
                .ToListAsync();

            var existingFlightNumbers = new HashSet<string>(
                existingFlights.Select(f => BuildFlightKey(f.FlightNumber, f.ScheduledArrivalTimeUtc, f.OriginAirport, f.DestinationAirport)),
                StringComparer.Ordinal
            );

            var inFileFlightNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: false);

            if (file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                // Read header and validate required columns (case-insensitive)
                string? headerLine = await reader.ReadLineAsync();
                int lineNumber = 1;
                if (string.IsNullOrWhiteSpace(headerLine))
                {
                    result.Errors.Add("Invalid CSV format. Missing header row.");
                    return result;
                }

                var headers = headerLine.Split(',').Select(h => h.Trim()).ToList();
                var map = headers
                    .Select((name, idx) => new { name, idx })
                    .ToDictionary(x => x.name, x => x.idx, StringComparer.OrdinalIgnoreCase);

                var requiredCols = new[] { "FlightNumber", "ScheduledArrivalTimeUtc", "OriginAirport", "DestinationAirport" };
                var missingCols = requiredCols.Where(c => !map.ContainsKey(c)).ToList();
                if (missingCols.Count > 0)
                {
                    result.Errors.Add($"Invalid CSV format. Required columns not found: {string.Join(", ", missingCols)}");
                    return result;
                }

                int iFN = map["FlightNumber"];
                int iSAT = map["ScheduledArrivalTimeUtc"];
                int iO = map["OriginAirport"];
                int iD = map["DestinationAirport"];

                while (!reader.EndOfStream)
                {
                    lineNumber++;
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var values = line.Split(',');
                    var maxIndex = Math.Max(Math.Max(iFN, iSAT), Math.Max(iO, iD));
                    if (values.Length <= maxIndex)
                    {
                        result.Errors.Add($"Line {lineNumber}: Not enough fields.");
                        continue;
                    }

                    var model = new AddFlightModel
                    {
                        FlightNumber = values[iFN]?.Trim(),
                        ScheduledArrivalTimeUtc = values[iSAT]?.Trim(),
                        OriginAirport = values[iO]?.Trim(),
                        DestinationAirport = values[iD]?.Trim()
                    };

                    if (!TryValidateAndMap(model, existingFlightNumbers, inFileFlightNumbers, now, out var flight, out var error))
                    {
                        result.Errors.Add($"Line {lineNumber}: {error}");
                        continue;
                    }

                    addedFlights.Add(flight!);
                    inFileFlightNumbers.Add(flight!.FlightNumber);
                }
            }
            else if (file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                var json = await reader.ReadToEndAsync();
                List<AddFlightModel>? items;
                try
                {
                    // Schema pre-check: ensure required keys appear in the uploaded JSON
                    using (var doc = JsonDocument.Parse(json))
                    {
                        if (doc.RootElement.ValueKind != JsonValueKind.Array)
                        {
                            result.Errors.Add("Invalid JSON format.");
                            return result;
                        }

                        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var el in doc.RootElement.EnumerateArray())
                        {
                            if (el.ValueKind != JsonValueKind.Object) continue;
                            foreach (var prop in el.EnumerateObject())
                            {
                                keys.Add(prop.Name);
                            }
                        }

                        var required = new[] { "flightNumber", "scheduledArrivalTimeUtc", "originAirport", "destinationAirport" };
                        var missing = required.Where(r => !keys.Contains(r)).ToList();
                        if (missing.Count > 0)
                        {
                            result.Errors.Add($"Invalid JSON format. Required keys not found: {string.Join(", ", missing)}");
                            return result;
                        }
                    }

                    items = JsonSerializer.Deserialize<List<AddFlightModel>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                catch (JsonException)
                {
                    result.Errors.Add("Invalid JSON format.");
                    return result;
                }

                if (items != null)
                {
                    int index = 0;
                    foreach (var model in items)
                    {
                        index++;
                        if (!TryValidateAndMap(model, existingFlightNumbers, inFileFlightNumbers, now, out var flight, out var error))
                        {
                            result.Errors.Add($"Item {index}: {error}");
                            continue;
                        }

                        addedFlights.Add(flight!);
                        inFileFlightNumbers.Add(flight!.FlightNumber);
                    }
                }
            }
            else
            {
                result.Errors.Add("Unsupported file format. Please upload a .csv or .json file.");
                return result;
            }

            if (addedFlights.Count == 0)
            {
                return result;
            }

            await _context.Flights.AddRangeAsync(addedFlights);
            await _context.SaveChangesAsync();

            result.Count = addedFlights.Count;
            return result;
        }

        private static readonly Regex AirportCodeRegex = new("^[A-Za-z]{3}$", RegexOptions.Compiled);

        private static bool TryValidateAndMap(
            AddFlightModel model,
            HashSet<string>? existingFlightNumbers,
            HashSet<string>? inFileFlightNumbers,
            DateTime nowUtc,
            out Flight? flight,
            out string error)
        {
            flight = null;
            error = string.Empty;

            var number = model.FlightNumber?.Trim();
            var scheduled = model.ScheduledArrivalTimeUtc?.Trim();
            var origin = model.OriginAirport?.Trim();
            var destination = model.DestinationAirport?.Trim();

            var missingFields = new List<string>(4);
            if (string.IsNullOrWhiteSpace(number)) missingFields.Add("FlightNumber");
            if (string.IsNullOrWhiteSpace(scheduled)) missingFields.Add("ScheduledArrivalTimeUtc");
            if (string.IsNullOrWhiteSpace(origin)) missingFields.Add("OriginAirport");
            if (string.IsNullOrWhiteSpace(destination)) missingFields.Add("DestinationAirport");
            if (missingFields.Count > 0)
            {
                error = $"Missing or empty fields: {string.Join(", ", missingFields)}.";
                return false;
            }

            // After earlier guard, all fields are non-null
            var numberValue = number!;
            var scheduledValue = scheduled!;
            var originValue = origin!;
            var destinationValue = destination!;

            if (numberValue.Length > 20)
            {
                error = "FlightNumber exceeds 20 characters.";
                return false;
            }

            if (!AirportCodeRegex.IsMatch(originValue) || !AirportCodeRegex.IsMatch(destinationValue))
            {
                error = "Airport codes must be 3 letters.";
                return false;
            }

            if (!DateTime.TryParse(scheduledValue, out var scheduledUtc))
            {
                error = "Invalid ScheduledArrivalUtc value.";
                return false;
            }

            // DB duplicate check only when DB has records; compare exact match across all fields
            var currentKey = BuildFlightKey(numberValue, scheduledUtc, originValue, destinationValue);
            if (existingFlightNumbers != null && existingFlightNumbers.Count > 0 && existingFlightNumbers.Contains(currentKey))
            {
                error = "Duplicate flight record (all fields match an existing one).";
                return false;
            }

            flight = new Flight
            {
                FlightNumber = numberValue,
                ScheduledArrivalTimeUtc = DateTime.SpecifyKind(scheduledUtc, DateTimeKind.Utc),
                OriginAirport = originValue.ToUpperInvariant(),
                DestinationAirport = destinationValue.ToUpperInvariant(),
                CreatedAt = nowUtc
            };

            return true;
        }
    }
}
