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
	public static IWebHostBuilder ConfigureAddIns(this IWebHostBuilder builder, string solutionFile, ITelemetry? telemetry = null)
	{
		return builder.ConfigureServices(services =>
		{
			var discoveredAddIns = AddIns.Discover(solutionFile, telemetry);
			var assemblies = AssemblyHelper.Load(discoveredAddIns, telemetry, throwIfLoadFailed: false);

			services.AddFromAttributes(assemblies);
		});
	}
}
