using System;
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
		// Construct one AssemblyDependencyResolver per add-in path, but tolerate
		// per-path failures: AssemblyDependencyResolver's ctor throws when the
		// path is invalid or the corresponding .deps.json is missing/corrupt. If
		// we let that bubble out, a single bad add-in would abort loading of
		// *all* add-ins — regressing the per-add-in failure isolation that the
		// prior Assembly.LoadFrom loop in AssemblyHelper provided. The actual
		// LoadFromAssemblyPath call for the offending add-in still runs in the
		// caller's per-DLL try/catch and will surface there with a meaningful
		// error.
		var resolvers = new List<AssemblyDependencyResolver>();
		foreach (var path in addInPaths)
		{
			try
			{
				resolvers.Add(new AssemblyDependencyResolver(path));
			}
			catch (Exception ex)
			{
				// No ILoggerFactory is available at AddInLoadContext construction
				// time, so emit a stderr breadcrumb. The per-add-in load failure
				// for this path will be logged through AssemblyHelper's existing
				// per-DLL catch.
				Console.Error.WriteLine(
					$"Uno.UI.RemoteControl.Host: AssemblyDependencyResolver construction failed for '{path}' " +
					$"({ex.GetType().Name}: {ex.Message}). Per-add-in dependency probing for this add-in will be skipped.");
			}
		}
		_resolvers = [.. resolvers];
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
			// Skip IsDynamic candidates (reflection-emit / source-generator
			// assemblies could have an auto-generated name that coincidentally
			// matches a real AssemblyRef) and require PublicKeyToken
			// compatibility — same identity-swap guard as the
			// AssemblyLoadContext.Default.Resolving handler in Program.cs.
			var requestedToken = assemblyName.GetPublicKeyToken();
			foreach (var loaded in Default.Assemblies)
			{
				if (loaded.IsDynamic)
				{
					continue;
				}

				var loadedName = loaded.GetName();
				if (!string.Equals(loadedName.Name, name, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				if (requestedToken is { Length: > 0 })
				{
					var loadedToken = loadedName.GetPublicKeyToken();
					if (loadedToken is null || !loadedToken.AsSpan().SequenceEqual(requestedToken))
					{
						continue;
					}
				}

				return loaded;
			}

			// 2. The assembly isn't loaded yet in the default ALC, but the host's
			//    TPA may still know about it (e.g. lazily-loaded framework OOB
			//    packages like System.Text.Encodings.Web). Ask the default ALC to
			//    load it by simple name — this triggers TPA-based resolution and
			//    succeeds for anything the host's deps.json describes. Kiota and
			//    other genuinely add-in-private assemblies aren't in host TPA, so
			//    they fall through to the plugin resolvers below.
			//
			// We catch both FileNotFoundException AND FileLoadException /
			// BadImageFormatException here: TPA can match an assembly by simple
			// name but reject the load on a strong-name / version / image
			// mismatch (exactly the cross-major scenario this class exists to
			// paper over). If the default ALC can't satisfy this name for any
			// of those reasons, the add-in's own deps.json may still carry a
			// usable copy, so we keep falling through rather than bubbling up.
			//
			// We also validate the loaded assembly's PublicKeyToken against the
			// request before returning — same identity-swap guard as step 1
			// and the Default.Resolving handler in Program.cs. Asking by simple
			// name alone could otherwise hand back a same-name-but-different-PKT
			// assembly from TPA, which we'd never silently substitute elsewhere.
			try
			{
				var loaded = Default.LoadFromAssemblyName(new AssemblyName(name));
				if (requestedToken is { Length: > 0 })
				{
					var loadedToken = loaded.GetName().GetPublicKeyToken();
					if (loadedToken is null || !loadedToken.AsSpan().SequenceEqual(requestedToken))
					{
						// PKT mismatch — fall through to the add-in's own resolvers
						// rather than substituting across strong-name identities.
						loaded = null;
					}
				}

				if (loaded is not null)
				{
					return loaded;
				}
			}
			catch (FileNotFoundException) { /* not in host TPA */ }
			catch (FileLoadException) { /* found in TPA but rejected (e.g. strong-name / version mismatch) */ }
			catch (BadImageFormatException) { /* found in TPA but unloadable image */ }
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
