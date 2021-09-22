using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataAccess.Contexts;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Service.Interfaces;

namespace Service.Services
{
    public class PositionTypeService : IPositionTypeService
    {
        private readonly ILogger _logger;
        private readonly DefaultContext _dbContext;

        public PositionTypeService(ILogger<PositionTypeService> logger, DefaultContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;    
        }

        public async Task<PositionType> CreateAsync(PositionType entity)
        {
            await _dbContext.PositionType.AddAsync(entity);
            await _dbContext.SaveChangesAsync();

            return entity;
        }

        public Task DeleteAsync(int id)
        {
            throw new System.NotImplementedException();
        }

        public Task<IEnumerable<PositionType>> GetAllAsync()
        {
            throw new System.NotImplementedException();
        }

        public Task<PositionType> GetAsync(int id)
        {
            throw new System.NotImplementedException();
        }

        public async Task<PositionType> GetByCodeAsync(string code)
        {
            return await _dbContext.PositionType
                .FirstOrDefaultAsync(x => x.Code == code);
        }

        public Task UpdateAsync(PositionType entity)
        {
            throw new System.NotImplementedException();
        }
    }
}
