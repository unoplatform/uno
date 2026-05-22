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
				TryBridgeBySimpleName(ctx, req);
		}

		var resolvedHostDir = hostDir
			?? System.IO.Path.GetDirectoryName(typeof(HostAssemblyResolution).Assembly.Location)
			?? AppContext.BaseDirectory;

		if (string.IsNullOrEmpty(resolvedHostDir))
		{
			return;
		}

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
					"for this assembly may still throw FileNotFoundException at runtime.");
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
	/// emitted so operators can observe which requests are being bridged. On a miss,
	/// a warning is emitted that lists the currently-loaded variants of the same simple
	/// name (if any) to give the operator a concrete starting point for diagnosis.
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

		// Collect every same-simple-name variant we observe so a miss can describe what
		// IS loaded instead of just saying "nothing matches" (much more actionable).
		List<AssemblyName>? variants = null;

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

			variants ??= [];
			variants.Add(loadedName);

			// Asymmetric PKT match: when the request specifies an identity (a non-empty
			// PublicKeyToken — typical for signed framework AssemblyRefs like
			// System.Text.Encodings.Web), refuse a loaded candidate whose token differs
			// or is absent. When the request is unsigned (no PKT — typical for add-in
			// contracts), skip the PKT check entirely. This is identity validation, not
			// an allow-list: signed callers get the assembly they asked for, unsigned
			// callers get whatever simple-name match is loaded.
			if (requestedPkt is { Length: > 0 })
			{
				var loadedPkt = loadedName.GetPublicKeyToken();
				if (loadedPkt is null || !loadedPkt.AsSpan().SequenceEqual(requestedPkt))
				{
					continue;
				}
			}

			// Version guard: refuse to bridge when the loaded version is strictly lower
			// than what was requested. That would be a silent downgrade — the caller
			// asked for vN but gets v(N-1).
			var loadedVersion = loadedName.Version;
			if (requestedVersion is not null && loadedVersion is not null
				&& loadedVersion < requestedVersion)
			{
				continue;
			}

			// Successful bridge. Log if the bridge involved a non-trivial version
			// substitution so the operator can correlate runtime warnings with the
			// resolution events.
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

		// Miss — no compatible candidate. Emit a warning that names what IS loaded so the
		// operator does not have to spelunk Default.Assemblies themselves.
		if (variants is { Count: > 0 })
		{
			var variantsSummary = string.Join(
				", ",
				variants.Select(v => $"Version={v.Version}, PublicKeyToken={FormatPkt(v.GetPublicKeyToken())}"));

			Console.Error.WriteLine(
				$"Uno.UI.RemoteControl.Host: could not bridge '{simpleName}, Version={requestedVersion?.ToString() ?? "<none>"}, " +
				$"PublicKeyToken={FormatPkt(requestedPkt)}'. Candidates with the same simple name are loaded but none is " +
				$"compatible: [{variantsSummary}]. Either the requested version is higher than what the host carries, or the " +
				"PublicKeyToken differs. Action for the add-in's maintainer: align the package version with what the host " +
				"references, or load the conflicting dependency inside a private AssemblyLoadContext owned by the add-in.");
		}
		else
		{
			Console.Error.WriteLine(
				$"Uno.UI.RemoteControl.Host: could not bridge '{simpleName}, Version={requestedVersion?.ToString() ?? "<none>"}, " +
				$"PublicKeyToken={FormatPkt(requestedPkt)}'. No assembly with that simple name is currently loaded in the " +
				"default AssemblyLoadContext. Action: ensure the assembly is shipped alongside an add-in's primary DLL " +
				"(in the same directory) or referenced by the host's own deps.json. If the assembly belongs to a third-party " +
				"add-in that isolates its own dependencies, load it in a private AssemblyLoadContext on the add-in side.");
		}

		return null;
	}

	private static string FormatPkt(byte[]? pkt)
		=> pkt is { Length: > 0 } ? Convert.ToHexString(pkt).ToLowerInvariant() : "<none>";
}
