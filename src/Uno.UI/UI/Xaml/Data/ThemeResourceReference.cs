#nullable enable

using System;
using Uno.Foundation.Logging;
using Uno.UI.DataBinding;

namespace Microsoft.UI.Xaml.Data;

/// <summary>
/// Represents a theme resource binding that pins the providing <see cref="ResourceDictionary"/>
/// for efficient re-resolution on theme change.
/// </summary>
/// <remarks>
/// This is the Uno equivalent of WinUI's CThemeResource (ThemeResource.h/ThemeResource.cpp).
/// Instead of re-walking the visual tree on every theme change, we store a weak reference to
/// the specific ResourceDictionary that provided the value. On theme change, RefreshValue()
/// re-queries that same dictionary, which automatically returns the new theme's value since
/// its active theme sub-dictionary switched.
/// </remarks>
internal sealed class ThemeResourceReference
{
	private WeakReference<ResourceDictionary>? _targetDictionary;

	/// <summary>
	/// The resource key to look up in the pinned dictionary.
	/// </summary>
	public SpecializedResourceDictionary.ResourceKey ResourceKey { get; }

	/// <summary>
	/// The last resolved value. Updated on theme change via RefreshValue().
	/// If the pinned dictionary is dead and fallback also fails, this cached value is returned
	/// (matching WinUI's CThemeResource::m_lastResolvedThemeValue behavior).
	/// </summary>
	public object? LastResolvedValue { get; internal set; }

	/// <summary>
	/// Whether this reference has been resolved at least once.
	/// Distinguishes "not yet resolved" (deferred) from "resolved to null" (e.g. x:Null).
	/// </summary>
	/// <remarks>
	/// MUX Reference: WinUI uses CValue type system — valueAny means "unset/unresolved",
	/// valueNull means "resolved to null". In C# both map to null, so we track this explicitly.
	/// See CThemeResource::SetLastResolvedValue and CValue::IsUnset() in CValue.h.
	/// </remarks>
	internal bool IsResolved { get; set; }

	/// <summary>
	/// Assembly parse context for fallback resolution when the pinned dictionary is dead.
	/// </summary>
	public object? ParseContext { get; }

	/// <summary>
	/// The resource update reason flags.
	/// </summary>
	public ResourceUpdateReason UpdateReason { get; }

	/// <summary>
	/// The precedence at which this theme resource is set on the target property.
	/// </summary>
	public DependencyPropertyValuePrecedences Precedence { get; }

	/// <summary>
	/// The binding path for VisualState Setters that target via Setter.Target path.
	/// Null for direct property bindings.
	/// </summary>
	public BindingPath? SetterBindingPath { get; }

	public ThemeResourceReference(
		SpecializedResourceDictionary.ResourceKey resourceKey,
		ResourceDictionary? targetDictionary,
		object? initialValue,
		bool isResolved,
		object? parseContext,
		ResourceUpdateReason updateReason,
		DependencyPropertyValuePrecedences precedence,
		BindingPath? setterBindingPath = null)
	{
		ResourceKey = resourceKey;
		_targetDictionary = targetDictionary is not null
			? new WeakReference<ResourceDictionary>(targetDictionary)
			: null;
		LastResolvedValue = initialValue;
		IsResolved = isResolved;
		ParseContext = parseContext;
		UpdateReason = updateReason;
		Precedence = precedence;
		SetterBindingPath = setterBindingPath;
	}

	/// <summary>
	/// Re-resolves the theme resource value from the pinned dictionary only.
	/// Used during theme change walks where the ancestor walk is handled by the caller.
	/// </summary>
	/// <param name="owner">The DependencyObject that owns this theme resource binding.</param>
	/// <param name="cache">Optional per-walk cache to avoid redundant dictionary lookups.</param>
	/// <returns>The resolved value for the current theme.</returns>
	/// <remarks>
	/// MUX Reference: CThemeResource::RefreshValue() in ThemeResource.cpp (lines 64-129)
	///
	/// Looks up the key in the pinned dictionary. If the dictionary is dead (teardown),
	/// returns LastResolvedValue. Does NOT do tree-walk — that's handled by
	/// UpdateAllThemeReferences (matching WinUI's UpdateThemeReference which walks
	/// ancestors before calling RefreshValue).
	/// </remarks>
	public object? RefreshValue(DependencyObject? owner, ThemeWalkResourceCache? cache = null)
	{
		// Try pinned dictionary (fast path)
		if (_targetDictionary is not null && _targetDictionary.TryGetTarget(out var dict))
		{
			// Check cache first
			if (cache is not null && cache.TryGetCachedValue(dict, ResourceKey, out var cachedValue))
			{
				SetResolvedValue(cachedValue);
				return cachedValue;
			}

			if (dict.TryGetValue(ResourceKey, out var value, shouldCheckSystem: false))
			{
				cache?.CacheValue(dict, ResourceKey, value);
				SetResolvedValue(value);
				return value;
			}

			// MUX Reference: ThemeResource.cpp:96-123 — WinUI raises AG_E_PARSER_FAILED_RESOURCE_FIND
			// when the pinned dictionary is alive but doesn't contain the key. We log a warning
			// instead of throwing, since Uno has additional fallback paths.
			if (typeof(ThemeResourceReference).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(ThemeResourceReference).Log().Debug(
					$"Theme resource '{ResourceKey.Key}' not found in pinned dictionary, falling back.");
			}
		}

