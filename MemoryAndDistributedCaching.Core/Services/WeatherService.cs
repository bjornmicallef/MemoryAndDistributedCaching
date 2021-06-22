using MemoryAndDistributedCaching.Core.Models;
using MemoryAndDistributedCaching.Core.Services.Interfaces;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace MemoryAndDistributedCaching.Core.Services
{
    public class WeatherService : IWeatherService
    {
        public WeatherService()
        {
        }

        public async Task<OpenWeather> GetWeather(string cityName)
        {
            if (string.IsNullOrWhiteSpace(cityName))
                throw new ArgumentNullException("Provide city name");

            var weather = new OpenWeather();
            var apiKey = "your OpenWeather API key";
            using (var httpClient = new HttpClient())
            {
                using (var response = await httpClient.GetAsync($"https://api.openweathermap.org/data/2.5/weather?q={cityName}&appid={apiKey}&units=metric"))
                {
                    weather = JsonConvert.DeserializeObject<OpenWeather>(await response.Content.ReadAsStringAsync());
                }
            }

            return weather;
        }
    }
}
