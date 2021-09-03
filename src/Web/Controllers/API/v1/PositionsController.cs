using System;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly IPositionService _positionService;

        public PositionsController(ILogger<PositionsController> logger, IPositionService positionService)
        {
            _logger = logger;
            _positionService = positionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync(string state = null, string department = null, int? companyId = null)
        {
            try
            {
                var positions = await _positionService.GetAllAsync(state, department, companyId);

                return Ok(positions.Select(position => new PositionResponse
                {
                    Id = position.Id,
                    Name = position.Name,
                    BreezyId = position.BreezyId,
                    State = position.State,
                    Description = position.Description,
                    Education = position.Education,
                    Department = position.Department,
                    RequisitionId = position.RequisitionId,
                    QuestionaireId = position.QuestionaireId,
                    PipelineId = position.PipelineId,
                    CandidateType = position.CandidateType,
                    Tags = position.OrgType,
                    CreatorId = position.CreatorId
                }));
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

                return Ok(new PositionResponse
                {
                    Id = position.Id,
                    Name = position.Name,
                    BreezyId = position.BreezyId,
                    State = position.State,
                    Description = position.Description,
                    Education = position.Education,
                    Department = position.Department,
                    RequisitionId = position.RequisitionId,
                    QuestionaireId = position.QuestionaireId,
                    PipelineId = position.PipelineId,
                    CandidateType = position.CandidateType,
                    Tags = position.OrgType,
                    CreatorId = position.CreatorId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                return Problem();
            }
        }
    }
}
