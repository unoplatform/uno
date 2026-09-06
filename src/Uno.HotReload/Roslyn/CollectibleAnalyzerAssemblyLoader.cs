using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Uno.HotReload.Tracking;
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
	private readonly IReporter? _reporter;
	private readonly ConcurrentDictionary<string, DirectoryLoadContext> _contexts = new(PathComparer.Comparer);
	private readonly ConcurrentDictionary<string, ImmutableHashSet<string>> _knownPathsByFileName = new(StringComparer.OrdinalIgnoreCase);
	private static readonly SearchValues<char> _invalidSimpleNameChars = SearchValues.Create(Path.GetInvalidFileNameChars());
	private static readonly Lazy<string> _shadowCopyRoot = new(CreateShadowCopyRoot);

	public CollectibleAnalyzerAssemblyLoader(IReporter? reporter = null)
		: this(
			AssemblyLoadContext.GetLoadContext(typeof(Compilation).Assembly)
				?? throw new InvalidOperationException("Unable to determine the embedded Roslyn's AssemblyLoadContext."),
			reporter)
	{
	}

	internal CollectibleAnalyzerAssemblyLoader(AssemblyLoadContext compilerContext, IReporter? reporter = null)
	{
		_compilerContext = compilerContext;
		_reporter = reporter;
	}

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
	/// mirroring Roslyn's own loader: every candidate must match the requested identity (simple
	/// name, and public key token when one is requested), an exact version match wins, otherwise
	/// the highest matching version — never just "whichever project registered that file name
	/// first". Returns <c>null</c> when nothing usable was registered.
	/// </summary>
	private string? GetBestKnownPath(string simpleName, AssemblyName requested)
	{
		if (!_knownPathsByFileName.TryGetValue(simpleName + ".dll", out var candidates))
		{
			return null;
		}

		var requestedToken = requested.GetPublicKeyToken();
		string? bestPath = null;
		Version? bestVersion = null;

		// Ordinal order is load-bearing, not cosmetic: ImmutableHashSet enumeration order is
		// unspecified, and a deterministic scan keeps the winner stable when candidates tie.
		foreach (var path in candidates.OrderBy(static p => p, StringComparer.Ordinal))
		{
			AssemblyName candidate;
			try
			{
				candidate = AssemblyName.GetAssemblyName(path);
			}
			catch (Exception e) when (e is IOException or BadImageFormatException or UnauthorizedAccessException or ArgumentException)
			{
				continue; // Deleted since it was registered, unreadable, or not a loadable assembly.
			}

			// The identity check applies to EVERY candidate, even a lone one: a stale
			// registration whose file name happens to match must fall through to the runtime's
			// own resolution instead of being force-loaded.
			if (!string.Equals(candidate.Name, simpleName, StringComparison.OrdinalIgnoreCase)
				|| (requestedToken is { Length: > 0 } && !requestedToken.AsSpan().SequenceEqual(candidate.GetPublicKeyToken().AsSpan())))
			{
				continue;
			}

			var candidateVersion = candidate.Version ?? new Version(0, 0);
			if (candidateVersion == requested.Version)
			{
				bestPath = path;
				bestVersion = candidateVersion;
				break;
			}

			if (bestVersion is null || candidateVersion > bestVersion)
			{
				bestPath = path;
				bestVersion = candidateVersion;
			}
		}

		if (candidates.Count > 1)
		{
			// The ambiguous case is worth a trace: when the picked version turns out binary-
			// incompatible downstream, this is the only record of what the decision was.
			_reporter?.Verbose(bestPath is null
				? $"None of the {candidates.Count} locations registered for '{simpleName}.dll' matches '{requested}'."
				: $"'{requested}' resolved to '{bestPath}' (v{bestVersion}) among {candidates.Count} registered locations.");
		}

		return bestPath;
	}

	private static string CreateShadowCopyRoot()
	{
		// Per-user, NOT under the shared temp root: %TEMP% is per-user on Windows but /tmp is
		// world-writable on Unix, and with deterministic keys a shared root would let any local
		// process pre-plant or swap entries (first-writer-owns). LocalApplicationData descends
		// from the user's profile on every OS — other accounts can neither pre-create nor swap
		// entries there (user-only mode on Unix for good measure). Moving OFF the historical
		// temp-rooted path also retires, wholesale, anything a pre-atomic-publish version of
		// this cache may have left truncated at a still-reachable key.
		var root = Path.Join(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create),
			"uno-devserver",
			"analyzers");

		if (OperatingSystem.IsWindows())
		{
			Directory.CreateDirectory(root);
		}
		else
		{
			Directory.CreateDirectory(root, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
		}

		SweepOrphanedStagingFiles(root);
		return root;
	}

	private static void SweepOrphanedStagingFiles(string root)
	{
		// Best effort, once per process: staging files normally live milliseconds (published or
		// cleaned up), so anything older than a day is debris from a killed process. The keyed
		// directories themselves are kept — their liveness across processes is unknowable.
		try
		{
			var cutoff = DateTime.UtcNow.AddDays(-1);
			foreach (var staging in Directory.EnumerateFiles(root, "*.staging", SearchOption.AllDirectories))
			{
				try
				{
					if (File.GetLastWriteTimeUtc(staging) < cutoff)
					{
						File.Delete(staging);
					}
				}
				catch (Exception e) when (e is IOException or UnauthorizedAccessException)
				{
					// Ignore: in use or protected — reconsidered at the next process start.
				}
			}
		}
		catch (Exception e) when (e is IOException or UnauthorizedAccessException)
		{
			// The enumeration itself failed — nothing to sweep.
		}
	}

	internal static string GetShadowCopyPath(string fullPath)
	{
		// One shadow directory per (path, timestamp, size): reused while the file is unchanged,
		// naturally re-keyed after a rebuild. The size guards the common timestamp-preserving
		// rewrites; a same-time same-size content swap still reuses the stale copy — hashing
		// every analyzer at every load would cost what the cache exists to save.
		var timestamp = File.GetLastWriteTimeUtc(fullPath).Ticks;
		var length = new FileInfo(fullPath).Length;
		var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{fullPath}|{timestamp}|{length}")))[..16];
		return Path.Join(_shadowCopyRoot.Value, key, Path.GetFileName(fullPath));
	}

	private static string EnsureShadowCopy(string fullPath)
	{
		// The original file is only ever read for the copy, never memory-mapped: it never locks.
		var shadowPath = GetShadowCopyPath(fullPath);
		if (!File.Exists(shadowPath))
		{
			Directory.CreateDirectory(Path.GetDirectoryName(shadowPath)!);

			// Keep the symbols next to the shadow copy so analyzer stack traces stay resolvable.
			// Published before the assembly: once the .dll is visible the bundle is complete.
			var pdb = Path.ChangeExtension(fullPath, ".pdb");
			if (File.Exists(pdb))
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
	/// load (another thread, or another dev-server process sharing the same root) treating
	/// existence as completion would load a partial PE; a process killed mid-copy would even
	/// poison the key until the source file changes.
	/// </summary>
	private static void Publish(string sourcePath, string targetPath)
	{
		var staging = $"{targetPath}.{Guid.NewGuid():N}.staging";
		try
		{
			File.Copy(sourcePath, staging);
			File.Move(staging, targetPath);
		}
		catch (Exception e) when (e is IOException or UnauthorizedAccessException)
		{
			// Whatever went wrong, never leak the staging file (File.Copy preserves the
			// read-only attribute, hence the attribute reset and the access filter).
			try
			{
				File.SetAttributes(staging, FileAttributes.Normal);
				File.Delete(staging);
			}
			catch (Exception cleanup) when (cleanup is IOException or UnauthorizedAccessException)
			{
				// Best effort: an orphaned staging file is inert and swept at a later start.
			}

			if (!File.Exists(targetPath))
			{
				// Genuine failure (disk full, protected directory, ...), not a lost race.
				throw;
			}

			// Lost the publish race with another thread or process — the winner's copy is
			// equivalent (the key embeds the source path, timestamp and size).
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
			=> LoadFromAssemblyPath(EnsureShadowCopy(fullPath));

		protected override Assembly? Load(AssemblyName assemblyName)
		{
			if (assemblyName.Name is not { Length: > 0 } simpleName
				|| simpleName.EndsWith(".resources", StringComparison.Ordinal)
				// The simple name becomes a FILE name below: reject separators and other invalid
				// file-name characters so a hostile reference cannot probe — and shadow-load —
				// outside the analyzer directory (Roslyn's loader rejects these too).
				|| simpleName.AsSpan().ContainsAny(_invalidSimpleNameChars))
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
			// added to the application through hot reload). Two Roslyn-parity trade-offs come
			// with the delegation: it probes the Default context too, so a Default-resolvable
			// simple name shadows any registered analyzer-local copy; and a dependency the
			// compiler's context CAN provide materializes into that longer-lived context.
			try
			{
				return _loader._compilerContext.LoadFromAssemblyName(assemblyName);
			}
			catch (Exception e) when (e is not OperationCanceledException)
			{
				// Not something the compiler's context can provide — resolve it as an analyzer-
				// local dependency below. Broad on purpose (like Roslyn's own loader): the throw
				// may also come from a custom Resolving handler on that context, and a handler
				// bug must not take analyzer resolution down with it.
				_loader._reporter?.Verbose($"The compiler's context could not provide '{assemblyName}' ({e.GetType().Name}); resolving as an analyzer-local dependency.");
			}

			// Then the analyzer's own directory, then the registered dependency locations.
			// Anything else returns null: the runtime then re-probes the default context
			// (redundantly — the delegation above already covered it) and finally raises this
			// context's Resolving event.
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
