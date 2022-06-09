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

		public void Configure(IApplicationBuilder app, IOptionsMonitor<RemoteControlOptions> optionsAccessor)
		{
			var provider = new ServiceLocatorAdapter(app.ApplicationServices);
			ServiceLocator.SetLocatorProvider(() => provider);

			var options = optionsAccessor.CurrentValue;
			app
				.UseDeveloperExceptionPage()
				.UseWebSockets()
				.UseRemoteControlServer(options);

			app.Use(async (context, next) =>
			{
				context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
				context.Response.Headers.Add("Access-Control-Allow-Methods", "*");
				context.Response.Headers.Add("Access-Control-Allow-Headers", "*");

				// Required for SharedArrayBuffer: https://developer.chrome.com/blog/enabling-shared-array-buffer/
				context.Response.Headers.Add("Cross-Origin-Embedder-Policy", "require-corp");
				context.Response.Headers.Add("Cross-Origin-Opener-Policy", "same-origin");
				await next();
			});
		}
	}
}
