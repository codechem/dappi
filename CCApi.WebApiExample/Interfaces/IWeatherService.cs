namespace CCApi.WebApiExample.Interfaces;

public interface IWeatherService {
    Task<int> GetCurrentTemperature();
}