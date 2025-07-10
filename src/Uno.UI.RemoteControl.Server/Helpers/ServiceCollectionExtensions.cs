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
				CreatedAt = DateTime.UtcNow
			});

			// Register global telemetry service as singleton
			services.AddSingleton<ITelemetry>(svc => CreateTelemetry(typeof(ITelemetry).Assembly));
			services.AddSingleton(typeof(ITelemetry<>), typeof(TelemetryGenericAdapter<>));

			return services;
		}

		/// <summary>
		/// Adds connection-specific telemetry services (Scoped) for individual WebSocket connections.
		/// These services are created per connection and disposed when the connection closes.
		/// </summary>
		public static IServiceCollection AddConnectionTelemetry(this IServiceCollection services)
		{
			// Register connection context as scoped to capture per-connection metadata
			services.AddScoped<ConnectionContext>();

			// Register TelemetrySession as scoped with connection context integration
			services.AddScoped<TelemetrySession>(svc => CreateConnectionTelemetrySession(svc));

			// Register connection-specific telemetry service as scoped
			services.AddScoped<ITelemetry>(svc => CreateTelemetry(typeof(ITelemetry).Assembly,
				svc.GetRequiredService<TelemetrySession>()));

			// Register ITelemetry<T> so that it resolves CreateTelemetry with the correct type argument T
			services.AddScoped(typeof(ITelemetry<>), typeof(TelemetryGenericAdapter<>));

			return services;
		}

		/// <summary>
		/// Creates a connection-specific telemetry session with metadata from ConnectionContext.
		/// </summary>
		private static TelemetrySession CreateConnectionTelemetrySession(IServiceProvider svc)
		{
			var connectionContext = svc.GetService<ConnectionContext>();
			var session = new TelemetrySession
			{
				SessionType = TelemetrySessionType.Connection,
				ConnectionId = connectionContext?.ConnectionId ?? Guid.NewGuid(),
				CreatedAt = DateTime.UtcNow
			};

			// Add connection metadata to the telemetry session only if connectionContext is available
			if (connectionContext != null)
			{
				session.AddMetadata("ConnectedAt",
					connectionContext.ConnectedAt.ToString("yyyy-MM-dd HH:mm:ss UTC", DateTimeFormatInfo.InvariantInfo));

				// Copy additional metadata from connection context (excluding network details)
				foreach (var kvp in connectionContext.Metadata)
				{
					// Skip network-related metadata that's not useful
					if (kvp.Key != "LocalPort" && kvp.Key != "RemotePort")
					{
						session.AddMetadata($"Connection.{kvp.Key}", kvp.Value);
					}
				}
			}

			return session;
		}

		/// <summary>
		/// Creates a telemetry instance with optional session ID.
		/// </summary>
		internal static ITelemetry CreateTelemetry(Assembly asm, TelemetrySession? session = null)
		{
			var sessionId = session?.Id.ToString("N");

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
				if (string.IsNullOrEmpty(sessionId))
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

			// Use normal telemetry
			var telemetry =
				new Uno.DevTools.Telemetry.Telemetry(
					instrumentationKey: config.InstrumentationKey,
					eventNamePrefix: eventsPrefix,
					versionAssembly: asm,
					sessionId: sessionId,
					productName: asm.GetName().Name);
			return new TelemetryAdapter(telemetry);
		}
	}
}
