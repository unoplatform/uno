#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;
using Uno;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.Globalization;

namespace Windows.ApplicationModel.Resources;

partial class ResourceLoader
{
	#region public static string DefaultLanguage
	/// <summary>
	/// Provides the default culture if CurrentUICulture cannot provide it.
	/// </summary>
	public static string? DefaultLanguage
	{
		get => _defaultLanguage;
		set
		{
			_defaultLanguage = value;

#if __WASM__
			if (CultureInfo.CurrentUICulture.IetfLanguageTag.Length == 0 &&  // is not invariant-culture
				!string.IsNullOrEmpty(value))
			{
				CultureInfo.CurrentCulture = new CultureInfo(value);
				CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture;
			}
#endif

			EnsureLoadersCultures();
		}
	}

	private static string? _defaultLanguage;
	#endregion

	private const string DefaultResourceLoaderName = "Resources";
	private static readonly Logger _log = typeof(ResourceLoader).Log();

	private static readonly List<Assembly> _lookupAssemblies = new();
	private static readonly HashSet<(Assembly Assembly, string ResourceName)> _parsedResources = new();
	private static readonly Dictionary<string, ResourceLoader> _loaders = new(StringComparer.OrdinalIgnoreCase); // _loaders[RES_PACK ?? "Resources"]._resources[CULTURE][RES_KEY]
	private static LoaderContext? _loaderContext;

	private static ReadOnlySpan<byte> _expectedUnoSequence => [0x75, 0x6E, 0x6F]; // == "uno"

	private static string[] EnsureLoadersCultures()
	{
		if (HasContextChangedSignificantly(out var context))
		{
			ReloadResources(context);
		}

		return context.LanguagePreferences;
	}

	public static void AddLookupAssembly(Assembly assembly)
	{
		_lookupAssemblies.Add(assembly);

		// Diagnostics only, never drives the rollback: the reload branch re-parses EVERY registered
		// assembly, so a failure there can come from a pre-existing registration's .upri.
		var reloadedEverything = false;

		try
		{
			if (HasContextChangedSignificantly(out var context))
			{
				// The context moved on since the cache was established: parse only this assembly for
				// the new preferences, the first later resolve reloads the rest.
				ProcessAssemblyTransactionally(assembly, context.LanguagePreferences);
			}
			else
			{
				// The cache matches the current context, so rebuilding it as a whole is what merges
				// the new assembly in.
				reloadedEverything = true;
				ReloadResources(context);
			}
		}
		catch (Exception error)
		{
			// A failed registration must not linger: the list is re-enumerated by every later
			// rebuild (culture change, ALC sweep), so a malformed assembly left registered would
			// re-hit the same parse failure forever. Both branches parse into temporaries and apply
			// only on success, so dropping the list entry is the whole rollback.
			var lastIndex = _lookupAssemblies.LastIndexOf(assembly);
			if (lastIndex >= 0)
			{
				_lookupAssemblies.RemoveAt(lastIndex);
			}

			if (_log.IsEnabled(LogLevel.Error))
			{
				_log.LogError($"AddLookupAssembly failed for '{assembly.FullName}' (whole cache reloaded: {reloadedEverything}); registration rolled back, live resources unchanged.", error);
			}

			throw;
		}
	}

	/// <summary>
	/// Parses <paramref name="assembly"/>'s .upri resources and merges them into the live loaders
	/// only once every file parsed successfully.
	/// </summary>
	private static void ProcessAssemblyTransactionally(Assembly assembly, string[] languagePreferences)
	{
		// Seeded with this assembly's already-parsed files so they are skipped exactly as a parse
		// against the live markers would skip them.
		var parse = new TransactionalParse(_parsedResources.Where(marker => marker.Assembly == assembly));

		parse.Parse(assembly, languagePreferences);
		parse.MergeIntoLiveLoaders();
	}

