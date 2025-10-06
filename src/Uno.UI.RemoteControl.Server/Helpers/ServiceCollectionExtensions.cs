using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Uno.UI.RemoteControl.Server.Telemetry;

#if DEBUG
[assembly: Telemetry("81286976-e3a4-49fb-b03b-30315092dbc4", EventsPrefix = "uno/dev-server")]
#else
[assembly: Telemetry("9a44058e-1913-4721-a979-9582ab8bedce", EventsPrefix = "uno/dev-server")]
#endif

namespace Uno.UI.RemoteControl.Server.Helpers
{
	public static class ServiceCollectionExtensions
	{
		/// <summary>
		/// Adds global telemetry services (Singleton) for the DevServer itself.
		/// These services live for the entire duration of the server process.
		/// </summary>
		public static IServiceCollection AddGlobalTelemetry(this IServiceCollection services)
		{
			// Register root telemetry session as singleton
			services.AddSingleton<TelemetrySession>(svc => new TelemetrySession
			{
				SessionType = TelemetrySessionType.Root,
				ConnectionId = "global",
			});

			// Register global telemetry service as singleton
			services.AddSingleton<ITelemetry>(svc => CreateTelemetry(typeof(ITelemetry).Assembly, svc.GetRequiredService<TelemetrySession>()));
			services.AddSingleton(typeof(ITelemetry<>), typeof(TelemetryGenericAdapter<>));

			services.AddSingleton(sp =>
			{
				var telemetry = sp.GetRequiredService<ITelemetry>();
				var launchOptions = new AppLaunch.ApplicationLaunchMonitor.Options();

				launchOptions.OnRegistered = ev =>
				{
					telemetry.TrackEvent("app-launch/launched", [
						("platform", ev.Platform),
						("debug", ev.IsDebug.ToString())
					], null);
				};

				launchOptions.OnConnected = (ev, wasTimedOut) =>
				{
					var latencyMs = (DateTimeOffset.UtcNow - ev.RegisteredAt).TotalMilliseconds;
					telemetry.TrackEvent("app-launch/connected",
						[
							("platform", ev.Platform),
							("debug", ev.IsDebug.ToString()),
							("wasTimedOut", wasTimedOut.ToString())
						],
						[("latencyMs", latencyMs)]);
				};

				launchOptions.OnTimeout = ev =>
				{
					var timeoutSeconds = launchOptions.Timeout.TotalSeconds;
					telemetry.TrackEvent("app-launch/connection-timeout",
						[
							("platform", ev.Platform),
							("debug", ev.IsDebug.ToString()),
						],
						[("timeoutSeconds", timeoutSeconds)]);
				};

				return new AppLaunch.ApplicationLaunchMonitor(options: launchOptions);
			});

			return services;
		}

		/// <summary>
		/// Adds connection-specific telemetry services (Scoped) for individual WebSocket connections.
		/// These services are created per connection and disposed when the connection closes.
		/// </summary>
		public static IServiceCollection AddConnectionTelemetry(this IServiceCollection services, string? solutionPath)
		{
			// Register connection context as scoped to capture per-connection metadata
			services.AddScoped<ConnectionContext>();

			// Register TelemetrySession as scoped with connection context integration
			services.AddScoped<TelemetrySession>(svc => CreateConnectionTelemetrySession(svc, solutionPath));

			// Register connection-specific telemetry service as scoped
			services.AddScoped<ITelemetry>(svc => CreateTelemetry(typeof(ITelemetry).Assembly, svc.GetRequiredService<TelemetrySession>()));

			// Register ITelemetry<T> so that it resolves CreateTelemetry with the correct type argument T
			services.AddScoped(typeof(ITelemetry<>), typeof(TelemetryGenericAdapter<>));

			return services;
		}

		/// <summary>
		/// Creates a connection-specific telemetry session with metadata from ConnectionContext.
		/// </summary>
		private static TelemetrySession CreateConnectionTelemetrySession(IServiceProvider svc, string? solutionPath)
		{
			var connectionContext = svc.GetRequiredService<ConnectionContext>();
			var session = new TelemetrySession
			{
				SessionType = TelemetrySessionType.Connection,
				ConnectionId = connectionContext.ConnectionId,
				SolutionPath = solutionPath,
			};

			return session;
		}

		/// <summary>
		/// Creates a telemetry instance with optional session ID.
		/// </summary>
		internal static ITelemetry CreateTelemetry(Assembly asm, TelemetrySession session)
		{
			var sessionId = session.Id;

			// Get telemetry configuration first
			if (asm.GetCustomAttribute<TelemetryAttribute>() is not { } config)
			{
				throw new InvalidOperationException($"No telemetry config found for assembly {asm}.");
			}

			var eventsPrefix = config.EventsPrefix ?? $"uno/{asm.GetName().Name?.ToLowerInvariant()}";

			// Check for telemetry redirection environment variable
			var telemetryFilePath = Environment.GetEnvironmentVariable("UNO_PLATFORM_TELEMETRY_FILE");
			if (!string.IsNullOrEmpty(telemetryFilePath))
			{
				// New behavior: use contextual naming with events prefix
				if (session.SessionType is TelemetrySessionType.Root)
				{
					// Global telemetry - use contextual naming
					return new FileTelemetry(telemetryFilePath, "global", eventsPrefix);
				}
				else
				{
					// Connection telemetry - use session ID as context
					var shortSessionId = sessionId.Length > 8 ? sessionId[..8] : sessionId;
					return new FileTelemetry(telemetryFilePath, $"connection-{shortSessionId}", eventsPrefix);
				}
			}

			var path = Path.GetDirectoryName(session.SolutionPath) ?? string.Empty;

			// Use normal telemetry
			var telemetry =
				new Uno.DevTools.Telemetry.Telemetry(
					instrumentationKey: config.InstrumentationKey,
					eventNamePrefix: eventsPrefix,
					currentDirectoryProvider: () => path,
					versionAssembly: asm,
					sessionId: sessionId,
					productName: asm.GetName().Name);
			return new TelemetryAdapter(telemetry);
		}
	}
}
