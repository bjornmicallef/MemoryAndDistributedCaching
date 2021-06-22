using MemoryAndDistributedCaching.Core.Models;
using MemoryAndDistributedCaching.Core.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace MemoryAndDistrubutedCaching.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly ILogger<WeatherForecastController> _logger;
        private readonly ICacheService _cacheService;
        private readonly IWeatherService _weatherService;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, ICacheService cacheService, IWeatherService weatherService)
        {
            _logger = logger;
            _cacheService = cacheService;
            _weatherService = weatherService;
        }

        [HttpGet]
        public async Task<WeatherForecast> GetAsync(string city)
        {
            var weather = new OpenWeather();
            var cacheExpiry = new TimeSpan(0, 0, 10);
            weather = await _cacheService.GetOrSet<OpenWeather>(city, () => _weatherService.GetWeather(city), cacheExpiry);

            return new WeatherForecast
            {
                Date = DateTime.Now,
                TemperatureC = weather.main.temp,
                Summary = weather.weather[0].description
            };
        }
    }
}
