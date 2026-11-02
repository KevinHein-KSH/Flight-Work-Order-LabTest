using System;
using LabTest.Data;
using LabTest.Models.DTOs;
using LabTest.Models.Entities;
using Microsoft.EntityFrameworkCore;
using LabTest.Repositories.IRepositories;

namespace LabTest.Repositories;

public class FlightRepository(AppDbContext context) : GenericRepository<Flight>(context), IFlightRepository
{
    public async Task<(List<ResponseFlightModel> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize)
    {
        var flights = await _context.Flights
            .AsNoTracking()
            .Where(f => f.DeletedAt == null)
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
                    .Where(wo => wo.FlightId == f.Id && wo.DeletedAt == null && wo.UpdatedAt == null)
                    .OrderByDescending(wo => wo.CreatedAt)
                    .Select(wo => (DateTime?)wo.CreatedAt)
                    .FirstOrDefault()
            })
            .ToListAsync();
        return (flights, await _context.Flights.AsNoTracking().CountAsync(f => f.DeletedAt == null));
    }

    public async Task<List<Flight>> GetAllExistingAsync()
    {
        var existingFlights = await _context.Flights
            .AsNoTracking()
            .Where(f => f.DeletedAt == null)
            .Select(f => new Flight
            {
                FlightNumber = f.FlightNumber,
                ScheduledArrivalTimeUtc = f.ScheduledArrivalTimeUtc,
                OriginAirport = f.OriginAirport,
                DestinationAirport = f.DestinationAirport,
                CreatedAt = f.CreatedAt
            }).ToListAsync();
        return existingFlights;
    }

    public async Task<Flight?> GetExistingByIdAsync(Guid id)
    {
        var flight = await _context.Flights
            .AsNoTracking()
            .Where(f => f.DeletedAt == null && f.Id == id)
            .FirstOrDefaultAsync();
        return flight;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        var exists = await _context.Flights
            .AsNoTracking()
            .AnyAsync(f => f.DeletedAt == null && f.Id == id);
        return exists;
    }

    public async Task AddRangeAsync(IEnumerable<Flight> flights)
    {
        await _context.Flights.AddRangeAsync(flights);
    }
}
