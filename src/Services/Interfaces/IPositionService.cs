using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities;

namespace Service.Interfaces
{
    public interface IPositionService
    {
        Task<IEnumerable<Position>> GetAllAsync(string state = null, string department = null,
            int? companyId = null, int? cityId = null, int? positionTypeId = null, int? page_size = null, int? page = null);
        Task<Position> GetAsync(int id);
        Task<Position> GetByBreezyIdAsync(string breezyId);
        Task<Position> CreateAsync(Position entity);
        Task CreateRangeAsync(IEnumerable<Position> entities);
        Task UpdateAsync(Position entity);
        Task UpdateRangeAsync(IEnumerable<Position> entities);
        Task DeleteAsync(int id);
    }
}