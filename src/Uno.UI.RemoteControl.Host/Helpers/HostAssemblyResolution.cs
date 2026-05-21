using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Uno.UI.RemoteControl.Host.Helpers;

/// <summary>
/// Shared assembly-bridging policy for the DevServer host and its add-ins.
/// Centralises the logic that returns an already-loaded assembly from the
/// default ALC when a simple-name match is found, so that host and add-ins
/// share a single <see cref="System.Type"/> identity for cross-boundary types.
/// </summary>
internal static class HostAssemblyResolution
{
	/// <summary>
	/// <see langword="true"/> when <c>UNO_DEVSERVER_DISABLE_ADDIN_ALC=1</c> is set.
	/// Re-read on every access so that tests that set the variable before calling
	/// can observe the updated value without a process restart.
	/// </summary>
	internal static bool IsKillSwitchActive =>
		Environment.GetEnvironmentVariable("UNO_DEVSERVER_DISABLE_ADDIN_ALC") == "1";


	/// <summary>
	/// 0 = handler not subscribed, 1 = handler subscribed.  Written once via
	/// <see cref="System.Threading.Interlocked.CompareExchange(ref int, int, int)"/>
	/// so <see cref="Install"/> can subscribe the <c>Resolving</c> handler exactly
	/// once even when called from multiple threads or from test fixtures that
	/// share the process-wide <see cref="AssemblyLoadContext.Default"/>.
	/// </summary>
	private static int s_handlerInstalled;


	/// <summary>
	/// Installs the cross-major-version assembly-resolution safety net on
	/// <see cref="AssemblyLoadContext.Default"/> and eager-loads the
	/// shared-framework OOB assemblies most prone to version-mismatch requests.
	/// </summary>
	/// <remarks>
	/// Idempotent: calling this method more than once (e.g. from unit tests that
	/// share the same process) registers the <c>Resolving</c> handler exactly once.
	/// The eager-load pass is also safe to repeat because it filters out
	/// already-loaded assemblies by simple name.
	/// <para>
	/// The kill switch <c>UNO_DEVSERVER_DISABLE_ADDIN_ALC=1</c> short-circuits
	/// this method entirely, leaving <see cref="AssemblyLoadContext.Default"/>
	/// untouched.
	/// </para>
	/// </remarks>
	/// <param name="hostDir">
	/// Optional override for the directory from which DLLs are eager-loaded.
	/// When <see langword="null"/> the directory of this assembly is used.
	/// </param>
	internal static void Install(string? hostDir = null)
	{
		// Operator-facing kill switch: disables both the Resolving handler and
		// the eager-load pass. Useful to bisect production issues that may have
		// been introduced by the add-in isolation work.
		if (IsKillSwitchActive)
		{
			return;
		}

		// Subscribe the Resolving handler exactly once (lock-free CAS). The
		// flag protects ONLY the subscription; the eager-load pass is gated
		// independently by the per-simple-name dedup inside the loop so it
		// remains safe to run on repeated Install() calls.
		if (System.Threading.Interlocked.CompareExchange(ref s_handlerInstalled, 1, 0) == 0)
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

		// Best-effort eager-load: IO / reflection errors must never crash the
		// host startup path. The handler subscribed above remains the primary
		// safety net even when this fails.
		try
		{
			EagerLoadFromDirectory(resolvedHostDir);
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine(
				$"Uno.UI.RemoteControl.Host: eager-load from '{resolvedHostDir}' failed " +
				$"({ex.GetType().Name}: {ex.Message}).");
		}
	}

	/// <summary>
	/// File-name globs used by <see cref="EagerLoadFromDirectory"/> to pre-load
	/// the OOB shared-framework assemblies most prone to cross-major-version
	/// <c>AssemblyRef</c> mismatches. Pre-loading them into
	/// <see cref="AssemblyLoadContext.Default"/> lets
	/// <see cref="TryBridgeBySimpleName"/> serve them to any <see cref="AddInLoadContext"/>
	/// that asks for a different major version of the same simple name.
	/// </summary>
	internal static readonly System.Collections.Immutable.ImmutableArray<string> DefaultEagerLoadPatterns =
	[
		"System.*.dll",
		"Microsoft.Extensions.*.dll",
	];

