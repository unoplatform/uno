using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Uno.UI.RemoteControl.Helpers;

/// <summary>
/// Isolated <see cref="AssemblyLoadContext"/> shared by all DevServer add-ins.
/// Resolves each add-in's own dependencies through its <c>.deps.json</c> (via
/// per-add-in <see cref="AssemblyDependencyResolver"/> instances), and bridges
/// host-loaded framework / contract assemblies (anything already present in
/// the default ALC by simple name) so the host and the add-ins share a single
/// <see cref="System.Type"/> identity for cross-boundary types.
/// <para>
/// Add-ins live in the same context as each other, so types defined in one
/// add-in (e.g. <c>Uno.Licensing.Sdk.Contracts</c>) can be consumed by
/// another (e.g. <c>Uno.UI.App.Mcp.Server</c>) — preserving the existing
/// behaviour of <see cref="Assembly.LoadFrom(string)"/>.
/// </para>
/// </summary>
internal sealed class AddInLoadContext : AssemblyLoadContext
{
	private readonly AssemblyDependencyResolver[] _resolvers;

	public AddInLoadContext(IEnumerable<string> addInPaths)
		: base(name: "AddIns", isCollectible: false)
	{
		_resolvers = addInPaths.Select(p => new AssemblyDependencyResolver(p)).ToArray();
	}

	protected override Assembly? Load(AssemblyName assemblyName)
	{
		var name = assemblyName.Name;

		if (name is not null)
		{
			// 1. If the host has an assembly with this simple name already loaded
			//    in the default ALC, return that exact instance. This:
			//     - preserves Type identity for contract types
			//       (IServiceCollection, IHostedService, ILogger, IConfiguration,
			//       ...) so the host's reflection-based add-in registration
			//       matches ctors,
			//     - and bridges cross-version AssemblyRefs without going through
			//       the default ALC's strict TPA binder. For example, Kiota's
			//       net8.0 binary references System.Text.Encodings.Web v8.0.0.0;
			//       the host has v9 or v10 loaded; returning the host's instance
			//       directly satisfies the v8 ref within this ALC's lax
			//       simple-name lookup.
			foreach (var loaded in Default.Assemblies)
			{
				if (string.Equals(loaded.GetName().Name, name, System.StringComparison.OrdinalIgnoreCase))
				{
					return loaded;
				}
			}

			// 2. The assembly isn't loaded yet in the default ALC, but the host's
			//    TPA may still know about it (e.g. lazily-loaded framework OOB
			//    packages like System.Text.Encodings.Web). Ask the default ALC to
			//    load it by simple name — this triggers TPA-based resolution and
			//    succeeds for anything the host's deps.json describes. Kiota and
			//    other genuinely add-in-private assemblies aren't in host TPA, so
			//    this safely throws FileNotFoundException for them and we fall
			//    through to the plugin resolvers below.
			try
			{
				return Default.LoadFromAssemblyName(new AssemblyName(name));
			}
			catch (FileNotFoundException)
			{
				// Not in host TPA — try plugin resolvers.
			}
		}

		// 3. Plugin-private dependency — try each registered add-in's deps.json.
		//    First match wins so add-ins can share private libraries (e.g. one
		//    add-in's Contracts dll referenced by another) without duplication.
		foreach (var resolver in _resolvers)
		{
			var path = resolver.ResolveAssemblyToPath(assemblyName);
			if (path is not null)
			{
				return LoadFromAssemblyPath(path);
			}
		}

		return null;
	}

	protected override nint LoadUnmanagedDll(string unmanagedDllName)
	{
		foreach (var resolver in _resolvers)
		{
			var path = resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
			if (path is not null)
			{
				return LoadUnmanagedDllFromPath(path);
			}
		}

		return nint.Zero;
	}
}