	private static void ProcessAssembly(
		Assembly assembly,
		string[] languagePreferences,
		Func<string, Dictionary<string, Dictionary<string, string>>> resolveLoaderResources,
		HashSet<(Assembly Assembly, string ResourceName)> parsedMarkers)
	{
		var resourceNames = assembly.GetManifestResourceNames();
		foreach (var name in resourceNames)
		{
			if (name.EndsWith(".upri", StringComparison.Ordinal))
			{
				ProcessResourceFile(assembly, name, assembly.GetManifestResourceStream(name), languagePreferences, resolveLoaderResources, parsedMarkers);
			}
		}
	}

	private static void ReloadResources(LoaderContext context)
	{
		var preserveParsedResources = WinRTFeatureConfiguration.ResourceLoader.PreserveParsedResources;

		// Preserving keeps what earlier language preferences already contributed, so the live markers
		// are carried over and the newly parsed values are merged on top rather than replacing them.
		var parse = preserveParsedResources ? new TransactionalParse(_parsedResources) : new TransactionalParse();
		foreach (var assembly in _lookupAssemblies)
		{
			parse.Parse(assembly, context.LanguagePreferences);
		}

		if (preserveParsedResources)
		{
			parse.MergeIntoLiveLoaders();
		}
		else
		{
			parse.ReplaceLiveLoaders();
		}

		_loaderContext = context;
	}

	/// <summary>
	/// Accumulates .upri parse results into TEMPORARY structures so the live loaders are mutated only
	/// once every file has parsed successfully. Every path that mutates the live state
	/// (<see cref="AddLookupAssembly"/>, <see cref="ReloadResources"/> — hence the culture-change
	/// path through <see cref="EnsureLoadersCultures"/> — and
	/// <see cref="RebuildLoaderResourcesFromSurvivors"/>) goes through this, so a malformed .upri
	/// (e.g. a stream truncated in the middle of a key/value pair) throws without leaving a partially
	/// parsed value or marker observable.
	/// </summary>
	private sealed class TransactionalParse
	{
		private readonly Dictionary<string, Dictionary<string, Dictionary<string, string>>> _loaderResources = new(StringComparer.OrdinalIgnoreCase);
		private readonly HashSet<(Assembly Assembly, string ResourceName)> _markers;

		public TransactionalParse()
			: this(Array.Empty<(Assembly Assembly, string ResourceName)>())
		{
		}

		/// <param name="alreadyParsed">
		/// Files to seed the marker set with, so they are skipped exactly as a parse against the live
		/// <see cref="_parsedResources"/> would skip them.
		/// </param>
		public TransactionalParse(IEnumerable<(Assembly Assembly, string ResourceName)> alreadyParsed)
			=> _markers = new HashSet<(Assembly Assembly, string ResourceName)>(alreadyParsed);

		/// <summary>
		/// Parses <paramref name="assembly"/>'s .upri files into the temporaries; throws (leaving the
		/// live state untouched) when one of them is malformed.
		/// </summary>
		public void Parse(Assembly assembly, string[] languagePreferences)
			=> ProcessAssembly(assembly, languagePreferences, ResolveLoaderResources, _markers);

		/// <summary>
		/// Merges the parsed state into the live loaders with the same last-writer-wins semantics
		/// <see cref="ProcessResourceFile"/> uses when writing to them directly, and records the
		/// parsed markers.
		/// </summary>
		public void MergeIntoLiveLoaders()
		{
			foreach (var parsed in _loaderResources)
			{
				var liveResources = GetOrCreateNamedLoaderResources(parsed.Key);
				foreach (var culture in parsed.Value)
				{
					if (liveResources.TryGetValue(culture.Key, out var resources))
					{
						foreach (var pair in culture.Value)
						{
							resources[pair.Key] = pair.Value;
						}
					}
					else
					{
						// Nothing live to merge with: hand the temporary over instead of copying it.
						liveResources[culture.Key] = culture.Value;
					}
				}
			}

			foreach (var marker in _markers)
			{
				_parsedResources.Add(marker);
			}
		}