		// Uno extension: Try top-level as last resort (for hot-reload/unpinned refs)
		if (Uno.UI.ResourceResolver.TryTopLevelRetrieval(ResourceKey, ParseContext, out var topLevelValue))
		{
			SetResolvedValue(topLevelValue);
			return topLevelValue;
		}

		// WinUI: If target dictionary is dead, returns m_lastResolvedThemeValue
		return LastResolvedValue;
	}

	/// <summary>
	/// Re-resolves the theme resource value with full tree-walk fallback.
	/// Used during initial resolution and hot-reload, NOT during theme change walks.
	/// </summary>
	public object? RefreshValueWithTreeWalk(DependencyObject? owner, ThemeWalkResourceCache? cache = null)
	{
		// 1. Try pinned dictionary (fast path)
		if (_targetDictionary is not null && _targetDictionary.TryGetTarget(out var dict))
		{
			if (cache is not null && cache.TryGetCachedValue(dict, ResourceKey, out var cachedValue))
			{
				SetResolvedValue(cachedValue);
				return cachedValue;
			}

			if (dict.TryGetValue(ResourceKey, out var value, shouldCheckSystem: false))
			{
				cache?.CacheValue(dict, ResourceKey, value);
				SetResolvedValue(value);
				return value;
			}
		}

		// 2. Fallback: tree-walk from owner's position (handles hot-reload / re-parenting)
		if (owner is IDependencyObjectStoreProvider provider)
		{
			var dictionaries = provider.Store.GetResourceDictionaries(includeAppResources: false);
			if (dictionaries is not null)
			{
				foreach (var walkDict in dictionaries)
				{
					if (walkDict.TryGetValue(ResourceKey, out var value, out var newProvidingDict, shouldCheckSystem: false))
					{
						// Re-pin the new dictionary
						_targetDictionary = new WeakReference<ResourceDictionary>(newProvidingDict);
						cache?.CacheValue(newProvidingDict, ResourceKey, value);
						SetResolvedValue(value);
						return value;
					}
				}
			}
		}

		// 3. Try top-level resources as last resort
		if (Uno.UI.ResourceResolver.TryTopLevelRetrieval(ResourceKey, ParseContext, out var topLevelValue))
		{
			SetResolvedValue(topLevelValue);
			return topLevelValue;
		}

		// 4. Return cached value (WinUI behavior when target dictionary is dead)
		return LastResolvedValue;
	}

	/// <summary>
	/// Updates the pinned target dictionary. Used when re-pinning after hot-reload
	/// or when the initial resolution captured a new dictionary.
	/// </summary>
	internal void SetTargetDictionary(ResourceDictionary? dictionary)
	{
		_targetDictionary = dictionary is not null
			? new WeakReference<ResourceDictionary>(dictionary)
			: null;
	}

	/// <summary>
	/// Creates a clone of this reference for use on a target DependencyObject (e.g., when applying
	/// a Style Setter's deferred ThemeResource to a control).
	/// </summary>
	/// <remarks>
	/// MUX Reference: When a Style Setter with PreserveThemeResourceExtension is applied to a control,
	/// a new CThemeResource binding is created on the target from the Setter's stored CThemeResource.
	/// </remarks>
	internal ThemeResourceReference CloneForTarget(DependencyPropertyValuePrecedences precedence, BindingPath? setterBindingPath = null)
	{
		var clone = new ThemeResourceReference(
			ResourceKey,
			targetDictionary: null, // will be set below
			LastResolvedValue,
			IsResolved,
			ParseContext,
			UpdateReason,
			precedence,
			setterBindingPath);

		// Share the same pinned dictionary
		clone._targetDictionary = _targetDictionary;
		return clone;
	}

	private void SetResolvedValue(object? value)
	{
		LastResolvedValue = value;
		IsResolved = true;
	}
}
