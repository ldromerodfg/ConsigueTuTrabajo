using System;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Service.Interfaces;
using Web.Models;

namespace Web.Controllers.API
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class PositionsController : ControllerBase
    {
        private readonly ILogger<PositionsController> _logger;
        private readonly ISettingService _settingService;
        private readonly IPositionService _positionService;
        private readonly IBackupService _backupService;

        public PositionsController(ILogger<PositionsController> logger, ISettingService settingService,
            IPositionService positionService, IBackupService backupService)
        {
            _logger = logger;
            _settingService = settingService;
            _positionService = positionService;
            _backupService = backupService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync(string state = null, string department = null,
            int? companyId = null, int? cityId = null, int? positionTypeId = null, string search = null,
            int? page_size = null, int? page = null)
        {
            if ((page_size != null && page == null) || (page_size == null && page != null)
            || (page_size != null && page_size.Value > 20))
            {
                return BadRequest();
            }

            try
            {
                var positions = await _positionService.GetAllAsync(state, department, companyId, cityId,
                    positionTypeId, search, page_size, page);

                return Ok(positions.Select(position => BuildResponse(position)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                return Problem();
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAsync(int id)
        {
            try
            {
                var position = await _positionService.GetAsync(id);

                if (position == null) return NotFound();

                return Ok(BuildResponse(position));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                return Problem();
            }
        }

        private PositionResponse BuildResponse(Position position)
        {
            return new PositionResponse
            {
                Id = position.Id,
                Name = position.Name,
                Description = position.Description,
                Location = position.City != null
                        ? $"{position.City.Name}, {position.City.State.Name}"
                        : null,
                Company = position.Company.Name,
                Type = position.Type != null
                        ? position.Type.Name
                        : null,
                CityId = position.City != null
                    ? position.City.Id
                    : null,
                PositionTypeId = position.Type != null
                    ? position.Type.Id
                    : null
            };
        }
    }
}
