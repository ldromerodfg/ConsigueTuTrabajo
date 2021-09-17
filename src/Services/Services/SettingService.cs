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
        private readonly IBackupService _backupService;

        public SettingService(ILogger<BackupService> logger, DefaultContext dbContext, IBackupService backupService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _backupService = backupService;
        }

        public async Task<Setting> Get()
        {
            try {
                if (!await _dbContext.Setting.AnyAsync())
                {
                    _dbContext.Setting.Add(new Setting());
                    _dbContext.SaveChanges();

                    await _backupService.GetBreezyToken();
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex.Message);
            }

           return await _dbContext.Setting.FirstAsync();
        }
    }
}
