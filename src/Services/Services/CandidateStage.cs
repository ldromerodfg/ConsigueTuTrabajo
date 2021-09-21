using System.Collections.Generic;
using System.Threading.Tasks;
using DataAccess.Contexts;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Service.Interfaces;

namespace Service.Services
{
    public class CandidateStageService : ICandidateStageService
    {
        private readonly ILogger _logger;
        private readonly DefaultContext _dbContext;

        public CandidateStageService(ILogger<CandidateStageService> logger, DefaultContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;    
        }

        public async Task<CandidateStage> CreateAsync(CandidateStage entity)
        {
            await _dbContext.CandidateStage.AddAsync(entity);
            await _dbContext.SaveChangesAsync();
            return entity;
        }

        public Task DeleteAsync(int id)
        {
            throw new System.NotImplementedException();
        }

        public Task<IEnumerable<CandidateStage>> GetAllAsync()
        {
            throw new System.NotImplementedException();
        }

        public Task<CandidateStage> GetAsync(int id)
        {
            throw new System.NotImplementedException();
        }

        public async Task<CandidateStage> GetByBreezyIdAsync(string breezyId)
        {
            return await _dbContext.CandidateStage
                .AsNoTracking()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.BreezyId == breezyId);
        }

        public Task UpdateAsync(CandidateStage entity)
        {
            throw new System.NotImplementedException();
        }
    }
}
