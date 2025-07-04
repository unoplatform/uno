using System;
using System.Globalization;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.UI.RemoteControl.Services;
using Uno.UI.RemoteControl.Server.Telemetry;

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
								connectionContext.RemoteIpAddress = context.Connection.RemoteIpAddress;
								connectionContext.ConnectedAt = DateTime.UtcNow;

								// Extract User-Agent from headers if available
								if (context.Request.Headers.TryGetValue("User-Agent", out var userAgent))
								{
									connectionContext.UserAgent = userAgent.ToString();
								}

								// Add additional connection metadata
								connectionContext.AddMetadata("LocalPort", context.Connection.LocalPort.ToString(NumberFormatInfo.InvariantInfo));
								connectionContext.AddMetadata("RemotePort", context.Connection.RemotePort.ToString(NumberFormatInfo.InvariantInfo));
								connectionContext.AddMetadata("Protocol", context.Request.Protocol);

								if (app.Log().IsEnabled(LogLevel.Debug))
								{
									app.Log().LogDebug($"Populated connection context: {connectionContext}");
								}
							}

							// Use context.RequestServices directly - it already contains both global and scoped services
							// The global service provider was injected as Singleton in Program.cs, so it's accessible here
							using (var server = new RemoteControlServer(
								configuration,
								context.RequestServices.GetService<IIdeChannel>() ?? throw new InvalidOperationException("IIdeChannel is required"),
								context.RequestServices))
							{
								await server.RunAsync(await context.WebSockets.AcceptWebSocketAsync(), CancellationToken.None);
							}
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
						if (app.Log().IsEnabled(LogLevel.Information))
						{
							app.Log().LogInformation($"Disposing connection from {context.Connection.RemoteIpAddress}");
						}
					}
				});
			});

			return app;
		}
	}
}
