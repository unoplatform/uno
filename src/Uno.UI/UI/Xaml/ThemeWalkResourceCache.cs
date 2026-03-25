#nullable enable

using System;
using System.Collections.Generic;
using Uno.Disposables;

namespace Microsoft.UI.Xaml;

/// <summary>
/// Global theme resource cache that prevents redundant dictionary lookups when many elements
/// reference the same resource key from the same dictionary during a single theme change walk.
/// </summary>
/// <remarks>
/// MUX Reference: ThemeWalkResourceCache in ThemeWalkResourceCache.h
/// In WinUI, this is stored on CCoreServices (one global instance). Resources are cached
/// during theme walks and cleared when the walk completes. Reentrant-safe: nested calls
/// return no-op guards.
/// </remarks>
internal sealed class ThemeWalkResourceCache
{
	/// <summary>
	/// Global singleton, matching WinUI's CCoreServices::m_themeWalkResourceCache.
	/// Safe because theme walks are UI-thread-only and serialized.
	/// </summary>
	internal static ThemeWalkResourceCache Instance { get; } = new();

	// Key: (ResourceDictionary, ResourceKey) -> resolved value
	// ResourceDictionary uses reference equality (default for classes).
	private readonly Dictionary<(ResourceDictionary Dict, SpecializedResourceDictionary.ResourceKey Key), object?> _cache = new();

	private bool _isCachingThemeResources;

	/// <summary>
	/// Begins a caching session. Returns an IDisposable that clears the cache when disposed.
	/// Reentrant-safe: nested calls return no-op disposables.
	/// </summary>
	/// <remarks>
	/// MUX Reference: ThemeWalkResourceCache::BeginCachingThemeResources()
	/// Called at the start of:
	/// - Theme change walks (CCoreServices::NotifyThemeChange)
	/// - UpdateLayout (elements entering tree)
	/// - AppBarButton VSM updates
	/// </remarks>
	internal IDisposable BeginCachingThemeResources()
	{
		if (!_isCachingThemeResources)
		{
			_isCachingThemeResources = true;
			return Disposable.Create(() =>
			{
				_isCachingThemeResources = false;
				_cache.Clear();
			});
		}

		return Disposable.Empty;
	}

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
	/// Clears all cached entries.
	/// </summary>
	public void Clear()
	{
		_cache.Clear();
	}
}