		/// <summary>
		/// Makes the parsed state the loaders' entire content and replaces the live markers with the
		/// parsed ones. Each live loader is cleared in place (instead of clearing
		/// <see cref="_loaders"/>) so already-captured <see cref="ResourceLoader"/> instances see the
		/// update; loader names with no live instance yet are materialized.
		/// </summary>
		public void ReplaceLiveLoaders()
		{
			foreach (var loader in _loaders.Values)
			{
				loader._resources.Clear();
				if (_loaderResources.TryGetValue(loader.LoaderName, out var cultures))
				{
					foreach (var culture in cultures)
					{
						loader._resources[culture.Key] = culture.Value;
					}
				}
			}

			foreach (var parsed in _loaderResources.Where(parsed => !_loaders.ContainsKey(parsed.Key)))
			{
				var liveResources = GetOrCreateNamedLoaderResources(parsed.Key);
				foreach (var culture in parsed.Value)
				{
					liveResources[culture.Key] = culture.Value;
				}
			}

			_parsedResources.Clear();
			foreach (var marker in _markers)
			{
				_parsedResources.Add(marker);
			}
		}

		private Dictionary<string, Dictionary<string, string>> ResolveLoaderResources(string name)
		{
			if (!_loaderResources.TryGetValue(name, out var cultures))
			{
				_loaderResources[name] = cultures = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
			}

			return cultures;
		}
	}

	/// <summary>
	/// Removes every previewed-app assembly registered via <see cref="AddLookupAssembly"/> whose
	/// <see cref="System.Runtime.Loader.AssemblyLoadContext"/> is non-default (collectible),
	/// together with those assemblies' parsed-resource markers. A downstream host that loads
	/// previewed apps into their own collectible AssemblyLoadContexts calls each of those apps'
	/// <c>AddLookupAssembly</c>; the process-lifetime <see cref="_lookupAssemblies"/> list then
	/// holds a strong reference to every loaded app assembly for the process lifetime, pinning the
	/// context after unload. Used for global shutdown when no specific dying ALC is identifiable;
	/// when it is, prefer <see cref="ClearAlcAssemblies"/> so sibling secondary apps' registrations
	/// survive.
	/// </summary>
	internal static void ClearNonDefaultAlcAssemblies()
		=> ClearAlcAssembliesCore(IsFromNonDefaultAlc, rebuildFromSurvivors: false);

	/// <summary>
	/// Removes only the lookup assemblies loaded into the specified dying
	/// <see cref="System.Runtime.Loader.AssemblyLoadContext"/> (and their parsed-resource markers),
	/// then rebuilds every named loader's merged resource values from the SURVIVING lookup
	/// assemblies. Unlike <see cref="ClearNonDefaultAlcAssemblies"/> (the global-shutdown,
	/// all-non-default sweep), registrations from OTHER live secondary ALCs (sibling previewed apps)
	/// survive — removal is destructive (a dropped registration is never re-added), so a
	/// whole-process sweep would break a live sibling app's resource lookups when only one app is
	/// being torn down.
	/// </summary>
	internal static void ClearAlcAssemblies(global::System.Runtime.Loader.AssemblyLoadContext alc)
		=> ClearAlcAssembliesCore(assembly => global::System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(assembly) == alc, rebuildFromSurvivors: true);

