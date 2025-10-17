using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using CommonServiceLocator;
using Microsoft.Extensions.Hosting;

namespace Uno.UI.RemoteControl.Host
{
	internal class Startup
	{
		public void ConfigureServices(IServiceCollection services) =>
			services.AddRouting()
				.Configure<RemoteControlOptions>(Configuration);

		public Startup(IConfiguration configuration) =>
			Configuration = configuration;

		public IConfiguration Configuration { get; }

		public void Configure(WebApplication app)
		{
			var services = app.Services;

			var provider = new ServiceLocatorAdapter(services);
			ServiceLocator.SetLocatorProvider(() => provider);

			var options = services.GetRequiredService<IOptionsMonitor<RemoteControlOptions>>().CurrentValue;

			if (app.Environment.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseWebSockets();

			// DevServer endpoints are registered here (http + websocket)
			app.UseRemoteControlServer(options);

			// CORS headers required for some platforms (WebAssembly)
			app.Use(async (context, next) =>
			{
				context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
				context.Response.Headers.Append("Access-Control-Allow-Methods", "*");
				context.Response.Headers.Append("Access-Control-Allow-Headers", "*");

				// Required for SharedArrayBuffer: https://developer.chrome.com/blog/enabling-shared-array-buffer/
				context.Response.Headers.Append("Cross-Origin-Embedder-Policy", "require-corp");
				context.Response.Headers.Append("Cross-Origin-Opener-Policy", "same-origin");
				await next();
			});
		}
	}
}
