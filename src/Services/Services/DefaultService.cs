using System.Collections.Generic;
using System.Threading.Tasks;
using DataAccess.Interfaces;
using Domain.Entities;
using Service.Interfaces;

namespace Service.Services
{
    public class DefaultService : IDefaultService
    {
        private IGenericRepository<Default> _repo;

        public DefaultService(IGenericRepository<Default> repository)
        {
            _repo = repository;
        }

        public Task<IEnumerable<Default>> GetAll()
        {
            return _repo.GetAll();
        }

        public Task<Default> GetById(int id)
        {
            return _repo.GetById(id);
        }
    }
}
