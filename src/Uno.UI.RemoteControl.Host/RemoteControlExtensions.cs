using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Uno.Extensions;

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
						using (var server = new RemoteControlServer())
						{
							await server.Run(await context.WebSockets.AcceptWebSocketAsync(), CancellationToken.None);
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
