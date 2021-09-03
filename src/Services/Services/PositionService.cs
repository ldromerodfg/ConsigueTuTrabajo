using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataAccess.Contexts;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Service.Interfaces;

namespace Service.Services
{
    public class PositionService : IPositionService
    {
        public readonly DefaultContext _dbContext;

        public PositionService(DefaultContext dbContext)
        {
            _dbContext = dbContext;    
        }

        public async Task<IEnumerable<Position>> GetAllAsync(string state = null, string department = null, int? companyId = null)
        {
            return await _dbContext.Position.Where(x => 
                (state == null || x.State == state)
                && (department == null || x.Department == department)
                && (companyId == null || x.CompanyId == companyId)).ToListAsync();
        }

        public async Task<Position> GetAsync(int id)
        {
            return await _dbContext.Position.FirstOrDefaultAsync(x => x.Id == id);
        }
    }
}
