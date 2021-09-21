using System.Collections.Generic;
using System.Threading.Tasks;
using DataAccess.Contexts;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Service.Interfaces;

namespace Service.Services
{
    public class StateService : IStateService
    {
        private readonly ILogger _logger;
        private readonly DefaultContext _dbContext;

        public StateService(ILogger<StateService> logger, DefaultContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;    
        }

        public async Task<State> CreateAsync(State entity)
        {
            await _dbContext.State.AddAsync(entity);
            await _dbContext.SaveChangesAsync();

            return entity;
        }

        public Task DeleteAsync(int id)
        {
            throw new System.NotImplementedException();
        }

        public Task<IEnumerable<State>> GetAllAsync()
        {
            throw new System.NotImplementedException();
        }

        public Task<State> GetAsync(int id)
        {
            throw new System.NotImplementedException();
        }

        public async Task<State> GetByCodeAsync(string code)
        {
            return await _dbContext.State
                .AsNoTracking()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Code == code);
        }

        public Task UpdateAsync(State entity)
        {
            throw new System.NotImplementedException();
        }
    }
}
