using System;
using System.Globalization;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Mono.Options;
using Microsoft.Extensions.Logging;

namespace Uno.UI.RemoteControl.Host
{
	class Program
	{
		static void Main(string[] args)
		{
			var httpPort = 0;

			var p = new OptionSet() {
				{
					"httpPort=", s => {
						if(!int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out httpPort))
						{
							throw new ArgumentException($"The httpPort parameter is invalid {s}");
						}
					}
				},
			};
			
			p.Parse(args);

			if(httpPort == 0)
			{
				throw new ArgumentException($"The httpPort parameter is required.");
			}

			var host = new WebHostBuilder()
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
				.Build();

			host.Run();
		}
	}
}
