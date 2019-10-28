using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Practices.ServiceLocation;
using Uno.Extensions;

namespace Uno.UI.RemoteControl.Host
{
	internal class Startup
	{
		public void ConfigureServices(IServiceCollection services) =>
			services.AddRouting()
				.Configure<ProxyOptions>(Configuration);

		public Startup(IConfiguration configuration) =>
			Configuration = configuration;

		public IConfiguration Configuration { get; }

		public void Configure(IApplicationBuilder app, IOptionsMonitor<ProxyOptions> optionsAccessor, IHostingEnvironment env)
		{
			var provider = new MyProvider(app.ApplicationServices);
			ServiceLocator.SetLocatorProvider(() => provider);

			var options = optionsAccessor.CurrentValue;
			app
				.UseDeveloperExceptionPage()
				.UseWebSockets()
				.UseRemoteControlServer(options);
		}

	}

	public class ProxyOptions
	{
	}

	static class DebugExtensions
	{
		public static IApplicationBuilder UseRemoteControlServer(
			this IApplicationBuilder app,
			ProxyOptions options)
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
						using (var server = new RemoteControlServer(app.ApplicationServices.GetRequiredService<ILogger<RemoteControlServer>>()))
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
