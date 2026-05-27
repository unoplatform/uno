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

			DependencyObject? next = null;
			if (current is FrameworkElement fe)
			{
				next = fe.GetParent() as DependencyObject ?? fe.Parent;
				if (next is null && fe.TemplatedParent is DependencyObject tp)
				{
					next = tp;
				}
			}
			else
			{
				next = GetAssociatedObject(current);
			}

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
		// Walk the parent chain from the owner upwards, returning the first stored
		// Light/Dark theme we find. Falls back to TemplatedParent when the chain runs
		// out of visual-tree parents (control-template internals that are being
		// materialised but not yet attached to their templated owner). For non-UIElement
		// DependencyObjects that participate in the visual tree only via an
		// AssociatedObject (Microsoft.Xaml.Behaviors-style behaviours that have a
		// ThemeResource-bound property), the walk hops to AssociatedObject and continues
		// from there — without this, the dictionary lookup for the behaviour's bound
		// property falls back to Themes.Active and resolves against the wrong theme.
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

			DependencyObject? next = null;
			if (current is FrameworkElement fe)
			{
				next = fe.GetParent() as DependencyObject ?? fe.Parent;
				if (next is null && fe.TemplatedParent is DependencyObject tp)
				{
					next = tp;
				}
			}
			else
			{
				// Non-UIElement DependencyObject (e.g. Microsoft.Xaml.Behaviors Behavior,
				// trigger, condition). Reflect for an "AssociatedObject" of type
				// DependencyObject so behaviours attached to a visual element can resolve
				// theme resources against that element's inherited theme. Reflected
				// rather than statically referenced so Uno.UI does not take a hard
				// dependency on Microsoft.Xaml.Behaviors.
				next = GetAssociatedObject(current);
			}

			if (next is null)
			{
				break;
			}

			current = next;
		}

		return Theme.None;
	}

	private static DependencyObject? GetAssociatedObject(DependencyObject source)
	{
		// Walk the type hierarchy and pick the first "AssociatedObject" property declared on
		// any level. Using DeclaredOnly avoids AmbiguousMatchException when a generic subclass
		// shadows the base property (Microsoft.Xaml.Behaviors' Behavior<T> declares
		// AssociatedObject : T while base Behavior declares AssociatedObject : DependencyObject).
		// The most-derived declaration takes priority because it returns the more specific type,
		// which is what callers want; both return values are convertible to DependencyObject.
		const global::System.Reflection.BindingFlags flags =
			global::System.Reflection.BindingFlags.Instance
			| global::System.Reflection.BindingFlags.Public
			| global::System.Reflection.BindingFlags.DeclaredOnly;

		for (var t = source.GetType(); t is not null; t = t.BaseType)
		{
			var prop = t.GetProperty("AssociatedObject", flags);
			if (prop is not null)
			{
				return prop.GetValue(source) as DependencyObject;
			}
		}

		return null;
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
