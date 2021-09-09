using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataAccess.Contexts;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Service.Interfaces;

namespace Service.Services
{
    public class CandidateService : ICandidateService
    {
        public readonly DefaultContext _dbContext;

        public CandidateService(DefaultContext dbContext)
        {
            _dbContext = dbContext;    
        }

        public async Task<IEnumerable<Candidate>> GetAllAsync(string email = null, string origin = null, string breezyId = null)
        {
            return await _dbContext.Candidate.Where(x => 
                (email == null || x.Email == email)
                && (origin == null || x.Origin == origin)
                && (breezyId == null || x.BreezyId == breezyId)).ToListAsync();
        }

        public async Task<Candidate> GetAsync(int id)
        {
            return await _dbContext.Candidate.FirstOrDefaultAsync(x => x.Id == id);
        }
    }
}
