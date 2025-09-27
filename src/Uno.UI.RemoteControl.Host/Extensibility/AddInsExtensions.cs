using System;
using System.Collections.Immutable;
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
			var discovery = AddIns.Discover(solutionFile, telemetry);
			var loadResults = AssemblyHelper.Load(discovery.AddIns, telemetry, throwIfLoadFailed: false);

			var assemblies = loadResults
				.Where(result => result.Assembly is not null)
				.Select(result => result.Assembly)
				.ToImmutableArray();

			services.AddFromAttributes(assemblies);
			services.AddSingleton(new AddInsStatus(discovery, loadResults));
		});
	}
}
