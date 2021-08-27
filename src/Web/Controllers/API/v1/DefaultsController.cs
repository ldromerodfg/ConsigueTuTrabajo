using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;

namespace Web.Controllers.API
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class DefaultsController : ControllerBase
    {
        private readonly IDefaultService _defaultService;

        public DefaultsController(IDefaultService defaultService)
        {
            _defaultService = defaultService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync()
        {
            return Ok(await _defaultService.GetAll());
        }
    }
}
