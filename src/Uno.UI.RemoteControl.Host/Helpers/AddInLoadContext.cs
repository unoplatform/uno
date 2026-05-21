using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;

namespace Uno.UI.RemoteControl.Host.Helpers;

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
	/// <summary>
	/// Per-async-flow re-entrancy guard for step 2 (delegating to
	/// <see cref="AssemblyLoadContext.Default"/>). If the host's
	/// <c>Default.Resolving</c> handler ends up routing a probe back into this
	/// context — directly or indirectly — we must not recurse into
	/// <see cref="AssemblyLoadContext.LoadFromAssemblyName"/> on
	/// <see cref="Default"/> again, otherwise we risk a StackOverflowException
	/// or an opaque "already loading" failure. Skipping step 2 in that case
	/// lets the call fall through to the add-in's private resolvers.
	/// </summary>
	private static readonly AsyncLocal<bool> s_inDefaultLoad = new();

	private readonly AssemblyDependencyResolver[] _resolvers;

	/// <summary>
	/// Snapshot of every <c>.dll</c> co-located with a registered add-in, keyed by
	/// simple name. Built once at construction time so step 4 of <see cref="Load"/>
	/// is an O(1) dictionary lookup instead of a directory scan on every miss, and
	/// so the resolution result is deterministic: when two add-in directories ship
	/// the same simple name, the first one in the registration order wins —
	/// independent of the host file-system enumeration order.
	/// </summary>
	private readonly Dictionary<string, string> _probeMap;

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
		var orderedDirectories = new List<string>();
		var seenDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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
					$"({ex}). Per-add-in dependency probing for this add-in will be skipped.");
			}

			// Remember the add-in's directory whether or not its dependency
			// resolver was constructed successfully: step 4 still scans on the
			// file system, which is independent of any deps.json. Preserve the
			// caller-supplied registration order so first-add-in-wins is stable.
			var directory = Path.GetDirectoryName(path);
			if (!string.IsNullOrEmpty(directory) && seenDirectories.Add(directory))
			{
				orderedDirectories.Add(directory);
			}
		}

		_resolvers = [.. resolvers];

		// Snapshot every .dll co-located with the registered add-ins exactly once.
		// First match wins: a DLL appearing in the first registered directory is
		// not shadowed by a same-named file in a later directory.
		_probeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (var directory in orderedDirectories)
		{
			IEnumerable<string> dllPaths;
			try
			{
				dllPaths = Directory.EnumerateFiles(directory, "*.dll");
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				// Best-effort: an inaccessible add-in directory must not abort the
				// whole probe snapshot; the per-add-in load will surface its own
				// failure via AssemblyHelper's catch.
				Console.Error.WriteLine(
					$"Uno.UI.RemoteControl.Host: cannot enumerate add-in directory '{directory}' " +
					$"for the file-system probe snapshot ({ex.GetType().Name}: {ex.Message}).");
				continue;
			}

			foreach (var dllPath in dllPaths)
			{
				var simpleName = Path.GetFileNameWithoutExtension(dllPath);
				if (!string.IsNullOrEmpty(simpleName))
				{
					// First-add-in-wins: TryAdd preserves the registration-order
					// precedence so a later add-in can't silently shadow a contract
					// shipped by an earlier one.
					_probeMap.TryAdd(simpleName, dllPath);
				}
			}
		}
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
			var bridged = HostAssemblyResolution.TryBridgeBySimpleName(Default, assemblyName);
			if (bridged is not null)
			{
				return bridged;
			}

			// 2. The assembly isn't loaded yet in the default ALC, but the host's
			//    TPA may still know about it (e.g. lazily-loaded framework OOB
			//    packages like System.Text.Encodings.Web). Ask the default ALC to
			//    load it by simple name — this triggers TPA-based resolution and
			//    succeeds for anything the host's deps.json describes. Kiota and
			//    other genuinely add-in-private assemblies aren't in host TPA, so
			//    they fall through to the plugin resolvers below.
			//
			// We catch FileNotFoundException, FileLoadException and
			// BadImageFormatException because TPA can match an assembly by
			// simple name but reject the load on a version / image mismatch
			// (exactly the cross-major scenario this class exists to paper
			// over). When that happens we keep falling through to the
			// add-in-private resolvers and the file-system probe.
			//
			// Guard against re-entrancy: if Default's Resolving handler (or a
			// downstream load) cycles back into this method, skip step 2 and
			// fall through to the add-in's private resolvers.
			if (!s_inDefaultLoad.Value)
			{
				s_inDefaultLoad.Value = true;
				try
				{
					// We request by simple name + culture only so the host's TPA
					// can pick whichever copy it shipped, regardless of the add-in's
					// compiled-against version. The only post-hoc check is the
					// version-downgrade guard below.
					Assembly? loaded = Default.LoadFromAssemblyName(new AssemblyName(name) { CultureInfo = assemblyName.CultureInfo });

					// Version guard: mirrors TryBridgeBySimpleName — refuse to serve a
					// version lower than what the add-in requested.
					var requestedVersion = assemblyName.Version;
					var loadedVersion = loaded.GetName().Version;
					if (requestedVersion is not null && loadedVersion is not null
						&& loadedVersion < requestedVersion)
					{
						loaded = null;
					}

					if (loaded is not null)
					{
						return loaded;
					}
				}
				catch (Exception ex) when (ex is FileNotFoundException or FileLoadException or BadImageFormatException)
				{
					/* not in host TPA, or found but rejected (version / image mismatch) */
				}
				finally
				{
					s_inDefaultLoad.Value = false;
				}
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

		// 4. File-system probe across the snapshot of DLLs co-located with the
		//    registered add-ins. AssemblyDependencyResolver only consults
		//    .deps.json
		//    (https://learn.microsoft.com/en-us/dotnet/api/system.runtime.loader.assemblydependencyresolver):
		//    when a contract assembly is physically present in an add-in's directory
		//    but absent from every add-in's deps.json (a transitive contract that
		//    isn't surfaced as a runtime asset by the consuming packages), the first
		//    three steps all return null and the runtime would throw
		//    FileNotFoundException. This step mirrors the implicit directory probing
		//    that Assembly.LoadFrom used to perform before add-in isolation was
		//    introduced. Loading the assembly here keeps it inside this shared
		//    AddInLoadContext, so every add-in gets the same Type identity and DI
		//    resolution works across the add-in boundary.
		if (name is null || !IsValidSimpleName(name))
		{
			return null;
		}

		if (!_probeMap.TryGetValue(name, out var candidatePath))
		{
			return null;
		}

		// Symmetric downgrade guard with steps 1 and 2: refuse a candidate whose
		// version is strictly lower than what the add-in requested. Reading the
		// metadata is cheap relative to LoadFromAssemblyPath and avoids returning
		// a silently-older copy that would later throw MissingMethodException at
		// the first cross-assembly call.
		var probeRequestedVersion = assemblyName.Version;
		if (probeRequestedVersion is not null)
		{
			Version? candidateVersion;
			try
			{
				candidateVersion = AssemblyName.GetAssemblyName(candidatePath).Version;
			}
			catch (Exception ex) when (ex is FileNotFoundException or FileLoadException or BadImageFormatException or IOException)
			{
				// Candidate vanished or is unreadable since the snapshot was taken;
				// fall through to a clean null so the runtime surfaces FNFE.
				Console.Error.WriteLine(
					$"Uno.UI.RemoteControl.Host: file-system probe could not read metadata for '{candidatePath}' " +
					$"({ex.GetType().Name}: {ex.Message}).");
				return null;
			}

			if (candidateVersion is not null && candidateVersion < probeRequestedVersion)
			{
				return null;
			}
		}

		try
		{
			var loaded = LoadFromAssemblyPath(candidatePath);
			Console.Error.WriteLine(
				$"Uno.UI.RemoteControl.Host: file-system probe loaded '{name}' from '{candidatePath}'.");
			return loaded;
		}
		catch (Exception ex) when (ex is FileNotFoundException or FileLoadException or BadImageFormatException)
		{
			// A malformed or partial DLL on disk must not abort the resolution
			// chain — returning null lets the runtime surface a clean
			// FileNotFoundException at the original AssemblyRef site.
			Console.Error.WriteLine(
				$"Uno.UI.RemoteControl.Host: file-system probe failed to load '{candidatePath}' " +
				$"({ex.GetType().Name}: {ex.Message}).");
			return null;
		}
	}

	/// <summary>
	/// Conservative filter to keep step 4 from probing names that would escape
	/// the registered add-in directories. Add-in metadata-driven AssemblyRefs
	/// never contain path separators, so any such name can be rejected.
	/// </summary>
	private static bool IsValidSimpleName(string name)
		=> name.IndexOfAny(s_pathSeparators) < 0;

	private static readonly char[] s_pathSeparators = ['/', '\\'];

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
