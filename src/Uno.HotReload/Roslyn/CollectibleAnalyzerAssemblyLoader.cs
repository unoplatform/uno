using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Uno.HotReload.Utils;

namespace Uno.HotReload.Roslyn;

/// <summary>
/// An <see cref="IAnalyzerAssemblyLoader"/> that loads analyzers into COLLECTIBLE
/// <see cref="AssemblyLoadContext"/>s (one per analyzer directory), delegating the
/// Microsoft.CodeAnalysis.* references to the context hosting the embedded Roslyn.
/// </summary>
/// <remarks>
/// <para>
/// Roslyn 5.x made its analyzer load contexts non-collectible (4.x created them with
/// <c>isCollectible: true</c>). The dev-server loads its hot-reload processors — embedded
/// Roslyn included — in a COLLECTIBLE per-application <see cref="AssemblyLoadContext"/> (so a
/// disconnected app's processors can unload). The runtime forbids a non-collectible assembly
/// from referencing a collectible one, so under Roslyn 5.x every analyzer load fails with
/// <see cref="NotSupportedException"/> ("A non-collectible assembly may not reference a
/// collectible assembly") the moment its Microsoft.CodeAnalysis reference is bound — and
/// <c>AnalyzerFileReference.GetGenerators</c> silently returns zero generators.
/// </para>
/// <para>
/// This loader restores the 4.x semantics: analyzer contexts are collectible (a collectible
/// assembly may reference both collectible and non-collectible ones), and any assembly the
/// embedded Roslyn's own context can provide is unified to it (an analyzer built against
/// Microsoft.CodeAnalysis 4.x binds to the loaded 5.x, exactly like Roslyn's own loader does).
/// Analyzer files are shadow-copied before loading so the originals never get locked while the
/// IDE or a build rebuilds them.
/// </para>
/// </remarks>
internal sealed class CollectibleAnalyzerAssemblyLoader : IAnalyzerAssemblyLoader
{
	private readonly AssemblyLoadContext _compilerContext;
	private readonly ConcurrentDictionary<string, DirectoryLoadContext> _contexts = new(PathComparer.Comparer);
	private readonly ConcurrentDictionary<string, ImmutableHashSet<string>> _knownPathsByFileName = new(StringComparer.OrdinalIgnoreCase);
	private static readonly string _shadowCopyRoot = Path.Join(Path.GetTempPath(), "uno-devserver", "analyzers");

	public CollectibleAnalyzerAssemblyLoader()
		: this(AssemblyLoadContext.GetLoadContext(typeof(Compilation).Assembly)
			?? throw new InvalidOperationException("Unable to determine the embedded Roslyn's AssemblyLoadContext."))
	{
	}

	internal CollectibleAnalyzerAssemblyLoader(AssemblyLoadContext compilerContext)
		=> _compilerContext = compilerContext;

	/// <inheritdoc />
	public void AddDependencyLocation(string fullPath)
		// Same-directory sibling assemblies resolve through the directory probe; out-of-directory
		// dependencies (rare, but AnalyzerFileReference registers them) are indexed by file name.
		// Distinct projects may register distinct copies under the same file name (e.g. two
		// versions of the same analyzer package), so every location is retained and the best
		// match for the requested identity is picked at resolution time.
		=> _knownPathsByFileName.AddOrUpdate(
			Path.GetFileName(fullPath),
			static (_, path) => ImmutableHashSet.Create(PathComparer.PathEqualityComparer, path),
			static (_, paths, path) => paths.Add(path),
			fullPath);

	/// <inheritdoc />
	public Assembly LoadFromPath(string fullPath)
	{
		fullPath = Path.GetFullPath(fullPath);
		var context = _contexts.GetOrAdd(Path.GetDirectoryName(fullPath)!, static (directory, loader) => new DirectoryLoadContext(directory, loader), this);
		return context.LoadShadowCopy(fullPath);
	}

	/// <summary>
	/// Picks the registered dependency location that best satisfies <paramref name="requested"/>,
	/// mirroring Roslyn's own loader: an exact version match wins, otherwise the highest version
	/// whose identity is compatible — never just "whichever project registered that file name
	/// first". Returns <c>null</c> when nothing usable was registered.
	/// </summary>
	private string? GetBestKnownPath(string simpleName, AssemblyName requested)
	{
		if (!_knownPathsByFileName.TryGetValue(simpleName + ".dll", out var candidates))
		{
			return null;
		}

		if (candidates.Count == 1)
		{
			// Single registered location: load it by name like the same-directory probe does
			// (identity inspection only matters when there are alternatives to pick between).
			var single = candidates.First();
			return File.Exists(single) ? single : null;
		}

		var requestedToken = requested.GetPublicKeyToken();
		string? bestPath = null;
		Version? bestVersion = null;
		foreach (var path in candidates.OrderBy(static p => p, StringComparer.Ordinal))
		{
			AssemblyName candidate;
			try
			{
				candidate = AssemblyName.GetAssemblyName(path);
			}
			catch (Exception e) when (e is IOException or BadImageFormatException)
			{
				continue; // Deleted since it was registered, or not a loadable assembly.
			}

			if (!string.Equals(candidate.Name, simpleName, StringComparison.OrdinalIgnoreCase)
				|| (requestedToken is { Length: > 0 } && !requestedToken.AsSpan().SequenceEqual(candidate.GetPublicKeyToken().AsSpan())))
			{
				continue;
			}

			var candidateVersion = candidate.Version ?? new Version(0, 0);
			if (candidateVersion == requested.Version)
			{
				return path;
			}

			if (bestVersion is null || candidateVersion > bestVersion)
			{
				bestPath = path;
				bestVersion = candidateVersion;
			}
		}

		return bestPath;
	}

