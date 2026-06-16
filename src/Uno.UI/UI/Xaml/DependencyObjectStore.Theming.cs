#nullable enable

// Port of WinUI Theming.cpp (CDependencyObject theme methods)
// MUX Reference: src/dxaml/xcp/components/DependencyObject/Theming.cpp
//
// In WinUI, all theming functions are methods on CDependencyObject.
// In Uno, CDependencyObject is represented by DependencyObjectStore.
// This partial contains every theme-related method in one place.

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.DataBinding;

namespace Microsoft.UI.Xaml;

public partial class DependencyObjectStore
{
	#region Per-object theme — WinUI: CDependencyObject::m_theme (CDependencyObject.h:1761)

	// MUX Reference: CDependencyObject.h:1757-1761
	//   Theming::Theme m_theme : 5;
	//   "Holds both the base theme (Light, Dark) and any HighContrast theme. ThemeNone indicates
	//    that the initial theme is being used, and that no theme change has occurred. ThemeNone is
	//    used to defer ThemeResourceExpression binding until the first theme change occurs, for
	//    better perf."
	//
	// In WinUI every CDependencyObject — not just elements — carries a resolved theme, established
	// at tree Enter (depends.cpp:1023-1048) and inherited from its (logical) inheritance parent.
	// In Uno it lives on the store that every DependencyObject owns, so non-UIElement DOs (brushes,
	// setters, storyboards) also carry a theme; UIElement/FrameworkElement keep GetTheme()/SetTheme()
	// as thin forwarders here.
	private Theme _theme = Theme.None;

	/// <summary>
	/// Gets the current per-object theme. Defaults to <see cref="Theme.None"/> (no theme established).
	/// </summary>
	/// <remarks>MUX Reference: CDependencyObject::GetTheme — CDependencyObject.h:1648.</remarks>
	internal Theme GetTheme() => _theme;

	/// <summary>
	/// Sets the per-object theme.
	/// </summary>
	internal void SetTheme(Theme theme) => _theme = theme;

	// MUX Reference: CDependencyObject.h:300-302
	//   XUINT32 fIsProcessingThemeWalk : 1;  // bit 16
	//   "Indicates whether the DO is currently processing themes. It is used to prevent stack
	//    overflows caused by cycles."
	// WinUI packs this with other lifecycle bits on every CDependencyObject (corep.h:224-348), so it
	// lives here on the store.
	private bool _isProcessingThemeWalk;

	/// <summary>
	/// Gets whether this object is currently processing a theme walk (re-entrancy guard).
	/// </summary>
	internal bool IsProcessingThemeWalk => _isProcessingThemeWalk;

	/// <summary>
	/// Sets whether this object is currently processing a theme walk.
	/// </summary>
	internal void SetIsProcessingThemeWalk(bool value) => _isProcessingThemeWalk = value;

	#endregion

	// The CDependencyObject::EnterImpl theme block (depends.cpp:1044-1069) and the enter-property
	// walks are ported in DependencyObjectStore.mux.cs and DependencyObjectStore.PropertySystem.mux.cs.

#if UNO_HAS_ENHANCED_LIFECYCLE
	#region Theme walk — WinUI: CDependencyObject::NotifyThemeChanged / NotifyThemeChangedCore (Theming.cpp lines 110-255)

	// Uno-specific: transient context of the walk this object is currently processing. WinUI threads
	// (theme, forceRefresh) through its recursive per-child NotifyThemeChanged calls
	// (Theming.cpp:220/:244); Uno's engine reaches non-UIElement property values through
	// UpdateChildResourceBindings, which has no walk parameters, so the walking store carries them
	// here while IsProcessingThemeWalk is set.
	private Theme _walkTheme;
	private bool _walkForceRefresh;

	// Valid only while IsProcessingThemeWalk — read by FrameworkElement.UpdateThemeBindings to
	// thread the walk context into the Resources dictionary's per-child notification.
	internal Theme WalkTheme => _walkTheme;
	internal bool WalkForceRefresh => _walkForceRefresh;

	/// <remarks>
	/// MUX Reference: CDependencyObject::NotifyThemeChanged — Theming.cpp:110-157.
	/// </remarks>
	internal void NotifyThemeChanged(Theme theme, bool forceRefresh = false)
	{
		// Make sure no funny business happens where someone tries to cast an unsigned int into
		// an invalid Theme value.
		global::System.Diagnostics.Debug.Assert(theme < Theme.Unused);

		// If IsProcessingEnterLeave is true, then this element is already part of the
		// theme walk.  This can happen, for instance, if a custom DP's value has
		// been set to some ancestor of this node.
		if (IsProcessingThemeWalk)
		{
			return;
		}

		// If this is a framework element, then get the requested theme.
		if (ActualInstance is FrameworkElement thisAsFe)
		{
			theme = thisAsFe.GetRequestedThemeOverride(theme);
		}

		// Has theme changed?
		if (theme == _theme && !forceRefresh)
		{
			return;
		}

		SetIsProcessingThemeWalk(true);

		var core = Uno.UI.Xaml.Core.CoreServices.Instance;
		bool removeRequestedTheme = false;
		var oldRequestedThemeForSubTree = core.GetRequestedThemeForSubTree();
		if (Theming.GetBaseValue(theme) != oldRequestedThemeForSubTree)
		{
			core.SetRequestedThemeForSubTree(theme);
			removeRequestedTheme = true;
		}

		// MUX Reference: CCoreServices::NotifyThemeChange() calls BeginCachingThemeResources()
		// before the theme walk (xcpcore.cpp:8015). Reentrant-safe: nested calls (recursive child
		// walks) get no-op guards.
		using var cacheGuard = core.ThemeWalkResourceCache.BeginCachingThemeResources();

		_walkTheme = theme;
		_walkForceRefresh = forceRefresh;

		try
		{
			// Notify children and properties of theme change
			NotifyThemeChangedCore(theme, forceRefresh);

			// Persist the theme on success
			_theme = theme;
		}
		finally
		{
			SetIsProcessingThemeWalk(false);
			if (removeRequestedTheme)
			{
				core.SetRequestedThemeForSubTree(oldRequestedThemeForSubTree);
			}
		}
	}

