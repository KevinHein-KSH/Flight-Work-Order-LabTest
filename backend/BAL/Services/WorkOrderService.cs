using System;
using LabTest.Models.DTOs;
using LabTest.Models.Entities;
using LabTest.Models.Enums;
using LabTest.Repositories.IRepositories;

namespace LabTest.Services
{
    // Template service mirroring FlightServices patterns for WorkOrders
    public class WorkOrderService
    {
        private readonly IFlightRepository _flightRepository;
        private readonly IWorkOrderRepository _workOrderRepository;

        public WorkOrderService(IFlightRepository flightRepository, IWorkOrderRepository workOrderRepository)
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
                        Parsed = WorkOrderCommandParser.ParseRaw(w.RawCommand),
                        CreatedAt = w.CreatedAt,
                        UpdatedAt = w.UpdatedAt
                    }).ToList()

                },
                ActiveWorkOrder = active == null ? null : new WorkOrderPreviewModel
                {
                    Id = active.Id,
                    Raw = active.RawCommand,
                    Parsed = WorkOrderCommandParser.ParseRaw(active.RawCommand),
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

            if (!WorkOrderCommandParser.TryParseAndValidate(raw!, out var parsed, out var validationError))
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
    }
}
