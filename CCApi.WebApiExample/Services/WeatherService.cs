using CCApi.WebApiExample.Interfaces;

namespace CCApi.WebApiExample.Services;

public class WeatherService : IWeatherService {
    public Task<int> GetCurrentTemperature() {
        return Task.FromResult(25);
    }
}