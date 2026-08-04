using System;
using System.Collections.Concurrent;
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
/// assembly may reference both collectible and non-collectible ones), and any assembly already
/// present in the embedded Roslyn's own context is unified to it (an analyzer built against
/// Microsoft.CodeAnalysis 4.x binds to the loaded 5.x, exactly like Roslyn's own loader does).
/// Analyzer files are shadow-copied before loading so the originals never get locked while the
/// IDE or a build rebuilds them.
/// </para>
/// </remarks>
internal sealed class CollectibleAnalyzerAssemblyLoader : IAnalyzerAssemblyLoader
{
	private readonly AssemblyLoadContext _compilerContext;
	private readonly ConcurrentDictionary<string, DirectoryLoadContext> _contexts = new(PathComparer.Comparer);
	private readonly ConcurrentDictionary<string, string> _knownPathsByFileName = new(StringComparer.OrdinalIgnoreCase);
	private static readonly string _shadowCopyRoot = Path.Combine(Path.GetTempPath(), "uno-devserver", "analyzers");

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
		=> _knownPathsByFileName.TryAdd(Path.GetFileName(fullPath), fullPath);

	/// <inheritdoc />
	public Assembly LoadFromPath(string fullPath)
	{
		fullPath = Path.GetFullPath(fullPath);
		var context = _contexts.GetOrAdd(Path.GetDirectoryName(fullPath)!, static (directory, loader) => new DirectoryLoadContext(directory, loader), this);
		return context.LoadShadowCopy(fullPath);
	}

	private static string GetShadowCopyPath(string fullPath)
	{
		// One shadow directory per (path, timestamp): reused while the file is unchanged, naturally
		// re-copied after a rebuild. The original file is never memory-mapped, so it never locks.
		var timestamp = File.GetLastWriteTimeUtc(fullPath).Ticks;
		var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{fullPath}|{timestamp}")))[..16];
		var directory = Path.Combine(_shadowCopyRoot, key);
		var shadowPath = Path.Combine(directory, Path.GetFileName(fullPath));

		if (!File.Exists(shadowPath))
		{
			Directory.CreateDirectory(directory);
			try
			{
				File.Copy(fullPath, shadowPath, overwrite: false);

				// Keep the symbols next to the shadow copy so analyzer stack traces stay resolvable.
				if (Path.ChangeExtension(fullPath, ".pdb") is { } pdb && File.Exists(pdb))
				{
					File.Copy(pdb, Path.ChangeExtension(shadowPath, ".pdb"), overwrite: false);
				}
			}
			catch (IOException) when (File.Exists(shadowPath))
			{
				// Lost a copy race with another thread — the winner's copy is equivalent.
			}
		}

		return shadowPath;
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
			// else it already loaded): mirrors Roslyn's own compiler-context redirect, including
			// binding an analyzer built against an older Microsoft.CodeAnalysis to the loaded one.
			if (_loader._compilerContext.Assemblies.FirstOrDefault(a => string.Equals(a.GetName().Name, simpleName, StringComparison.OrdinalIgnoreCase)) is { } fromCompiler)
			{
				return fromCompiler;
			}

			// Then the analyzer's own directory, then the registered dependency locations.
			// Anything else falls back to the default context (framework assemblies).
			var candidate = Path.Combine(_directory, simpleName + ".dll");
			if (!File.Exists(candidate) && !_loader._knownPathsByFileName.TryGetValue(simpleName + ".dll", out candidate!))
			{
				return null;
			}

			return File.Exists(candidate) ? LoadShadowCopy(candidate) : null;
		}

		protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
		{
			// Deliberate scope boundary: native DLLs are loaded from their original location, NOT
			// shadow-copied (so a rebuild while loaded can fail to replace them on Windows).
			// Native analyzer dependencies are rare and Roslyn's own loaders share the limitation.
			var candidate = Path.Combine(_directory, unmanagedDllName + ".dll");
			return File.Exists(candidate) ? LoadUnmanagedDllFromPath(candidate) : IntPtr.Zero;
		}
	}
}
