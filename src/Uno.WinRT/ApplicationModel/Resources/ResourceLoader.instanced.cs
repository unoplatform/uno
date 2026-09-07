#nullable enable

using System;
using System.Collections.Concurrent;
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
	private readonly Dictionary<string, Dictionary<string, string>> _resources = new(StringComparer.OrdinalIgnoreCase); // _resources[CULTURE][RES_KEY] => RES_VALUE

	// Keyed only by resource cultures, which are fixed at build time. Concurrent because, unlike the
	// per-loader state, this is shared process-wide and GetString carries no thread affinity.
	private static readonly ConcurrentDictionary<string, (string? Script, string? Region)> _resourceSubtags = new(StringComparer.OrdinalIgnoreCase);

	// The requested culture comes from PrimaryLanguageOverride and is caller-controlled, so it gets a
	// separate memo that is dropped wholesale instead of growing. Entries can never go stale: a name's
	// script and region are pure functions of the name.
	private static readonly ConcurrentDictionary<string, (string? Script, string? Region)> _requestedSubtags = new(StringComparer.OrdinalIgnoreCase);

	// Comfortably above the handful of language preferences a lookup cycles through.
	private const int MaxRequestedSubtags = 32;

	internal string LoaderName { get; }

	public ResourceLoader() : this(DefaultResourceLoaderName, true)
	{
	}

	public ResourceLoader(string name) : this(name, true)
	{
	}

	/// <summary>
	/// Creates a loader with a given name.
	/// If the loader does not exist yet, it can add it if requested.
	/// </summary>
	/// <param name="name">Name of the loader.</param>
	/// <param name="addLoader">
	/// A value indicating whether the loader
	/// should be added to the list of loaders.
	/// </param>
	private ResourceLoader(string name, bool addLoader)
	{
		if (_log.IsEnabled(LogLevel.Debug))
		{
			_log.LogDebug($"Initializing ResourceLoader[\"{name}\"]");
		}

		LoaderName = name;

		if (_loaders.TryGetValue(name, out var existingLoader))
		{
			// If there is already a loader with the same name,
			// they should share the same resources.
			_resources = existingLoader._resources;
		}
		else if (addLoader)
		{
			_loaders[name] = this;
		}
	}

	public string? GetString(string resource)
	{
		// "/[file]/[name]" format support
		if (resource.ElementAtOrDefault(0) == '/')
		{
			var separatorIndex = resource.IndexOf('/', 1);
			if (separatorIndex < 1)
			{
				return "";
			}
			var resourceFile = resource.Substring(1, separatorIndex - 1);
			var resourceName = resource.Substring(separatorIndex + 1);
			return GetForCurrentView(resourceFile).GetString(resourceName);
		}

		// First make sure that resource cache matches the current culture
		var cultures = EnsureLoadersCultures();

		// Walk the culture hierarchy and the default
		foreach (var culture in cultures)
		{
			if (FindForCulture(culture, resource, out var value))
			{
				return value;
			}
		}

		return string.Empty;
	}

	private bool FindForCulture(string culture, string resource, out string? resourceValue)
	{
		if (_log.IsEnabled(LogLevel.Debug))
		{
			_log.LogDebug($"[{LoaderName}] FindForCulture {culture}, {resource}");
		}

		if (TryGetForCulture(culture, resource, out resourceValue))
		{
			return true;
		}

		foreach (var candidate in GetFallbackCultures(culture))
		{
			if (TryGetForCulture(candidate, resource, out resourceValue))
			{
				return true;
			}
		}

		resourceValue = null;
		return false;
	}

	/// <summary>
	/// Orders the resource cultures that may stand in for <paramref name="culture"/> — its ancestors
	/// (zh-CN -> zh-Hans -> zh) and the siblings sharing its language (zh-Hant-TW for zh-TW) — from
	/// the closest match down. Script comes first, so that zh-Hant never answers a Simplified Chinese
	/// request, whether it is reached as a sibling or as a bare zh folder holding Traditional content.
	/// </summary>
	private List<string> GetFallbackCultures(string culture)
	{
		var baseCulture = culture.Split('-', 2)[0];
		var ancestors = GetAncestors(culture).ToArray();
		var (script, region) = GetRequestedSubtags(culture);

		var candidates = new List<string>();
		foreach (var resourceCulture in _resources.Keys)
		{
			if (AncestorDepth(resourceCulture) >= 0 ||
				(baseCulture.Length > 0 && resourceCulture.StartsWith(baseCulture, StringComparison.OrdinalIgnoreCase)))
			{
				candidates.Add(resourceCulture);
			}
		}

		candidates.Sort(CompareCandidates);

		return candidates;

		int CompareCandidates(string left, string right)
		{
			var byScript = SameScript(right).CompareTo(SameScript(left)); // same script first
			if (byScript != 0)
			{
				return byScript;
			}

			var byRegion = SameRegion(right).CompareTo(SameRegion(left)); // then same region
			if (byRegion != 0)
			{
				return byRegion;
			}

			var byAncestor = Proximity(left).CompareTo(Proximity(right)); // then the closest ancestor
			if (byAncestor != 0)
			{
				return byAncestor;
			}

			var byBaseCulture = IsBaseCulture(right).CompareTo(IsBaseCulture(left)); // then the base culture
			if (byBaseCulture != 0)
			{
				return byBaseCulture;
			}

			return StringComparer.Ordinal.Compare(right, left); // and finally from ex-ZZ to ex-AA
		}

		bool SameScript(string candidate)
			=> script is not null && string.Equals(GetResourceSubtags(candidate).Script, script, StringComparison.OrdinalIgnoreCase);

		bool SameRegion(string candidate)
			=> region is not null && string.Equals(GetResourceSubtags(candidate).Region, region, StringComparison.OrdinalIgnoreCase);

		int Proximity(string candidate)
			=> AncestorDepth(candidate) is var depth && depth >= 0 ? depth : int.MaxValue;

		int AncestorDepth(string candidate)
		{
			for (var i = 0; i < ancestors.Length; i++)
			{
				if (string.Equals(ancestors[i], candidate, StringComparison.OrdinalIgnoreCase))
				{
					return i;
				}
			}

			return -1;
		}

		bool IsBaseCulture(string candidate)
			=> string.Equals(candidate, baseCulture, StringComparison.OrdinalIgnoreCase);
	}

	private bool TryGetForCulture(string culture, string resource, out string? resourceValue)
	{
		if (_resources.TryGetValue(culture, out var map) &&
			map.TryGetValue(resource, out resourceValue))
		{
			return true;
		}

		resourceValue = null;
		return false;
	}

	/// <summary>
	/// Enumerates the parent cultures of <paramref name="culture"/>, closest first, excluding itself
	/// and the invariant culture.
	/// </summary>
	private static IEnumerable<string> GetAncestors(string culture)
		=> TryGetCulture(culture) is { } info ? GetSelfAndAncestors(info).Skip(1) : [];

	/// <summary>
	/// Enumerates <paramref name="culture"/> and its parents, closest first, excluding the invariant
	/// culture.
	/// </summary>
	private static IEnumerable<string> GetSelfAndAncestors(CultureInfo culture)
	{
		for (var current = culture; current.Name is { Length: > 0 } name; current = current.Parent)
		{
			yield return name;

			if (string.Equals(current.Parent.Name, name, StringComparison.Ordinal))
			{
				// A culture whose parent is itself would loop forever.
				yield break;
			}
		}
	}

	/// <summary>
	/// Gets the ISO 15924 script (Hans, Hant, Latn, Cyrl, ...) and the region of a culture holding
	/// resources, either being null when the culture carries none. Cached: the resource cultures are
	/// fixed at build time, and each is scored repeatedly while ordering the fallback candidates.
	/// </summary>
	private static (string? Script, string? Region) GetResourceSubtags(string culture)
		=> _resourceSubtags.GetOrAdd(culture, static x => (ResolveScript(x), GetRegionSubtag(x)));

	/// <summary>
	/// Same for the requested culture, memoized under <see cref="MaxRequestedSubtags"/> entries
	/// because its name is caller-controlled.
	/// </summary>
	private static (string? Script, string? Region) GetRequestedSubtags(string culture)
	{
		if (_requestedSubtags.TryGetValue(culture, out var subtags))
		{
			return subtags;
		}

		subtags = (ResolveScript(culture), GetRegionSubtag(culture));

		if (_requestedSubtags.Count >= MaxRequestedSubtags)
		{
			_requestedSubtags.Clear();
		}

		_requestedSubtags[culture] = subtags;

		return subtags;
	}

	private static string? ResolveScript(string culture)
	{
		if (TryGetCulture(culture) is not { } info)
		{
			return null;
		}

		// A neutral culture carrying no script of its own (zh) only reveals its default one through
		// the specific culture the platform considers likely for it (zh -> zh-CN -> zh-Hans).
		if (info.IsNeutralCulture && GetScriptSubtag(info.Name) is null)
		{
			info = TryGetSpecificCulture(info.Name) ?? info;
		}

		foreach (var name in GetSelfAndAncestors(info))
		{
			if (GetScriptSubtag(name) is { } script)
			{
				return script;
			}
		}

		return null;
	}

	/// <summary>
	/// Extracts the script subtag of a culture name, i.e. a four-letter subtag that is not the
	/// language itself (zh-Hant-TW -> Hant, ca-ES-VALENCIA -> null).
	/// </summary>
	private static string? GetScriptSubtag(string cultureName)
		=> cultureName.Split('-')
			.Skip(1) // index 0 is never a script: a four-letter subtag there is a (reserved) language
			.FirstOrDefault(subtag => subtag.Length is 4 && subtag.All(char.IsLetter));

	/// <summary>
	/// Extracts the region subtag of a culture name, i.e. the two-letter or three-digit subtag that
	/// follows the language and the optional script (zh-Hant-HK -> HK, es-419 -> 419, zh-Hant -> null).
	/// </summary>
	private static string? GetRegionSubtag(string cultureName)
		=> cultureName.Split('-')
			.Skip(1) // index 0 is the language
			.FirstOrDefault(subtag =>
				(subtag.Length is 2 && subtag.All(char.IsLetter)) ||
				(subtag.Length is 3 && subtag.All(char.IsDigit)));

	private static CultureInfo? TryGetCulture(string name)
	{
		try
		{
			return CultureInfo.GetCultureInfo(name);
		}
		catch (CultureNotFoundException)
		{
			return null;
		}
	}

	private static CultureInfo? TryGetSpecificCulture(string name)
	{
		try
		{
			return CultureInfo.CreateSpecificCulture(name);
		}
		catch (CultureNotFoundException)
		{
			return null;
		}
	}
}
