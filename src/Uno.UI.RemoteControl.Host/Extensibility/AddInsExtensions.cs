using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Uno.UI.RemoteControl.Helpers;
using Uno.UI.RemoteControl.Server.Telemetry;
using Uno.Utils.DependencyInjection;

namespace Uno.UI.RemoteControl.Host.Extensibility;

public static class AddInsExtensions
{
	public static WebApplicationBuilder ConfigureAddIns(this WebApplicationBuilder builder, string solutionFile, ITelemetry? telemetry = null)
	{
		// TODO: Move this to new pattern with a .AddAddIns() method.

		return builder.RegisterAddIns(AddIns.Discover(solutionFile, telemetry), telemetry);
	}

	public static WebApplicationBuilder ConfigureAddInsFromPaths(this WebApplicationBuilder builder, string addinsValue, ITelemetry? telemetry = null)
	{
		var dllPaths = addinsValue
			.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToImmutableList();

		var discovery = dllPaths.Count > 0
			? AddInsDiscoveryResult.Success(dllPaths)
			: AddInsDiscoveryResult.Empty();

		return builder.RegisterAddIns(discovery, telemetry);
	}

	private static WebApplicationBuilder RegisterAddIns(this WebApplicationBuilder builder, AddInsDiscoveryResult discovery, ITelemetry? telemetry)
	{
		var loadResults = AssemblyHelper.Load(discovery.AddIns, telemetry, throwIfLoadFailed: false);

		var assemblies = loadResults
			.Where(result => result.Assembly is not null)
			.Select(result => result.Assembly)
			.ToImmutableArray();

		var addInServicesStart = builder.Services.Count;
		builder.Services.AddFromAttributes(assemblies);

		// A broken add-in hosted service must degrade that add-in only — never
		// prevent the host from becoming ready (see uno-private#1968).
		AddInHostedServiceQuarantine.Apply(
			builder.Services,
			addInServicesStart,
			(service, error) => telemetry?.TrackEvent(
				"addin-hosted-service-quarantined",
				new Dictionary<string, string>
				{
					["QuarantinedService"] = service,
					["QuarantineErrorType"] = error.GetType().Name,
					["QuarantineErrorMessage"] = error.Message,
				},
				null));

		builder.Services.AddSingleton(new AddInsStatus(discovery, loadResults));

		return builder;
	}
}
