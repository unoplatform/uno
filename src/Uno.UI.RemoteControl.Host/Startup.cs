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
using Microsoft.Extensions.Options;

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
		public static Dictionary<string, string> MapValues(Dictionary<string, string> response, HttpContext context, Uri debuggerHost)
		{
			var filtered = new Dictionary<string, string>();
			var request = context.Request;

			foreach (var key in response.Keys)
			{
				switch (key)
				{
					default:
						filtered[key] = response[key];
						break;
				}
			}
			return filtered;
		}

		public static IApplicationBuilder UseRemoteControlServer(this IApplicationBuilder app, ProxyOptions options)
			=> UseRemoteControlServer(app, options, MapValues);

		public static IApplicationBuilder UseRemoteControlServer(
			this IApplicationBuilder app,
			ProxyOptions options,
			Func<Dictionary<string, string>, HttpContext, Uri, Dictionary<string, string>> mapFunc)
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

					Console.WriteLine($"Accepted connection from {context.Connection.RemoteIpAddress}");

					var server = new RemoteControlServer();
					await server.Run(await context.WebSockets.AcceptWebSocketAsync(), CancellationToken.None);
				});
			});

			return app;
		}
	}
}
