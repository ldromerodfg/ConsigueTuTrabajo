using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities;

namespace Service.Interfaces
{
    public interface ICompanyService
    {
        Task<IEnumerable<Company>> GetAllAsync();
        Task<Company> GetAsync(int id);
        Task<Company> CreateAsync(Company entity);
        Task UpdateAsync(Company entity);
        Task DeleteAsync(int id);
    }
}