	/// <remarks>
	/// MUX Reference: CDependencyObject::NotifyThemeChangedCore — Theming.cpp:159-162. The virtual
	/// dispatch (CUIElement/CFrameworkElement/CPopup/CPopupRoot overrides) goes through the
	/// UIElement.NotifyThemeChangedCore chain, since Uno's DependencyObject is an interface.
	/// </remarks>
	private void NotifyThemeChangedCore(Theme theme, bool forceRefresh)
	{
		if (ActualInstance is UIElement uiElement)
		{
			uiElement.NotifyThemeChangedCore(theme, forceRefresh);
		}
		else
		{
			NotifyThemeChangedCoreImpl(theme, forceRefresh);
		}
	}

	// ignoreGetValueFailures currently addresses [Blue Bug 637457]: For setter values which are invalid, the value may not be resolvable.
	// This should only ever be set to true when being called from a Setter.
	/// <remarks>
	/// MUX Reference: CDependencyObject::NotifyThemeChangedCoreImpl — Theming.cpp:166-255.
	/// </remarks>
	internal void NotifyThemeChangedCoreImpl(Theme theme, bool forceRefresh, bool ignoreGetValueFailures = false)
	{
		global::System.Diagnostics.Debug.Assert(!ignoreGetValueFailures || ActualInstance is Setter);

		// Update theme references first, and skip them below in the property value notifications.
		// Notify field-backed and sparse property values of theme change, and notify the peer so
		// expressions can be refreshed (e.g. Binding.TargetNullValue might be a ThemeResource).
		//
		// Uno: the UpdateResourceBindings engine performs exactly these Theming.cpp:173-252 steps —
		// UpdateAllThemeReferences (:173), the property-value walk (:176-248, via
		// UpdateChildResourceBindings with the ShouldNotifyPropertyOfThemeChange filter and the
		// active-UIElement skip), and the binding-expression refresh (:252, the
		// FxCallbacks::DependencyObject_RefreshExpressionsOnThemeChange analog). Controls extend it
		// through their UpdateThemeBindings overrides exactly as their C++ NotifyThemeChangedCore
		// overrides extend the walk.
		// TODO Uno: the property-value propagation goes through the child store's
		// UpdateResourceBindings rather than per-child NotifyThemeChanged, so non-UIElement children
		// keep their per-object theme unchanged and resolve via the ambient slot; re-point to true
		// per-child NotifyThemeChanged recursion when the engine internals are aligned (Phase 5).
		if (ActualInstance is FrameworkElement fe)
		{
			fe.UpdateThemeBindings(ResourceUpdateReason.ThemeResource);
		}
		else
		{
			UpdateResourceBindings(ResourceUpdateReason.ThemeResource);
		}
	}

	#endregion
#endif

	#region Theme resource binding storage — WinUI: SetThemeResource / SetThemeResourceBinding (Theming.cpp lines 349-400)

	// Sets theme value on property. Also stores the theme reference so we can refresh the value on theme changes.
	/// <remarks>
	/// MUX Reference: CDependencyObject::SetThemeResourceBinding in Theming.cpp (lines 349-400).
	///
	/// The C++ sequence — push the owner's theme onto the core requested-theme-for-subtree slot
	/// (gated on !IsProcessingThemeWalk() &amp;&amp; m_theme != Theme::None, Theming.cpp:364-379),
	/// UpdateThemeReference(pThemeResource) refresh, GetLastResolvedThemeValue, SetValue, then
	/// SetThemeResource(pDP, pThemeResource) — is hosted end-to-end by the shared
	/// <see cref="UpdateThemeReference"/> path below, which inlines the slot push.
	///
	/// TODO Uno: WinUI's pModifiedValue bookkeeping (SetModifierValueBeingSet, Theming.cpp:355-362)
	/// and the freeze/unfreeze around SetValue (Theming.cpp:390-392) have no Uno analog — modified
	/// (animated) values are tracked per-precedence by the store, and Uno has no SimulateFreeze.
	/// </remarks>
	internal void SetThemeResourceBinding(DependencyProperty property, ThemeResourceReference themeRef, DependencyPropertyValuePrecedences? precedence = null)
	{
		// Resolve the effective precedence using the same logic as SetResourceBinding
		var effectivePrecedence = precedence ?? themeRef.Precedence;
		if (effectivePrecedence == DependencyPropertyValuePrecedences.Local && _overriddenPrecedences?.Count > 0)
		{
			effectivePrecedence = _overriddenPrecedences.Peek() ?? effectivePrecedence;
		}

		// Store the reference first so the staleness guard in UpdateThemeReference (the
		// Theming.cpp:297-298 analog) recognizes it; UpdateThemeReference stores it back after the
		// SetValue, exactly like the C++ SetThemeResource call at Theming.cpp:397.
		SetThemeResource(property, themeRef, effectivePrecedence);

		UpdateThemeReference(property, effectivePrecedence, themeRef, ActualInstance, cache: null);
	}

