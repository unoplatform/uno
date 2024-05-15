namespace uno51recommended.DataContracts;

/// <summary>
/// A Weather Forecast for a specific date
/// </summary>
/// <param name="Date">Gets the Date of the Forecast.</param>
/// <param name="TemperatureC">Gets the Forecast Temperature in Celsius.</param>
/// <param name="Summary">Get a description of how the weather will feel.</param>
public record WeatherForecast(DateOnly Date, double TemperatureC, string? Summary)
{
    /// <summary>
    /// Gets the Forecast Temperature in Fahrenheit
    /// </summary>
    public double TemperatureF => 32 + (TemperatureC * 9 / 5);
}
