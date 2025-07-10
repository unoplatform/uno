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

public class AssemblyHelper
{
	private static readonly ILogger _log = typeof(AssemblyHelper).Log();

	public static IImmutableList<Assembly> Load(IImmutableList<string> dllFiles, ITelemetry? telemetry = null, bool throwIfLoadFailed = false)
	{
		var startTime = Stopwatch.GetTimestamp();

		telemetry?.TrackEvent("AddIn.Loading.Start", default(Dictionary<string, string>), null);

		var assemblies = ImmutableList.CreateBuilder<Assembly>();
		var failedCount = 0;

		try
		{
			foreach (var dll in dllFiles.Distinct(StringComparer.OrdinalIgnoreCase))
			{
				try
				{
					_log.Log(LogLevel.Debug, $"Loading add-in assembly '{dll}'.");

					assemblies.Add(Assembly.LoadFrom(dll));
				}
				catch (Exception err)
				{
					failedCount++;
					_log.Log(LogLevel.Error, $"Failed to load assembly '{dll}'.", err);

					if (throwIfLoadFailed)
					{
						throw;
					}
				}
			}

			var result = assemblies.ToImmutable();

			// Track completion
			var completionProperties = new Dictionary<string, string>
			{
				["devserver/AssemblyLoad/Result"] = failedCount == 0 ? "Success" : "PartialFailure",
			};

			var completionMeasurements = new Dictionary<string, double>
			{
				["devserver/AssemblyLoad/DurationMs"] = Stopwatch.GetElapsedTime(startTime).TotalMilliseconds,
				["devserver/AssemblyLoad/FailedAssemblies"] = failedCount,
			};

			telemetry?.TrackEvent("AddIn.Loading.Complete", completionProperties, completionMeasurements);

			return result;
		}
		catch (Exception ex)
		{
			var errorProperties = new Dictionary<string, string>
			{
				["devserver/AssemblyLoad/ErrorMessage"] = ex.Message,
				["devserver/AssemblyLoad/ErrorType"] = ex.GetType().Name,
			};

			var errorMeasurements = new Dictionary<string, double>
			{
				["devserver/AssemblyLoad/DurationMs"] = Stopwatch.GetElapsedTime(startTime).TotalMilliseconds,
				["devserver/AssemblyLoad/FailedAssembliesCount"] = failedCount,
			};

			telemetry?.TrackEvent("AddIn.Loading.Error", errorProperties, errorMeasurements);
			throw;
		}
	}
}