	private static void ClearAlcAssembliesCore(Predicate<Assembly> shouldRemove, bool rebuildFromSurvivors)
	{
		// Identify the dying assemblies first, then drop their list entries (the ALC pins) and their
		// (assembly, fileName) parsed markers, so a later AddLookupAssembly of the SAME logical app
		// (a fresh Assembly instance in a fresh ALC) re-parses its resources.
		HashSet<Assembly>? removed = null;
		foreach (var assembly in _lookupAssemblies.Where(assembly => shouldRemove(assembly)))
		{
			(removed ??= new HashSet<Assembly>()).Add(assembly);
		}

		if (removed is null)
		{
			return;
		}

		_lookupAssemblies.RemoveAll(removed.Contains);
		var removedMarkers = _parsedResources.RemoveWhere(marker => removed.Contains(marker.Assembly));

		if (_log.IsEnabled(LogLevel.Debug))
		{
			_log.LogDebug($"[ALC-CLEANUP] ResourceLoader: removed {removed.Count} lookup assemblie(s) and {removedMarkers} parsed-resource marker(s).");
		}

		if (rebuildFromSurvivors)
		{
			// ProcessResourceFile merges each .upri's entries as plain culture/key/value pairs with
			// no back-reference to the contributing assembly, and a later assembly overwrites any
			// earlier value for the same loader/culture/key (last writer wins). So a dying app that
			// overrode a host/sibling key leaves its value observable in loader._resources after its
			// list entry and markers are gone — a correctness bug and an override that outlives its
			// ALC. Because the merged value cannot be attributed back to one assembly (dropping "the
			// dying app's keys" would also drop a host value it had overridden), re-derive the merged
			// state from the survivors instead.
			RebuildLoaderResourcesFromSurvivors();
		}
	}

	/// <summary>
	/// Re-derives every named loader's merged culture/key/value entries from the surviving
	/// <see cref="_lookupAssemblies"/> (called after a dying ALC's assemblies have been removed),
	/// through the <see cref="TransactionalParse"/> temp-then-apply pattern with a per-assembly guard
	/// so a single malformed .upri is logged and skipped instead of leaving a live loader empty.
	/// </summary>
	/// <remarks>
	/// The re-derivation only covers the last established context's language preferences, so with
	/// <see cref="WinRTFeatureConfiguration.ResourceLoader.PreserveParsedResources"/> enabled, values
	/// preserved for OTHER cultures by earlier language changes are dropped here; they are re-parsed
	/// the next time those languages become active. Attributing a merged value back to a single
	/// assembly is impossible (see <see cref="ClearAlcAssembliesCore"/>), so re-deriving from the
	/// survivors is the only way to stop a dying ALC's override outliving it.
	/// </remarks>
	private static void RebuildLoaderResourcesFromSurvivors()
	{
		// The loaders were merged for the last established context's language preferences. Reuse
		// them so the rebuilt state matches what is currently observable; if no resolve has happened
		// yet (context not established), derive the current preferences without mutating state.
		var languagePreferences = _loaderContext?.LanguagePreferences;
		if (languagePreferences is null)
		{
			HasContextChangedSignificantly(out var context);
			languagePreferences = context.LanguagePreferences;
		}

		// Started from an empty marker set: the rebuilt state replaces the live one wholesale.
		var parse = new TransactionalParse();
		foreach (var assembly in _lookupAssemblies)
		{
			try
			{
				parse.Parse(assembly, languagePreferences);
			}
			catch (Exception error) when (error is global::System.IO.InvalidDataException or global::System.IO.IOException or NotSupportedException or BadImageFormatException or global::System.IO.FileLoadException or ArgumentException or FormatException)
			{
				// Recoverable per-assembly parse/reflection failure: skip this survivor and keep
				// rebuilding. ProcessResourceFile surfaces malformed .upri content (bad magic,
				// unsupported version, truncated/unreadable stream) as InvalidDataException, so every
				// parser failure is covered here. Fatal exceptions are intentionally not caught: the
				// live loaders are untouched until the apply phase, so an escaping exception here
				// cannot leave a loader empty.
				if (_log.IsEnabled(LogLevel.Error))
				{
					_log.LogError($"[ALC-CLEANUP] ResourceLoader: skipping lookup assembly '{assembly.FullName}' while rebuilding merged resources after ALC unload.", error);
				}
			}
		}

		parse.ReplaceLiveLoaders();
	}

	private static bool IsFromNonDefaultAlc(Assembly assembly)
	{
		var alc = global::System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(assembly);
		return alc is not null && alc != global::System.Runtime.Loader.AssemblyLoadContext.Default;
	}

