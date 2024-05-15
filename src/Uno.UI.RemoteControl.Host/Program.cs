using System;
using System.Globalization;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mono.Options;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.ComponentModel;
using System.Threading.Tasks;
using Uno.UI.RemoteControl.Host.IdeChannel;

namespace Uno.UI.RemoteControl.Host
{
	class Program
	{
		static async Task Main(string[] args)
		{
			var httpPort = 0;
			var parentPID = 0;

			var p = new OptionSet() {
				{
					"httpPort=", s => {
						if(!int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out httpPort))
						{
							throw new ArgumentException($"The httpPort parameter is invalid {s}");
						}
					}
				},
				{
					"ppid=", s => {
						if(!int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out parentPID))
						{
							throw new ArgumentException($"The parent process id parameter is invalid {s}");
						}
					}
				}
			};

			p.Parse(args);

			if (httpPort == 0)
			{
				throw new ArgumentException($"The httpPort parameter is required.");
			}

			var builder = new WebHostBuilder()
				.UseSetting("UseIISIntegration", false.ToString())
				.UseKestrel()
				.UseUrls($"http://*:{httpPort}/")
				.UseContentRoot(Directory.GetCurrentDirectory())
				.UseStartup<Startup>()
				.ConfigureLogging(logging =>
					logging
						.ClearProviders()
						.AddConsole()
						.SetMinimumLevel(LogLevel.Debug))
				.ConfigureAppConfiguration((hostingContext, config) =>
				{
					config.AddCommandLine(args);
				})
				.ConfigureServices(services =>
				{
					services.AddSingleton<IIdeChannelServerProvider, IdeChannelServerProvider>();
				});

			var host = builder.Build();

			host.Services.GetService<IIdeChannelServerProvider>();

			using var parentObserver = ParentProcessObserver.Observe(host, parentPID);

			await host.RunAsync();
		}
	}
}
