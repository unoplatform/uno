using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Uno.HotReload.Host
{
	class Program
	{
		static void Main(string[] args)
		{
			var host = new WebHostBuilder()
				.UseSetting(nameof(WebHostBuilderIISExtensions.UseIISIntegration), false.ToString())
				.UseKestrel()
				.UseUrls("http://*:5000/")
				.UseContentRoot(Directory.GetCurrentDirectory())
				.UseStartup<Startup>()
				.ConfigureAppConfiguration((hostingContext, config) =>
				{
					config.AddCommandLine(args);
				})
				.Build();

			host.Run();
		}
	}
}
