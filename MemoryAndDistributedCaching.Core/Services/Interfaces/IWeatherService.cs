using MemoryAndDistributedCaching.Core.Models;
using System.Threading.Tasks;

namespace MemoryAndDistributedCaching.Core.Services.Interfaces
{
    public interface IWeatherService
    {
        Task<OpenWeather> GetWeather(string cityName);
    }
}
