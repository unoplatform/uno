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
/// MUX Reference: ThemeWalkResourceCache in ThemeWalkResourceCache.h / .cpp
/// In WinUI, this is stored on CCoreServices (one global instance). Resources are cached
/// during theme walks and cleared when the walk completes. Reentrant-safe: nested calls
/// return no-op guards.
///
/// WinUI cache item (ThemeWalkResourceCache.h:55):
///   std::tuple&lt;CResourceDictionary*, Theming::Theme, xstring_ptr, xref::weakref_ptr&lt;CDependencyObject&gt;&gt;
/// The first three elements are the lookup key — (dictionary, theme, resource key); the fourth is the
/// cached value, held as a weak reference. The theme is part of the key because different subtrees can
/// have different themes (element-level theming islands), so the same dict+key can legitimately resolve
/// to different values depending on the theme. WinUI's theme is the BASE value (Theming::GetBaseValue),
/// set on the cache by CCoreServices::SetRequestedThemeForSubTree → SetSubTreeTheme (xcpcore.cpp:7903-7905).
///
/// Phase 3 (D3, Mechanism 1): Uno keys on the same (Dict, Theme, Key) tuple, with the value held as a
/// ManagedWeakReference (== xref::weakref_ptr). The one mechanism difference from WinUI is that the
/// owner's effective theme is threaded in as a method parameter rather than read from a stored
/// m_subTreeTheme ambient — consistent with the rest of the resolution leaf (architecture.md §6), which
/// avoids reintroducing process-global mutable theme state. Behavior is identical: the cached theme is
/// the same base value WinUI would have stored.
/// </remarks>
internal sealed class ThemeWalkResourceCache
{
	/// <summary>
	/// Global singleton, matching WinUI's CCoreServices::m_themeWalkResourceCache.
	/// Safe because theme walks are UI-thread-only and serialized.
	/// </summary>
	internal static ThemeWalkResourceCache Instance { get; } = new();

	// Key: (ResourceDictionary, Theme, ResourceKey) -> weak reference to the resolved value.
	// MUX Reference: ThemeWalkResourceCache.h:55 — key is (CResourceDictionary*, Theming::Theme,
	// xstring_ptr); the value is xref::weakref_ptr<CDependencyObject>. The Theme is the BASE value
	// (Theming::GetBaseValue), as stored by CCoreServices::SetRequestedThemeForSubTree (xcpcore.cpp:7903).
	// ResourceDictionary uses reference equality (default for classes). We use ManagedWeakReference for the
	// value to match xref::weakref_ptr — the cache must not extend object lifetimes.
	// Phase 3 (D3, Mechanism 1): the theme slot is the per-object Theme (base value) threaded in by the
	// caller, replacing the prior "Light"/"Dark" ResourceKey read from the process-global GetActiveTheme().
	private readonly Dictionary<(ResourceDictionary Dict, Theme Theme, SpecializedResourceDictionary.ResourceKey Key), ManagedWeakReference> _cache = new();

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
				_owner.ReturnAllWeakReferences();
				_owner._cache.Clear();
			}
		}
	}

	/// <summary>
	/// Tries to retrieve a cached resource value.
	/// Only returns values when a caching session is active.
	/// </summary>
	/// <remarks>
	/// MUX Reference: ThemeWalkResourceCache::TryGetCachedResource() (ThemeWalkResourceCache.cpp:50-74).
	/// WinUI guards with if (m_isCachingThemeResources) and uses weakref_ptr::lock_noref, matching on
	/// (dictionary, m_subTreeTheme, resourceKey). Phase 3 (Mechanism 1): the subtree theme is threaded in
	/// as <paramref name="theme"/> (the resolving owner's effective theme) instead of read from a stored
	/// ambient, then normalized to its base value to match WinUI's m_subTreeTheme (xcpcore.cpp:7903).
	/// </remarks>
	public bool TryGetCachedValue(ResourceDictionary dictionary, SpecializedResourceDictionary.ResourceKey key, Theme theme, out object? value)
	{
		if (!_isCachingThemeResources)
		{
			value = null;
			return false;
		}

		var baseTheme = Theming.GetBaseValue(theme);
		if (_cache.TryGetValue((dictionary, baseTheme, key), out var weakRef) && weakRef.TryGetTarget<object>(out var target))
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
	/// MUX Reference: ThemeWalkResourceCache::AddCachedResource() (ThemeWalkResourceCache.cpp:76-98).
	/// WinUI guards with if (m_isCachingThemeResources) and stores a weakref_ptr, keying by
	/// (dictionary, m_subTreeTheme, resourceKey). Phase 3 (Mechanism 1): the subtree theme is threaded in
	/// as <paramref name="theme"/> and normalized to its base value, matching WinUI's m_subTreeTheme.
	/// </remarks>
	public void CacheValue(ResourceDictionary dictionary, SpecializedResourceDictionary.ResourceKey key, Theme theme, object? value)
	{
		if (!_isCachingThemeResources || value is null)
		{
			return;
		}

		var baseTheme = Theming.GetBaseValue(theme);
		var cacheKey = (dictionary, baseTheme, key);
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
		List<(ResourceDictionary Dict, Theme Theme, SpecializedResourceDictionary.ResourceKey Key)>? keysToRemove = null;
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
				if (_cache.Remove(cacheKey, out var weakRef))
				{
					WeakReferencePool.ReturnWeakReference(this, weakRef);
				}
			}
		}
	}

	/// <summary>
	/// Clears all cached entries, returning rented weak references to the pool.
	/// </summary>
	public void Clear()
	{
		ReturnAllWeakReferences();
		_cache.Clear();
	}

	private void ReturnAllWeakReferences()
	{
		foreach (var weakRef in _cache.Values)
		{
			WeakReferencePool.ReturnWeakReference(this, weakRef);
		}
	}
}
