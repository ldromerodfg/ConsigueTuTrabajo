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

        public async Task<IEnumerable<Position>> GetAllAsync(string state = null, string department = null,
            int? companyId = null, int? cityId = null, int? positionTypeId = null, string search = null,
            int? page_size = null, int? page = null)
        {
            if (page != null && page_size != null)
            {
                var toSkip = page_size.Value * page;

                if (search != null)
                {
                    search = search.ToLower();
                }

                return await _dbContext.Position.Where(x =>
                    (state == null || x.State == state)
                    && (department == null || x.Department == department)
                    && (companyId == null || x.CompanyId == companyId)
                    && (cityId == null || x.CityId == cityId)
                    && (positionTypeId == null || (x.Type != null && x.Type.Id == positionTypeId))
                    && (search == null ||
                        (
                            x.Name.ToLower().Contains(search)
                            || x.Company.Name.ToLower().Contains(search)
                            || (x.City != null && x.City.Name.ToLower().Contains(search)
                            || (x.City != null && x.State != null
                                && x.City.State.Name.ToLower().Contains(search))
                            || (x.Type != null && x.Type.Name.ToLower().Contains(search)))
                        )))
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .Include(x => x.Company)
                    .Include(x => x.City)
                        .ThenInclude(x => x.State)
                            .ThenInclude(x => x.Country)
                    .Include(x => x.Type)
                    .Select(x => new Position
                    {
                        Id = x.Id,
                        BreezyId = x.BreezyId,
                        Name = x.Name,
                        Description = x.Description,
                        City = x.City != null
                            ? new City
                            {
                                Name = x.City.Name,
                                Id = x.City.Id,
                                State = x.City.State != null
                                    ? new State
                                    {
                                        Id = x.City.State.Id,
                                        Name = x.City.State.Name,
                                        Country = x.City.State.Country != null
                                        ? new Country
                                        {
                                            Id = x.City.State.Country.Id,
                                            Name = x.City.State.Country.Name
                                        }
                                        : null
                                    }
                                    : null
                            }
                            : null,
                        Type = x.Type != null
                        ? new PositionType
                        {
                            Id = x.Type.Id,
                            Name = x.Type.Name
                        }
                        : null,
                        Company = x.Company != null
                        ? new Company
                        {
                            Name = x.Company.Name,
                            Id = x.Company.Id
                        }
                        : null
                    })
                    .Skip(toSkip.Value)
                    .Take(page_size.Value)
                    .OrderBy(x => x.Name).ToListAsync();
            }
            else
            {
                return await _dbContext.Position.Where(x =>
                    (state == null || x.State == state)
                    && (department == null || x.Department == department)
                    && (companyId == null || x.CompanyId == companyId)
                    && (cityId == null || x.CityId == cityId)
                    && (positionTypeId == null || (x.Type != null && x.Type.Id == positionTypeId))
                    && (search == null ||
                        (
                            x.Name.ToLower().Contains(search)
                            || x.Company.Name.ToLower().Contains(search)
                            || (x.City != null && x.City.Name.ToLower().Contains(search)
                            || (x.City != null && x.State != null
                                && x.City.State.Name.ToLower().Contains(search))
                            || (x.Type != null && x.Type.Name.ToLower().Contains(search)))
                        )))
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .Include(x => x.Company)
                    .Include(x => x.City)
                        .ThenInclude(x => x.State)
                            .ThenInclude(x => x.Country)
                    .Include(x => x.Type)
                    .Select(x => new Position
                    {
                        Id = x.Id,
                        BreezyId = x.BreezyId,
                        Name = x.Name,
                        Description = x.Description,
                        City = x.City != null
                            ? new City
                            {
                                Name = x.City.Name,
                                Id = x.City.Id,
                                State = x.City.State != null
                                    ? new State
                                    {
                                        Id = x.City.State.Id,
                                        Name = x.City.State.Name,
                                        Country = x.City.State.Country != null
                                        ? new Country
                                        {
                                            Id = x.City.State.Country.Id,
                                            Name = x.City.State.Country.Name
                                        }
                                        : null
                                    }
                                    : null
                            }
                            : null,
                        Type = x.Type != null
                        ? new PositionType
                        {
                            Id = x.Type.Id,
                            Name = x.Type.Name
                        }
                        : null,
                        Company = x.Company != null
                        ? new Company
                        {
                            Name = x.Company.Name,
                            Id = x.Company.Id
                        }
                        : null
                    })
                    .OrderBy(x => x.Name).ToListAsync();
            }
        }

        public async Task<Position> GetAsync(int id)
        {
            return await _dbContext.Position
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Include(x => x.Company)
                    .Include(x => x.City)
                        .ThenInclude(x => x.State)
                            .ThenInclude(x => x.Country)
                    .Include(x => x.Type)
                    .Select(x => new Position
                    {
                        Id = x.Id,
                        BreezyId = x.BreezyId,
                        Name = x.Name,
                        Description = x.Description,
                        City = x.City != null
                            ? new City
                            {
                                Name = x.City.Name,
                                Id = x.City.Id,
                                State = x.City.State != null
                                    ? new State
                                    {
                                        Id = x.City.State.Id,
                                        Name = x.City.State.Name,
                                        Country = x.City.State.Country != null
                                        ? new Country
                                        {
                                            Id = x.City.State.Country.Id,
                                            Name = x.City.State.Country.Name
                                        }
                                        : null
                                    }
                                    : null
                            }
                            : null,
                        Type = x.Type != null
                        ? new PositionType
                        {
                            Id = x.Type.Id,
                            Name = x.Type.Name
                        }
                        : null,
                        Company = x.Company != null
                        ? new Company
                        {
                            Name = x.Company.Name,
                            Id = x.Company.Id
                        }
                        : null
                    })
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
