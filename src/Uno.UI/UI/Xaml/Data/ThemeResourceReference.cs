// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference ThemeResource.h & ThemeResource.cpp (CThemeResource), commit fc2f82117

#nullable enable

using System;
using Uno.Foundation.Logging;
using Uno.UI.DataBinding;

namespace Microsoft.UI.Xaml.Data;

/// <summary>
/// Represents a theme resource binding that pins the providing <see cref="ResourceDictionary"/>
/// for efficient re-resolution on theme change. This is the Uno counterpart of WinUI's
/// CThemeResource (ThemeResource.h/ThemeResource.cpp).
/// </summary>
/// <remarks>
/// On theme change, <see cref="RefreshValue"/> re-queries the pinned dictionary, which
/// automatically returns the new theme's value since its active theme sub-dictionary switched.
///
/// NOT PORTED from CThemeResource (no Uno call path):
/// - AddRef/Release and m_cRef — lifetime is GC-managed.
/// - CThemeResource(CThemeResourceExtension*) — Uno has no CThemeResourceExtension peer; the
///   parse-time {ThemeResource} flow (ResourceResolver.ApplyResource → TryStaticRetrieval)
///   constructs this type directly. <see cref="CloneForTarget"/> covers the copy-from-existing
///   shape that constructor serves.
/// - GetValue(CValue*) (ThemeResource.cpp:53-61) — callers refresh and read
///   <see cref="LastResolvedValue"/> directly (DependencyObjectStore.UpdateThemeReference).
/// - SetInitialValueAndTargetDictionary (ThemeResource.cpp:39-51) — folded into the constructor
///   (target dictionary + initial value are supplied at construction).
/// - SetThemeResourceBinding (ThemeResource.cpp:171-200) — the PreserveThemeResourceExtension /
///   Style-Setter dispatch lives in Uno's Setter/Style application
///   (ResourceResolver.ApplyThemeResource); the plain branch is
///   DependencyObjectStore.SetThemeResourceBinding (Theming.cpp:349-400 port).
/// - static LookupResource (ThemeResource.cpp:202-256) — the parse-time resolve-and-pin lives in
///   ResourceResolver.ApplyResource/TryStaticRetrieval.
/// - m_themeWalkResourceCache — Uno passes the walk cache as a parameter to
///   <see cref="RefreshValue"/> instead of capturing it per-reference.
/// </remarks>
internal sealed class ThemeResourceReference
{
	// MUX Reference: CThemeResource::m_isValueFromInitialTheme (ThemeResource.h:74).
	// "Was the last resolved value from the app's initial theme?" — maintained by
	// SetLastResolvedValue, consumed by the UpdateThemeReference refresh gate (Theming.cpp:340).
	internal bool IsValueFromInitialTheme { get; private set; } = true;

	/// <summary>
	/// The resource key to look up in the pinned dictionary.
	/// </summary>
	/// <remarks>MUX Reference: CThemeResource::m_strResourceKey (ThemeResource.h:75).</remarks>
	public SpecializedResourceDictionary.ResourceKey ResourceKey { get; }

	// Last resolved theme value. If no theme switch has happened, this will be the initial theme value resolved during parse.
	/// <remarks>
	/// MUX Reference: CThemeResource::m_lastResolvedThemeValue (ThemeResource.h:82). If the pinned
	/// dictionary is dead and fallback also fails, this cached value is returned.
	/// </remarks>
	public object? LastResolvedValue { get; private set; }

	// ThemeDictionaries from which theme value can be obtained
	// Use WeakRef to avoid cycles between theme resources and their dictionary.
	/// <remarks>MUX Reference: CThemeResource::m_pTargetDictionaryWeakRef (ThemeResource.h:86).</remarks>
	private WeakReference<ResourceDictionary>? _targetDictionary;

	/// <summary>
	/// Whether this reference has been resolved at least once.
	/// Distinguishes "not yet resolved" (deferred) from "resolved to null" (e.g. x:Null).
	/// </summary>
	/// <remarks>
	/// Uno-specific: WinUI uses the CValue type system — valueAny means "unset/unresolved",
	/// valueNull means "resolved to null". In C# both map to null, so we track this explicitly
	/// (the CValue::IsUnset() analog, see SetLastResolvedValue).
	/// </remarks>
	internal bool IsResolved { get; private set; }

	/// <summary>
	/// Uno-specific: assembly parse context for fallback resolution when the pinned dictionary is dead.
	/// </summary>
	public object? ParseContext { get; }

	/// <summary>
	/// Uno-specific: the resource update reason flags.
	/// </summary>
	public ResourceUpdateReason UpdateReason { get; }

	/// <summary>
	/// Uno-specific: the precedence at which this theme resource is set on the target property.
	/// WinUI threads the equivalent BaseValueSource through SetThemeResourceBinding instead.
	/// </summary>
	public DependencyPropertyValuePrecedences Precedence { get; }

	/// <summary>
	/// Uno-specific: the binding path for VisualState Setters that target via Setter.Target path.
	/// Null for direct property bindings.
	/// </summary>
	public BindingPath? SetterBindingPath { get; }

