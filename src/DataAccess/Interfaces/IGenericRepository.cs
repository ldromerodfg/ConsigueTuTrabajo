using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Common;

namespace DataAccess.Interfaces
{
    public interface IGenericRepository<T> where T : BaseEntity
    {
        Task<IEnumerable<T>> GetAll();
        Task<T> GetById(int id);
    }
}
