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
		// When RefreshValue is invoked OUTSIDE an active theme walk (deferred refresh
		// from item-container generation, re-templating, animation key-frame application,
		// etc.) the global `_requestedThemeForSubTree` stack is empty and `GetActiveTheme()`
		// falls back to `Themes.Active`. If the application theme differs from a subtree's
		// element-level RequestedTheme (e.g. app=Dark while subtree=Light, or vice-versa),
		// the pinned dictionary's TryGetValue then selects the wrong Light/Dark sub-dictionary
		// and the brush resolves against the application theme rather than the owner's own
		// ActualTheme. Push the owner's element-level theme so the lookup honours the owner's
		// theme boundary; the matching pop runs in the finally block.
		var pushedOwnerTheme = PushOwnerThemeIfDifferent(owner);

		try
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
		finally
		{
			if (pushedOwnerTheme)
			{
				ResourceDictionary.PopRequestedThemeForSubTree();
			}
		}
	}

	// Push the owner element's stored theme onto the global subtree theme stack when it
	// differs from the current top, so the subsequent dictionary lookup selects the owner's
	// Light/Dark variant instead of falling back to `Themes.Active`. This matches the WinUI
	// invariant that a ThemeResource resolves against the element's inherited ActualTheme
	// even when refresh happens outside the theme-walk's push/pop scope. Returns true when
	// a push occurred; the caller is responsible for popping in a finally block.
	//
	// If the owner has no stored theme yet (Theme.None), walks up the visual-tree parent
	// chain to find the nearest ancestor with a stored theme. This covers the case where
	// default-style application or template materialisation for a newly-created element
	// runs BEFORE OnLoadingPartial has propagated themes from its ancestors — without the
	// walk, the lookup would fall back to `Themes.Active` and resolve against the wrong
	// Light/Dark sub-dictionary for any subtree whose root pinned its own RequestedTheme.
	internal static bool PushOwnerThemeIfDifferent(DependencyObject? owner)
	{
		// During NotifyThemeChangedCore, the cascade pushes the NEW theme onto the
		// global stack BEFORE calling UpdateThemeBindings, and the element's stored
		// `_theme` is updated AFTERWARDS (by SetTheme at step 5). If we read the
		// chain's _theme here, we get the OLD theme and would overwrite the correctly
		// pushed NEW theme with the stale value — inverting the toggle by one step.
		// Skip the push entirely whenever the owner (or any UIElement ancestor in the
		// resolution chain) is currently inside a theme walk; the cascade's own push
		// is authoritative in that case.
		if (IsAnyAncestorProcessingThemeWalk(owner))
		{
			return false;
		}

		var baseTheme = ResolveInheritedBaseTheme(owner);
		if (baseTheme is not Theme.Light and not Theme.Dark)
		{
			return false;
		}

		var ownerThemeKey = baseTheme == Theme.Light ? "Light" : "Dark";
		var currentTheme = ResourceDictionary.GetActiveTheme();
		if (ownerThemeKey.Equals(currentTheme.Key, StringComparison.Ordinal))
		{
			return false;
		}

		ResourceDictionary.PushRequestedThemeForSubTree(ownerThemeKey);
		return true;
	}

	private static bool IsAnyAncestorProcessingThemeWalk(DependencyObject? start)
	{
		var current = start;
		var depth = 0;
		while (current is not null && depth < 80)
		{
			depth++;
			if (current is UIElement uiElement && uiElement.IsProcessingThemeWalk)
			{
				return true;
			}

			var next = GetThemeResolutionParent(current);
			if (next is null)
			{
				break;
			}

			current = next;
		}

		return false;
	}

	private static Theme ResolveInheritedBaseTheme(DependencyObject? owner)
	{
		// Walk up from the owner, returning the first stored Light/Dark theme we find.
		// Parent resolution (including the non-UIElement behaviour case) is handled by
		// GetThemeResolutionParent.
		var current = owner;
		var depth = 0;
		while (current is not null && depth < 80)
		{
			depth++;
			if (current is UIElement uiElement)
			{
				var baseTheme = Theming.GetBaseValue(uiElement.GetTheme());
				if (baseTheme is Theme.Light or Theme.Dark)
				{
					return baseTheme;
				}
			}

			var next = GetThemeResolutionParent(current);
			if (next is null)
			{
				break;
			}

			current = next;
		}

		return Theme.None;
	}

	// Resolves the next ancestor to consider when walking up the tree for theme resolution.
	//
	// For FrameworkElements we prefer the store parent (DependencyObjectStore.Parent), then
	// fall back to the publicly-visible logical Parent and finally the TemplatedParent. The
	// latter two can legitimately differ from the store parent (see
	// FrameworkElement.LogicalParentOverride / VisualParent) and cover control-template
	// internals that are being materialised before they are attached to their templated owner.
	//
	// For any other DependencyObject we follow the store parent directly. Non-UIElement
	// DependencyObjects participate in the tree exclusively through Store.Parent: a
	// Microsoft.Xaml.Behaviors-style behaviour added through an Interaction.Behaviors
	// (DependencyObjectCollection) attached property has its Store.Parent wired to the host
	// element by DependencyObjectCollection.OnAdded / DependencyObjectStore.UpdateAutoParent —
	// the same chain that already powers {Binding} DataContext inheritance and ResourceDictionary
	// resolution (GetResourceDictionaries walks these parents via VisualTreeHelper.GetParent,
	// which itself resolves to Store.Parent on Skia/WASM). Following Store.Parent therefore
	// reaches the same host element the old reflected "AssociatedObject" hop did, without taking
	// a hard dependency on, or reflecting against, Microsoft.Xaml.Behaviors.
	private static DependencyObject? GetThemeResolutionParent(DependencyObject current)
	{
		if (current is FrameworkElement fe)
		{
			return fe.GetParent() as DependencyObject
				?? fe.Parent
				?? fe.TemplatedParent as DependencyObject;
		}

		return current.GetParent() as DependencyObject;
	}

	/// <summary>
	/// Re-resolves the theme resource value with full tree-walk fallback.
	/// Used during initial resolution and hot-reload, NOT during theme change walks.
	/// </summary>
	public object? RefreshValueWithTreeWalk(DependencyObject? owner, ThemeWalkResourceCache? cache = null)
	{
		// See PushOwnerThemeIfDifferent docs on RefreshValue above — same reasoning applies to
		// the tree-walk-fallback path so the dictionary lookup honours the owner's element-level theme.
		var pushedOwnerTheme = PushOwnerThemeIfDifferent(owner);

		try
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
		finally
		{
			if (pushedOwnerTheme)
			{
				ResourceDictionary.PopRequestedThemeForSubTree();
			}
		}
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
