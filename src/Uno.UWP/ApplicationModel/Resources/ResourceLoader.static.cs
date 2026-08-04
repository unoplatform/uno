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

		// Tracks whether the parse below merged directly into the LIVE loaders (the reload path
		// processes every lookup assembly straight into them), so the rollback knows whether the
		// merged state must be re-derived from the surviving registrations.
		var liveLoadersTouched = false;

		try
		{
			if (HasContextChangedSignificantly(out var context))
			{
				// The cache is still valid, we only have to load resources from the given assembly.
				// Parse into temporaries and merge only on success, so a malformed .upri (e.g. a
				// stream truncated in the middle of a key/value pair) can never leave a partially
				// parsed value or marker in the live state.
				ProcessAssemblyTransactionally(assembly, context.LanguagePreferences);
			}
			else
			{
				// The current culture was altered, rebuild the whole/missing cache
				liveLoadersTouched = true;
				ReloadResources(context);
			}
		}
		catch (Exception)
		{
			// A failed registration must not linger: the list is re-enumerated by every later
			// rebuild (culture change, ALC sweep), so a malformed assembly left registered would
			// re-hit the same parse failure forever. Roll back the just-added entry before
			// rethrowing to the caller.
			var lastIndex = _lookupAssemblies.LastIndexOf(assembly);
			if (lastIndex >= 0)
			{
				_lookupAssemblies.RemoveAt(lastIndex);
			}

			if (liveLoadersTouched)
			{
				// The reload merged each assembly's pairs (and parsed markers) straight into the
				// live state as it read, so the failed assembly may have left partially parsed
				// values behind. Re-derive the merged values and the markers from the surviving
				// registrations — the same recovery the ALC sweep uses — so nothing contributed by
				// the failed attempt remains observable.
				RebuildLoaderResourcesFromSurvivors();
			}

			throw;
		}
	}

	private static void ProcessAssembly(Assembly assembly, string[] languagePreferences)
		=> ProcessAssembly(assembly, languagePreferences, GetOrCreateNamedLoaderResources, _parsedResources);

	/// <summary>
	/// Parses <paramref name="assembly"/>'s .upri resources into TEMPORARY structures and merges
	/// them into the live loaders only once every file parsed successfully — the same
	/// temp-then-apply pattern as <see cref="RebuildLoaderResourcesFromSurvivors"/>. A malformed
	/// .upri therefore throws without any partially parsed value or marker becoming observable.
	/// </summary>
	private static void ProcessAssemblyTransactionally(Assembly assembly, string[] languagePreferences)
	{
		var parsedResources = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>(StringComparer.OrdinalIgnoreCase);

		// Seed the temporary marker set with the assembly's already-parsed files so they are
		// skipped exactly as a parse against the live markers would skip them.
		var parsedMarkers = new HashSet<(Assembly Assembly, string ResourceName)>();
		foreach (var marker in _parsedResources)
		{
			if (marker.Assembly == assembly)
			{
				parsedMarkers.Add(marker);
			}
		}

		Dictionary<string, Dictionary<string, string>> ResolveParsedLoader(string name)
		{
			if (!parsedResources.TryGetValue(name, out var cultures))
			{
				parsedResources[name] = cultures = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
			}

			return cultures;
		}

		ProcessAssembly(assembly, languagePreferences, ResolveParsedLoader, parsedMarkers);

		// Apply phase: parsing succeeded, merge into the live loaders with the same
		// last-writer-wins semantics ProcessResourceFile uses when writing to them directly.
		foreach (var parsed in parsedResources)
		{
			var loaderResources = GetOrCreateNamedLoaderResources(parsed.Key);
			foreach (var culture in parsed.Value)
			{
				if (!loaderResources.TryGetValue(culture.Key, out var resources))
				{
					loaderResources[culture.Key] = resources = new Dictionary<string, string>();
				}

				foreach (var pair in culture.Value)
				{
					resources[pair.Key] = pair.Value;
				}
			}
		}

		foreach (var marker in parsedMarkers)
		{
			_parsedResources.Add(marker);
		}
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
		if (!WinRTFeatureConfiguration.ResourceLoader.PreserveParsedResources)
		{
			ClearResources();
			_parsedResources.Clear();
		}

		foreach (var assembly in _lookupAssemblies)
		{
			ProcessAssembly(assembly, context.LanguagePreferences);
		}

		_loaderContext = context;
	}

	private static void ClearResources()
	{
		// We clear each loader independently instead of clearing the '_loaders'
		// so if a loader instance has been captured, it will be updated
		foreach (var loader in _loaders.Values)
		{
			loader._resources.Clear();
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
	/// <see cref="_lookupAssemblies"/> (called after a dying ALC's assemblies have been removed).
	/// Parses into TEMPORARY dictionaries first and only copies the result into the live loaders
	/// once every survivor has been processed, wrapping each assembly in try/catch, so a single
	/// malformed .upri (logged and skipped) can never leave a live loader empty.
	/// </summary>
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

		var rebuiltResources = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>(StringComparer.OrdinalIgnoreCase);
		var rebuiltMarkers = new HashSet<(Assembly Assembly, string ResourceName)>();

		Dictionary<string, Dictionary<string, string>> ResolveRebuiltLoader(string name)
		{
			if (!rebuiltResources.TryGetValue(name, out var cultures))
			{
				rebuiltResources[name] = cultures = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
			}

			return cultures;
		}

		// Parse only into the temporaries here — the phase that can throw (malformed .upri) must not
		// touch the live loaders. A per-assembly guard keeps one bad file from aborting the rebuild.
		foreach (var assembly in _lookupAssemblies)
		{
			try
			{
				ProcessAssembly(assembly, languagePreferences, ResolveRebuiltLoader, rebuiltMarkers);
			}
			catch (Exception error) when (error is global::System.IO.InvalidDataException or global::System.IO.IOException or NotSupportedException or BadImageFormatException or global::System.IO.FileLoadException or ArgumentException or FormatException)
			{
				// Recoverable per-assembly parse/reflection failure: skip this survivor and keep
				// rebuilding. ProcessResourceFile surfaces malformed .upri content (bad magic,
				// unsupported version, unreadable stream) as InvalidDataException, so every parser
				// failure is covered here. Fatal exceptions are intentionally not caught; the live
				// loaders are untouched until the apply phase
				// below, so an escaping exception here cannot leave a loader empty.
				if (_log.IsEnabled(LogLevel.Error))
				{
					_log.LogError($"[ALC-CLEANUP] ResourceLoader: skipping lookup assembly '{assembly.FullName}' while rebuilding merged resources after ALC unload.", error);
				}
			}
		}

		// Apply the rebuilt state to the live loaders in place. Copying into the existing _resources
		// dictionaries (rather than swapping the readonly field) preserves the shared references that
		// captured ResourceLoader instances rely on. Parsing already succeeded into the temporaries,
		// so this phase only mutates dictionaries and cannot throw partway and leave a loader empty.
		foreach (var loader in _loaders.Values)
		{
			loader._resources.Clear();
			if (rebuiltResources.TryGetValue(loader.LoaderName, out var cultures))
			{
				foreach (var culture in cultures)
				{
					loader._resources[culture.Key] = culture.Value;
				}
			}
		}

		// A survivor may contribute a loader name with no live instance yet; materialize it.
		foreach (var rebuilt in rebuiltResources.Where(rebuilt => !_loaders.ContainsKey(rebuilt.Key)))
		{
			var loaderResources = GetOrCreateNamedResourceLoader(rebuilt.Key)._resources;
			foreach (var culture in rebuilt.Value)
			{
				loaderResources[culture.Key] = culture.Value;
			}
		}

		// Replace the parsed markers with the survivor-derived set so a later AddLookupAssembly of
		// the same logical app re-parses correctly.
		_parsedResources.Clear();
		foreach (var marker in rebuiltMarkers)
		{
			_parsedResources.Add(marker);
		}
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

			var resourceCount = reader.ReadInt32();
			StringBuilder sb = new();
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
