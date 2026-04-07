#nullable enable

using System;
using System.Collections.Generic;
using Uno.Disposables;
using Uno.UI.DataBinding;

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

	// Key: (ResourceDictionary, ResourceKey) -> weak reference to resolved value.
	// ResourceDictionary uses reference equality (default for classes).
	// WinUI uses xref::weakref_ptr<CDependencyObject> for cached values,
	// so we use ManagedWeakReference to match — the cache must not extend object lifetimes.
	private readonly Dictionary<(ResourceDictionary Dict, SpecializedResourceDictionary.ResourceKey Key), ManagedWeakReference> _cache = new();

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
	/// Only returns values when a caching session is active.
	/// </summary>
	/// <remarks>
	/// MUX Reference: ThemeWalkResourceCache::TryGetCachedResource()
	/// WinUI guards with if (m_isCachingThemeResources) and uses weakref_ptr::lock_noref.
	/// </remarks>
	public bool TryGetCachedValue(ResourceDictionary dictionary, SpecializedResourceDictionary.ResourceKey key, out object? value)
	{
		if (!_isCachingThemeResources)
		{
			value = null;
			return false;
		}

		if (_cache.TryGetValue((dictionary, key), out var weakRef) && weakRef.TryGetTarget<object>(out var target))
		{
			value = target;
			return true;
		}

		value = null;
		return false;
	}

	/// <summary>
	/// Caches a resolved resource value for the given dictionary and key.
	/// Only caches when a session is active (via BeginCachingThemeResources),
	/// to prevent the global dictionary from retaining strong references
	/// to per-instance ResourceDictionaries indefinitely.
	/// </summary>
	/// <remarks>
	/// MUX Reference: ThemeWalkResourceCache::AddCachedResource()
	/// WinUI guards with if (m_isCachingThemeResources) and stores a weakref_ptr.
	/// </remarks>
	public void CacheValue(ResourceDictionary dictionary, SpecializedResourceDictionary.ResourceKey key, object? value)
	{
		if (!_isCachingThemeResources || value is null)
		{
			return;
		}

		var cacheKey = (dictionary, key);
		if (!_cache.ContainsKey(cacheKey))
		{
			_cache[cacheKey] = WeakReferencePool.RentWeakReference(this, value);
		}
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
