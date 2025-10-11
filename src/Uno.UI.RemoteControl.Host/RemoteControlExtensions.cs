using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.UI.RemoteControl.Services;
using Uno.UI.RemoteControl.Server.Telemetry;
using Uno.UI.RemoteControl.Server.AppLaunch;
using Microsoft.AspNetCore.Http;
using System.IO;
using Uno.UI.RemoteControl.VS.Helpers;

namespace Uno.UI.RemoteControl.Host
{
	static class RemoteControlExtensions
	{
		public static IApplicationBuilder UseRemoteControlServer(
			this IApplicationBuilder app,
			RemoteControlOptions options)
		{
			app.UseRouter(router =>
			{
				router.MapGet("rc", async context =>
				{
					if (!context.WebSockets.IsWebSocketRequest)
					{
						context.Response.StatusCode = 400;
						return;
					}

					if (app.Log().IsEnabled(LogLevel.Information))
					{
						app.Log().LogInformation($"Accepted connection from {context.Connection.RemoteIpAddress}");
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
									app.Log().LogDebug($"Populated connection context: {connectionContext}");
								}
							}

							// Use context.RequestServices directly - it already contains both global and scoped services
							// The global service provider was injected as Singleton in Program.cs, so it's accessible here
							using var server = new RemoteControlServer(
								configuration,
								context.RequestServices.GetService<IIdeChannel>() ?? throw new InvalidOperationException("IIdeChannel is required"),
								context.RequestServices);

							await server.RunAsync(await context.WebSockets.AcceptWebSocketAsync(), CancellationToken.None);
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
							app.Log().LogInformation($"Disposing connection from {context.Connection.RemoteIpAddress}");
						}
					}
				});

				// HTTP GET endpoint to register an app launch
				router.MapGet("applaunch/{mvid}", async context =>
				{
					var mvidValue = context.GetRouteValue("mvid")?.ToString();
					if (!Guid.TryParse(mvidValue, out var mvid))
					{
						context.Response.StatusCode = StatusCodes.Status400BadRequest;
						await context.Response.WriteAsync("Invalid MVID - must be a valid GUID.");
						return;
					}

					string? platform = null;
					var isDebug = false;

					// Query string support: ?platform=...&isDebug=true
					if (context.Request.Query.TryGetValue("platform", out var p))
					{
						platform = p.ToString();
					}

					if (context.Request.Query.TryGetValue("isDebug", out var d))
					{
						if (bool.TryParse(d.ToString(), out var qParsed))
						{
							isDebug = qParsed;
						}
					}

					if (string.IsNullOrWhiteSpace(platform))
					{
						context.Response.StatusCode = StatusCodes.Status400BadRequest;
						await context.Response.WriteAsync("Missing required 'platform'.");
						return;
					}

					var monitor = context.RequestServices.GetRequiredService<ApplicationLaunchMonitor>();
					monitor.RegisterLaunch(mvid, platform!, isDebug);

					context.Response.StatusCode = StatusCodes.Status200OK;
					await context.Response.WriteAsync("registered");
				});

				// Alternate HTTP GET endpoint to register an app launch by providing the absolute assembly file path with encoded path string
				// Example: /app-launch/asm/C%3A%5Cpath%5Cto%5Capp.dll?IsDebug=true
				router.MapGet("applaunch/asm/{*assemblyPath}", async context =>
				{
					var assemblyPathValue = Uri.UnescapeDataString(context.GetRouteValue("assemblyPath")?.ToString() ?? string.Empty);
					if (string.IsNullOrWhiteSpace(assemblyPathValue))
					{
						context.Response.StatusCode = StatusCodes.Status400BadRequest;
						await context.Response.WriteAsync("Missing assembly path.");
						return;
					}

					// On Windows, the route will capture slashes; ensure it is a full path
					var assemblyPath = assemblyPathValue!;
					if (!Path.IsPathRooted(assemblyPath) || !File.Exists(assemblyPath))
					{
						context.Response.StatusCode = StatusCodes.Status400BadRequest;
						await context.Response.WriteAsync("Assembly path must be an existing absolute path.");
						return;
					}

					var isDebug = false;
					if (context.Request.Query.TryGetValue("isDebug", out var isDebugVal))
					{
						if (!bool.TryParse(isDebugVal.ToString(), out isDebug))
						{
							isDebug = false;
						}
					}

					try
					{
						// Read MVID and TargetPlatform without loading the assembly
						var (mvid, platform) = AssemblyInfoReader.Read(assemblyPath);

						var monitor = context.RequestServices.GetRequiredService<ApplicationLaunchMonitor>();
						monitor.RegisterLaunch(mvid, platform ?? "Desktop", isDebug);

						context.Response.StatusCode = StatusCodes.Status200OK;
						await context.Response.WriteAsync("registered - application with MVID=" + mvid + " and platform=" + platform + " is now registered for launch.");
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
							app.Log().LogError(ex, "Failed to read assembly info for path: {path}", assemblyPath);
						}

						context.Response.StatusCode = StatusCodes.Status500InternalServerError;
						await context.Response.WriteAsync("Failed to process assembly path.");
					}
				});
			});


			return app;
		}
	}
}
