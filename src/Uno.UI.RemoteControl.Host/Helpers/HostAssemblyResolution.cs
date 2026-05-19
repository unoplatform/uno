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
