using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
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
                return Problem(ex.Message);
            }
        }

        [HttpGet("{id}", Name = "GetByIdAsync")]
        public async Task<IActionResult> GetByIdAsync(int id)
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
                return Problem(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync()
        {
            try
            {
                var exists = await _candidateService
                    .GetByEmailOrPhone(Request.Form["Email"], Request.Form["PhoneNumber"]);

                if (exists != null)
                {
                    return Problem(statusCode: 400);
                }

                // <<< POST TO BREEZY >>>

                var candidate = new Candidate
                {
                    Name = Request.Form["Name"],
                    Email = Request.Form["Email"],
                    PhoneNumber = Request.Form["PhoneNumber"],
                    PositionId = int.Parse(Request.Form["PositionId"]),
                    CandidateStageId = 1 // Default???
                };

                candidate = await _candidateService.CreateAsync(candidate);

                if (Request.Form.Files.Any())
                {
                    var file = Request.Form.Files[0];

                    try
                    {
                        if (!Directory.Exists("./wwwroot/cv"))
                        {
                            Directory.CreateDirectory("./wwwroot/cv");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogInformation(ex.Message);
                    }

                    using (var inputStream = new FileStream($"./wwwroot/cv/{candidate.Name}.pdf", FileMode.Create))
                    {
                        await file.CopyToAsync(inputStream);
                        byte[] array = new byte[inputStream.Length];
                        inputStream.Seek(0, SeekOrigin.Begin);
                        inputStream.Read(array, 0, array.Length);
                    }
                }

                return CreatedAtRoute(
                    nameof(GetByIdAsync),
                    new { id = candidate.Id },
                    new CandidateResponse
                    {
                        Id = candidate.Id,
                        Name = candidate.Name,
                        Email = candidate.Email,
                        PhoneNumber = candidate.PhoneNumber
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                return Problem(ex.Message);
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
