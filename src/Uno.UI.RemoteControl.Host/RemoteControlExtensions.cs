using System;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.UI.RemoteControl.Host.IdeChannel;

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
							using (var server = new RemoteControlServer(
								configuration,
								context.RequestServices.GetService<IIdeChannelServerProvider>() ?? throw new InvalidOperationException("IIDEChannelServerProvider is required"),
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
