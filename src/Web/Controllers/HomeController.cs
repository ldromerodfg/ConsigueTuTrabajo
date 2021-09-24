using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Service.Interfaces;
using Web.Models;

namespace Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IPositionService _positionService;

        public HomeController(ILogger<HomeController> logger, IPositionService positionService)
        {
            _logger = logger;
            _positionService = positionService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("position/{id}")]
        public async Task<IActionResult> PositionAsync(int id)
        {
            ViewData["id"] = id;

            var position = await _positionService.GetAsync(id);

            if (position == null)
            {
                return NotFound();   
            }

            ViewData["id"] = id;
            ViewData["Title"] = $"{position.Name} - Consigue Tu Trabajo";
            
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
