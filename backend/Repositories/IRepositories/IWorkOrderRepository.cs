using System;
using LabTest.Models.DTOs;
using LabTest.Models.Entities;

namespace LabTest.Repositories.IRepositories;

public interface IWorkOrderRepository : IGenericRepository<WorkOrder>
{
    Task<WorkOrder?> GetActiveAsync(Guid flightId);
    Task<int> CountAsync(Guid flightId);
    Task<List<WorkOrder>> GetHistoryPagedAsync(Guid flightId, int pageNumber, int pageSize);
    Task CloseActiveAsync(Guid flightId, DateTime now);
}
