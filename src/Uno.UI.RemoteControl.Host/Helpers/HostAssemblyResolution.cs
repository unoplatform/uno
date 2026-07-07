using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;

namespace Uno.UI.RemoteControl.Host.Helpers;

/// <summary>
/// Shared assembly-bridging policy for the DevServer host and its add-ins.
/// Centralises the logic that returns an already-loaded assembly from the
/// default ALC when a simple-name match is found, so that host and add-ins
/// share a single <see cref="System.Type"/> identity for cross-boundary types.
/// When nothing compatible is loaded yet, an on-demand pass loads the assembly
/// from an identity-validated runtime/TPA file or a registered add-in directory,
/// so resolution does not depend on what happened to load first.
/// </summary>
/// <remarks>
/// Diagnostic messages go to <see cref="Console.Out"/> / <see cref="Console.Error"/>
/// rather than an <see cref="Microsoft.Extensions.Logging.ILogger"/>: this code runs
/// before the host's logging pipeline is wired, so plumbing an
/// <see cref="Microsoft.Extensions.Logging.ILoggerFactory"/> here would force the
/// caller to construct one early. The host's parent process (CLI, VS, Cursor) captures
/// stdout/stderr — these messages reach the operator regardless.
/// </remarks>
internal static class HostAssemblyResolution
{
	/// <summary>
	/// 0 = handler not subscribed, 1 = handler subscribed. Written once via
	/// <see cref="Interlocked.CompareExchange(ref int, int, int)"/> so
	/// <see cref="Install"/> can subscribe the <c>Resolving</c> handler exactly
	/// once even when called from multiple threads or from test fixtures that
	/// share the process-wide <see cref="AssemblyLoadContext.Default"/>.
	/// </summary>
	private static int s_handlerInstalled;

	/// <summary>
	/// Directories probed by the on-demand pass in <see cref="Resolve"/>, in
	/// registration order: the host directory plus every add-in directory.
	/// </summary>
	private static ImmutableArray<string> s_probingDirectories = ImmutableArray<string>.Empty;

