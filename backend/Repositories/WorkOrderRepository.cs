using System;
using LabTest.Data;
using LabTest.Models.DTOs;
using LabTest.Models.Entities;
using LabTest.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace LabTest.Repositories;

public class WorkOrderRepository(AppDbContext context) : GenericRepository<WorkOrder>(context), IWorkOrderRepository
{
    public async Task<WorkOrder?> GetActiveAsync(Guid flightId)
    {
        var activeWorkOrder = await _context.WorkOrders
            .AsNoTracking()
            .OrderByDescending(wo => wo.CreatedAt)
            .Where(wo => wo.FlightId == flightId && wo.DeletedAt == null && wo.UpdatedAt == null)
            .FirstOrDefaultAsync();
        return activeWorkOrder;
    }
    public async Task<int> CountAsync(Guid flightId)
    {
        return await _context.WorkOrders.AsNoTracking().CountAsync(wo => wo.FlightId == flightId && wo.DeletedAt == null);
    }
    public async Task<List<WorkOrder>> GetHistoryPagedAsync(Guid flightId, int pageNumber, int pageSize)
    {
        var baseQuery = _context.WorkOrders
                .AsNoTracking()
                .Where(w => w.DeletedAt == null && w.FlightId == flightId);
        var items = await baseQuery
                .OrderByDescending(w => w.CreatedAt)
                .Where(w => w.UpdatedAt != null)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        return items;
    }
    public async Task CloseActiveAsync(Guid flightId, DateTime now)
    {
        await _context.WorkOrders.Where(w => w.FlightId == flightId && w.DeletedAt == null && w.UpdatedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(w => w.UpdatedAt, _ => now));
    }
}
