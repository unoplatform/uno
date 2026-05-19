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
	/// 0 = not installed, 1 = installed.  Written once via
	/// <see cref="System.Threading.Interlocked.CompareExchange(ref int, int, int)"/>
	/// so <see cref="Install"/> is safe to call from multiple threads or from
	/// test fixtures that share the process-wide <see cref="AssemblyLoadContext.Default"/>.
	/// </summary>
	private static int s_installed;

	/// <summary>
	/// Installs the cross-major-version assembly-resolution safety net on
	/// <see cref="AssemblyLoadContext.Default"/> and eager-loads the
	/// shared-framework OOB assemblies most prone to version-mismatch requests.
	/// </summary>
	/// <remarks>
	/// Idempotent: calling this method more than once (e.g. from unit tests that
	/// share the same process) registers the <c>Resolving</c> handler exactly once.
	/// </remarks>
	/// <param name="hostDir">
	/// Optional override for the directory from which DLLs are eager-loaded.
	/// When <see langword="null"/> the directory of this assembly is used.
	/// </param>
	internal static void Install(string? hostDir = null)
	{
		// CAS from 0 → 1: if the exchange returns anything other than 0 another
		// caller already ran Install(), so bail out immediately (lock-free).
		if (System.Threading.Interlocked.CompareExchange(ref s_installed, 1, 0) != 0)
		{
			return;
		}

		AssemblyLoadContext.Default.Resolving += static (ctx, req) =>
			TryBridgeBySimpleName(ctx, req);

		var resolvedHostDir = hostDir
			?? System.IO.Path.GetDirectoryName(typeof(HostAssemblyResolution).Assembly.Location)
			?? AppContext.BaseDirectory;

		if (string.IsNullOrEmpty(resolvedHostDir))
		{
			return;
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
			System.IO.Directory.EnumerateFiles(resolvedHostDir, "System.*.dll")
			.Concat(System.IO.Directory.EnumerateFiles(resolvedHostDir, "Microsoft.Extensions.*.dll"));

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
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine(
					$"Uno.UI.RemoteControl.Host: eager-load of '{simpleName}' failed " +
					$"({ex.GetType().Name}: {ex.Message}). " +
					"Cross-major-version AssemblyRefs for this assembly may still throw " +
					"FileNotFoundException at runtime.");
			}
		}
	}


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
