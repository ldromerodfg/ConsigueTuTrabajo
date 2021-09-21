using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities;

namespace Service.Interfaces
{
    public interface ICityService
    {
        Task<IEnumerable<City>> GetAllAsync();
        Task<City> GetAsync(int id);
        Task<City> GetByNameAsync(string name);
        Task<City> CreateAsync(City entity);
        Task UpdateAsync(City entity);
        Task DeleteAsync(int id);
    }
}