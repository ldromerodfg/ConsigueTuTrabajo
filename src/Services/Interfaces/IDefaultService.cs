using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities;

namespace Service.Interfaces
{
    public interface IDefaultService
    {
        Task<IEnumerable<Default>> GetAll();
        Task<Default> GetById(int id);
    }
}
