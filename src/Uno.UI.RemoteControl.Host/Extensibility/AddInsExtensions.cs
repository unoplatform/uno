using System;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Uno.Utils.DependencyInjection;
using Uno.UI.RemoteControl.Helpers;
using Uno.UI.RemoteControl.Server.Telemetry;

namespace Uno.UI.RemoteControl.Host.Extensibility;

public static class AddInsExtensions
{
	public static IWebHostBuilder ConfigureAddIns(this IWebHostBuilder builder, string solutionFile)
	{
		return builder.ConfigureServices(services =>
		{
			// Get telemetry service for add-in discovery and loading
			var serviceProvider = services.BuildServiceProvider();
			var telemetry = serviceProvider.GetService<ITelemetry>();

			var discoveredAddIns = AddIns.Discover(solutionFile, telemetry);
			var assemblies = AssemblyHelper.Load(discoveredAddIns, telemetry, throwIfLoadFailed: false);

			services.AddFromAttributes(assemblies);
		});
	}
}
