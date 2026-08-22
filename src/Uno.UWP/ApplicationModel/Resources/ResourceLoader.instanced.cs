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

	// Keyed only by resource cultures, which are fixed at build time — never by the requested
	// culture, which comes from PrimaryLanguageOverride and is caller-controlled. Concurrent
	// because, unlike the per-loader state, this is shared process-wide and GetString carries no
	// thread affinity.
	private static readonly ConcurrentDictionary<string, string?> _resourceScripts = new(StringComparer.OrdinalIgnoreCase); // _resourceScripts[CULTURE] => ISO 15924 script

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

		// Then the culture's ancestors (zh-CN -> zh-Hans -> zh). The script tag only ever appears
		// there, so a region-named culture can reach its script-named resources and vice versa.
		foreach (var ancestor in GetAncestors(culture))
		{
			if (TryGetForCulture(ancestor, resource, out resourceValue))
			{
				return true;
			}
		}

		// Finally sibling cultures, same-script ones first: zh-Hant must never answer a request
		// for a Simplified Chinese culture just because both are written "zh-".
		if (culture.Split('-', 2)[0] is { Length: > 0 } baseCulture)
		{
			// Resolved rather than cached: the requested culture is caller-controlled, so caching it
			// would let a process-wide dictionary grow without bound.
			var script = ResolveScript(culture);
			var relatedCultures = _resources.Keys
				.Where(x => x.StartsWith(baseCulture, StringComparison.OrdinalIgnoreCase))
				.OrderByDescending(x => script is not null && GetResourceScript(x) == script) // same script first
				.ThenByDescending(x => string.Equals(x, baseCulture, StringComparison.OrdinalIgnoreCase)) // then base culture
				.ThenByDescending(x => x, StringComparer.Ordinal); // and then, sibling cultures in reverse order (from ex-ZZ to ex-AA)
			foreach (var related in relatedCultures)
			{
				if (TryGetForCulture(related, resource, out resourceValue))
				{
					return true;
				}
			}
		}

		resourceValue = null;
		return false;
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
	{
		if (TryGetCulture(culture) is not { } info)
		{
			yield break;
		}

		for (var parent = info.Parent; parent.Name is { Length: > 0 } name; parent = parent.Parent)
		{
			yield return name;

			if (string.Equals(parent.Parent.Name, name, StringComparison.Ordinal))
			{
				// A culture whose parent is itself would loop forever.
				yield break;
			}
		}
	}

	/// <summary>
	/// Gets the ISO 15924 script of a culture holding resources (Hans, Hant, Latn, Cyrl, ...), or
	/// null when the platform doesn't associate one with it. Cached: the resource cultures are
	/// fixed at build time, and each is scored repeatedly while ordering the fallback candidates.
	/// </summary>
	private static string? GetResourceScript(string culture)
		=> _resourceScripts.GetOrAdd(culture, static x => ResolveScript(x));

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

		for (var current = info; current.Name is { Length: > 0 } name; current = current.Parent)
		{
			if (GetScriptSubtag(name) is { } script)
			{
				return script;
			}

			if (string.Equals(current.Parent.Name, name, StringComparison.Ordinal))
			{
				break;
			}
		}

		return null;
	}

	/// <summary>
	/// Extracts the script subtag of a culture name, i.e. a four-letter subtag that is not the
	/// language itself (zh-Hant-TW -> Hant, ca-ES-VALENCIA -> null).
	/// </summary>
	private static string? GetScriptSubtag(string cultureName)
	{
		var remaining = cultureName.AsSpan();

		// Skip the language subtag, a script can never be in first position.
		var separator = remaining.IndexOf('-');
		if (separator < 0)
		{
			return null;
		}

		remaining = remaining.Slice(separator + 1);

		while (!remaining.IsEmpty)
		{
			separator = remaining.IndexOf('-');
			var subtag = separator < 0 ? remaining : remaining.Slice(0, separator);

			if (subtag.Length == 4 && IsAllLetters(subtag))
			{
				return subtag.ToString();
			}

			if (separator < 0)
			{
				break;
			}

			remaining = remaining.Slice(separator + 1);
		}

		return null;

		static bool IsAllLetters(ReadOnlySpan<char> value)
		{
			foreach (var c in value)
			{
				if (!char.IsLetter(c))
				{
					return false;
				}
			}

			return true;
		}
	}

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
