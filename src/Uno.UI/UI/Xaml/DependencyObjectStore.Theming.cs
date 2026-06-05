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
	internal void UpdateAllThemeReferences(DependencyObject? owner, ThemeWalkResourceCache? cache = null)
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
				cache);
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
		ThemeWalkResourceCache? cache)
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

			// Phase A: Ancestor walk (WinUI: FindNextResolvedValueNoRef → ScopedResources::TraverseVisualTreeResources)
			// If element is active, walk ancestor ResourceDictionaries to find
			// the resource. This handles re-parenting correctly.
			if (owner is FrameworkElement { IsLoaded: true })
			{
				var dicts = GetResourceDictionaries(includeAppResources: false);
				foreach (var dict in dicts)
				{
					if (dict.TryGetValue(themeRef.ResourceKey, out var ancestorValue, shouldCheckSystem: false))
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
				newValue = themeRef.RefreshValue(owner, cache);
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
			UpdateAllThemeReferences(ActualInstance, ThemeWalkResourceCache.Instance);
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
