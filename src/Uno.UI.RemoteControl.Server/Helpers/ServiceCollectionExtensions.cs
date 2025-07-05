using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Uno.UI.RemoteControl.Server.Telemetry;

[assembly: Telemetry("DevServer", EventsPrefix = "uno/dev-server")]

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
			services.AddSingleton(typeof(ITelemetry<>), typeof(TelemetryAdapter<>));

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
			services.AddScoped<ITelemetry>(svc => CreateTelemetry(typeof(ITelemetry).Assembly, svc.GetRequiredService<TelemetrySession>().Id.ToString("N")));
			services.AddScoped(typeof(ITelemetry<>), typeof(TelemetryAdapter<>));

			return services;
		}

		/// <summary>
		/// Adds the telemetry session to the service collection.
		/// This method is kept for backward compatibility and will be deprecated in favor of AddGlobalTelemetry/AddConnectionTelemetry.
		/// </summary>
		[Obsolete("Use AddGlobalTelemetry() and AddConnectionTelemetry() instead for better separation of concerns.")]
		public static IServiceCollection AddTelemetry(this IServiceCollection services)
		{
			// Register connection context as scoped to capture per-connection metadata
			services.AddScoped<ConnectionContext>();

			// Register TelemetrySession as scoped with connection context integration
			services.AddScoped<TelemetrySession>(svc => CreateTelemetrySession(svc));

			services.AddScoped<ITelemetry>(svc => CreateTelemetry(typeof(ITelemetry).Assembly, svc.GetRequiredService<TelemetrySession>().Id.ToString("N")));
			services.AddScoped(typeof(ITelemetry<>), typeof(TelemetryAdapter<>));

			services.AddSingleton<ITelemetry>(svc => CreateTelemetry(typeof(ITelemetry).Assembly));
			services.AddSingleton(typeof(ITelemetry<>), typeof(TelemetryAdapter<>));

			return services;
		}

		/// <summary>
		/// Creates a connection-specific telemetry session with metadata from ConnectionContext.
		/// </summary>
		private static TelemetrySession CreateConnectionTelemetrySession(IServiceProvider svc)
		{
			var connectionContext = svc.GetRequiredService<ConnectionContext>();
			var session = new TelemetrySession
			{
				SessionType = TelemetrySessionType.Connection,
				ConnectionId = connectionContext.ConnectionId,
				CreatedAt = DateTime.UtcNow
			};

			// Add connection metadata to the telemetry session
			session.AddMetadata("RemoteIpAddress", TelemetryHashHelper.Hash(connectionContext.RemoteIpAddress?.ToString() ?? "Unknown"));
			session.AddMetadata("ConnectedAt", connectionContext.ConnectedAt.ToString("yyyy-MM-dd HH:mm:ss UTC", DateTimeFormatInfo.InvariantInfo));

			if (!string.IsNullOrEmpty(connectionContext.UserAgent))
			{
				session.AddMetadata("UserAgent", connectionContext.UserAgent);
			}

			// Copy additional metadata from connection context
			foreach (var kvp in connectionContext.Metadata)
			{
				session.AddMetadata($"Connection.{kvp.Key}", kvp.Value);
			}

			return session;
		}

		/// <summary>
		/// Creates a telemetry session for backward compatibility (can be either root or connection based on context).
		/// </summary>
		private static TelemetrySession CreateTelemetrySession(IServiceProvider svc)
		{
			var connectionContext = svc.GetService<ConnectionContext>();
			var session = new TelemetrySession
			{
				SessionType = connectionContext != null ? TelemetrySessionType.Connection : TelemetrySessionType.Root,
				ConnectionId = connectionContext?.ConnectionId,
				CreatedAt = DateTime.UtcNow
			};

			// Add connection metadata to the telemetry session
			if (connectionContext != null)
			{
				session.AddMetadata("RemoteIpAddress", connectionContext.RemoteIpAddress?.ToString() ?? "Unknown");
				session.AddMetadata("ConnectedAt", connectionContext.ConnectedAt.ToString("yyyy-MM-dd HH:mm:ss UTC", DateTimeFormatInfo.InvariantInfo));

				if (!string.IsNullOrEmpty(connectionContext.UserAgent))
				{
					session.AddMetadata("UserAgent", connectionContext.UserAgent);
				}

				// Copy additional metadata from connection context
				foreach (var kvp in connectionContext.Metadata)
				{
					session.AddMetadata($"Connection.{kvp.Key}", kvp.Value);
				}
			}

			return session;
		}

		/// <summary>
		/// Creates a telemetry instance with optional session ID.
		/// </summary>
		private static ITelemetry CreateTelemetry(Assembly asm, string? sessionId = null)
		{
			// Check for telemetry redirection environment variable
			var telemetryFilePath = Environment.GetEnvironmentVariable("UNO_DEVSERVER_TELEMETRY_FILE");
			if (!string.IsNullOrEmpty(telemetryFilePath))
			{
				// New behavior: use contextual naming
				if (string.IsNullOrEmpty(sessionId))
				{
					// Global telemetry - use contextual naming
					return new FileTelemetry(telemetryFilePath, "global");
				}
				else
				{
					// Connection telemetry - use session ID as context
					var shortSessionId = sessionId.Length > 8 ? sessionId.Substring(0, 8) : sessionId;
					return new FileTelemetry(telemetryFilePath, $"connection-{shortSessionId}");
				}
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
