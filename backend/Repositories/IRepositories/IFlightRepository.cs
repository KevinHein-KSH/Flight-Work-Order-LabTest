using System;
using LabTest.Models.DTOs;
using LabTest.Models.Entities;

namespace LabTest.Repositories.IRepositories;

public interface IFlightRepository : IGenericRepository<Flight>
{
    Task<(List<ResponseFlightModel> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize);
    Task<List<Flight>> GetAllExistingAsync();
    Task<Flight?> GetExistingByIdAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task AddRangeAsync(IEnumerable<Flight> flights);
}
