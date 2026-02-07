using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.UI.RemoteControl.Server.Telemetry;

namespace Uno.UI.RemoteControl.Helpers;

public record AssemblyLoadResult(string DllFile, Assembly? Assembly, Exception? Error);

public class AssemblyHelper
{
	private static readonly ILogger _log = typeof(AssemblyHelper).Log();

	public static IImmutableList<AssemblyLoadResult> Load(IImmutableList<string> dllFiles, ITelemetry? telemetry = null, bool throwIfLoadFailed = false)
	{
		var startTime = Stopwatch.GetTimestamp();

		telemetry?.TrackEvent("addin-loading-start", default(Dictionary<string, string>), null);

		var results = ImmutableList.CreateBuilder<AssemblyLoadResult>();
		var failedCount = 0;

		try
		{
			foreach (var dll in dllFiles.Distinct(StringComparer.OrdinalIgnoreCase))
			{
				try
				{
					_log.Log(LogLevel.Debug, $"Loading add-in assembly '{dll}'.");

					results.Add(new AssemblyLoadResult(dll, Assembly.LoadFrom(dll), null));
				}
				catch (Exception err)
				{
					failedCount++;
					_log.Log(LogLevel.Error, $"Failed to load assembly '{dll}'.", err);
					results.Add(new AssemblyLoadResult(dll, null, err));

					if (throwIfLoadFailed)
					{
						throw;
					}
				}
			}

			var result = results.ToImmutable();

			// Track completion
			var completionProperties = new Dictionary<string, string>
			{
				["AssemblyLoadResult"] = failedCount == 0 ? "Success" : "PartialFailure",
			};

			var completionMeasurements = new Dictionary<string, double>
			{
				["AssemblyLoadDurationMs"] = Stopwatch.GetElapsedTime(startTime).TotalMilliseconds,
				["AssemblyLoadFailedAssemblies"] = failedCount,
			};

			telemetry?.TrackEvent("addin-loading-complete", completionProperties, completionMeasurements);

			return result;
		}
		catch (Exception ex)
		{
			var errorMeasurements = new Dictionary<string, double>
			{
				["AssemblyLoadDurationMs"] = Stopwatch.GetElapsedTime(startTime).TotalMilliseconds,
				["AssemblyLoadFailedAssembliesCount"] = failedCount,
			};

			telemetry?.TrackException(ex, properties: null, errorMeasurements);
			throw;
		}
	}
}