	// Stores the theme resource reference for a property — the map-store primitive.
	/// <remarks>
	/// MUX Reference: CDependencyObject::SetThemeResource — depends.cpp:1494-1503.
	/// Also used directly (registration without the resolve+apply sequence) by the Setter flows that
	/// resolve and apply the value themselves — see ResourceResolver.ApplyThemeResource and
	/// ApplyVisualStateSetter.
	/// </remarks>
	internal void SetThemeResource(DependencyProperty property, ThemeResourceReference themeRef, DependencyPropertyValuePrecedences? precedence = null)
	{
		_themeResources ??= new ThemeResourceMap();

		// Resolve the effective precedence using the same logic as SetResourceBinding
		var effectivePrecedence = precedence ?? themeRef.Precedence;
		if (effectivePrecedence == DependencyPropertyValuePrecedences.Local && _overriddenPrecedences?.Count > 0)
		{
			effectivePrecedence = _overriddenPrecedences.Peek() ?? effectivePrecedence;
		}

		_themeResources.Set(property, effectivePrecedence, themeRef);
	}

	#endregion

	#region Theme reference updates — WinUI: UpdateAllThemeReferences / UpdateThemeReference (Theming.cpp lines 260-346)

	/// <summary>
	/// Updates all <see cref="ThemeResourceReference"/> entries using WinUI's
	/// snapshot + clear + re-apply pattern.
	/// </summary>
	/// <remarks>
	/// MUX Reference: CDependencyObject::UpdateAllThemeReferences in Theming.cpp (lines 260-286)
	///
	/// WinUI snapshots property indices into a stack_vector (size 50 to handle ListViewItemPresenter
	/// with 41+ theme refs), then calls UpdateThemeReference(propertyIndex) for each.
	/// </remarks>
	internal void UpdateAllThemeReferences(DependencyObject? owner, ThemeWalkResourceCache? cache = null, Theme? ownerThemeOverride = null, bool preferAppResourceOverride = false)
	{
		if (_themeResources is not { HasEntries: true })
		{
			return;
		}

		// MUX: Theming.cpp:271-276 — snapshot property indices to avoid mutation issues.
		// "Get the properties that have theme refs" into a separate collection before iterating,
		// because updating one property could cascade and modify _themeResources (GetAll returns the live
		// list). The vast majority of objects carry exactly one theme ref (e.g. a single Foreground), so
		// take a zero-allocation fast path for that case; otherwise rent a pooled buffer (re-entrancy-safe,
		// unlike a shared scratch buffer) instead of allocating a fresh array per call — this runs per
		// themed object on every theme change and every Enter.
		var entries = _themeResources.GetAll();
		var snapshotCount = entries.Count;

		if (snapshotCount == 1)
		{
			var entry = entries[0];
			UpdateThemeReference(entry.Property, entry.Precedence, entry.Reference, owner, cache, ownerThemeOverride, preferAppResourceOverride);
			return;
		}

		var snapshot = ArrayPool<ThemeResourceMap.Entry>.Shared.Rent(snapshotCount);
		try
		{
			for (var i = 0; i < snapshotCount; i++)
			{
				snapshot[i] = entries[i];
			}

			// MUX: Theming.cpp:279-282 — update the theme ref on each property.
			for (var i = 0; i < snapshotCount; i++)
			{
				UpdateThemeReference(
					snapshot[i].Property,
					snapshot[i].Precedence,
					snapshot[i].Reference,
					owner,
					cache,
					ownerThemeOverride,
					preferAppResourceOverride);
			}
		}
		finally
		{
			// clearArray: true so the pooled buffer does not retain DependencyProperty/ThemeResourceReference
			// references after return.
			ArrayPool<ThemeResourceMap.Entry>.Shared.Return(snapshot, clearArray: true);
		}
	}

