using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Uno.UI.RemoteControl.Server.Telemetry;

[assembly: Telemetry("DevServer", EventsPrefix = "uno/dev-server")]

namespace Uno.UI.RemoteControl.Helpers
{
	internal static class ServiceCollectionExtensions
	{
		/// <summary>
		/// Adds the telemetry session to the service collection.
		/// </summary>
		public static IServiceCollection AddTelemetry(this IServiceCollection services)
		{
			services.AddScoped<TelemetrySession>();
			services.AddScoped<ITelemetry>(svc => CreateTelemetry(svc, typeof(ITelemetry).Assembly, svc.GetRequiredService<TelemetrySession>().Id.ToString("N")));
			services.AddScoped(typeof(ITelemetry<>), typeof(TelemetryAdapter<>));

			services.AddSingleton<ITelemetry>(svc => CreateTelemetry(svc, typeof(ITelemetry).Assembly));
			services.AddSingleton(typeof(ITelemetry<>), typeof(TelemetryAdapter<>));

			return services;

			static ITelemetry CreateTelemetry(IServiceProvider svc, Assembly asm, string? sessionId = null)
			{
				// Check for telemetry redirection environment variable
				var telemetryFilePath = Environment.GetEnvironmentVariable("UNO_DEVSERVER_TELEMETRY_FILE");
				if (!string.IsNullOrEmpty(telemetryFilePath))
				{
					return new FileTelemetry(telemetryFilePath);
				}

				if (asm.GetCustomAttribute<TelemetryAttribute>() is { } config)
				{
					var telemetry = new Uno.DevTools.Telemetry.Telemetry(config.InstrumentationKey, config.EventsPrefix ?? $"uno/{asm.GetName().Name?.ToLowerInvariant()}", asm, sessionId);
					return new TelemetryWrapper(telemetry);
				}

				throw new InvalidOperationException($"No telemetry config found for assembly {asm}.");
			}
		}
	}
}
