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
    public class PositionService : IPositionService
    {
        private readonly ILogger _logger;
        private readonly DefaultContext _dbContext;

        public PositionService(ILogger<PositionService> logger, DefaultContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;    
        }

        public async Task<Position> CreateAsync(Position entity)
        {
            await _dbContext.Position.AddAsync(entity);
            await _dbContext.SaveChangesAsync();
            return entity;
        }

        public async Task CreateRangeAsync(IEnumerable<Position> entities)
        {
            await _dbContext.Position.AddRangeAsync(entities);
            await _dbContext.SaveChangesAsync();
        }

        public Task DeleteAsync(int id)
        {
            throw new System.NotImplementedException();
        }

        public async Task<IEnumerable<Position>> GetAllAsync(string state = null, string department = null, int? companyId = null)
        {
            return await _dbContext.Position.Where(x => 
                (state == null || x.State == state)
                && (department == null || x.Department == department)
                && (companyId == null || x.CompanyId == companyId))
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Include(x=>x.Company)
                .ToListAsync();
        }

        public async Task<Position> GetAsync(int id)
        {
            return await _dbContext.Position
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Include(x=>x.Company)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Position> GetByBreezyIdAsync(string breezyId)
        {
            return await _dbContext.Position
                .AsNoTracking()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.BreezyId == breezyId);
        }

        public async Task UpdateAsync(Position entity)
        {
            _dbContext.Position.Update(entity);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateRangeAsync(IEnumerable<Position> entities)
        {
            _dbContext.Position.UpdateRange(entities);
            await _dbContext.SaveChangesAsync();
        }
    }
}
