using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities;

namespace Service.Interfaces
{
    public interface IStateService
    {
        Task<IEnumerable<State>> GetAllAsync();
        Task<State> GetAsync(int id);
        Task<State> GetByCodeAsync(string code);
        Task<State> CreateAsync(State entity);
        Task UpdateAsync(State entity);
        Task DeleteAsync(int id);
    }
}