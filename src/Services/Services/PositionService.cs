using System.Collections.Generic;
using System.Threading.Tasks;
using DataAccess.Interfaces;
using Domain.Entities;
using Service.Interfaces;

namespace Service.Services
{
    public class PositionService : IPositionService
    {
        private IGenericRepository<Position> _repo;

        public PositionService(IGenericRepository<Position> repository)
        {
            _repo = repository;
        }

        public Task<IEnumerable<Position>> GetAll()
        {
            return _repo.GetAll();
        }

        public Task<Position> GetById(int id)
        {
            return _repo.GetById(id);
        }
    }
}