	/// <summary>
	/// Test seam: whether <paramref name="assembly"/> is currently registered as a lookup assembly.
	/// Lets the ALC sweep be verified without reflecting over the private
	/// <see cref="_lookupAssemblies"/> field.
	/// </summary>
	internal static bool ContainsLookupAssembly(Assembly assembly) => _lookupAssemblies.Contains(assembly);

	/// <summary>
	/// Test seam: whether a loader instance exists for <paramref name="loaderName"/>, so a test can
	/// tell "no loader was ever created" from "a loader exists but holds no value" — the difference
	/// between a parse that never touched the live state and one that did and was cleaned up after.
	/// </summary>
	internal static bool ContainsNamedLoader(string loaderName) => _loaders.ContainsKey(loaderName);

	/// <summary>
	/// Test seam: reads a merged value straight from the per-loader dictionaries, without going
	/// through <c>GetString</c> — which re-establishes the loader context and therefore reloads (and
	/// wipes) the loaders when the culture changed, masking a value leaked by a failed parse.
	/// </summary>
	internal static bool TryGetMergedResourceForTests(string loaderName, string culture, string key, out string? value)
	{
		value = null;

		return _loaders.TryGetValue(loaderName, out var loader)
			&& loader._resources.TryGetValue(culture, out var resources)
			&& resources.TryGetValue(key, out value);
	}

	private static bool HasContextChangedSignificantly(out LoaderContext context)
	{
		var capture = new LoaderContext(
			WinRTFeatureConfiguration.ResourceLoader.UsePrimaryLanguageOverride,
			ApplicationLanguages.PrimaryLanguageOverride,
			CultureInfo.CurrentUICulture,
			DefaultLanguage,
			default!);
		if (_loaderContext is null ||
			capture != _loaderContext with { LanguagePreferences = default! })
		{
			var preferences = GetLanguagePreferences(capture);
			if (_loaderContext is null ||
				!_loaderContext.LanguagePreferences.SequenceEqual(preferences))
			{
				_log.Trace($"HasContextChangedSignificantly: true");
				context = capture with { LanguagePreferences = preferences };
				return true;
			}
		}

		context = _loaderContext;
		return false;
	}

	private static string[] GetLanguagePreferences(LoaderContext context)
	{
		var plo = WinRTFeatureConfiguration.ResourceLoader.UsePrimaryLanguageOverride
				? context.PLO
				// invariant culture doesn't have an IetfLanguageTag, and will be discarded below
				: context.UICulture?.IetfLanguageTag;
		return (ApplicationLanguages.Languages ?? Array.Empty<string>())
			.Prepend(plo)
			.Append(context.DefaultLanguage)
			.Distinct()
			.OrderBy(x => string.IsNullOrEmpty(plo) ? false : !FastBaseCultureComparer.Instance.Equals(x, plo))
			.Where(x => !string.IsNullOrEmpty(x))
			.OfType<string>()
			.ToArray();
	}