	/// <summary>
	/// Runtime/TPA assembly file paths indexed by simple name, letting the
	/// on-demand pass validate a candidate's identity from file metadata before
	/// loading it. The TPA list is process-constant, hence the one-time parse.
	/// </summary>
	private static readonly Lazy<ILookup<string, string>> s_tpaBySimpleName = new(static () =>
		(AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? string.Empty)
			.Split(System.IO.Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
			.ToLookup(
				static path => System.IO.Path.GetFileNameWithoutExtension(path),
				StringComparer.OrdinalIgnoreCase));

	/// <summary>
	/// File-name globs used by <see cref="EagerLoadFromDirectory"/> to pre-load the
	/// OOB shared-framework assemblies most prone to cross-major-version
	/// <c>AssemblyRef</c> mismatches. Pre-loading them into
	/// <see cref="AssemblyLoadContext.Default"/> lets <see cref="TryBridgeBySimpleName"/>
	/// serve them to any caller asking for a different major version of the same
	/// simple name.
	/// </summary>
	internal static readonly ImmutableArray<string> DefaultEagerLoadPatterns =
	[
		"System.*.dll",
		"Microsoft.Extensions.*.dll",
	];

	/// <summary>
	/// Installs the cross-major-version assembly-resolution safety net on
	/// <see cref="AssemblyLoadContext.Default"/> and eager-loads the shared-framework
	/// OOB assemblies most prone to version-mismatch requests.
	/// </summary>
	/// <remarks>
	/// Idempotent: calling this method more than once (e.g. from unit tests that share
	/// the same process) registers the <c>Resolving</c> handler exactly once. The
	/// eager-load pass is also safe to repeat because it filters out already-loaded
	/// assemblies by simple name.
	/// </remarks>
	/// <param name="hostDir">
	/// Optional override for the directory from which DLLs are eager-loaded. When
	/// <see langword="null"/> the directory of this assembly is used.
	/// </param>
	internal static void Install(string? hostDir = null)
	{
		// Subscribe the Resolving handler exactly once (lock-free CAS). The flag
		// protects ONLY the subscription; the eager-load pass is gated independently
		// by the per-simple-name dedup inside the loop so it remains safe to run on
		// repeated Install() calls.
		if (Interlocked.CompareExchange(ref s_handlerInstalled, 1, 0) == 0)
		{
			AssemblyLoadContext.Default.Resolving += static (ctx, req) =>
				Resolve(ctx, req);
		}

		var resolvedHostDir = hostDir
			?? System.IO.Path.GetDirectoryName(typeof(HostAssemblyResolution).Assembly.Location)
			?? AppContext.BaseDirectory;

		if (string.IsNullOrEmpty(resolvedHostDir))
		{
			return;
		}

		RegisterProbingDirectory(resolvedHostDir);

		// Best-effort eager-load: IO / reflection errors must never crash the host
		// startup path. The handler subscribed above remains the primary safety net
		// even when this fails.
		try
		{
			EagerLoadFromDirectory(resolvedHostDir);
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine(
				$"Uno.UI.RemoteControl.Host: eager-load from '{resolvedHostDir}' failed " +
				$"({ex.GetType().Name}: {ex.Message}). Cross-major-version AssemblyRefs " +
				"may still resolve at runtime via the Default.Resolving handler.");
		}
	}

	/// <summary>
	/// Adds <paramref name="dir"/> to the directories probed by the on-demand pass
	/// of <see cref="Resolve"/>. Idempotent per directory; registration order is
	/// preserved and determines probing order.
	/// </summary>
	internal static void RegisterProbingDirectory(string dir)
	{
		if (string.IsNullOrEmpty(dir))
		{
			return;
		}

		try
		{
			dir = System.IO.Path.GetFullPath(dir);
		}
		catch (Exception)
		{
			// Keep the caller-supplied form; probing tolerates non-canonical paths.
		}

		ImmutableInterlocked.Update(
			ref s_probingDirectories,
			static (dirs, d) => dirs.Contains(d, StringComparer.OrdinalIgnoreCase) ? dirs : dirs.Add(d),
			dir);
	}

	/// <summary>
	/// Handler for <see cref="AssemblyLoadContext.Resolving"/> on the default ALC:
	/// bridges to an already-loaded assembly first (preserves <see cref="System.Type"/>
	/// identity), then falls back to loading on demand from the runtime or a
	/// registered add-in directory. Emits a diagnostic only when everything failed.
	/// </summary>
	internal static Assembly? Resolve(AssemblyLoadContext context, AssemblyName requested)
	{
		if (requested.Name is not { Length: > 0 } simpleName)
		{
			return null;
		}

		if (TryBridgeBySimpleName(context, requested) is { } bridged)
		{
			return bridged;
		}

		// Satellite lookups routinely miss during culture fallback; stay silent.
		if (simpleName.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}

		if (TryLoadOnDemand(context, requested) is { } loaded)
		{
			return loaded;
		}

		ReportResolutionMiss(context, requested);
		return null;
	}

	/// <summary>
	/// Eager-loads DLLs matching <see cref="DefaultEagerLoadPatterns"/> from
	/// <paramref name="dir"/> into <see cref="AssemblyLoadContext.Default"/>.
	/// </summary>
	/// <remarks>
	/// Idempotent: assemblies already present in <see cref="AssemblyLoadContext.Default"/>
	/// (by simple name) are skipped. Once loaded, <see cref="TryBridgeBySimpleName"/>
	/// can return these assemblies to any caller asking for them by name, preserving a
	/// single <see cref="System.Type"/> identity across the host/add-in boundary.
	/// </remarks>
	/// <returns>The number of assemblies successfully eager-loaded by this call.</returns>
	internal static int EagerLoadFromDirectory(string dir)
	{
		if (string.IsNullOrEmpty(dir) || !System.IO.Directory.Exists(dir))
		{
			return 0;
		}

		// Build a set of simple names already loaded so we can skip duplicates.
		var alreadyLoaded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var asm in AssemblyLoadContext.Default.Assemblies)
		{
			if (!asm.IsDynamic && asm.GetName().Name is { } n)
			{
				alreadyLoaded.Add(n);
			}
		}

		// Discover DLLs matching the default glob patterns.
		var candidates = DefaultEagerLoadPatterns
			.SelectMany(p => System.IO.Directory.EnumerateFiles(dir, p));

		var loaded = 0;
		var failed = 0;

		foreach (var path in candidates)
		{
			var simpleName = System.IO.Path.GetFileNameWithoutExtension(path);
			if (alreadyLoaded.Contains(simpleName))
			{
				continue;
			}

			try
			{
				AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
				loaded++;
			}
			catch (Exception ex)
			{
				failed++;
				Console.Error.WriteLine(
					$"Uno.UI.RemoteControl.Host: eager-load of '{simpleName}' from '{path}' failed " +
					$"({ex.GetType().Name}: {ex.Message}). Cross-major-version AssemblyRefs " +
					"for this assembly may still resolve at runtime via the Default.Resolving handler.");
			}
		}

		if (loaded + failed > 0)
		{
			Console.Out.WriteLine(
				$"Uno.UI.RemoteControl.Host: eager-loaded {loaded} assemblies from '{dir}' ({failed} failures).");
		}

		return loaded;
	}

