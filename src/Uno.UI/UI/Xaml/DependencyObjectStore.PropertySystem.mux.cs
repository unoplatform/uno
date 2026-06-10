// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference PropertySystem.cpp, commit fc2f82117
//
// CDependencyObject::EnterSparseProperties/EnterObjectProperty/LeaveSparseProperties/
// LeaveObjectProperty, plus the field-backed enter/leave-property loops that WinUI inlines in
// CDependencyObject::EnterImpl/LeaveImpl (depends.cpp:1013-1032 / 1292-1309). WinUI drives those
// loops from per-class CEnterDependencyProperty metadata (DoNotEnterLeave / IsObjectProperty /
// NeedsInvoke flags) plus the sparse IsVisualTreeProperty walk; Uno's store keeps all DP values in
// one DependencyPropertyDetailsCollection, so a single enumeration with an equivalent exclusion
// set covers both. EnterEffectiveValue/LeaveEffectiveValue (SetValue-time enter/leave of DO
// values, PropertySystem.cpp:1093-1161/1355-1403) are NOT PORTED yet — tracked by the theming
// alignment plan (Phase 1, step 7).

#nullable enable

#if UNO_HAS_ENHANCED_LIFECYCLE

using System;
using System.Collections;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.Foundation.Logging;
using Uno.UI.Xaml;

namespace Microsoft.UI.Xaml;

public partial class DependencyObjectStore
{
	/// <summary>
	/// The field-backed enter-property loop of CDependencyObject::EnterImpl (depends.cpp:1013-1032):
	/// recursively Enter every DependencyObject reachable through this object's DP property values.
	/// </summary>
	/// <remarks>
	/// WinUI tags DPs as <c>CEnterDependencyProperty</c> in metadata and EnterImpl recursively calls
	/// Enter on each tagged DP's DO value. Each child's own EnterImpl then runs the
	/// depends.cpp:1044-1069 theme block, inheriting theme from its (logical) inheritance parent. The
	/// result is that — by the time the parent's Enter finishes — every DO reachable through DP
	/// properties (including logical-only collections like MenuFlyout.Items or
	/// CommandBar.PrimaryCommands) carries the correct inherited theme.
	///
	/// Uno's DP metadata has no Enter flag, so we walk every property value, excluding the
	/// equivalents of WinUI's DoNotEnterLeave set (see <see cref="ShouldEnterLeaveProperty"/>).
	/// </remarks>
	private void EnterProperties(DependencyObject? namescopeOwner, EnterParams @params)
	{
		// For SparseProperties we don't propogate down the visualTree pointer.  These elements seem to be
		// able to have parents in different trees.  TODO: Investigate this more.
		// Bug 19548424: Investigate places where an element entering the tree doesn't have a unique VisualTree ptr
		// (MUX: EnterSparseProperties, PropertySystem.cpp:1171-1175. Uno's store enumeration carries the
		// non-visual values WinUI walks as sparse properties — e.g. an attached flyout must not gain a
		// visual-tree association, and thereby a XamlRoot, just because its anchor entered the tree.)
		EnterParams newParams = @params;
		newParams.VisualTree = null;
		@params = newParams;

		foreach (var propertyDetail in _properties.GetAllDetails())
		{
			if (!ShouldEnterLeaveProperty(propertyDetail))
			{
				continue;
			}

			var propertyValue = GetValue(propertyDetail!);

			// Enter the DO value itself before iterating any child collection it carries. Items in
			// a DependencyObjectCollection inherit their theme from their Store.Parent — the
			// collection — so the collection's _theme must be established before each item's
			// Enter reads it.
			if (propertyValue is DependencyObject dependencyObject)
			{
				EnterObjectProperty(dependencyObject, namescopeOwner, @params);
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
			// TODO Uno: in WinUI the collection's own CDOCollection::EnterImpl enters its items;
			// Uno enumerates from the owner until DO-collections get their own EnterImpl override.
			if (propertyValue is IEnumerable enumerable && propertyValue is not string)
			{
				// Defense-in-depth: a custom IEnumerable DP value could throw from GetEnumerator/MoveNext
				// (e.g. a virtualizing data source). The Enter walk must never break element Enter,
				// so guard the enumeration and skip the collection if it can't be safely enumerated.
				try
				{
					foreach (var innerValue in enumerable)
					{
						if (innerValue is DependencyObject innerDependencyObject)
						{
							EnterObjectProperty(innerDependencyObject, namescopeOwner, @params);
						}
					}
				}
				catch (Exception e)
				{
					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().Debug($"Skipping Enter walk of non-enumerable collection on property '{propertyDetail!.Property.Name}': {e.Message}");
					}
				}
			}

			if (propertyValue is IAdditionalChildrenProvider additional)
			{
				foreach (var innerValue in additional.GetAdditionalChildObjects())
				{
					EnterObjectProperty(innerValue, namescopeOwner, @params);
				}
			}
		}
	}

