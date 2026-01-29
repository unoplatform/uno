using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.UI.RemoteControl.Helpers;
using Uno.UI.RemoteControl.Server.AppLaunch;
using Uno.UI.RemoteControl.Server.Telemetry;
using Uno.UI.RemoteControl.ServerCore.Configuration;
using Uno.UI.RemoteControl.Services;
using Uno.UI.RemoteControl.VS.Helpers;

namespace Uno.UI.RemoteControl.Host
{
	internal static class RemoteControlExtensions
	{
		public static WebApplication UseRemoteControlServer(
			this WebApplication app,
			RemoteControlOptions options)
		{
			app
				.MapGet(
					"/rc",
					async context =>
					{
						await HandleWebSocketConnectionRequest(app, context);
					}).WithName("RC Protocol");

			// HTTP GET endpoint to register an app launch
			app.MapGet(
				"/applaunch/{mvid:guid}",
				async (HttpContext context, Guid mvid, [FromQuery] string? platform, [FromQuery] bool? isDebug, [FromQuery] string? ide, [FromQuery] string? plugin) =>
				{
					await HandleAppLaunchRegistrationRequest(context, mvid, platform, isDebug, ide, plugin);
				})
				.WithName("AppLaunchRegistration");

			// Alternate HTTP GET endpoint to register an app launch by providing the absolute assembly file path with encoded path string
			// Example: /app-launch/asm/C%3A%5Cpath%5Cto%5Capp.dll?IsDebug=true
			app.MapGet(
					"/applaunch/asm/{*assemblyPath}",
					async (HttpContext context, string assemblyPath, [FromQuery] bool? isDebug, [FromQuery] string? ide, [FromQuery] string? plugin) =>
					{
						ide ??= "Unknown";
						plugin ??= "Unknown";
						await HandleAppLaunchRegistrationRequest(app, assemblyPath, context, isDebug, ide, plugin);
					})
				.WithName("AppLaunchRegistration(Assembly)");
			return app;
		}

		private static async Task HandleWebSocketConnectionRequest(WebApplication app, HttpContext context)
		{
			if (!context.WebSockets.IsWebSocketRequest)
			{
				context.Response.StatusCode = 400;
				return;
			}

			if (app.Log().IsEnabled(LogLevel.Information))
			{
				app.Log().LogInformation("Accepted connection from {ConnectionRemoteIpAddress}", context.Connection.RemoteIpAddress);
			}

			ConnectionContext? connectionContext = null;
			ITelemetry? telemetry = null;
			await using var scope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateAsyncScope();
			var services = scope.ServiceProvider;
			try
			{
				var configuration = services.GetService<IRemoteControlConfiguration>();
				var launchMonitor = services.GetService<IApplicationLaunchMonitor>();
				var ideChannel = services.GetService<IIdeChannel>();

				if (configuration is not null && launchMonitor is not null && ideChannel is not null)
				{
					if (app.Log().IsEnabled(LogLevel.Trace))
					{
						app.Log().LogTrace(
							"Resolved devserver services: {ConfigurationType}, {LaunchMonitorType}, {IdeChannelType}",
							configuration.GetType().FullName,
							launchMonitor.GetType().FullName,
							ideChannel.GetType().FullName);
					}

					// Populate the scoped ConnectionContext with connection metadata
					connectionContext = services.GetService<ConnectionContext>();
					telemetry = services.GetService<ITelemetry>();
					if (connectionContext != null)
					{
						connectionContext.ConnectedAt = DateTimeOffset.UtcNow;

						// Track client connection in telemetry
						if (telemetry != null)
						{
							var properties = new Dictionary<string, string>
							{
								["ConnectionId"] = connectionContext.ConnectionId,
							};

							telemetry.TrackEvent("client-connection-opened", properties, null);
						}

						if (app.Log().IsEnabled(LogLevel.Debug))
						{
							app.Log().LogDebug("Populated connection context: {ConnectionContext}", connectionContext);
						}
					}

					using var transport = new WebSocketFrameTransport(await context.WebSockets.AcceptWebSocketAsync());
					var connectionHandler = services.GetRequiredService<IRemoteControlServerConnection>();

					await connectionHandler.HandleConnectionAsync(transport, context.RequestAborted);
				}
				else
				{
					if (app.Log().IsEnabled(LogLevel.Error))
					{
						app.Log().LogError("Unable to resolve required devserver services (configuration, IDE channel, or application launch monitor).");
					}
				}
			}
			finally
			{
				// Track client disconnection in telemetry
				if (telemetry != null && connectionContext != null)
				{
					var connectionDuration = DateTimeOffset.UtcNow - connectionContext.ConnectedAt;

					var properties = new Dictionary<string, string>
					{
						["ConnectionId"] = connectionContext.ConnectionId
					};

					var measurements = new Dictionary<string, double>
					{
						["ConnectionDurationSeconds"] = connectionDuration.TotalSeconds
					};

					telemetry.TrackEvent("client-connection-closed", properties, measurements);
				}

				if (app.Log().IsEnabled(LogLevel.Information))
				{
					app.Log().LogInformation(
						"Disposing connection from {ConnectionRemoteIpAddress}", context.Connection.RemoteIpAddress);
				}
			}
		}

