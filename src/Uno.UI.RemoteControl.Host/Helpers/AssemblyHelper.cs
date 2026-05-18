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
			// All add-ins share a single AssemblyLoadContext backed by per-add-in
			// AssemblyDependencyResolver instances. This isolates add-in dependency
			// trees (e.g. Kiota's net8.0 binaries that hard-reference
			// System.Text.Encodings.Web 8.0.0.0) from the host's framework
			// assemblies while letting Microsoft.*/System.* and shared contract types
			// resolve against the host's already-loaded versions. Add-ins still see
			// each other's types (matching the prior Assembly.LoadFrom behaviour).
			var distinctDlls = dllFiles.Distinct(StringComparer.OrdinalIgnoreCase).ToImmutableList();
			var loadContext = distinctDlls.Count > 0
				? new AddInLoadContext(distinctDlls)
				: null;

			foreach (var dll in distinctDlls)
			{
				try
				{
					_log.Log(LogLevel.Debug, $"Loading add-in assembly '{dll}'.");

					// Load the top-level add-in by file path so the caller's
					// specific DLL is always what ends up in the context — going
					// through LoadFromAssemblyName here would route through the
					// Default.Assemblies / TPA fallback in AddInLoadContext.Load
					// and could silently substitute a host-loaded assembly if the
					// add-in's simple name happened to collide. Transitive deps
					// still flow through that override as intended.
					var assembly = loadContext!.LoadFromAssemblyPath(dll);

					results.Add(new AssemblyLoadResult(dll, assembly, null));
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
			var errorProperties = new Dictionary<string, string>
			{
				["AssemblyLoadErrorMessage"] = ex.Message,
				["AssemblyLoadErrorType"] = ex.GetType().Name,
			};

			var errorMeasurements = new Dictionary<string, double>
			{
				["AssemblyLoadDurationMs"] = Stopwatch.GetElapsedTime(startTime).TotalMilliseconds,
				["AssemblyLoadFailedAssembliesCount"] = failedCount,
			};

			telemetry?.TrackEvent("addin-loading-error", errorProperties, errorMeasurements);
			throw;
		}
	}
}
