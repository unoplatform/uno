#nullable enable

using System;
using System.Collections.Generic;
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
///
/// WinUI cache key (ThemeWalkResourceCache.h:55):
///   std::tuple&lt;CResourceDictionary*, Theming::Theme, xstring_ptr, xref::weakref_ptr&lt;CDependencyObject&gt;&gt;
/// The active theme is part of the key because different subtrees can have different
/// RequestedTheme pushed (element-level theming islands), so the same dict+key can
/// legitimately resolve to different values depending on the active theme context.
/// </remarks>
internal sealed class ThemeWalkResourceCache
{
	/// <summary>
	/// Global singleton, matching WinUI's CCoreServices::m_themeWalkResourceCache.
	/// Safe because theme walks are UI-thread-only and serialized.
	/// </summary>
	internal static ThemeWalkResourceCache Instance { get; } = new();

	// Key: (ResourceDictionary, ResourceKey, ActiveTheme) -> weak reference to resolved value.
	// MUX Reference: ThemeWalkResourceCache.h:55 uses (CResourceDictionary*, Theming::Theme, xstring_ptr).
	// ResourceDictionary uses reference equality (default for classes).
	// WinUI uses xref::weakref_ptr<CDependencyObject> for cached values,
	// so we use ManagedWeakReference to match — the cache must not extend object lifetimes.
	private readonly Dictionary<(ResourceDictionary Dict, SpecializedResourceDictionary.ResourceKey Key, SpecializedResourceDictionary.ResourceKey ActiveTheme), ManagedWeakReference> _cache = new();

	private bool _isCachingThemeResources;

	/// <summary>
	/// Begins a caching session. Returns a struct guard that clears the cache when disposed.
	/// Reentrant-safe: nested calls return no-op guards.
	/// Uses a struct to avoid heap allocation (WinUI uses stack-allocated wil::scope_exit).
	/// </summary>
	/// <remarks>
	/// MUX Reference: ThemeWalkResourceCache::BeginCachingThemeResources()
	/// Called at the start of:
	/// - Theme change walks (CCoreServices::NotifyThemeChange)
	/// - UpdateLayout (elements entering tree)
	/// - AppBarButton VSM updates
	/// </remarks>
	internal CacheSession BeginCachingThemeResources()
	{
		if (!_isCachingThemeResources)
		{
			_isCachingThemeResources = true;
			return new CacheSession(this);
		}

		return default; // re-entrant call: returns a CacheSession with null owner, so Dispose() is a no-op
	}

	/// <summary>
	/// Zero-allocation disposable guard for a caching session.
	/// When used with <c>using var</c>, the compiler calls Dispose() directly
	/// on the struct without boxing to IDisposable.
	/// </summary>
	internal readonly struct CacheSession : IDisposable
	{
		private readonly ThemeWalkResourceCache? _owner;

		internal CacheSession(ThemeWalkResourceCache owner) => _owner = owner;

		public void Dispose()
		{
			if (_owner is not null)
			{
				_owner._isCachingThemeResources = false;
				_owner._cache.Clear();
			}
		}
	}

	/// <summary>
	/// Tries to retrieve a cached resource value.
	/// Only returns values when a caching session is active.
	/// </summary>
	/// <remarks>
	/// MUX Reference: ThemeWalkResourceCache::TryGetCachedResource()
	/// WinUI guards with if (m_isCachingThemeResources) and uses weakref_ptr::lock_noref.
	/// WinUI matches on (dictionary, subTreeTheme, resourceKey).
	/// </remarks>
	public bool TryGetCachedValue(ResourceDictionary dictionary, SpecializedResourceDictionary.ResourceKey key, out object? value)
	{
		if (!_isCachingThemeResources)
		{
			value = null;
			return false;
		}

		var activeTheme = ResourceDictionary.GetActiveTheme();
		if (_cache.TryGetValue((dictionary, key, activeTheme), out var weakRef) && weakRef.TryGetTarget<object>(out var target))
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
	/// WinUI keys by (dictionary, subTreeTheme, resourceKey).
	/// </remarks>
	public void CacheValue(ResourceDictionary dictionary, SpecializedResourceDictionary.ResourceKey key, object? value)
	{
		if (!_isCachingThemeResources || value is null)
		{
			return;
		}

		var activeTheme = ResourceDictionary.GetActiveTheme();
		var cacheKey = (dictionary, key, activeTheme);
		if (!_cache.ContainsKey(cacheKey))
		{
			_cache[cacheKey] = WeakReferencePool.RentWeakReference(this, value);
		}
	}

	/// <summary>
	/// Removes all cached entries for a given resource key across all dictionaries and themes.
	/// Called when a resource is added/removed from a dictionary during the theme walk
	/// (e.g., when UpdateLayout calls out to app code that modifies dictionaries).
	/// </summary>
	/// <remarks>
	/// MUX Reference: ThemeWalkResourceCache::RemoveThemeResourceCacheEntry()
	/// WinUI removes by key only (not dictionary), because a new/removed resource can
	/// shadow/unshadow entries from other dictionaries in the lookup chain.
	/// </remarks>
	public void RemoveCacheEntry(SpecializedResourceDictionary.ResourceKey key)
	{
		if (!_isCachingThemeResources || _cache.Count == 0)
		{
			return;
		}

		// Remove entries for all dictionaries and themes since the resource itself changed.
		List<(ResourceDictionary Dict, SpecializedResourceDictionary.ResourceKey Key, SpecializedResourceDictionary.ResourceKey ActiveTheme)>? keysToRemove = null;
		foreach (var entry in _cache.Keys)
		{
			if (entry.Key.Equals(key))
			{
				keysToRemove ??= new();
				keysToRemove.Add(entry);
			}
		}

		if (keysToRemove is not null)
		{
			foreach (var cacheKey in keysToRemove)
			{
				_cache.Remove(cacheKey);
			}
		}
	}

	/// <summary>
	/// Clears all cached entries.
	/// </summary>
	public void Clear()
	{
		_cache.Clear();
	}
}