	/// <summary>
	/// Eager-loads DLLs matching <see cref="DefaultEagerLoadPatterns"/> from
	/// <paramref name="dir"/> into <see cref="AssemblyLoadContext.Default"/>.
	/// </summary>
	/// <remarks>
	/// Idempotent: assemblies already present in <see cref="AssemblyLoadContext.Default"/>
	/// (by simple name) are skipped, and individual load failures are swallowed
	/// with a stderr breadcrumb. Once loaded, <see cref="TryBridgeBySimpleName"/>
	/// can return these assemblies to any <c>AddInLoadContext</c> that requests
	/// them by name, preserving a single <see cref="System.Type"/> identity
	/// across the host/add-in boundary.
	/// </remarks>
	/// <returns>The number of assemblies successfully eager-loaded by this call.</returns>
	internal static int EagerLoadFromDirectory(string dir)
	{
		if (string.IsNullOrEmpty(dir) || !System.IO.Directory.Exists(dir))
		{
			return 0;
		}

		// Build a set of simple names already loaded so we can skip duplicates.
		var alreadyLoaded = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
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
					$"Uno.UI.RemoteControl.Host: eager-load of '{simpleName}' failed " +
					$"({ex.GetType().Name}: {ex.Message}). " +
					"Cross-major-version AssemblyRefs for this assembly may still throw " +
					"FileNotFoundException at runtime.");
			}
		}

		if (loaded + failed > 0)
		{
			Console.Error.WriteLine(
				$"Uno.UI.RemoteControl.Host: eager-loaded {loaded} assemblies from '{dir}' " +
				$"({failed} failures).");
		}

		return loaded;
	}

	/// <summary>
	/// Attempts to satisfy <paramref name="requested"/> by returning an
	/// already-loaded assembly from <paramref name="context"/> whose simple
	/// name matches. Dynamic (reflection-emit) assemblies are skipped so
	/// runtime-generated names that coincidentally collide with a real
	/// <c>AssemblyRef</c> do not silently substitute the wrong type; and a
	/// loaded assembly whose <see cref="System.Version"/> is strictly lower
	/// than the requested version is rejected to prevent silent downgrades.
	/// </summary>
	/// <remarks>
	/// No public-key-token comparison is performed: the host's add-in
	/// scenario is unsigned-only on both sides (neither add-in DLLs nor
	/// the contract assemblies they ship carry a strong name), so a PKT
	/// check would never trigger in practice. Adding one back means
	/// re-introducing a guard for a scenario the host never sees.
	/// </remarks>
	/// <returns>
	/// The matching <see cref="Assembly"/> instance, or <c>null</c> when no
	/// compatible candidate exists in <paramref name="context"/>.
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

		foreach (var loaded in context.Assemblies)
		{
			// Skip reflection-emit / source-generator assemblies: their
			// auto-generated names could coincidentally match a real AssemblyRef
			// and we'd silently substitute them, causing downstream
			// TypeLoadException / MissingMethodException.
			if (loaded.IsDynamic)
			{
				continue;
			}

			var loadedName = loaded.GetName();
			if (!string.Equals(loadedName.Name, simpleName, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			// Version guard: refuse to bridge when the loaded version is strictly
			// lower than what was requested. That would be a silent downgrade —
			// the caller asked for vN but gets v(N-1).
			var requestedVersion = requested.Version;
			var loadedVersion = loadedName.Version;
			if (requestedVersion is not null && loadedVersion is not null
				&& loadedVersion < requestedVersion)
			{
				continue;
			}

			return loaded;
		}

		return null;
	}
}