	private static string GetShadowCopyPath(string fullPath)
	{
		// One shadow directory per (path, timestamp): reused while the file is unchanged, naturally
		// re-copied after a rebuild. The original file is never memory-mapped, so it never locks.
		var timestamp = File.GetLastWriteTimeUtc(fullPath).Ticks;
		var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{fullPath}|{timestamp}")))[..16];
		var directory = Path.Join(_shadowCopyRoot, key);
		var shadowPath = Path.Join(directory, Path.GetFileName(fullPath));

		if (!File.Exists(shadowPath))
		{
			Directory.CreateDirectory(directory);

			// Keep the symbols next to the shadow copy so analyzer stack traces stay resolvable.
			// Published before the assembly: once the .dll is visible the bundle is complete.
			if (Path.ChangeExtension(fullPath, ".pdb") is { } pdb && File.Exists(pdb))
			{
				Publish(pdb, Path.ChangeExtension(shadowPath, ".pdb"));
			}

			Publish(fullPath, shadowPath);
		}

		return shadowPath;
	}

	/// <summary>
	/// Copies <paramref name="sourcePath"/> to <paramref name="targetPath"/> so that the target
	/// only ever becomes visible COMPLETE: the bytes land in a unique staging file first, then
	/// are published with an atomic rename. A bare <c>File.Copy</c> creates the destination
	/// before the content is through — and since the shadow key is deterministic, a concurrent
	/// load (another thread, or another dev-server process sharing the same temp root) treating
	/// existence as completion would load a partial PE; a process killed mid-copy would even
	/// poison the key until the source file's timestamp changes.
	/// </summary>
	private static void Publish(string sourcePath, string targetPath)
	{
		var staging = $"{targetPath}.{Guid.NewGuid():N}.staging";
		File.Copy(sourcePath, staging);
		try
		{
			File.Move(staging, targetPath);
		}
		catch (IOException) when (File.Exists(targetPath))
		{
			// Lost the publish race with another thread or process — the winner's copy is
			// equivalent (the key embeds the source path and timestamp).
			try
			{
				File.Delete(staging);
			}
			catch (IOException)
			{
				// Best effort: an orphaned staging file in the keyed directory is harmless.
			}
		}
	}

	private sealed class DirectoryLoadContext : AssemblyLoadContext
	{
		private readonly string _directory;
		private readonly CollectibleAnalyzerAssemblyLoader _loader;

		public DirectoryLoadContext(string directory, CollectibleAnalyzerAssemblyLoader loader)
			// Collectible is the point of this type: it may reference the (collectible)
			// per-application context hosting the embedded Roslyn — see the type remarks.
			: base($"UnoHotReloadAnalyzers({directory})", isCollectible: true)
		{
			_directory = directory;
			_loader = loader;
		}

		public Assembly LoadShadowCopy(string fullPath)
			=> LoadFromAssemblyPath(GetShadowCopyPath(fullPath));

		protected override Assembly? Load(AssemblyName assemblyName)
		{
			if (assemblyName.Name is not { Length: > 0 } simpleName || simpleName.EndsWith(".resources", StringComparison.Ordinal))
			{
				return null;
			}

			// Unify to the embedded Roslyn's context first (Microsoft.CodeAnalysis.* and anything
			// else it can provide): mirrors Roslyn's own compiler-context redirect, including
			// binding an analyzer built against an older Microsoft.CodeAnalysis to the loaded one.
			// The resolution is DELEGATED to that context (not probed against its already-loaded
			// assemblies) so its own load policy can materialize dependencies it has not loaded
			// yet — probing would fall through and load a split-identity duplicate below — and so
			// it stays correct when assemblies keep appearing at runtime (e.g. NuGet packages
			// added to the application through hot reload).
			try
			{
				return _loader._compilerContext.LoadFromAssemblyName(assemblyName);
			}
			catch
			{
				// Not something the compiler's context can provide (same bare catch as Roslyn's
				// loader) — resolve it as an analyzer-local dependency below.
			}

			// Then the analyzer's own directory, then the registered dependency locations.
			// Anything else returns null: the runtime then probes the default context and
			// raises this context's Resolving event (framework assemblies land there).
			var candidate = Path.Join(_directory, simpleName + ".dll");
			if (File.Exists(candidate))
			{
				return LoadShadowCopy(candidate);
			}

			return _loader.GetBestKnownPath(simpleName, assemblyName) is { } known ? LoadShadowCopy(known) : null;
		}

		protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
		{
			// Deliberate scope boundary: native DLLs are loaded from their original location, NOT
			// shadow-copied (so a rebuild while loaded can fail to replace them on Windows).
			// Native analyzer dependencies are rare and Roslyn's own loaders share the limitation.
			var candidate = Path.Join(_directory, unmanagedDllName + ".dll");
			return File.Exists(candidate) ? LoadUnmanagedDllFromPath(candidate) : IntPtr.Zero;
		}
	}
}