	/// <summary>
	/// Updates a single theme resource reference for a property: clears from map,
	/// re-resolves via ancestor walk + pinned dict fallback, sets value, stores ref back.
	/// </summary>
	/// <remarks>
	/// MUX Reference: CDependencyObject::UpdateThemeReference(KnownPropertyIndex) in Theming.cpp (lines 288-311)
	///   — Clears the theme ref, then calls SetThemeResourceBinding which calls
	///     UpdateThemeReference(CThemeResource*) (lines 315-346) for ancestor walk + refresh.
	///
	/// MUX Reference: CDependencyObject::UpdateThemeReference(CThemeResource*) in Theming.cpp (lines 315-346)
	///   — If element is active: walks ancestors via FindNextResolvedValueNoRef
	///   — Falls back to themeResource->RefreshValue() (pinned dict lookup)
	///
	/// MUX Reference: CDependencyObject::SetThemeResourceBinding in Theming.cpp (lines 349-400)
	///   — Gets resolved value, calls SetValue, stores reference back in map.
	/// </remarks>
	private void UpdateThemeReference(
		DependencyProperty property,
		DependencyPropertyValuePrecedences precedence,
		ThemeResourceReference themeRef,
		DependencyObject? owner,
		ThemeWalkResourceCache? cache,
		Theme? ownerThemeOverride = null,
		bool preferAppResourceOverride = false)
	{
#if UNO_HAS_ENHANCED_LIFECYCLE
		// MUX Reference: Theming.cpp:364-379 — SetThemeResourceBinding pushes the owner's theme onto
		// the core requested-theme-for-subtree slot ("Push theme that resource lookup should use to
		// get the property value") so the lookup — including any lazy materialization it triggers —
		// resolves in the owner's theme; scope-restored below. During a walk the slot already carries
		// the per-element walk theme (Theming.cpp:137-149), so the set is a no-op then. The resolution
		// leaf reads the slot (EnsureActiveThemeDictionary, Resources.cpp:764-768).
		// Element-level theming is enhanced-lifecycle only — on native the slot stays None.
		var core = Uno.UI.Xaml.Core.CoreServices.Instance;
		var prevSlotTheme = Theme.None;
		var popSlotTheme = false;
#endif

		try
		{
			// MUX: Theming.cpp:297-298 — clear the theme ref first.
			// "Just continue to the next one if the ref isn't in the map now,
			//  which means it no longer applies to this element. That happens
			//  if it's removed while updating the ref for a previous property.
			//  For example, a ref for the Style property could resolve to a new
			//  style that applies different theme refs."
			if (_themeResources!.Get(property, precedence) != themeRef)
			{
				return; // ref was removed by a cascading update
			}

			_themeResources.Clear(property, precedence);

			// MUX: Theming.cpp:315-346 — UpdateThemeReference(CThemeResource*)
			bool resolved = false;

#if UNO_HAS_ENHANCED_LIFECYCLE
			// Compute the owner's effective theme ONCE here — it feeds the slot scope below, the analog of
			// WinUI's SetThemeResourceBinding pushing this->m_theme before resolving (Theming.cpp:368-376).
			// Both the ancestor walk and the pinned-dict refresh resolve under that ambient.
			// Prefer the explicit owner theme passed by the caller (the resource-context element's theme for
			// a standalone resource DO that has no inheritance parent of its own); otherwise resolve from the
			// owner's own inheritance chain.
			var ownerTheme = ownerThemeOverride ?? ThemeResolution.ResolveOwnerTheme(owner);

			// MUX: Theming.cpp:368-376 — "Push theme that resource lookup should use to get the
			// property value": only OUTSIDE a theme walk (`!IsProcessingThemeWalk()`) and when the
			// owner's base theme differs from the slot. During a walk the slot already carries the
			// walk's theme (set in NotifyThemeChanged, Theming.cpp:137-149) while the owner's
			// per-object theme is not yet persisted — pushing here would re-scope to the stale theme.
			if (!IsProcessingThemeWalk)
			{
				prevSlotTheme = core.GetRequestedThemeForSubTree();
				if (prevSlotTheme != Theming.GetBaseValue(ownerTheme))
				{
					core.SetRequestedThemeForSubTree(ownerTheme);
					popSlotTheme = true;
				}
			}
#endif

			// Phase A: Ancestor walk (WinUI: FindNextResolvedValueNoRef → ScopedResources::TraverseVisualTreeResources)
			// If element is active, walk ancestor ResourceDictionaries to find the resource. This handles
			// re-parenting correctly. Gate on IsActiveInVisualTree (set at tree Enter) rather than IsLoaded
			// to match WinUI's IsActive() window — so an element resolves against its current ancestor scope
			// from Enter onward (incl. after reparenting, before Loaded re-fires), not only once Loaded.
			// Resources declared locally and unreachable from the live tree (reparented popup content) simply
			// don't resolve here and fall through to the parse-time pinned dictionary in Phase B.
			if (owner is UIElement { IsActiveInVisualTree: true })
			{
				var dicts = GetResourceDictionaries(includeAppResources: false);
				foreach (var dict in dicts)
				{
					if (dict.TryGetValue(themeRef.ResourceKey, out var ancestorValue, shouldCheckSystem: false))
					{
						// MUX: Theming.cpp:332-336 — SetLastResolvedValue; the refresh below is skipped.
						themeRef.SetLastResolvedValue(ancestorValue);
						resolved = true;
						break;
					}
				}
			}

			// Phase B: Pinned dict fallback (WinUI: themeResource->RefreshValue())
			// MUX: Theming.cpp:338-343 — "Call refresh if we're in a theme walk or the ref has been
			// updated in the past *and* the value wasn't updated already by the tree lookup above."
#if UNO_HAS_ENHANCED_LIFECYCLE
			// Uno: the owner-theme override threaded by the re-resolution flows (the resource-context
			// element's theme for a standalone resource DO whose own store theme is still None) is the
			// transitional stand-in for the owner's established m_theme, so it participates in the
			// m_theme != Theme::None term of the C++ gate.
			if (!resolved
				&& (IsProcessingThemeWalk
					|| _theme != Theme.None
					|| ownerThemeOverride is not null
					|| !themeRef.IsValueFromInitialTheme))
#else
			if (!resolved)
#endif
			{
				themeRef.RefreshValue(cache, preferAppResourceOverride);
			}

			// MUX: Theming.cpp:385-387 — GetLastResolvedThemeValue: the value to apply is the ref's
			// last resolved value (unchanged when the refresh gate above skipped).
			var newValue = themeRef.LastResolvedValue;

			// MUX: Theming.cpp:385-393 — SetValue with resolved value
			var convertedValue = BindingPropertyHelper.Convert(property.Type, newValue);

			// Skip deferred entries that haven't been resolved yet. They will be resolved
			// by the _resourceBindings fallback path during loading.
			// MUX Reference: WinUI uses CValue::IsUnset() to distinguish "not yet resolved"
			// from "resolved to null" (x:Null). We use IsResolved for the same purpose.
			if (convertedValue is null && !themeRef.IsResolved)
			{
				// MUX: Theming.cpp:397 — store ref back even if we skip the value set
				_themeResources.Set(property, precedence, themeRef);
				return;
			}

			if (themeRef.SetterBindingPath is { } bindingPath)
			{
				try
				{
					_isSettingPersistentResourceBinding = true;
					bindingPath.Value = convertedValue;
				}
				finally
				{
					_isSettingPersistentResourceBinding = false;
				}
			}
			else
			{
				SetValue(property, convertedValue, precedence, isPersistentResourceBinding: true);
			}

			// MUX: Theming.cpp:397 — store the reference back in the map
			_themeResources.Set(property, precedence, themeRef);
		}
		catch (Exception e)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"Failed to update theme resource binding for {property.Name}", e);
			}

			// Restore the ref even on failure to avoid losing it
			_themeResources!.Set(property, precedence, themeRef);
		}
		finally
		{
#if UNO_HAS_ENHANCED_LIFECYCLE
			// MUX: Theming.cpp:377-379 — scope-restore the slot.
			if (popSlotTheme)
			{
				core.SetRequestedThemeForSubTree(prevSlotTheme);
			}
#endif
		}
	}

	#endregion

	#region Property-value theme propagation — WinUI: NotifyThemeChangedCoreImpl property walk (Theming.cpp lines 166-255)

	private bool _isUpdatingChildResourceBindings;

