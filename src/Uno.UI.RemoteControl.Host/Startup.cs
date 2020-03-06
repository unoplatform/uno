using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Practices.ServiceLocation;

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

		public void Configure(IApplicationBuilder app, IOptionsMonitor<RemoteControlOptions> optionsAccessor, IHostingEnvironment env)
		{
			var provider = new ServiceLocatorAdapter(app.ApplicationServices);
			ServiceLocator.SetLocatorProvider(() => provider);

			var options = optionsAccessor.CurrentValue;
			app
				.UseDeveloperExceptionPage()
				.UseWebSockets()
				.UseRemoteControlServer(options);
		}
	}
}
