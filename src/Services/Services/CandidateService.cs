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
    public class CandidateService : ICandidateService
    {
        public readonly ILogger _logger;
        public readonly DefaultContext _dbContext;

        public CandidateService(ILogger<CandidateService> logger, DefaultContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;    
        }

        public async Task<Candidate> CreateAsync(Candidate entity)
        {
            await _dbContext.Candidate.AddAsync(entity);
            await _dbContext.SaveChangesAsync();
            return entity;
        }

        public Task DeleteAsync(int id)
        {
            throw new System.NotImplementedException();
        }

        public async Task<IEnumerable<Candidate>> GetAllAsync(string email = null, string origin = null, string breezyId = null)
        {
            return await _dbContext.Candidate
                .Include(x => x.Stage)
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(x => 
                    (email == null || x.Email == email)
                    && (origin == null || x.Origin == origin)
                    && (breezyId == null || x.BreezyId == breezyId))
                .ToListAsync();
        }

        public async Task<Candidate> GetAsync(int id)
        {
            return await _dbContext.Candidate.FirstOrDefaultAsync(x => x.Id == id);
        }

        public Task UpdateAsync(Candidate entity)
        {
            throw new System.NotImplementedException();
        }
    }
}