#if UNO_HAS_ENHANCED_LIFECYCLE
	// Notifies new property value of theme change that was applied to the property owner.
	/// <remarks>
	/// MUX Reference: CDependencyObject::NotifyPropertyValueOfThemeChange — Theming.cpp:41-54.
	/// Called from the SetValue pipeline (the CDependencyObject::UpdateEffectiveValue analog,
	/// PropertySystem.cpp:1893-1898) so a DO value set on an already-themed owner carries the
	/// owner's theme.
	/// </remarks>
	private void NotifyPropertyValueOfThemeChange(DependencyProperty dp, object? effectiveValue)
	{
		if (_theme != Theme.None &&
			effectiveValue is IDependencyObjectStoreProvider provider &&
			ShouldNotifyPropertyOfThemeChange(dp))
		{
			provider.Store.NotifyThemeChanged(_theme);
		}
	}
#endif

	// Should this property be notified of theme change?
	/// <remarks>
	/// MUX Reference: CDependencyObject::ShouldNotifyPropertyOfThemeChange in Theming.cpp (lines 57-77).
	/// Uno: C# cannot switch on a <see cref="DependencyProperty"/> (it is not a compile-time constant),
	/// so the WinUI KnownPropertyIndex switch is expressed as equality checks. The comments are carried
	/// verbatim from the C++ source.
	/// </remarks>
	private static bool ShouldNotifyPropertyOfThemeChange(DependencyProperty property)
	{
		if (property == ButtonBase.CommandParameterProperty ||
			property == MenuFlyoutItem.CommandParameterProperty ||

			// These can contain AppBarButtons that aren't in the visual tree yet (i.e. aren't in the child collection of the
			// CommandBar). These buttons won't render, so there's no reason to update their theme resources. We can do that
			// when they are parented to the CommandBar. This saves us from many resource lookups from those AppBarButtons.
			property == CommandBar.PrimaryCommandsProperty ||
			property == CommandBar.SecondaryCommandsProperty)
		{
			return false;
		}

		// Don't notify property values that contain objects up the visual tree to prevent theme from propagating up the tree.
		return !IsDependencyPropertyBackReference(property);
	}

	// Returns TRUE if the dependency property is a back reference to an object up the tree.
	/// <remarks>
	/// MUX Reference: CDependencyObject::IsDependencyPropertyBackReference in PropertySystem.cpp
	/// (lines 126-156). Uno: the WinUI KnownPropertyIndex switch is expressed as equality checks, kept
	/// in source order. Entries whose Uno <see cref="DependencyProperty"/> does not exist yet are left
	/// as <c>TODO Uno:</c> markers so this stays a faithful 1:1 of the WinUI list; add them here when
	/// the DP is introduced.
	/// </remarks>
	private static bool IsDependencyPropertyBackReference(DependencyProperty property)
		=> property == Page.FrameProperty ||                                       // Page_Frame
			property == Hub.SemanticZoomOwnerProperty ||                           // Hub_SemanticZoomOwner
			property == ListViewBase.SemanticZoomOwnerProperty ||                  // ListViewBase_SemanticZoomOwner
			property == UIElement.AccessKeyScopeOwnerProperty ||                   // UIElement_AccessKeyScopeOwner
			property == UIElement.KeyTipTargetProperty ||                          // UIElement_KeyTipTarget
			property == Documents.TextElement.AccessKeyScopeOwnerProperty ||       // TextElement_AccessKeyScopeOwner
			property == UIElement.XYFocusLeftProperty ||                           // UIElement_XYFocusLeft
			property == UIElement.XYFocusRightProperty ||                          // UIElement_XYFocusRight
			property == UIElement.XYFocusUpProperty ||                             // UIElement_XYFocusUp
			property == UIElement.XYFocusDownProperty ||                           // UIElement_XYFocusDown
			property == Documents.Hyperlink.XYFocusLeftProperty ||                 // Hyperlink_XYFocusLeft
			property == Documents.Hyperlink.XYFocusRightProperty ||                // Hyperlink_XYFocusRight
			property == Documents.Hyperlink.XYFocusUpProperty ||                   // Hyperlink_XYFocusUp
			property == Documents.Hyperlink.XYFocusDownProperty ||                 // Hyperlink_XYFocusDown
			property == FlyoutBase.OverlayInputPassThroughElementProperty ||       // FlyoutBase_OverlayInputPassThroughElement
			// TODO Uno: FlyoutBase_Target — no FlyoutBase.TargetProperty DP in Uno yet.
			property == Popup.OverlayInputPassThroughElementProperty ||            // Popup_OverlayInputPassThroughElement
			property == Microsoft.UI.Xaml.Input.KeyboardAccelerator.ScopeOwnerProperty || // KeyboardAccelerator_ScopeOwner
			property == UIElement.KeyboardAcceleratorPlacementTargetProperty;      // UIElement_KeyboardAcceleratorPlacementTarget
			// TODO Uno: Control_FocusTargetDescendant, CommandingContainer_CommandingTarget,
			// CommandingContainer_CommandingContainer — no corresponding Uno DPs yet.

	/// <summary>
	/// Propagates resource binding updates to non-FrameworkElement DependencyObject values
	/// stored as property values on this object (e.g. Brushes, Storyboards, GradientStops).
	/// </summary>
	/// <remarks>
	/// MUX Reference: CDependencyObject::NotifyThemeChangedCoreImpl in Theming.cpp (lines 166-255)
	///
	/// In WinUI, NotifyThemeChangedCoreImpl walks field-backed and sparse property values,
	/// and for each DO that is not an active UIElement, calls NotifyThemeChanged.
	/// In Uno, the equivalent is iterating DependencyPropertyDetailsCollection and calling
	/// UpdateResourceBindings on non-FE DOs.
	/// </remarks>
	private void UpdateChildResourceBindings(ResourceUpdateReason updateReason, FrameworkElement? resourceContextProvider = null)
	{
		if (_isUpdatingChildResourceBindings)
		{
			// Some DPs might be creating reference cycles, so we make sure not to enter an infinite loop.
			return;
		}

		if ((updateReason & ResourceUpdateReason.PropagatesThroughTree) != ResourceUpdateReason.None)
		{
			try
			{
				InnerUpdateChildResourceBindings(updateReason, resourceContextProvider);
			}
			finally
			{
				_isUpdatingChildResourceBindings = false;
			}

			if (ActualInstance is IThemeChangeAware themeChangeAware)
			{
				// Call OnThemeChanged after bindings of descendants have been updated
				themeChangeAware.OnThemeChanged();
			}

			// Note: theme-resolved binding values (TargetNullValue/FallbackValue {ThemeResource}) are
			// re-resolved and re-applied by UpdateBindingExpressions in the binding-expression phase above;
			// no separate per-binding RefreshTarget pass is needed here.
		}
	}

	/// <remarks>
	/// This method contains or is called by a try/catch containing method and
	/// can be significantly slower than other methods as a result on WebAssembly.
	/// See https://github.com/dotnet/runtime/issues/56309
	/// </remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void InnerUpdateChildResourceBindings(ResourceUpdateReason updateReason, FrameworkElement? resourceContextProvider = null)
	{
		_isUpdatingChildResourceBindings = true;

		foreach (var propertyDetail in _properties.GetAllDetails())
		{
			if (propertyDetail == null
				|| propertyDetail == _properties.DataContextPropertyDetails)
			{
				continue;
			}

			// MUX: Theming.cpp:180 — ShouldNotifyPropertyOfThemeChange filter
			if (!ShouldNotifyPropertyOfThemeChange(propertyDetail.Property))
			{
				continue;
			}

			var propertyValue = GetValue(propertyDetail);

			if (propertyValue is IEnumerable<DependencyObject> dependencyObjectCollection &&
				// Try to avoid enumerating collections that shouldn't be enumerated, since we may be encountering user-defined values. This may need to be refined to somehow only consider values coming from the framework itself.
				(propertyValue is ICollection || propertyValue is DependencyObjectCollectionBase)
			)
			{
				foreach (var innerValue in dependencyObjectCollection)
				{
					UpdateResourceBindingsIfNeeded(innerValue, updateReason, resourceContextProvider);
				}
			}

			if (propertyValue is IAdditionalChildrenProvider updateable)
			{
				foreach (var innerValue in updateable.GetAdditionalChildObjects())
				{
					UpdateResourceBindingsIfNeeded(innerValue, updateReason, resourceContextProvider);
				}
			}

			// MUX: Theming.cpp:218 — skip UIElements that are active (they're handled
			// by the visual tree walk in CUIElement::NotifyThemeChangedCore)
			if (propertyValue is DependencyObject dependencyObject)
			{
				UpdateResourceBindingsIfNeeded(dependencyObject, updateReason, resourceContextProvider);
			}
		}
	}

	/// <summary>
	/// Propagates resource binding updates to a non-FrameworkElement DependencyObject.
	/// </summary>
	/// <remarks>
	/// MUX Reference: Theming.cpp lines 218-221 / 239-244 — notifying non-UIElement
	/// property values of theme changes by calling NotifyThemeChanged on them.
	/// In Uno, non-FE DOs don't have NotifyThemeChanged, so we call UpdateResourceBindings
	/// on their store directly.
	/// </remarks>
	private void UpdateResourceBindingsIfNeeded(DependencyObject dependencyObject, ResourceUpdateReason updateReason, FrameworkElement? resourceContextProvider = null)
	{
#if UNO_HAS_ENHANCED_LIFECYCLE
		// MUX: Theming.cpp:218-221 / :242-244 — during a theme walk, property values are notified
		// through the recursive per-child NotifyThemeChanged, which persists the child's own
		// per-object theme, re-scopes the requested-theme-for-subtree slot, and early-outs when the
		// child already carries the walk theme. "Any DependencyObject property that happens to be a
		// UIElement in the visual tree is skipped ... covered anyways via a full traversal of the
		// live visual tree ... in CUIElement::NotifyThemeChangedCore" — detached UIElements (e.g.
		// flyout items not yet opened) ARE notified. The engine propagation below remains for the
		// non-walk reasons (loading, hot reload).
		if (IsProcessingThemeWalk)
		{
			if (dependencyObject is not UIElement { IsActiveInVisualTree: true }
				&& dependencyObject is IDependencyObjectStoreProvider walkProvider)
			{
				walkProvider.Store.NotifyThemeChanged(_walkTheme, _walkForceRefresh);
			}

			return;
		}
#endif

		// propagate to non-FE DO
		if (dependencyObject is not IFrameworkElement && dependencyObject is IDependencyObjectStoreProvider storeProvider)
		{
			storeProvider.Store.UpdateResourceBindings(
				updateReason,
				// when propagating to non-FE, we need to inject a FE as the resource context
				resourceContextProvider: resourceContextProvider ?? ActualInstance as FrameworkElement
			);
		}
	}

	#endregion

	#region Resource binding orchestration — WinUI: NotifyThemeChangedCoreImpl + FxCallbacks (Theming.cpp lines 166-255)

	/// <summary>
	/// Do a tree walk to find the correct values of StaticResource and ThemeResource assignations.
	/// </summary>
	/// <remarks>
	/// MUX Reference: This method combines several WinUI concerns:
	/// - UpdateAllThemeReferences (Theming.cpp:260) — Phase 1: theme resource re-resolution
	/// - FxCallbacks::DependencyObject_RefreshExpressionsOnThemeChange (Theming.cpp:252) — binding expression refresh
	/// - NotifyThemeChangedCoreImpl property walk (Theming.cpp:176-248) — propagation to non-tree DOs
	/// </remarks>
	internal void UpdateResourceBindings(ResourceUpdateReason updateReason, FrameworkElement? resourceContextProvider = null, ResourceDictionary? containingDictionary = null)
	{
		if (updateReason == ResourceUpdateReason.None)
		{
			throw new ArgumentException();
		}

		// The owner whose effective theme drives every {ThemeResource} resolution below: this element, or —
		// for a standalone resource DO with no inheritance parent — the injected resource-context element
		// (the dictionary's owning element), matching WinUI's per-owner {ThemeResource} resolution. Resolve
		// it lazily and at most once, reused across all three phases, instead of walking the inheritance
		// chain separately in each. Not computed at all for elements that need none of the phases.
		Theme? ownerThemeCache = null;
		Theme GetOwnerTheme()
			=> ownerThemeCache ??= ThemeResolution.ResolveOwnerTheme(
				ActualInstance as FrameworkElement ?? resourceContextProvider ?? ActualInstance);

		// Phase 1: Update theme resources via ancestor-walk + pinned-dictionary path
		// MUX Reference: CDependencyObject::UpdateAllThemeReferences() in Theming.cpp
		if ((updateReason & ResourceUpdateReason.ThemeResource) != 0)
		{
			UpdateAllThemeReferences(ActualInstance, Uno.UI.Xaml.Core.CoreServices.Instance.ThemeWalkResourceCache, GetOwnerTheme());
		}

		ResourceDictionary[]? dictionariesInScope = null;

		// MUX Reference: FxCallbacks::DependencyObject_RefreshExpressionsOnThemeChange (Theming.cpp:252)
		// Refresh binding expressions that may reference theme resources (e.g. Binding.TargetNullValue).
		// Gate on HasThemeResourceBindingExpressions so the (allocating) GetResourceDictionaries().ToArray()
		// + scope push only runs for elements that actually have a {ThemeResource} TargetNullValue/Fallback,
		// not for every element that merely has bindings. Covers ThemeResource and HotReload (the same set
		// the now-removed legacy OnThemeChanged binding refresh handled); UpdateBindingExpressions both
		// re-resolves the value and re-applies it (only when changed), so no second refresh pass is needed.
		if ((updateReason & (ResourceUpdateReason.ThemeResource | ResourceUpdateReason.HotReload)) != 0 &&
			_properties.HasBindings &&
			_properties.HasThemeResourceBindingExpressions)
		{
			dictionariesInScope = GetResourceDictionaries(includeAppResources: false, resourceContextProvider, containingDictionary).ToArray();
			for (var i = dictionariesInScope.Length - 1; i >= 0; i--)
			{
				ResourceResolver.PushSourceToScope(dictionariesInScope[i]);
			}

			try
			{
				// Resolve {ThemeResource} markup used inside a binding (Binding.TargetNullValue /
				// FallbackValue) against the OWNER's effective theme — the same owner theme the
				// UpdateThemeReference / Phase-2 choke points use — by scoping it onto the core
				// requested-theme-for-subtree slot (the LookupThemeResource pattern,
				// xcpcore.cpp:2371-2394), which the resolution leaf reads
				// (EnsureActiveThemeDictionary, Resources.cpp:764-768).
				// During a theme walk the slot already carries the walk's theme while this object's
				// per-object theme is not yet persisted (Theming.cpp:155) — only scope outside walks.
				using var bindingThemeScope = IsProcessingThemeWalk
					? default
					: Uno.UI.Xaml.Core.CoreServices.Instance.ScopeRequestedThemeForSubTree(GetOwnerTheme());
				_properties.UpdateBindingExpressions();
			}
			finally
			{
				for (int i = 0; i < dictionariesInScope.Length; i++)
				{
					ResourceResolver.PopSourceFromScope();
				}
			}
		}

		// Phase 2: Update remaining resource bindings (StaticResourceLoading, HotReload) via tree walk
		if (_resourceBindings?.HasBindings == true)
		{
			dictionariesInScope ??= GetResourceDictionaries(includeAppResources: false, resourceContextProvider, containingDictionary).ToArray();

			// Resolve deferred/unpinned {ThemeResource} (and theme-sensitive {StaticResource}) bindings
			// against the OWNER's effective theme — the same owner theme the UpdateThemeReference choke point
			// uses — by scoping it onto the core requested-theme-for-subtree slot (the LookupThemeResource
			// pattern, xcpcore.cpp:2371-2394), which the resolution leaf reads (EnsureActiveThemeDictionary,
			// Resources.cpp:764-768). For a non-FrameworkElement owner, the injected FE resource context
			// supplies the theme.
			// During a theme walk the slot already carries the walk's theme while this object's
			// per-object theme is not yet persisted (Theming.cpp:155) — only scope outside walks.
			using var resourceBindingsThemeScope = IsProcessingThemeWalk
				? default
				: Uno.UI.Xaml.Core.CoreServices.Instance.ScopeRequestedThemeForSubTree(GetOwnerTheme());

			var bindings = _resourceBindings.GetAllBindings();
			foreach (var binding in bindings)
			{
				InnerUpdateResourceBindings(updateReason, dictionariesInScope, binding.Property, binding.Binding);
			}
		}

		// Phase 3: Propagate to non-FE DOs (brushes, animations, etc.)
		// MUX Reference: NotifyThemeChangedCoreImpl property walk (Theming.cpp:176-248)
		UpdateChildResourceBindings(updateReason, resourceContextProvider);
	}

	/// <remarks>
	/// This method contains or is called by a try/catch containing method and
	/// can be significantly slower than other methods as a result on WebAssembly.
	/// See https://github.com/dotnet/runtime/issues/56309
	/// </remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void InnerUpdateResourceBindings(ResourceUpdateReason updateReason, ResourceDictionary[] dictionariesInScope, DependencyProperty property, ResourceBinding binding)
	{
		try
		{
			InnerUpdateResourceBindingsUnsafe(updateReason, dictionariesInScope, property, binding);
		}
		catch (Exception e)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"Failed to update binding, target may have been disposed", e);
			}
		}
	}

	/// <remarks>
	/// This method contains or is called by a try/catch containing method and
	/// can be significantly slower than other methods as a result on WebAssembly.
	/// See https://github.com/dotnet/runtime/issues/56309
	/// </remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void InnerUpdateResourceBindingsUnsafe(ResourceUpdateReason updateReason, ResourceDictionary[] dictionariesInScope, DependencyProperty property, ResourceBinding binding)
	{
		if ((binding.UpdateReason & updateReason) == ResourceUpdateReason.None)
		{
			// If the reason for the update doesn't match the reason(s) that the binding was created for, don't update it
			return;
		}

		// The Light/Dark sub-dictionary is selected at the dictionary leaf by the ambient active theme —
		// the owner's effective theme scoped onto the core requested-theme-for-subtree slot by
		// UpdateResourceBindings (EnsureActiveThemeDictionary, Resources.cpp:764-768).

		// Note: we intentionally do NOT skip theme resource bindings here even though
		// Phase 1 (UpdateAllThemeReferences) may have already resolved them. The Phase 2
		// tree walk serves as a fallback when Phase 1 can't resolve (e.g., element not yet
		// loaded, pinned dictionary missing). Skipping here could cause stale values for
		// elements like ToolTip content that enter the tree outside a theme walk.

		if ((updateReason & ResourceUpdateReason.ResolvedOnLoading) != 0)
		{
			// Add the current dictionaries to the resolver scope,
			// this allows for StaticResource.ResourceKey to resolve properly

			for (var i = dictionariesInScope.Length - 1; i >= 0; i--)
			{
				ResourceResolver.PushSourceToScope(dictionariesInScope[i]);
			}
		}

		try
		{
			var wasSet = false;
			foreach (var dict in dictionariesInScope)
			{
				if (dict.TryGetValue(binding.ResourceKey, out var value, out var providingDict, shouldCheckSystem: false))
				{
					wasSet = true;
					SetResourceBindingValue(property, binding, value);

					// Pin the providing dictionary in the _themeResources entry (if any)
					// so that subsequent theme changes can re-resolve from it directly.
					if (providingDict is not null && _themeResources is not null)
					{
						var themeRef = _themeResources.Get(property, binding.Precedence);
						themeRef?.SetTargetDictionary(providingDict);
						themeRef?.SetLastResolvedValue(value);
					}

					break;
				}
			}

			if (!wasSet)
			{
				if (ResourceResolver.TryTopLevelRetrieval(binding.ResourceKey, binding.ParseContext, out var value))
				{
					SetResourceBindingValue(property, binding, value);
				}
			}
		}
		finally
		{
			if ((updateReason & ResourceUpdateReason.ResolvedOnLoading) != 0)
			{
				foreach (var dict in dictionariesInScope)
				{
					ResourceResolver.PopSourceFromScope();
				}
			}
		}
	}

	[UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "normal flow of operation")]
	[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "normal flow of operation")]
	private void SetResourceBindingValue(DependencyProperty property, ResourceBinding binding, object? value)
	{
		var convertedValue = BindingPropertyHelper.Convert(property.Type, value);
		if (binding.SetterBindingPath != null)
		{
			try
			{
				_isSettingPersistentResourceBinding = binding.IsPersistent;
				binding.SetterBindingPath.Value = convertedValue;
			}
			finally
			{
				_isSettingPersistentResourceBinding = false;
			}
		}
		else
		{
			SetValue(property, convertedValue, binding.Precedence, isPersistentResourceBinding: binding.IsPersistent);
		}
	}

	#endregion
}
