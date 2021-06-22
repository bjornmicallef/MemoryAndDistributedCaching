using System;

namespace MemoryAndDistrubutedCaching.Api
{
    public class WeatherForecast
    {
        public DateTime Date { get; set; }

        public double TemperatureC { get; set; }

        public double TemperatureF => 32 + (double)(TemperatureC / 0.5556);

        public string Summary { get; set; }
    }
}
