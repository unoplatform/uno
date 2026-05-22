using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.UI.RemoteControl.Host.Helpers;
using Uno.UI.RemoteControl.Server.Telemetry;

namespace Uno.UI.RemoteControl.Helpers;

public record AssemblyLoadResult(string DllFile, Assembly? Assembly, Exception? Error);

public class AssemblyHelper
{
	private static readonly ILogger _log = typeof(AssemblyHelper).Log();

	/// <summary>
	/// Loads the supplied add-in DLLs into <see cref="AssemblyLoadContext.Default"/>
	/// via <see cref="Assembly.LoadFrom(string)"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Every add-in lives in the same <see cref="AssemblyLoadContext"/> as the host —
	/// the default ALC. This guarantees a single <see cref="System.Type"/> identity for
	/// any assembly loaded by name, regardless of which add-in triggered the load. The
	/// host's <see cref="IServiceProvider"/> can therefore register and resolve types
	/// defined in any add-in without the cross-ALC identity mismatches that arise when
	/// add-ins are loaded into a private context.
	/// </para>
	/// <para>
	/// Cross-major-version <c>AssemblyRef</c>s (e.g. an add-in compiled against
	/// <c>System.Text.Encodings.Web v8.0.0.0</c> while the host carries v10 in its TPA)
	/// are handled by the <c>Default.Resolving</c> handler installed in
	/// <see cref="HostAssemblyResolution.Install"/>, which bridges by simple name and
	/// returns the host's already-loaded instance.
	/// </para>
	/// <para>
	/// Each successful load also triggers a proactive scan of the assembly's
	/// <c>AssemblyRef</c> table to surface version/PKT misalignments early, with a
	/// message that explains both the problem and the next step for the add-in's
	/// maintainer.
	/// </para>
	/// </remarks>
	public static IImmutableList<AssemblyLoadResult> Load(IImmutableList<string> dllFiles, ITelemetry? telemetry = null, bool throwIfLoadFailed = false)
	{
		var startTime = Stopwatch.GetTimestamp();

		var results = ImmutableList.CreateBuilder<AssemblyLoadResult>();
		var failedCount = 0;

		try
		{
			var distinctDlls = dllFiles.Distinct(StringComparer.OrdinalIgnoreCase).ToImmutableArray();

			if (distinctDlls.Length == 0)
			{
				return ImmutableList<AssemblyLoadResult>.Empty;
			}

			telemetry?.TrackEvent("addin-loading-start", default(Dictionary<string, string>), null);

			foreach (var dll in distinctDlls)
			{
				try
				{
					_log.LogDebug("Loading add-in assembly {Dll}.", dll);

					var assembly = Assembly.LoadFrom(dll);
					results.Add(new AssemblyLoadResult(dll, assembly, null));

					// Proactive diagnostic: surface version / PKT misalignments early so add-in
					// maintainers have actionable feedback before the misalignment manifests as
					// a runtime FileNotFoundException, MissingMethodException, or TypeLoadException.
					ReportAssemblyRefMismatches(assembly, dll);
				}
				catch (Exception err)
				{
					failedCount++;
					_log.LogError(err, "Failed to load assembly {Dll}.", dll);
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

	/// <summary>
	/// Walks <paramref name="addIn"/>'s <c>AssemblyRef</c> table and logs a warning
	/// for any reference whose simple name is already present in
	/// <see cref="AssemblyLoadContext.Default"/> with a different version or
	/// <see cref="AssemblyName.GetPublicKeyToken"/>. The message names the add-in,
	/// the misaligned reference, the requested vs loaded version, and the concrete
	/// action the add-in maintainer should take. Verbose mode (env-var
	/// <c>UNO_DEVSERVER_VERBOSE_ADDIN_REFS=1</c>) additionally enumerates every
	/// reference of every add-in for deep-dive debugging.
	/// </summary>
	private static void ReportAssemblyRefMismatches(Assembly addIn, string sourcePath)
	{
		var addInName = addIn.GetName().Name ?? Path.GetFileNameWithoutExtension(sourcePath);
		var verbose = Environment.GetEnvironmentVariable("UNO_DEVSERVER_VERBOSE_ADDIN_REFS") == "1";

		AssemblyName[] refs;
		try
		{
			refs = addIn.GetReferencedAssemblies();
		}
		catch (Exception ex)
		{
			_log.LogDebug(ex, "Could not enumerate AssemblyRefs for {AddIn}; skipping the mismatch scan.", addInName);
			return;
		}

		// Build an index of loaded assemblies once, keyed by simple name.
		var loadedByName = new Dictionary<string, AssemblyName>(StringComparer.OrdinalIgnoreCase);
		foreach (var loaded in AssemblyLoadContext.Default.Assemblies)
		{
			if (loaded.IsDynamic)
			{
				continue;
			}

			var loadedName = loaded.GetName();
			if (loadedName.Name is { } simple && !loadedByName.ContainsKey(simple))
			{
				loadedByName[simple] = loadedName;
			}
		}

		foreach (var reference in refs)
		{
			if (reference.Name is not { } refName)
			{
				continue;
			}

			if (!loadedByName.TryGetValue(refName, out var loadedRef))
			{
				if (verbose)
				{
					_log.LogDebug(
						"Add-in {AddIn} references {Ref} (not currently loaded — will be resolved on demand).",
						addInName, reference.FullName);
				}
				continue;
			}

			var refVersion = reference.Version;
			var loadedVersion = loadedRef.Version;
			var versionMismatch = refVersion is not null && loadedVersion is not null && refVersion != loadedVersion;

			var refPkt = reference.GetPublicKeyToken();
			var loadedPkt = loadedRef.GetPublicKeyToken();
			var pktMismatch = refPkt is { Length: > 0 } &&
				(loadedPkt is null || !loadedPkt.AsSpan().SequenceEqual(refPkt));

			if (!versionMismatch && !pktMismatch)
			{
				if (verbose)
				{
					_log.LogDebug(
						"Add-in {AddIn} references {Ref} — aligned with the host's loaded version.",
						addInName, reference.FullName);
				}
				continue;
			}

			// Build a single, actionable warning. The structure is:
			//   - what happened (which add-in, which ref, what's mismatched)
			//   - why it matters (the runtime consequence)
			//   - what to do (concrete next step for the add-in's maintainer)
			_log.LogWarning(
				"Add-in '{AddIn}' (loaded from '{Source}') references '{RefName}, Version={RefVersion}, PublicKeyToken={RefPkt}' " +
				"but the host has '{RefName}, Version={LoadedVersion}, PublicKeyToken={LoadedPkt}' loaded. " +
				"The Default.Resolving handler will bridge this request at runtime to the host's instance; " +
				"if you later see TypeLoadException, MissingMethodException, or 'Unable to resolve service' for types " +
				"from '{RefName}', this version misalignment is the most likely cause. " +
				"Action for the add-in's maintainer — pick one: " +
				"(1) align the '{RefName}' package version in '{AddIn}' with the version the host (and the other add-ins " +
				"of the same Uno.Sdk release) reference; or " +
				"(2) load '{RefName}' inside a private AssemblyLoadContext owned by '{AddIn}' so its own copy stays " +
				"isolated from the host's, then route the contract surface across the boundary via interfaces that the " +
				"host already loads. Option 1 is preferred whenever possible because it avoids cross-ALC Type-identity issues entirely.",
				addInName,
				sourcePath,
				refName,
				refVersion?.ToString() ?? "<none>",
				FormatPkt(refPkt),
				loadedVersion?.ToString() ?? "<none>",
				FormatPkt(loadedPkt),
				refName,
				refName,
				refName,
				addInName,
				refName,
				addInName);
		}
	}

	private static string FormatPkt(byte[]? pkt)
		=> pkt is { Length: > 0 } ? Convert.ToHexString(pkt).ToLowerInvariant() : "<none>";
}
