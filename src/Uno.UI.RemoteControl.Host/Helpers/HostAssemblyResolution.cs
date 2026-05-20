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
	/// Trusted public key tokens accepted by <see cref="EagerLoadFromDirectory"/>.
	/// Eager-loading an assembly whose PKT does not match one of these emits a
	/// stderr warning (the assembly is still loaded — there is no .NET API to
	/// unload it — but the <see cref="TryBridgeBySimpleName"/> guards prevent
	/// returning a non-Microsoft assembly for a signed Microsoft request).
	/// </summary>
	private static readonly byte[][] s_trustedPkts =
	[
		[0xb0, 0x3f, 0x5f, 0x7f, 0x11, 0xd5, 0x0a, 0x3a], // Microsoft (b03f5f7f11d50a3a)
		[0xcc, 0x7b, 0x13, 0xff, 0xcd, 0x2d, 0xdd, 0x51], // corefx / dotnet (cc7b13ffcd2ddd51)
		[0xad, 0xb9, 0x79, 0x38, 0x29, 0xdd, 0xae, 0x60], // Microsoft.Extensions.* NuGet (adb9793829ddae60)
	];

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
		if (Environment.GetEnvironmentVariable("UNO_DEVSERVER_DISABLE_ADDIN_ALC") == "1")
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
	/// Eager-loads <c>System.*.dll</c> and <c>Microsoft.Extensions.*.dll</c>
	/// from <paramref name="hostDir"/> into <see cref="AssemblyLoadContext.Default"/>.
	/// </summary>
	/// <remarks>
	/// Idempotent: assemblies already present in <see cref="AssemblyLoadContext.Default"/>
	/// (by simple name) are skipped, and individual load failures are
	/// swallowed with a stderr breadcrumb. After a successful load the assembly's
	/// public key token is validated against <see cref="s_trustedPkts"/>; a
	/// mismatch only emits a warning because .NET provides no API to unload
	/// from <see cref="AssemblyLoadContext.Default"/>. The <see cref="TryBridgeBySimpleName"/>
	/// PKT guard prevents such an assembly from being substituted for a signed
	/// Microsoft request later.
	/// </remarks>
	/// <returns>The number of assemblies successfully eager-loaded by this call.</returns>
	internal static int EagerLoadFromDirectory(string hostDir)
	{
		if (string.IsNullOrEmpty(hostDir) || !System.IO.Directory.Exists(hostDir))
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

		// Discover System.* and Microsoft.Extensions.* DLLs in the host directory.
		var candidates =
			System.IO.Directory.EnumerateFiles(hostDir, "System.*.dll")
			.Concat(System.IO.Directory.EnumerateFiles(hostDir, "Microsoft.Extensions.*.dll"));

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
				var asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);

				// DLL-planting defence: if a file matching our System.* /
				// Microsoft.Extensions.* glob is signed with a non-trusted PKT
				// (or unsigned), warn loudly. We cannot unload it, but
				// TryBridgeBySimpleName's PKT symmetry guard means a signed
				// Microsoft request will still refuse to bridge to this
				// assembly.
				var pkt = asm.GetName().GetPublicKeyToken();
				if (!IsTrustedPkt(pkt))
				{
					Console.Error.WriteLine(
						$"Uno.UI.RemoteControl.Host: eager-loaded '{simpleName}' from '{path}' has an " +
						$"untrusted public key token. Cross-boundary type identity for this assembly " +
						"will be refused by the bridge.");
				}

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
				$"Uno.UI.RemoteControl.Host: eager-loaded {loaded} assemblies from '{hostDir}' " +
				$"({failed} failures).");
		}

		return loaded;
	}

	private static bool IsTrustedPkt(byte[]? pkt) =>
		pkt is { Length: > 0 } &&
		s_trustedPkts.Any(t => t.AsSpan().SequenceEqual(pkt));


	/// <summary>
	/// Attempts to satisfy <paramref name="requested"/> by returning an
	/// already-loaded assembly from <paramref name="context"/> whose simple
	/// name matches. Dynamic (reflection-emit) assemblies and assemblies with
	/// an incompatible <see cref="AssemblyName.GetPublicKeyToken"/> are
	/// skipped, preventing silent identity swaps.
	/// </summary>
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

		var requestedToken = requested.GetPublicKeyToken();

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

			// Enforce symmetric PKT policy:
			//   - Unsigned request → only bridge to an unsigned loaded assembly.
			//     Returning a strong-named assembly for an unsigned reference
			//     could silently substitute a different assembly whose name
			//     coincidentally matches.
			//   - Signed request → loaded assembly must carry the same PKT.
			//     Returning a differently-signed assembly would be a
			//     security-relevant identity swap.
			var loadedToken = loadedName.GetPublicKeyToken();
			if (requestedToken is { Length: > 0 })
			{
				if (loadedToken is null || !loadedToken.AsSpan().SequenceEqual(requestedToken))
				{
					continue;
				}
			}
			else
			{
				// Request is unsigned: skip any strong-named loaded assembly.
				if (loadedToken is { Length: > 0 })
				{
					continue;
				}
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
