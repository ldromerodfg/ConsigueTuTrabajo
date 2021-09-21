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
    public class CompanyService : ICompanyService
    {
        private readonly ILogger _logger;
        private readonly DefaultContext _dbContext;

        public CompanyService(ILogger<CompanyService> logger, DefaultContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;    
        }

        public async Task<Company> CreateAsync(Company entity)
        {
            await _dbContext.Company.AddAsync(entity);
            await _dbContext.SaveChangesAsync();
            return entity;
        }

        public Task DeleteAsync(int id)
        {
            throw new System.NotImplementedException();
        }

        public async Task<IEnumerable<Company>> GetAllAsync()
        {
            return await _dbContext.Company
                .AsNoTracking()
                .IgnoreQueryFilters()
                .ToListAsync();
        }

        public Task<Company> GetAsync(int id)
        {
            throw new System.NotImplementedException();
        }

        public Task UpdateAsync(Company entity)
        {
            throw new System.NotImplementedException();
        }
    }
}
