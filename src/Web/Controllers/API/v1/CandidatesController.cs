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
    public class CandidatesController : ControllerBase
    {
        private readonly ILogger<CandidatesController> _logger;
        private readonly ICandidateService _candidateService;

        public CandidatesController(ILogger<CandidatesController> logger, ICandidateService candidateService)
        {
            _logger = logger;
            _candidateService = candidateService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync(string email = null, string origin = null, string breezyId = null)
        {
            try
            {
                var candidates = await _candidateService.GetAllAsync(email, origin, breezyId);

                return Ok(candidates.Select(candidate => BuildResponse(candidate)));
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
                var candidate = await _candidateService.GetAsync(id);

                if (candidate == null) return NotFound();

                return Ok(BuildResponse(candidate));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                return Problem();
            }
        }

        private CandidateResponse BuildResponse(Candidate candidate)
        {
            return new CandidateResponse
                {
                    Id = candidate.Id,
                    Name = candidate.Name,
                    BreezyId = candidate.BreezyId,
                    MetaId = candidate.MetaId,
                    Email = candidate.Email,
                    Headline = candidate.Headline,
                    Initial = candidate.Initial,
                    Origin = candidate.Origin,
                    PhoneNumber = candidate.PhoneNumber,
                    Stage = candidate.Stage != null
                        ? new CandidateStageResponse()
                        {
                            Id = candidate.Stage.Id,
                            Name = candidate.Stage.Name
                        }
                        : null
                };
        }
    }
}
