using Logger.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Logger.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<WeatherForecast>>> GetWeatherAsync()
        {
            var weatherList = Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
               .ToArray();

            return await Task.FromResult(weatherList);
        }

        
        [HttpPost]
        [LoggerFilter]
        public async Task<IActionResult> CreateWeatherAsync(WeatherForecast request)
            => await Task.FromResult(Ok());

    }
}