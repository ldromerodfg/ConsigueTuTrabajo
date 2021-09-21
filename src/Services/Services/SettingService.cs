using System;
using System.Threading.Tasks;
using DataAccess.Contexts;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Service.Interfaces;

namespace Service.Services
{
    public class SettingService : ISettingService
    {
        private readonly ILogger _logger;
        private readonly DefaultContext _dbContext;

        public SettingService(ILogger<SettingService> logger, DefaultContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task<Setting> GetAsync()
        {
            try
            {
                if (!await _dbContext.Setting.AnyAsync())
                {
                    _dbContext.Setting.Add(new Setting());
                    _dbContext.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return await _dbContext.Setting.FirstAsync();
        }

        public async Task UpdateAsync(Setting entity)
        {
            _dbContext.Setting.Update(entity);
            await _dbContext.SaveChangesAsync();
        }
    }
}
