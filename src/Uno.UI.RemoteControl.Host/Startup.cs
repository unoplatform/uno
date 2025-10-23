﻿using System;
using CommonServiceLocator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Uno.Extensions;

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
				.UseRemoteControlServer(options)
				.UseRouting();

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

			app.UseEndpoints(endpoints =>
			{
				try
				{
					endpoints.MapMcp("/mcp");
				}
				catch (Exception ex)
				{
					// MCP registration may fail if no MCP tooling is resolved
					// through ServiceCollectionExtensionAttribute. This might indicate
					// a missing package reference in the Uno.SDK.

					typeof(Program).Log().Log(LogLevel.Warning, ex, "Unable to find the MCP Tooling in the environment, the MCP feature is disabled.");
				}
			});
		}
	}
}
