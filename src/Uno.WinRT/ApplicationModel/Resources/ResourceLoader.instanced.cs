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
	private readonly Dictionary<string, Dictionary<string, string>> _resources = new(StringComparer.OrdinalIgnoreCase); // _resources[CULTURE][RES_KEY] => RES_VALUE

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

		if (_resources.TryGetValue(culture, out var map1) &&
			map1.TryGetValue(resource, out resourceValue))
		{
			return true;
		}

		// if we cant find using the specific culture, fallback on base culture, and then sibling cultures
		if (culture.Split('-', 2)[0] is { Length: > 0 } baseCulture)
		{
			var relatedCultures = _resources.Keys
				.Where(x => x.StartsWith(baseCulture, StringComparison.OrdinalIgnoreCase))
				.OrderByDescending(x => x == baseCulture) // base culture first
				.ThenByDescending(x => x); // and then, sibling cultures in reverse order (from ex-ZZ to ex-AA)
			foreach (var related in relatedCultures)
			{
				if (_resources.TryGetValue(related, out var map2) &&
					map2.TryGetValue(resource, out resourceValue))
				{
					return true;
				}
			}
		}

		resourceValue = null;
		return false;
	}
}