	/// <summary>
	/// Attempts to satisfy <paramref name="requested"/> by returning an already-loaded
	/// assembly from <paramref name="context"/> whose simple name matches. Dynamic
	/// (reflection-emit) assemblies are skipped so runtime-generated names that
	/// coincidentally collide with a real <c>AssemblyRef</c> do not silently substitute
	/// the wrong type; a loaded assembly whose <see cref="System.Version"/> is strictly
	/// lower than the requested version is rejected to prevent silent downgrades; and
	/// when the request carries a <see cref="AssemblyName.GetPublicKeyToken"/> the loaded
	/// assembly's token must match — an identity check, not an allow-list. Unsigned
	/// requests skip the PKT check (covers the cross-add-in unsigned-contract scenario).
	/// </summary>
	/// <remarks>
	/// On a successful bridge with a version mismatch, an informational message is
	/// emitted so operators can observe which requests are being bridged. A miss is
	/// silent here — <see cref="Resolve"/> reports it only after the on-demand pass
	/// also failed, so a successful fallback is not preceded by a spurious error.
	/// </remarks>
	/// <returns>
	/// The matching <see cref="Assembly"/> instance, or <c>null</c> when no compatible
	/// candidate exists in <paramref name="context"/>.
	/// </returns>
	internal static Assembly? TryBridgeBySimpleName(
		AssemblyLoadContext context,
		AssemblyName requested)
	{
		var simpleName = requested.Name;
		if (simpleName is null)
		{
			return null;
		}

		var requestedPkt = requested.GetPublicKeyToken();
		var requestedVersion = requested.Version;

		foreach (var loaded in context.Assemblies)
		{
			// Skip reflection-emit / source-generator assemblies: their auto-generated
			// names could coincidentally match a real AssemblyRef and we'd silently
			// substitute them, causing downstream TypeLoadException / MissingMethodException.
			if (loaded.IsDynamic)
			{
				continue;
			}

			var loadedName = loaded.GetName();
			if (!string.Equals(loadedName.Name, simpleName, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			if (!SatisfiesIdentity(loadedName, requestedPkt, requestedVersion))
			{
				continue;
			}

			// Successful bridge. Log if the bridge involved a non-trivial version
			// substitution so the operator can correlate runtime warnings with the
			// resolution events.
			var loadedVersion = loadedName.Version;
			if (requestedVersion is not null && loadedVersion is not null
				&& requestedVersion != loadedVersion)
			{
				Console.Out.WriteLine(
					$"Uno.UI.RemoteControl.Host: bridged '{simpleName}' request: caller asked for " +
					$"Version={requestedVersion}, returning Version={loadedVersion} (host's loaded instance). " +
					"This is normal when an add-in compiled against an older OOB package is loaded into a host with a newer one.");
			}

			return loaded;
		}

		return null;
	}

	/// <summary>
	/// On-demand pass, used when nothing compatible is loaded yet: probes the
	/// runtime/TPA file list first, then the registered add-in directories. Every
	/// candidate's identity is validated from file metadata (same PKT / no-downgrade
	/// guards as <see cref="TryBridgeBySimpleName"/>, plus a simple-name match)
	/// BEFORE loading — a load-then-check would pin an incompatible copy in the ALC
	/// (one assembly per simple name) and block the remaining candidates.
	/// </summary>
	internal static Assembly? TryLoadOnDemand(AssemblyLoadContext context, AssemblyName requested)
	{
		var simpleName = requested.Name;
		if (simpleName is null)
		{
			return null;
		}

		var requestedPkt = requested.GetPublicKeyToken();
		var requestedVersion = requested.Version;

		foreach (var candidatePath in s_tpaBySimpleName.Value[simpleName])
		{
			if (TryLoadValidatedCandidate(context, candidatePath, simpleName, requestedPkt, requestedVersion) is { } fromRuntime)
			{
				return fromRuntime;
			}
		}

		foreach (var dir in s_probingDirectories)
		{
			var candidatePath = System.IO.Path.Combine(dir, simpleName + ".dll");
			if (TryLoadValidatedCandidate(context, candidatePath, simpleName, requestedPkt, requestedVersion) is { } fromDirectory)
			{
				return fromDirectory;
			}
		}

		return null;
	}

	/// <summary>
	/// Loads <paramref name="candidatePath"/> only when its file metadata carries the
	/// requested simple name and passes <see cref="SatisfiesIdentity"/>. The name check
	/// guards against a file whose name does not match the assembly it contains.
	/// </summary>
	private static Assembly? TryLoadValidatedCandidate(
		AssemblyLoadContext context,
		string candidatePath,
		string simpleName,
		byte[]? requestedPkt,
		Version? requestedVersion)
	{
		try
		{
			if (!System.IO.File.Exists(candidatePath))
			{
				return null;
			}

			var candidateName = AssemblyName.GetAssemblyName(candidatePath);
			if (!string.Equals(candidateName.Name, simpleName, StringComparison.OrdinalIgnoreCase)
				|| !SatisfiesIdentity(candidateName, requestedPkt, requestedVersion))
			{
				return null;
			}

			var loaded = context.LoadFromAssemblyPath(candidatePath);
			Console.Out.WriteLine(
				$"Uno.UI.RemoteControl.Host: loaded '{simpleName}' on demand from '{candidatePath}' " +
				$"(requested Version={requestedVersion?.ToString() ?? "<none>"}, " +
				$"loaded Version={loaded.GetName().Version?.ToString() ?? "<none>"}).");
			return loaded;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine(
				$"Uno.UI.RemoteControl.Host: on-demand load of '{simpleName}' from '{candidatePath}' failed " +
				$"({ex.GetType().Name}: {ex.Message}).");
			return null;
		}
	}

	/// <summary>
	/// Identity guards shared by the bridge and on-demand paths: a signed request's
	/// PublicKeyToken must match the candidate's, and the candidate's version must not
	/// be lower than the requested one (no silent downgrades). Unsigned requests skip
	/// the PKT check — a request without an identity claim cannot demand identity matching.
	/// </summary>
	private static bool SatisfiesIdentity(AssemblyName candidate, byte[]? requestedPkt, Version? requestedVersion)
	{
		if (requestedPkt is { Length: > 0 })
		{
			var candidatePkt = candidate.GetPublicKeyToken();
			if (candidatePkt is null || !candidatePkt.AsSpan().SequenceEqual(requestedPkt))
			{
				return false;
			}
		}

		var candidateVersion = candidate.Version;
		if (requestedVersion is not null && candidateVersion is not null
			&& candidateVersion < requestedVersion)
		{
			return false;
		}

		return true;
	}

	/// <summary>
	/// Emits the total-miss diagnostic: names the same-simple-name variants that ARE
	/// loaded (if any) so the operator does not have to spelunk
	/// <see cref="AssemblyLoadContext.Assemblies"/> themselves.
	/// </summary>
	private static void ReportResolutionMiss(AssemblyLoadContext context, AssemblyName requested)
	{
		var simpleName = requested.Name;
		var requestedVersion = requested.Version;
		var requestedPkt = requested.GetPublicKeyToken();

		var variants = context.Assemblies
			.Where(a => !a.IsDynamic)
			.Select(a => a.GetName())
			.Where(n => string.Equals(n.Name, simpleName, StringComparison.OrdinalIgnoreCase))
			.ToList();

		if (variants.Count > 0)
		{
			var variantsSummary = string.Join(
				", ",
				variants.Select(v => $"Version={v.Version}, PublicKeyToken={FormatPkt(v.GetPublicKeyToken())}"));

			Console.Error.WriteLine(
				$"Uno.UI.RemoteControl.Host: could not resolve '{simpleName}, Version={requestedVersion?.ToString() ?? "<none>"}, " +
				$"PublicKeyToken={FormatPkt(requestedPkt)}'. Candidates with the same simple name are loaded but none is " +
				$"compatible: [{variantsSummary}]. Either the requested version is higher than what the host carries, or the " +
				"PublicKeyToken differs. Action for the add-in's maintainer: align the package version with what the host " +
				"references, or load the conflicting dependency inside a private AssemblyLoadContext owned by the add-in.");
		}
		else
		{
			Console.Error.WriteLine(
				$"Uno.UI.RemoteControl.Host: could not resolve '{simpleName}, Version={requestedVersion?.ToString() ?? "<none>"}, " +
				$"PublicKeyToken={FormatPkt(requestedPkt)}'. No assembly with that simple name is loaded, available from the " +
				$"runtime, or present in the {s_probingDirectories.Length} registered probing director{(s_probingDirectories.Length == 1 ? "y" : "ies")}. " +
				"Action: ensure the assembly is shipped alongside an add-in's primary DLL (in the same directory) or referenced " +
				"by the host's own deps.json. If the assembly belongs to a third-party add-in that isolates its own dependencies, " +
				"load it in a private AssemblyLoadContext on the add-in side.");
		}
	}

	private static string FormatPkt(byte[]? pkt)
		=> pkt is { Length: > 0 } ? Convert.ToHexString(pkt).ToLowerInvariant() : "<none>";
}
