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
    public class CityService : ICityService
    {
        private readonly ILogger _logger;
        private readonly DefaultContext _dbContext;

        public CityService(ILogger<CityService> logger, DefaultContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;    
        }

        public async Task<City> CreateAsync(City entity)
        {
            await _dbContext.City.AddAsync(entity);
            await _dbContext.SaveChangesAsync();

            return entity;
        }

        public Task DeleteAsync(int id)
        {
            throw new System.NotImplementedException();
        }

        public Task<IEnumerable<City>> GetAllAsync()
        {
            throw new System.NotImplementedException();
        }

        public Task<City> GetAsync(int id)
        {
            throw new System.NotImplementedException();
        }

        public async Task<City> GetByNameAsync(string name)
        {
            return await _dbContext.City
                .AsNoTracking()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Name == name);
        }

        public Task UpdateAsync(City entity)
        {
            throw new System.NotImplementedException();
        }
    }
}
