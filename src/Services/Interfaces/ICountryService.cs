using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities;

namespace Service.Interfaces
{
    public interface ICountryService
    {
        Task<IEnumerable<Country>> GetAllAsync();
        Task<Country> GetAsync(int id);
        Task<Country> GetByCodeAsync(string code);
        Task<Country> CreateAsync(Country entity);
        Task UpdateAsync(Country entity);
        Task DeleteAsync(int id);
    }
}