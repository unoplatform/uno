using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.UI.RemoteControl.Services;
using Uno.UI.RemoteControl.Server.Telemetry;
using Uno.UI.RemoteControl.Server.AppLaunch;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Uno.UI.RemoteControl.VS.Helpers;
using System.Text.Json;

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

			try
			{
				if (context.RequestServices.GetService<IConfiguration>() is { } configuration)
				{
					// Populate the scoped ConnectionContext with connection metadata
					var connectionContext = context.RequestServices.GetService<ConnectionContext>();
					if (connectionContext != null)
					{
						connectionContext.ConnectedAt = DateTimeOffset.UtcNow;

						// Track client connection in telemetry
						var telemetry = context.RequestServices.GetService<ITelemetry>();
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

					// Use context.RequestServices directly - it already contains both global and scoped services
					// The global service provider was injected as Singleton in Program.cs, so it's accessible here
					using var server = new RemoteControlServer(
						configuration,
						context.RequestServices.GetService<IIdeChannel>() ??
						throw new InvalidOperationException("IIdeChannel is required"),
						context.RequestServices);

					await server.RunAsync(await context.WebSockets.AcceptWebSocketAsync(), context.RequestAborted);
				}
				else
				{
					if (app.Log().IsEnabled(LogLevel.Error))
					{
						app.Log().LogError($"Unable to find configuration service");
					}
				}
			}
			finally
			{
				// Track client disconnection in telemetry
				var connectionContext = context.RequestServices.GetService<ConnectionContext>();
				var telemetry = context.RequestServices.GetService<ITelemetry>();
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
			var monitor = context.RequestServices.GetRequiredService<ApplicationLaunchMonitor>();
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

				var monitor = context.RequestServices.GetRequiredService<ApplicationLaunchMonitor>();
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
