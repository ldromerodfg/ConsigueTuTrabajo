using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities;

namespace Service.Interfaces
{
    public interface IPositionService
    {
        Task<IEnumerable<Position>> GetAllAsync(string state = null, string department = null, int? companyId = null);
        Task<Position> GetAsync(int id);
    }
}
