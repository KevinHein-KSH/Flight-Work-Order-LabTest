using System;
using LabTest.Models.DTOs;
using LabTest.Models.Entities;
using LabTest.Models.Enums;
using LabTest.Repositories.IRepositories;

namespace LabTest.Services
{
    // Template service mirroring FlightServices patterns for WorkOrders
    public class WorkOrderServices
    {
        private readonly IFlightRepository _flightRepository;
        private readonly IWorkOrderRepository _workOrderRepository;

        public WorkOrderServices(IFlightRepository flightRepository, IWorkOrderRepository workOrderRepository)
        {
            _flightRepository = flightRepository;
            _workOrderRepository = workOrderRepository;
        }

        // Get a flight with its work orders preview and the active work order
        public async Task<FlightWorkOrdersResponse?> GetByFlightIdAsync(Guid flightId, int pageNumber = 1, int pageSize = 5)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 5;

            var flight = await _flightRepository.GetExistingByIdAsync(flightId);

            if (flight == null)
            {
                return null;
            }

            var active = await _workOrderRepository.GetActiveAsync(flightId);

            var totalCount = await _workOrderRepository.CountAsync(flightId);

            var items = await _workOrderRepository.GetHistoryPagedAsync(flightId, pageNumber, pageSize);

            var response = new FlightWorkOrdersResponse
            {
                Id = flight.Id,
                FlightNumber = flight.FlightNumber,
                ScheduledArrivalUtc = flight.ScheduledArrivalTimeUtc,
                OriginAirport = flight.OriginAirport,
                DestinationAirport = flight.DestinationAirport,
                WorkOrdersPreview = new PagedResult<WorkOrderPreviewModel>
                {
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    Items = items.Select(w => new WorkOrderPreviewModel
                    {
                        Id = w.Id,
                        Raw = w.RawCommand,
                        Parsed = ParseRaw(w.RawCommand),
                        CreatedAt = w.CreatedAt,
                        UpdatedAt = w.UpdatedAt
                    }).ToList()

                },
                ActiveWorkOrder = active == null ? null : new WorkOrderPreviewModel
                {
                    Id = active.Id,
                    Raw = active.RawCommand,
                    Parsed = ParseRaw(active.RawCommand),
                    CreatedAt = active.CreatedAt,
                    UpdatedAt = active.UpdatedAt
                }
            };

            return response;
        }

        public async Task<(bool ok, string? error, WorkOrderPreviewModel? created)> AddWorkOrderAsync(Guid flightId, string? raw)
        {
            if (flightId == Guid.Empty)
            {
                return (false, "Invalid flight id.", null);
            }
            if (string.IsNullOrWhiteSpace(raw))
            {
                return (false, "Raw command is required.", null);
            }

            var flightExists = await _flightRepository.ExistsAsync(flightId);

            if (!flightExists)
            {
                return (false, "Flight not found.", null);
            }

            if (!TryParseAndValidate(raw!, out var parsed, out var validationError))
            {
                return (false, validationError, null);
            }

            var now = DateTime.UtcNow;
            await _workOrderRepository.CloseActiveAsync(flightId, now);

            var activeWorkOrder = new WorkOrder
            {
                Id = Guid.NewGuid(),
                FlightId = flightId,
                Flight = null!,
                RawCommand = raw!.Trim(),
                CheckInMinutes = parsed.Chk,
                BaggageMinutes = parsed.Bag,
                CleaningMinutes = parsed.Clean,
                PbbAngle = parsed.Pbb.HasValue ? (PbbAngle?)parsed.Pbb.Value : null,
                CreatedAt = now,
                UpdatedAt = null,
            };

            await _workOrderRepository.AddAsync(activeWorkOrder);
            await _workOrderRepository.SaveChangesAsync();

            var created = new WorkOrderPreviewModel
            {
                Id = activeWorkOrder.Id,
                Raw = activeWorkOrder.RawCommand,
                Parsed = parsed,
                CreatedAt = activeWorkOrder.CreatedAt,
                UpdatedAt = activeWorkOrder.UpdatedAt
            };

            return (true, null, created);
        }
        private static IEnumerable<(string key, string numPart)> Tokens(string rawCmd)
        {
            if (string.IsNullOrWhiteSpace(rawCmd)) yield break;

            var parts = rawCmd.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var part in parts)
            {
                var p = part.Trim();
                var keyPart = new string(p.TakeWhile(char.IsLetter).ToArray()).ToUpperInvariant();
                var numPart = new string(p.SkipWhile(char.IsLetter).ToArray());
                if (!string.IsNullOrWhiteSpace(keyPart))
                    yield return (keyPart, numPart);
            }
        }

        private static ParsedWorkOrderModel ParseRaw(string raw)
        {
            var result = new ParsedWorkOrderModel();
            foreach (var (key, numPart) in Tokens(raw))
            {
                if (!int.TryParse(numPart, out var value)) continue;
                switch (key)
                {
                    case "CHK": result.Chk = value; break;
                    case "BAG": result.Bag = value; break;
                    case "CLEAN": result.Clean = value; break;
                    case "PBB": result.Pbb = value; break;
                }
            }
            return result;
        }

        // Strict writes (your validator), reusing Tokens
        private static bool TryParseAndValidate(string raw, out ParsedWorkOrderModel parsed, out string error)
        {
            parsed = new ParsedWorkOrderModel();
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(raw))
            {
                error = "Raw command is empty.";
                return false;
            }

            int? chk = null, bag = null, clean = null, pbb = null;
            var validKeyExit = false;

            foreach (var (key, numPart) in Tokens(raw))
            {
                var validKey = key is "CHK" or "BAG" or "CLEAN" or "PBB";
                if (validKey) validKeyExit = true;

                if (!int.TryParse(numPart, out var value))
                {
                    if (validKey)
                    {
                        error = $"Invalid numeric value for '{key}'.";
                        return false;
                    }
                    continue; // unknown junk
                }

                if (value < 0)
                {
                    error = $"Negative value is not allowed for '{key}'.";
                    return false;
                }

                switch (key)
                {
                    case "CHK": chk = value; break;
                    case "BAG": bag = value; break;
                    case "CLEAN": clean = value; break;
                    case "PBB":
                        if (value is not (0 or 90 or 180 or 270))
                        {
                            error = "PBB must be one of 0, 90, 180, 270.";
                            return false;
                        }
                        pbb = value; break;
                    default:
                        // ignore unknown keys
                        break;
                }
            }

            if (!validKeyExit)
            {
                error = "No valid commands found in raw string.";
                return false;
            }

            parsed = new ParsedWorkOrderModel { Chk = chk, Bag = bag, Clean = clean, Pbb = pbb };
            return true;
        }

    }
}
