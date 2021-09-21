using System.Collections.Generic;
using System.Threading.Tasks;
using DataAccess.Contexts;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Service.Interfaces;

namespace Service.Services
{
    public class CountryService : ICountryService
    {
        private readonly ILogger _logger;
        private readonly DefaultContext _dbContext;

        public CountryService(ILogger<CountryService> logger, DefaultContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;    
        }

        public async Task<Country> CreateAsync(Country entity)
        {
            await _dbContext.Country.AddAsync(entity);
            await _dbContext.SaveChangesAsync();
            return entity;
        }

        public Task DeleteAsync(int id)
        {
            throw new System.NotImplementedException();
        }

        public async Task<IEnumerable<Country>> GetAllAsync()
        {
            return await _dbContext.Country
                .AsNoTracking()
                .IgnoreQueryFilters()
                .ToListAsync();
        }

        public Task<Country> GetAsync(int id)
        {
            throw new System.NotImplementedException();
        }

        public async Task<Country> GetByCodeAsync(string code)
        {
            return await _dbContext.Country
                .AsNoTracking()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Code == code);
        }

        public Task UpdateAsync(Country entity)
        {
            throw new System.NotImplementedException();
        }
    }
}
