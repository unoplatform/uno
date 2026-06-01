#nullable enable

// Port of WinUI Theming.cpp (CDependencyObject theme methods)
// MUX Reference: src/dxaml/xcp/components/DependencyObject/Theming.cpp
//
// In WinUI, all theming functions are methods on CDependencyObject.
// In Uno, CDependencyObject is represented by DependencyObjectStore.
// This partial contains every theme-related method in one place.

using System;
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

	#region Theme establishment at tree Enter — WinUI: CDependencyObject::EnterImpl theme block (depends.cpp:1023-1048)

	/// <summary>
	/// Establishes this object's theme as it goes live in the visual tree, inheriting from its
	/// (logical) inheritance parent before any {ThemeResource} resolves. Mirrors the theme block of
	/// WinUI's <c>CDependencyObject::EnterImpl</c>.
	/// </summary>
	/// <remarks>
	/// MUX Reference: CDependencyObject::EnterImpl theme block — depends.cpp:1023-1048.
	///
	/// Runs for every <see cref="DependencyObject"/> going live so that <see cref="GetTheme"/> is reliably
	/// non-<see cref="Theme.None"/> at the first {ThemeResource} resolution — the precondition that lets
	/// resolution key on the owner's own theme rather than a process-global ambient. WinUI hosts this on
	/// CDependencyObject; Uno hosts it on the <see cref="DependencyObjectStore"/> that every DO owns,
	/// invoked from the enhanced-lifecycle Enter walk.
	///
	/// The Enter walk runs synchronously on attach (UIElement.AddChild → ChildEnter → EnterImpl), before the
	/// first measure pass raises Loading — at which point deferred {ThemeResource} refs are first resolved
	/// (OnLoadingPartial → UpdateThemeBindings → _resourceBindings walk). So the theme is in place first.
	/// </remarks>
	internal void EstablishThemeAtEnter(DependencyObjectStore? inheritFromCaller = null)
	{
		var owner = ActualInstance;

		// MUX: depends.cpp:1026-1037 — a FrameworkElement inherits from its *logical* inheritance parent
		// (GetInheritanceParentInternal(fLogicalParent=TRUE), framework.cpp:3097-3130) "so popups and
		// flyouts inherit theme changes"; other DOs use the visual parent. In Uno the logical inheritance
		// parent is FrameworkElement.Parent (LogicalParentOverride ?? Store.Parent): a Popup sets its
		// Child's LogicalParentOverride to itself, so the content follows the opener's theme rather than the
		// PopupRoot it is visually reparented under. Non-FrameworkElement DOs use the store's Parent.
		var parent = owner is FrameworkElement frameworkElementOwner
			? frameworkElementOwner.Parent
			: Parent as DependencyObject;
		var parentTheme = (parent as IDependencyObjectStoreProvider)?.Store.GetTheme() ?? Theme.None;

		// Fallback: when a DP-property Enter walk reaches a DO whose Store.Parent / LogicalParentOverride
		// isn't set yet (e.g. ContentControl.Content carries no LogicalChild flag, so a CommandBarFlyoutCommandBar
		// assigned as FlyoutPresenter.Content has no logical parent at this point) we use the DP-owner the
		// walk arrived through as the inheritance source. This mirrors WinUI's InheritanceContext —
		// CDependencyObject::SetInheritanceContext sets the property owner as the inherited context when
		// the value goes live (depends.cpp ::EnterImpl + AddParent), and the theme block at depends.cpp:1026
		// then walks that context. Uno hasn't ported per-DP InheritanceContext yet (see Uno issue #22949
		// referenced in UIElement.mux.cs:1245), so this argument carries the same data point.
		if (parentTheme == Theme.None && inheritFromCaller is not null)
		{
			parentTheme = inheritFromCaller.GetTheme();
		}

		// MUX: depends.cpp:1039-1047. Note: the WinUI check is `pParent && ...`, but in Uno the fallback
		// above can supply a non-None parentTheme through inheritFromCaller without an actual `parent`
		// object (Content-style DPs that don't propagate Store.Parent). Gate the inherit branch on
		// parentTheme alone — parent is only consulted for the lookup, not as a precondition.
		if (parentTheme != Theme.None && parentTheme != _theme)
		{
			if (owner is FrameworkElement frameworkElement)
			{
				// Inherit the parent theme and walk this subtree. NotifyThemeChanged re-applies this
				// element's own RequestedTheme override (FrameworkElement.Theming.cs:180-183).
				frameworkElement.NotifyThemeChanged(parentTheme);
			}
			else
			{
				// Non-FrameworkElement DO: there is no NotifyThemeChanged walk; adopt the parent theme
				// and re-resolve this object's own theme references. The owner's theme is now established,
				// so compute the effective owner theme once and pass it as the override — otherwise
				// UpdateThemeReference would call ResolveOwnerTheme per theme-ref.
				SetTheme(parentTheme);
				UpdateAllThemeReferences(owner, cache: null, ThemeResolution.ResolveOwnerTheme(owner));
			}
		}
		else
		{
			// MUX: depends.cpp:1046 — update theme references to account for the new ancestor theme
			// dictionaries now reachable from this position in the tree. The owner's theme is already
			// established here, so resolve it once and pass it as the override (avoids a per-theme-ref walk).
			UpdateAllThemeReferences(owner, cache: null, ThemeResolution.ResolveOwnerTheme(owner));
		}

		// MUX: depends.cpp:993-1010 — CDependencyObject::EnterImpl walks "Enter properties" (DPs tagged
		// CEnterDependencyProperty / IsVisualTreeProperty in WinUI metadata: Button.Flyout, MenuFlyout.Items,
		// CommandBar.PrimaryCommands / SecondaryCommands, Popup.Child, etc.) and recursively calls Enter on
		// each DO value, whose own EnterImpl runs the depends.cpp:1023-1048 theme block above. That recursion
		// is what carries theme into logical-only collections — items in MenuFlyout.Items or AppBarButtons in
		// CommandBar.PrimaryCommands — at the moment the *opener* (e.g. a Button) enters the live tree, well
		// before any popup opens.
		//
		// Uno's metadata has no per-DP Enter flag and no separate CEnterDependencyProperty list, so we walk
		// every property value the store carries and trigger each non-active DO's own EstablishThemeAtEnter
		// here. UIElements already live in the visual tree are skipped — UIElement.EnterImpl
		// (UIElement.mux.cs:1136-1139) calls EstablishThemeAtEnter on them from the visual Enter path, so
		// re-entering through a parent's DP-walk would only repeat the work the visual walk already did.
		PropagateThemeEnterToDPPropertyValues();
	}

	private bool _isPropagatingThemeEnter;

	/// <summary>
	/// Recursively triggers <see cref="EstablishThemeAtEnter"/> on every <see cref="DependencyObject"/>
	/// reachable through this object's DP property values, skipping UIElements already active in the
	/// visual tree.
	/// </summary>
	/// <remarks>
	/// MUX Reference: CDependencyObject::EnterImpl property walk (depends.cpp:993-1010 + EnterSparseProperties).
	///
	/// WinUI tags DPs as <c>CEnterDependencyProperty</c> in metadata and EnterImpl recursively calls Enter
	/// on each tagged DP's DO value. Each child's own EnterImpl then runs depends.cpp:1023-1048, inheriting
	/// theme from its (logical) inheritance parent. The result is that — by the time the parent's Enter
	/// finishes — every DO reachable through DP properties (including logical-only collections like
	/// MenuFlyout.Items or CommandBar.PrimaryCommands) carries the correct inherited theme.
	///
	/// Uno's DP metadata has no Enter flag, so we walk every property value and propagate Enter via
	/// EstablishThemeAtEnter on each non-active DO. The walk is theme-only — name registration, keyboard
	/// accelerators, and IsVisualTreeProperty parenting are handled by their own Uno-specific paths
	/// (UIElement.mux.cs ContextFlyout block + KeyboardAcceleratorFlyoutItemEnter); this walk only carries
	/// the depends.cpp:1023-1048 theme block, which is the part PR #23325's EstablishThemeAtEnter already
	/// implements per-DO. Adding the property walk here lifts that per-DO theme establishment up into a
	/// recursive logical-tree walk, matching what WinUI's recursive Enter achieves.
	///
	/// Filters mirror WinUI's <c>ShouldNotifyPropertyOfThemeChange</c> back-reference list (Theming.cpp:60-75):
	/// CommandParameter properties hold the command source (often the parent VM), and walking them would
	/// recurse *up* the logical tree. The general DataContext property is skipped for the same reason.
	/// </remarks>
	private void PropagateThemeEnterToDPPropertyValues()
	{
		if (_isPropagatingThemeEnter)
		{
			return;
		}

		_isPropagatingThemeEnter = true;
		try
		{
			foreach (var propertyDetail in _properties.GetAllDetails())
			{
				if (propertyDetail == null
					|| propertyDetail == _properties.DataContextPropertyDetails)
				{
					continue;
				}

				// MUX: Theming.cpp:60-75 — back-reference properties (CommandParameter) hold references
				// up the logical tree. Excluding them keeps Enter walking *down*.
				var property = propertyDetail.Property;
				if (property == ButtonBase.CommandParameterProperty
					|| property == MenuFlyoutItem.CommandParameterProperty)
				{
					continue;
				}

				// ItemsSource holds arbitrary user data, not themed DO children, and is NOT a WinUI Enter
				// property (CEnterDependencyProperty). Enumerating it here is both wrong (WinUI never walks it)
				// and unsafe: a virtualizing/custom data source may throw on GetEnumerator (e.g. an indexed
				// ItemsSourceView) or have enumeration side effects. The realized item containers are themed
				// by the normal visual-tree Enter walk when they materialize, so skipping the raw data is safe.
				if (property.Name == "ItemsSource")
				{
					continue;
				}

				var propertyValue = GetValue(propertyDetail);

				// Theme the DO value itself before iterating any child collection it carries. Items in
				// a DependencyObjectCollection inherit their theme from their Store.Parent — the
				// collection — so the collection's _theme must be Light/Dark before each item's
				// EstablishThemeAtEnter reads it.
				if (propertyValue is DependencyObject dependencyObject)
				{
					EstablishThemeAtEnterOnPropertyValue(dependencyObject, this);
				}

				// Framework DO-collections come in two shapes:
				//   (1) DependencyObjectCollectionBase subclasses (MenuFlyoutItemBaseCollection,
				//       DependencyObjectCollection<T>, etc.) — themselves DOs, items are DOs
				//   (2) Plain IObservableVector<TInterface> implementations that are NOT DOs
				//       (CommandBarElementCollection : IObservableVector<ICommandBarElement>) — the
				//       collection is not a DO, but the items still are
				// Both must yield to the Enter walk. We can't gate by "value is DependencyObject"
				// (excludes shape 2) and can't gate by IEnumerable<DependencyObject> (excludes shape
				// 2 because the element-type interface doesn't derive from DependencyObject). Iterate
				// any non-string IEnumerable and filter per item — only items that are themselves
				// DependencyObjects participate in the walk.
				if (propertyValue is IEnumerable enumerable && propertyValue is not string)
				{
					// Defense-in-depth: a custom IEnumerable DP value could throw from GetEnumerator/MoveNext
					// (e.g. a virtualizing data source). The theme-Enter walk must never break element Enter,
					// so guard the enumeration and skip the collection if it can't be safely enumerated.
					try
					{
						foreach (var innerValue in enumerable)
						{
							if (innerValue is DependencyObject innerDependencyObject)
							{
								EstablishThemeAtEnterOnPropertyValue(innerDependencyObject, this);
							}
						}
					}
					catch (Exception e)
					{
						if (this.Log().IsEnabled(LogLevel.Debug))
						{
							this.Log().Debug($"Skipping theme-Enter walk of non-enumerable collection on property '{property.Name}': {e.Message}");
						}
					}
				}

				if (propertyValue is IAdditionalChildrenProvider additional)
				{
					foreach (var innerValue in additional.GetAdditionalChildObjects())
					{
						EstablishThemeAtEnterOnPropertyValue(innerValue, this);
					}
				}
			}
		}
		finally
		{
			_isPropagatingThemeEnter = false;
		}
	}

	/// <summary>
	/// Calls <see cref="EstablishThemeAtEnter"/> on a DP-property DO value unless the value is a UIElement
	/// already active in the visual tree (it will be reached by the visual-tree Enter walk instead).
	/// </summary>
	/// <remarks>
	/// MUX Reference: Theming.cpp:218 — "Any DependencyObject property that happens to be a UIElement in the
	/// visual tree is skipped... they are covered anyways via a full traversal of the live visual tree
	/// starting from the root and going recursive in CUIElement::NotifyThemeChangedCore."
	/// In Uno, the same skip is enforced by checking <see cref="UIElement.IsActiveInVisualTree"/>, set by
	/// UIElement.mux.cs's EnterImpl when the element enters the live tree.
	/// </remarks>
	private static void EstablishThemeAtEnterOnPropertyValue(DependencyObject dependencyObject, DependencyObjectStore caller)
	{
		if (dependencyObject is UIElement uiElement && uiElement.IsActiveInVisualTree)
		{
			return;
		}

		if (dependencyObject is IDependencyObjectStoreProvider storeProvider)
		{
			storeProvider.Store.EstablishThemeAtEnter(inheritFromCaller: caller);
		}
	}

	#endregion

	#region Theme resource binding storage — WinUI: SetThemeResource / SetThemeResourceBinding (Theming.cpp lines 349-400)

	/// <summary>
	/// Stores a <see cref="ThemeResourceReference"/> on this object for the given property.
	/// The reference pins the providing dictionary for O(1) re-resolution on theme change.
	/// </summary>
	/// <remarks>
	/// MUX Reference: CDependencyObject::SetThemeResourceBinding in Theming.cpp (lines 349-400)
	///
	/// In WinUI, SetThemeResourceBinding does: push theme context → UpdateThemeReference(CThemeResource*) →
	/// GetLastResolvedThemeValue → SetValue → SetThemeResource (store reference).
	/// In Uno, the resolve+set is done by the caller (ResourceResolver.ApplyResource), and this method
	/// handles just the "store reference" part (equivalent to WinUI line 397: SetThemeResource(pDP, pThemeResource)).
	/// </remarks>
	internal void SetThemeResourceBinding(DependencyProperty property, ThemeResourceReference themeRef)
	{
		_themeResources ??= new ThemeResourceMap();

		// Resolve the effective precedence using the same logic as SetResourceBinding
		var precedence = themeRef.Precedence;
		if (precedence == DependencyPropertyValuePrecedences.Local && _overriddenPrecedences?.Count > 0)
		{
			precedence = _overriddenPrecedences.Peek() ?? precedence;
		}

		_themeResources.Set(property, precedence, themeRef);
	}

	/// <summary>
	/// Gets the theme resource references for a specific property, if any.
	/// Used by animation code to re-resolve ThemeResource values with the
	/// target element's theme context.
	/// </summary>
	internal IEnumerable<ThemeResourceReference>? GetThemeResourceReferences(DependencyProperty property)
	{
		return _themeResources?.GetForProperty(property);
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
	internal void UpdateAllThemeReferences(DependencyObject? owner, ThemeWalkResourceCache? cache = null, Theme? ownerThemeOverride = null)
	{
		if (_themeResources is not { HasEntries: true })
		{
			return;
		}

		// MUX: Theming.cpp:271-276 — snapshot property indices to avoid mutation issues.
		// "Get the properties that have theme refs" into a separate collection before iterating,
		// because updating one property could cascade and modify _themeResources.
		var entries = _themeResources.GetAll();
		var snapshotCount = entries.Count;
		var snapshot = new ThemeResourceMap.Entry[snapshotCount];
		for (var i = 0; i < snapshotCount; i++)
		{
			snapshot[i] = entries[i];
		}

		// MUX: Theming.cpp:279-282 — update the theme ref on each property.
		for (var i = 0; i < snapshot.Length; i++)
		{
			UpdateThemeReference(
				snapshot[i].Property,
				snapshot[i].Precedence,
				snapshot[i].Reference,
				owner,
				cache,
				ownerThemeOverride);
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
		Theme? ownerThemeOverride = null)
	{
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
			object? newValue = null;
			bool resolved = false;

			// Compute the owner's effective theme ONCE here — the centralized "use the owner's theme" choke
			// point, the analog of WinUI's SetThemeResourceBinding pushing this->m_theme before resolving
			// (Theming.cpp:368-376). Both the ancestor walk and the pinned-dict refresh below resolve
			// against this theme.
			// Prefer the explicit owner theme passed by the caller (the resource-context element's theme for
			// a standalone resource DO that has no inheritance parent of its own); otherwise resolve from the
			// owner's own inheritance chain.
			var ownerTheme = ownerThemeOverride ?? ThemeResolution.ResolveOwnerTheme(owner);
			var ownerThemeKey = ResourceDictionary.GetThemeKey(ownerTheme);

			// Phase A: Ancestor walk (WinUI: FindNextResolvedValueNoRef → ScopedResources::TraverseVisualTreeResources)
			// If element is active, walk ancestor ResourceDictionaries to find
			// the resource. This handles re-parenting correctly.
			if (owner is FrameworkElement { IsLoaded: true })
			{
				var dicts = GetResourceDictionaries(includeAppResources: false);
				foreach (var dict in dicts)
				{
					if (dict.TryGetValue(themeRef.ResourceKey, ownerThemeKey, out var ancestorValue, shouldCheckSystem: false))
					{
						themeRef.LastResolvedValue = ancestorValue;
						themeRef.IsResolved = true;
						newValue = ancestorValue;
						resolved = true;
						break;
					}
				}
			}

			// Phase B: Pinned dict fallback (WinUI: themeResource->RefreshValue())
			// MUX: Theming.cpp:340-342 — "Call refresh if we're in a theme walk
			// or the ref has been updated in the past and the value wasn't updated
			// already by the tree lookup above."
			if (!resolved)
			{
				newValue = themeRef.RefreshValue(ownerTheme, cache);
			}

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
	}

	#endregion

	#region Property-value theme propagation — WinUI: NotifyThemeChangedCoreImpl property walk (Theming.cpp lines 166-255)

	private bool _isUpdatingChildResourceBindings;

	/// <summary>
	/// Determines whether a property's value should be notified of a theme change.
	/// </summary>
	/// <remarks>
	/// MUX Reference: CDependencyObject::ShouldNotifyPropertyOfThemeChange in Theming.cpp (lines 57-76)
	///
	/// WinUI skips:
	/// - ButtonBase_CommandParameter, MenuFlyoutItem_CommandParameter (back-references)
	/// - CommandBar_PrimaryCommands, CommandBar_SecondaryCommands (contain AppBarButtons
	///   not yet in the visual tree — saves many unnecessary resource lookups)
	/// - Properties that are back-references (IsDependencyPropertyBackReference)
	///
	/// Note: WinUI also has a generic IsDependencyPropertyBackReference(propertyIndex) check
	/// (Theming.cpp:75) that filters ALL back-reference properties. Uno doesn't have a
	/// centralized back-reference concept in its DP system, so we enumerate the known cases
	/// explicitly. If new back-reference properties are added, they should be listed here.
	/// </remarks>
	private static bool ShouldNotifyPropertyOfThemeChange(DependencyProperty property)
	{
		// WinUI: Theming.cpp:67-68 — CommandBar collections contain AppBarButtons
		// not yet in the visual tree. Skip to avoid unnecessary resource lookups.
		if (property == CommandBar.PrimaryCommandsProperty ||
			property == CommandBar.SecondaryCommandsProperty)
		{
			return false;
		}

		// WinUI: Theming.cpp:61-62 — Skip command parameters (back-references)
		if (property == ButtonBase.CommandParameterProperty ||
			property == MenuFlyoutItem.CommandParameterProperty)
		{
			return false;
		}

		return true;
	}

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

			_properties.OnThemeChanged();
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

		// Phase 1: Update theme resources via ancestor-walk + pinned-dictionary path
		// MUX Reference: CDependencyObject::UpdateAllThemeReferences() in Theming.cpp
		if ((updateReason & ResourceUpdateReason.ThemeResource) != 0)
		{
			// Resolve against the OWNER's effective theme. For a standalone resource DO (e.g. a brush in a
			// FrameworkElement.Resources dictionary) that has no inheritance parent, the injected resource
			// context (the dictionary's owning element) supplies the theme, matching WinUI's per-owner
			// {ThemeResource} resolution. For an element owner this is just its own theme.
			var themeOwner = ActualInstance as FrameworkElement ?? resourceContextProvider ?? ActualInstance;
			var ownerThemeOverride = themeOwner is null ? (Theme?)null : ThemeResolution.ResolveOwnerTheme(themeOwner);
			UpdateAllThemeReferences(ActualInstance, ThemeWalkResourceCache.Instance, ownerThemeOverride);
		}

		ResourceDictionary[]? dictionariesInScope = null;

		// MUX Reference: FxCallbacks::DependencyObject_RefreshExpressionsOnThemeChange (Theming.cpp:252)
		// Refresh binding expressions that may reference theme resources (e.g. Binding.TargetNullValue)
		if ((updateReason & ResourceUpdateReason.ThemeResource) != 0 &&
			_properties.HasBindings)
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
				// UpdateThemeReference / Phase-2 choke points use — rather than the process-global active
				// theme. For a non-FrameworkElement owner, the injected FE resource context supplies the theme.
				var bindingThemeOwner = ActualInstance as FrameworkElement ?? resourceContextProvider ?? ActualInstance;
				var bindingThemeKey = ResourceDictionary.GetThemeKey(ThemeResolution.ResolveOwnerTheme(bindingThemeOwner));
				_properties.UpdateBindingExpressions(bindingThemeKey);
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
			// uses, threaded in as a parameter. For a non-FrameworkElement owner, the injected FE resource
			// context supplies the theme.
			var themeOwner = ActualInstance as FrameworkElement ?? resourceContextProvider ?? ActualInstance;
			var ownerTheme = ThemeResolution.ResolveOwnerTheme(themeOwner);

			var bindings = _resourceBindings.GetAllBindings();
			foreach (var binding in bindings)
			{
				InnerUpdateResourceBindings(updateReason, dictionariesInScope, ownerTheme, binding.Property, binding.Binding);
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
	private void InnerUpdateResourceBindings(ResourceUpdateReason updateReason, ResourceDictionary[] dictionariesInScope, Theme ownerTheme, DependencyProperty property, ResourceBinding binding)
	{
		try
		{
			InnerUpdateResourceBindingsUnsafe(updateReason, dictionariesInScope, ownerTheme, property, binding);
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
	private void InnerUpdateResourceBindingsUnsafe(ResourceUpdateReason updateReason, ResourceDictionary[] dictionariesInScope, Theme ownerTheme, DependencyProperty property, ResourceBinding binding)
	{
		if ((binding.UpdateReason & updateReason) == ResourceUpdateReason.None)
		{
			// If the reason for the update doesn't match the reason(s) that the binding was created for, don't update it
			return;
		}

		// Select the Light/Dark sub-dictionary by the owner's effective theme passed from
		// UpdateResourceBindings, not the process-global active theme.
		var ownerThemeKey = ResourceDictionary.GetThemeKey(ownerTheme);

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
				if (dict.TryGetValue(binding.ResourceKey, ownerThemeKey, out var value, out var providingDict, shouldCheckSystem: false))
				{
					wasSet = true;
					SetResourceBindingValue(property, binding, value);

					// Pin the providing dictionary in the _themeResources entry (if any)
					// so that subsequent theme changes can re-resolve from it directly.
					if (providingDict is not null && _themeResources is not null)
					{
						var themeRef = _themeResources.Get(property, binding.Precedence);
						themeRef?.SetTargetDictionary(providingDict);
						if (themeRef is not null)
						{
							themeRef.LastResolvedValue = value;
						}
					}

					break;
				}
			}

			if (!wasSet)
			{
				if (ResourceResolver.TryTopLevelRetrieval(binding.ResourceKey, ownerThemeKey, binding.ParseContext, out var value))
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
