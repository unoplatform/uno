using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mono.Options;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Uno.DevTools.Telemetry;
using Uno.Extensions;
using Uno.UI.RemoteControl.Host;
using Uno.UI.RemoteControl.Host.Extensibility;
using Uno.UI.RemoteControl.Host.IdeChannel;
using Uno.UI.RemoteControl.Services;

[assembly:Telemetry("abc", EventsPrefix = "uno/dev-server")]

namespace Uno.UI.RemoteControl.Host
{
	//public interface ITelemetryScope
	//{
	//	static abstract TelemetryKey GetKey();
	//}

	//public record TelemetryKey(string InstrumentationKey, string EventsPrefix);

	//public class DevServerTelemetry : ITelemetryScope
	//{
	//	/// <inheritdoc />
	//	public static TelemetryKey GetKey()
	//		=> new("", "uno/devserver");
	//}


	public interface ITelemetry<T> : ITelemetry;

	[AttributeUsage(AttributeTargets.Assembly)]
	public class TelemetryAttribute(string instrumentationKey) : Attribute
	{
		public string InstrumentationKey { get; } = instrumentationKey;
		
		public string? EventsPrefix { get; set; } = string.Empty;
	}

	public record TelemetryAdapter<T>(IServiceProvider services) : ITelemetry<T>
	{
		private ITelemetry Inner { get; } = services.GetRequiredKeyedService<ITelemetry>(typeof(T).Assembly);

		/// <inheritdoc />
		public void Dispose()
			=> Inner.Dispose();

		/// <inheritdoc />
		public void Flush()
			=> Inner.Flush();

		/// <inheritdoc />
		public Task FlushAsync(CancellationToken ct)
			=> Inner.FlushAsync(ct);

		/// <inheritdoc />
		public void ThreadBlockingTrackEvent(string eventName, IDictionary<string, string> properties, IDictionary<string, double> measurements)
			=> Inner.ThreadBlockingTrackEvent(eventName, properties, measurements);

		/// <inheritdoc />
		public void TrackEvent(string eventName, (string key, string value)[]? properties, (string key, double value)[]? measurements)
			=> Inner.TrackEvent(eventName, properties, measurements);

		/// <inheritdoc />
		public void TrackEvent(string eventName, IDictionary<string, string>? properties, IDictionary<string, double>? measurements)
			=> Inner.TrackEvent(eventName, properties, measurements);

		/// <inheritdoc />
		public bool Enabled => Inner.Enabled;
	}

	public record TelemetrySession
	{
		public Guid Id { get; } = Guid.NewGuid();
	}
	
	public static class ServiceCollectionExtensions
	{
		/// <summary>
		/// Adds the telemetry session to the service collection.
		/// </summary>
		public static IServiceCollection AddTelemetry(this IServiceCollection services)
		{
			services.AddScoped<TelemetrySession>();
			services.AddKeyedScoped<ITelemetry>(
				KeyedService.AnyKey,
				(svc, key) => CreateTelemetry(svc, key, svc.GetRequiredService<TelemetrySession>().Id.ToString("N")));
			services.AddScoped(typeof(ITelemetry<>), typeof(TelemetryAdapter<>));

			services.AddKeyedSingleton<ITelemetry>(
				KeyedService.AnyKey,
				(svc, key) =>
				{
					var desc = new ServiceDescriptor()
					return CreateTelemetry(svc, key);
				});
			services.AddSingleton(typeof(ITelemetry<>), typeof(TelemetryAdapter<>));

			return services;

			static ITelemetry CreateTelemetry(IServiceProvider svc, object key, string? sessionId = null)
				=> key switch
				{
					Type type => svc.GetRequiredKeyedService<ITelemetry>(type.Assembly),
					Assembly asm when asm.GetCustomAttribute<TelemetryAttribute>() is { } config => new Telemetry(config.InstrumentationKey, config.EventsPrefix ?? $"uno/{asm.GetName().Name?.ToLowerInvariant()}", asm, sessionId),
					Assembly => throw new InvalidOperationException($"No telemetry config found for assembly {key}."),
					_ => throw new InvalidOperationException($"Unsupported telemetry key type {key.GetType().FullName}. Expected Assembly or Type.")
				};
		}
	}

	//public class Bla(ITelemetry<DevServerTelemetry> telemetry)
	//{

	//}

	class Program
	{
		static async Task Main(string[] args)
		{
			var httpPort = 0;
			var parentPID = 0;
			var solution = default(string);

			var p = new OptionSet
			{
				{
					"httpPort=", s => {
						if(!int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out httpPort))
						{
							throw new ArgumentException($"The httpPort parameter is invalid {s}");
						}
					}
				},
				{
					"ppid=", s => {
						if(!int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out parentPID))
						{
							throw new ArgumentException($"The parent process id parameter is invalid {s}");
						}
					}
				},
				{
					"solution=", s =>
					{
						if (string.IsNullOrWhiteSpace(s) || !File.Exists(s))
						{
							throw new ArgumentException($"The provided solution path '{s}' does not exists");
						}

						solution = s;
					}
				}
			};

			p.Parse(args);

			if (httpPort == 0)
			{
				throw new ArgumentException($"The httpPort parameter is required.");
			}

			const LogLevel logLevel = LogLevel.Debug;

			// During init, we dump the logs to the console, until the logger is set up
			Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(logLevel).AddConsole());

			var builder = new WebHostBuilder()
				.UseSetting("UseIISIntegration", false.ToString())
				.UseKestrel()
				.UseUrls($"http://*:{httpPort}/")
				.UseContentRoot(Directory.GetCurrentDirectory())
				.UseStartup<Startup>()
				.ConfigureLogging(logging =>
					logging
						.ClearProviders()
						.AddConsole()
						.SetMinimumLevel(LogLevel.Debug))
				.ConfigureAppConfiguration((hostingContext, config) =>
				{
					config.AddCommandLine(args);
				})
				.ConfigureServices(services =>
				{
					services.AddSingleton<IIdeChannel, IdeChannelServer>();
					services.AddTelemetry();
				});

			if (solution is not null)
			{
				// For backward compatibility, we allow to not have a solution file specified.
				builder.ConfigureAddIns(solution);
			}
			else
			{
				typeof(Program).Log().Log(LogLevel.Warning, "No solution file specified, add-ins will not be loaded which means that you won't be able to use any of the uno-studio features. Usually this indicates that your version of uno's IDE extension is too old.");
			}

			var host = builder.Build();

			// Once the app has started, we use the logger from the host
			Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = host.Services.GetRequiredService<ILoggerFactory>();

			host.Services.GetService<IIdeChannel>();

			using var parentObserver = ParentProcessObserver.Observe(host, parentPID);

			await host.RunAsync();
		}
	}
}