	/// <summary>
	/// The field-backed leave-property loop of CDependencyObject::LeaveImpl (depends.cpp:1292-1309),
	/// mirroring <see cref="EnterProperties"/>.
	/// </summary>
	private void LeaveProperties(DependencyObject? namescopeOwner, LeaveParams @params)
	{
		foreach (var propertyDetail in _properties.GetAllDetails())
		{
			if (!ShouldEnterLeaveProperty(propertyDetail))
			{
				continue;
			}

			var propertyValue = GetValue(propertyDetail!);

			if (propertyValue is DependencyObject dependencyObject)
			{
				LeaveObjectProperty(dependencyObject, namescopeOwner, @params);
			}

			if (propertyValue is IEnumerable enumerable && propertyValue is not string)
			{
				try
				{
					foreach (var innerValue in enumerable)
					{
						if (innerValue is DependencyObject innerDependencyObject)
						{
							LeaveObjectProperty(innerDependencyObject, namescopeOwner, @params);
						}
					}
				}
				catch (Exception e)
				{
					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().Debug($"Skipping Leave walk of non-enumerable collection on property '{propertyDetail!.Property.Name}': {e.Message}");
					}
				}
			}

			if (propertyValue is IAdditionalChildrenProvider additional)
			{
				foreach (var innerValue in additional.GetAdditionalChildObjects())
				{
					LeaveObjectProperty(innerValue, namescopeOwner, @params);
				}
			}
		}
	}

	/// <summary>
	/// Uno's equivalent of WinUI's per-property enter metadata: returns false for the properties
	/// WinUI's CEnterDependencyProperty table excludes (DoNotEnterLeave) or never contains.
	/// </summary>
	/// <remarks>
	/// Filters mirror WinUI's <c>ShouldNotifyPropertyOfThemeChange</c> back-reference list
	/// (Theming.cpp:60-75) and the CEnterDependencyProperty table semantics:
	/// <list type="bullet">
	/// <item>DataContext holds arbitrary user data up the logical tree — never an enter property.</item>
	/// <item>CommandParameter properties hold the command source (often the parent VM); walking them
	/// would recurse *up* the logical tree.</item>
	/// <item>ItemsSource holds arbitrary user data, not themed DO children, and is NOT a WinUI Enter
	/// property. Enumerating it is both wrong (WinUI never walks it) and unsafe: a virtualizing/custom
	/// data source may throw on GetEnumerator (e.g. an indexed ItemsSourceView) or have enumeration
	/// side effects. The realized item containers are entered by the visual-tree walk when they
	/// materialize, so skipping the raw data is safe.</item>
	/// <item>Only properties whose declared type can hold a DependencyObject (or a collection of
	/// them) can carry enter-able children. Value-typed (numeric/enum/struct like Width, Visibility,
	/// Thickness) and string properties never can, so they are skipped before the GetValue
	/// materialization — this avoids reading/coercing the bulk of an object's DPs on every Enter.
	/// WinUI sidesteps this entirely via its curated CEnterDependencyProperty metadata list; porting
	/// that allowlist would narrow the walk further (tracked as follow-up).</item>
	/// </list>
	/// </remarks>
	private bool ShouldEnterLeaveProperty(DependencyPropertyDetails? propertyDetail)
	{
		if (propertyDetail == null
			|| propertyDetail == _properties.DataContextPropertyDetails)
		{
			return false;
		}

		var property = propertyDetail.Property;
		if (property == ButtonBase.CommandParameterProperty
			|| property == MenuFlyoutItem.CommandParameterProperty)
		{
			return false;
		}

		if (property.Name == "ItemsSource")
		{
			return false;
		}

		var propertyType = property.Type;
		if (propertyType.IsValueType || propertyType == typeof(string))
		{
			return false;
		}

		return true;
	}

	internal void EnterSparseProperties(DependencyObject? pAdjustedNamescopeOwner, EnterParams @params)
	{
		// TODO Uno: NOT PORTED — WinUI's EnterSparseProperties (PropertySystem.cpp:1163-1196)
		// enumerates sparse values and enters those whose DP is IsVisualTreeProperty (resetting
		// params.visualTree to null because "these elements seem to be able to have parents in
		// different trees"), and Invokes NeedsInvoke DPs. Uno's store enumeration in EnterProperties
		// covers the generic walk; the IsVisualTreeProperty live-enter semantics (Popup.Child,
		// ContextFlyout, KeyboardAccelerators) are still handled by their explicit Uno paths
		// (UIElement.mux.cs ContextFlyout/KeyboardAccelerators blocks).

		// Uno-specific: FrameworkElement.Resources values are entered as elements — the one known
		// IsVisualTreeProperty effect ported so far. In WinUI this happens through
		// CDependencyObject::EnterImpl → EnterSparseProperties.
		if (ActualInstance is FrameworkElement fe && fe.TryGetResources() is { } resources)
		{
			// Using ValuesInternal to avoid Enumerator boxing
			foreach (var resource in resources.ValuesInternal)
			{
				if (resource is FrameworkElement resourceAsUIElement)
				{
					resourceAsUIElement.XamlRoot = fe.XamlRoot;
					resourceAsUIElement.EnterImpl(@params, int.MinValue);
				}
			}
		}
	}

	private void EnterObjectProperty(DependencyObject pDO, DependencyObject? namescopeOwner, EnterParams @params)
	{
		DependencyObject? pAdjustedNamescopeOwner = namescopeOwner;

		// TODO Uno: NOT PORTED — logical-tree namescope adjustment (PropertySystem.cpp:1200-1217):
		// when the value is a FrameworkElement whose logical parent is not this object, WinUI skips
		// name registration ("name registration should follow the logical tree") and re-resolves the
		// namescope owner via GetStandardNameScopeOwner. Uno has no namescope tracking yet.

		// Uno-specific: a UIElement already active in the visual tree is owned by the visual Enter
		// walk (CUIElement children, UIElement.mux.cs) — re-entering it through a parent's DP walk
		// would only repeat that work. Matches WinUI, where visual children enter via m_pChildren,
		// not the enter-property table.
		if (pDO is UIElement { IsActiveInVisualTree: true })
		{
			return;
		}

		if (pDO is not IDependencyObjectStoreProvider provider)
		{
			return;
		}

		// Uno-specific: the DP-owner store is the theme-inheritance fallback for values without a
		// Store.Parent/logical parent — see EstablishThemeOnEnterCore.
		@params.ThemeInheritanceCaller = this;

		if (pDO is UIElement)
		{
			// TODO Uno: without WinUI's IsVisualTreeProperty metadata, a detached UIElement value
			// must not be live-entered here (it is not in the visual tree — activating it would lie
			// to IsActiveInVisualTree and fire visual lifecycle). It is theme-established only; it
			// live-enters through the visual walk when (if) it joins the tree.
			provider.Store.EstablishThemeOnEnter(@params);
		}
		else
		{
			provider.Store.Enter(pAdjustedNamescopeOwner, @params);
		}
	}

	private void LeaveObjectProperty(DependencyObject pDO, DependencyObject? namescopeOwner, LeaveParams @params)
	{
		DependencyObject? pAdjustedNamescopeOwner = namescopeOwner;

		// TODO Uno: NOT PORTED — logical-tree namescope adjustment (PropertySystem.cpp:1432-1449),
		// mirroring EnterObjectProperty.

		// Uno-specific: UIElements are owned by the visual Leave walk (active) or were never
		// live-entered through this walk (detached) — see EnterObjectProperty.
		if (pDO is UIElement)
		{
			return;
		}

		if (pDO is IDependencyObjectStoreProvider provider)
		{
			provider.Store.Leave(pAdjustedNamescopeOwner, @params);
		}
	}

	/// <summary>
	/// Theme-only enter for detached UIElements reached through the enter-property walk: runs the
	/// EnterImpl theme block and the property walk without activating the element. See
	/// EnterObjectProperty for why activation must not happen.
	/// </summary>
	internal void EstablishThemeOnEnter(EnterParams @params)
	{
		if (IsProcessingEnterLeave)
		{
			return;
		}

		_isProcessingEnterLeave = true;
		try
		{
			EstablishThemeOnEnterCore(@params);
			EnterProperties(namescopeOwner: null, @params);
		}
		finally
		{
			_isProcessingEnterLeave = false;
		}
	}
}

#endif