	private static void ProcessResourceFile(
		Assembly assembly,
		string fileName,
		Stream? stream,
		string[] languagePreferences,
		Func<string, Dictionary<string, Dictionary<string, string>>> resolveLoaderResources,
		HashSet<(Assembly Assembly, string ResourceName)> parsedMarkers)
	{
		// Malformed/unreadable .upri content is surfaced as InvalidDataException (a data-format
		// exception) so the per-assembly guard in RebuildLoaderResourcesFromSurvivors can skip the
		// offending assembly instead of aborting the whole rebuild.
		if (stream is null)
		{
			throw new InvalidDataException($"The resource file {fileName} could not be read.");
		}

		using (var reader = new BinaryReader(stream))
		{
			// "Magic" sequence to ensure we're reading a proper resource file
			Span<byte> magic = stackalloc byte[3];
			var magicCount = reader.Read(magic);
			if (magicCount != 3 || !magic.SequenceEqual(_expectedUnoSequence))
			{
				throw new InvalidDataException($"The file {fileName} is not a resource file");
			}

			var version = reader.ReadInt32();
			if (version is not (3 or 2))
			{
				throw new InvalidDataException($"The resource file {fileName} has an invalid version (got {version}, expecting 2 or 3)");
			}

			var name = reader.ReadString();
			var culture = reader.ReadString();

			if (!languagePreferences.Contains(culture, FastBaseCultureComparer.Instance))
			{
				// Currently only load the resources for the current culture.
				if (_log.IsEnabled(LogLevel.Debug))
				{
					_log.LogDebug($"Skipping resource file {fileName} for {culture} (preferences: {string.Join(",", languagePreferences)})");
				}
				return;
			}
			if (!parsedMarkers.Add((assembly, fileName/* keyed by fileName, not name */)))
			{
				if (_log.IsEnabled(LogLevel.Debug))
				{
					_log.LogDebug($"Skipping already parsed resource file {fileName} for {culture}");
				}
				return;
			}

			var loaderResources = resolveLoaderResources(name);
			if (!loaderResources.TryGetValue(culture, out var resources))
			{
				loaderResources[culture] = resources = new Dictionary<string, string>();
			}

			StringBuilder sb = new();
			int? declaredCount = null;
			try
			{
				var resourceCount = reader.ReadInt32();
				declaredCount = resourceCount;

				for (var i = 0; i < resourceCount; i++)
				{
					var key = reader.ReadString();
					var value = reader.ReadString();

					if (version == 2)
					{
						// Restore the original format
						key = key.Replace("/", ".");

						var firstDotIndex = key.IndexOf('.');
						if (firstDotIndex != -1)
						{
							sb.Clear();
							sb.Append(key);

							sb[firstDotIndex] = '/';

							key = sb.ToString();
						}
					}

					if (_log.IsEnabled(LogLevel.Debug))
					{
						_log.LogDebug($"[{name}, {culture}, {fileName}] Adding resource: {key}={value}");
					}

					resources[key] = value;
				}
			}
			catch (EndOfStreamException error)
			{
				// A stream that ends anywhere in the entry list — mid pair, or inside the declared
				// count itself — surfaces as BinaryReader's context-free EndOfStreamException;
				// restate it as the documented InvalidDataException naming the file, like every
				// other malformed-.upri case.
				throw new InvalidDataException(
					declaredCount is { } count
						? $"Truncated resource file {fileName}: it declares {count} resource(s) but the stream ended early."
						: $"Truncated resource file {fileName}: the stream ended before its resource count could be read.",
					error);
			}
		}
	}

	private static ResourceLoader GetOrCreateNamedResourceLoader(string name) =>
		_loaders.FindOrCreate(name, () => new ResourceLoader(name, addLoader: false));

	private static Dictionary<string, Dictionary<string, string>> GetOrCreateNamedLoaderResources(string name) =>
		GetOrCreateNamedResourceLoader(name)._resources;

	private record class LoaderContext(bool UsePrimaryLanguageOverride, string? PLO, CultureInfo? UICulture, string? DefaultLanguage, string[] LanguagePreferences);

	/// <summary>
	/// Allows for case-insensitive culture comparison using base culture: FR == fr-CA
	/// </summary>
	private class FastBaseCultureComparer : EqualityComparer<string>
	{
		public static FastBaseCultureComparer Instance { get; } = new();

		private static ReadOnlySpan<char> GetBaseCulture(ReadOnlySpan<char> span)
		{
			var dashIndex = span.IndexOf('-');
			if (dashIndex != -1)
			{
				span = span.Slice(0, dashIndex);
			}

			return span;
		}

		public override int GetHashCode([DisallowNull] string x)
		{
			return string.GetHashCode(GetBaseCulture(x), StringComparison.OrdinalIgnoreCase);
		}

		public override bool Equals(string? c1, string? c2)
		{
			if (c1 is not null && c2 is not null)
			{
				var span1 = GetBaseCulture(c1);
				var span2 = GetBaseCulture(c2);
				return span1.Equals(span2, StringComparison.OrdinalIgnoreCase);
			}

			return c1 is null && c2 is null;
		}
	}
}
