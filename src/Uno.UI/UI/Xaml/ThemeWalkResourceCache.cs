#nullable enable

using System.Collections.Generic;

namespace Microsoft.UI.Xaml;

/// <summary>
/// Per-theme-walk cache that prevents redundant dictionary lookups when many elements
/// reference the same resource key from the same dictionary during a single theme change walk.
/// </summary>
/// <remarks>
/// MUX Reference: ThemeWalkResourceCache in ThemeWalkResourceCache.h
/// In WinUI, resources are cached during a theme walk to alleviate the cost of
/// repeatedly searching large resource dictionaries multiple times for the same keys.
/// The cache is created at walk start and cleared at walk end.
/// </remarks>
internal sealed class ThemeWalkResourceCache
{
	// Key: (ResourceDictionary, ResourceKey) -> resolved value
	// ResourceDictionary uses reference equality (default for classes).
	private readonly Dictionary<(ResourceDictionary Dict, SpecializedResourceDictionary.ResourceKey Key), object?> _cache = new();

	/// <summary>
	/// Tries to retrieve a cached resource value.
	/// </summary>
	public bool TryGetCachedValue(ResourceDictionary dictionary, SpecializedResourceDictionary.ResourceKey key, out object? value)
	{
		return _cache.TryGetValue((dictionary, key), out value);
	}

	/// <summary>
	/// Caches a resolved resource value for the given dictionary and key.
	/// </summary>
	public void CacheValue(ResourceDictionary dictionary, SpecializedResourceDictionary.ResourceKey key, object? value)
	{
		_cache[(dictionary, key)] = value;
	}

	/// <summary>
	/// Removes a cached entry. Called when a resource is added/removed from a dictionary
	/// during the theme walk (e.g., when UpdateLayout calls out to app code that modifies dictionaries).
	/// </summary>
	/// <remarks>
	/// MUX Reference: ThemeWalkResourceCache::RemoveThemeResourceCacheEntry()
	/// </remarks>
	public void RemoveCacheEntry(ResourceDictionary dictionary, SpecializedResourceDictionary.ResourceKey key)
	{
		_cache.Remove((dictionary, key));
	}

	/// <summary>
	/// Clears all cached entries. Called at the end of a theme walk.
	/// </summary>
	public void Clear()
	{
		_cache.Clear();
	}
}