	// MUX Reference: CThemeResource::CThemeResource(ThemeWalkResourceCache*) +
	// SetInitialValueAndTargetDictionary (ThemeResource.cpp:32-51): construct with the key, pin the
	// providing dictionary, and store the initially resolved value.
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
	/// <param name="cache">Optional per-walk cache to avoid redundant dictionary lookups.</param>
	/// <returns>The resolved value for the ambient theme.</returns>
	/// <remarks>
	/// MUX Reference: CThemeResource::RefreshValue() in ThemeResource.cpp (lines 63-129)
	///
	/// Looks up the key in the pinned dictionary. The Light/Dark sub-dictionary is selected by the
	/// ambient active theme (ResourceDictionary.GetActiveBaseTheme — the core
	/// requested-theme-for-subtree slot scoped by the caller from the owner's theme, else the app
	/// base theme), matching CResourceDictionary::EnsureActiveThemeDictionary reading the core slot
	/// (Resources.cpp:764-768). The same ambient theme keys the ThemeWalkResourceCache, matching
	/// WinUI's m_subTreeTheme cache key. If the dictionary is dead (teardown), returns
	/// LastResolvedValue. Does NOT do tree-walk — that's handled by UpdateThemeReference
	/// (matching WinUI's UpdateThemeReference which walks ancestors before calling RefreshValue).
	/// </remarks>
	public object? RefreshValue(ThemeWalkResourceCache? cache = null, bool preferAppResourceOverride = false)
	{
		var theme = ResourceDictionary.GetActiveBaseTheme();

		// Get value from target dictionary.
		// If target dictionary has been released, which can occur during teardown, return
		// last resolved value.
		if (_targetDictionary is not null && _targetDictionary.TryGetTarget(out var dict))
		{
			// Check cache first
			if (cache is not null && cache.TryGetCachedValue(dict, ResourceKey, theme, out var cachedValue))
			{
				SetLastResolvedValue(cachedValue);
				return cachedValue;
			}

			if (dict.TryGetValue(ResourceKey, out var value, shouldCheckSystem: false))
			{
				// MUX: Resources.cpp:668-682 (GetKeyFromThemeDictionariesNoRef) — "Always allow
				// Application.Resources to override values found in the global ThemeDictionaries"
				// (GetKeyOverrideFromApplicationResourcesNoRef). When this ref is pinned to a global/framework
				// theme dictionary (a control template's {ThemeResource} pins to generic/Fluent), an app-level
				// override of the same key must still win on re-resolution outside an ancestor walk — e.g. a
				// storyboard keyframe re-begun on a visual-state re-entry would otherwise revert to the stock
				// value. The caller passes preferAppResourceOverride for those framework-pinned cases (keyframes);
				// dict.IsSystemDictionary covers Uno.UI's own generic dictionaries.
				if ((preferAppResourceOverride || dict.IsSystemDictionary)
					&& Uno.UI.ResourceResolver.TryApplicationResourceOverride(ResourceKey, out var appOverride))
				{
					value = appOverride;
				}

				// Cache this value so that we can skip the resource dictionary lookup
				// for other theme resources with the same key.
				cache?.CacheValue(dict, ResourceKey, theme, value);

				// Cache the value locally for this theme resource object.
				SetLastResolvedValue(value);
				return value;
			}

			// MUX Reference: ThemeResource.cpp:95-123 — WinUI re-runs the search with the resource
			// lookup logger and raises AG_E_PARSER_FAILED_RESOURCE_FIND when the pinned dictionary is
			// alive but doesn't contain the key. We log a warning instead of throwing, since Uno has
			// additional fallback paths.
			if (typeof(ThemeResourceReference).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(ThemeResourceReference).Log().Debug(
					$"Theme resource '{ResourceKey.Key}' not found in pinned dictionary, falling back.");
			}
		}

		// Uno extension: Try top-level as last resort (for hot-reload/unpinned refs)
		if (Uno.UI.ResourceResolver.TryTopLevelRetrieval(ResourceKey, ParseContext, out var topLevelValue))
		{
			SetLastResolvedValue(topLevelValue);
			return topLevelValue;
		}

		// WinUI: If target dictionary is dead, returns m_lastResolvedThemeValue
		return LastResolvedValue;
	}

	/// <summary>
	/// Stores a newly resolved value, maintaining the initial-theme bookkeeping.
	/// </summary>
	/// <remarks>
	/// MUX Reference: CThemeResource::SetLastResolvedValue in ThemeResource.cpp (lines 137-169).
	/// </remarks>
	internal void SetLastResolvedValue(object? value)
	{
		// MUX: m_isValueFromInitialTheme = m_lastResolvedThemeValue.IsUnset() — the stored value is
		// from the initial theme iff nothing had been resolved before this set (IsResolved is the
		// CValue::IsUnset() analog, see IsResolved remarks).
		IsValueFromInitialTheme = !IsResolved;

		// TODO Uno: WinUI unwraps CDependencyObjectWrapper and CManagedObjectReference and maps
		// CNullKeyedResource to null here (ThemeResource.cpp:142-167) — those wrappers work around
		// core/framework parenting when inserting into a ResourceDictionary; Uno's dictionaries store
		// CLR values directly, so no unwrapping applies.
		LastResolvedValue = value;
		IsResolved = true;
	}

	/// <summary>
	/// Uno-specific: updates the pinned target dictionary. Used when re-pinning after hot-reload
	/// or when the load-time tree walk captured a new providing dictionary.
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
	/// MUX Reference: the CThemeResource(CThemeResourceExtension*) copy construction
	/// (ThemeResource.cpp:22-30) — when a Style Setter with PreserveThemeResourceExtension is
	/// applied to a control, a new live binding is created on the target from the Setter's stored
	/// theme resource, sharing key, last resolved value, initial-theme flag and pinned dictionary.
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

		// Share the same pinned dictionary (and the initial-theme flag, like the C++ copy ctor).
		clone._targetDictionary = _targetDictionary;
		clone.IsValueFromInitialTheme = IsValueFromInitialTheme;
		return clone;
	}
}