		private static async Task HandleAppLaunchRegistrationRequest(
			HttpContext context,
			Guid mvid,
			string? platform,
			bool? isDebug,
			string? ide,
			string? plugin)
		{
			var monitor = context.RequestServices.GetRequiredService<IApplicationLaunchMonitor>();
			monitor.RegisterLaunch(mvid, platform, isDebug ?? false, ide ?? "Unknown", plugin ?? "Unknown");

			context.Response.StatusCode = StatusCodes.Status200OK;
			context.Response.ContentType = "application/json";

			var response = new { mvid = mvid, targetPlatform = platform };

			await context.Response.WriteAsync(JsonSerializer.Serialize(response));
		}

		private static async Task HandleAppLaunchRegistrationRequest(
			WebApplication app,
			string assemblyPath,
			HttpContext context,
			bool? isDebug,
			string ide,
			string plugin)
		{
			// Decode the path, if necessary (can be url-encoded)
			assemblyPath = Uri.UnescapeDataString(assemblyPath);

			// On Windows, the route will capture slashes; ensure it is a full path
			if (!Path.IsPathRooted(assemblyPath) || !File.Exists(assemblyPath))
			{
				context.Response.StatusCode = StatusCodes.Status400BadRequest;
				await context.Response.WriteAsync("Assembly path must be an existing absolute path.");
				return;
			}

			try
			{
				// Read MVID and TargetPlatform without loading the assembly
				var (mvid, platform) = AssemblyInfoReader.Read(assemblyPath);

				var monitor = context.RequestServices.GetRequiredService<IApplicationLaunchMonitor>();
				monitor.RegisterLaunch(mvid, platform, isDebug ?? false, ide, plugin);

				context.Response.StatusCode = StatusCodes.Status200OK;
				context.Response.ContentType = "application/json";

				var response = new { mvid = mvid, targetFramework = platform ?? "unknown" };

				await context.Response.WriteAsync(JsonSerializer.Serialize(response));
			}
			catch (BadImageFormatException)
			{
				context.Response.StatusCode = StatusCodes.Status400BadRequest;
				await context.Response.WriteAsync("The specified file is not a valid .NET assembly.");
			}
			catch (Exception ex)
			{
				if (app.Log().IsEnabled(LogLevel.Error))
				{
					var fileName = Path.GetFileName(assemblyPath);
					app.Log().LogError(ex, "Failed to read assembly info for assembly file: {AssemblyFileName}", fileName);
				}

				context.Response.StatusCode = StatusCodes.Status500InternalServerError;
				await context.Response.WriteAsync("Failed to process assembly path.");
			}
		}
	}
}
