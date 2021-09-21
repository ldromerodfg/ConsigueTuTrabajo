using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities;

namespace Service.Interfaces
{
    public interface IPositionTypeService
    {
        Task<IEnumerable<PositionType>> GetAllAsync();
        Task<PositionType> GetAsync(int id);
        Task<PositionType> GetByCodeAsync(string id);
        Task<PositionType> CreateAsync(PositionType entity);
        Task UpdateAsync(PositionType entity);
        Task DeleteAsync(int id);
    }
}