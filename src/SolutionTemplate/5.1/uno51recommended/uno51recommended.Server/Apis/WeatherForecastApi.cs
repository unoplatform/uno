namespace uno51recommended.Server.Apis;

internal static class WeatherForecastApi
{
    private const string Tag = "Weather";
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    internal static WebApplication MapWeatherApi(this WebApplication app)
    {
        app.MapGet("/api/weatherforecast", GetForecast)
            .WithTags(Tag)
            .WithName(nameof(GetForecast));
        return app;
    }

    /// <summary>
    /// Creates a make believe weather forecast for the next 5 days.
    /// </summary>
    /// <param name="loggerFactory"></param>
    /// <returns>A fake 5 day forecast</returns>
    /// <remarks>A 5 Day Forecast</remarks>
    /// <response code="200">Weather Forecast returned</response>
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<WeatherForecast>), 200)]
    private static IEnumerable<WeatherForecast> GetForecast(ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger(nameof(WeatherForecastApi));
        logger.LogDebug("Getting Weather Forecast.");

        return Enumerable.Range(1, 5).Select(index =>
            new WeatherForecast(
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Random.Shared.Next(-20, 55),
                Summaries[Random.Shared.Next(Summaries.Length)]
            )
        )
        .Select(x =>
        {
            logger.LogInformation("Weather forecast for {Date} is a {Summary} {TemperatureC}°C", x.Date, x.Summary, x.TemperatureC);
            return x;
        })
        .ToArray();
    }
}
