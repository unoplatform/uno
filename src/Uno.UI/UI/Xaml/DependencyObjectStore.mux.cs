// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference depends.cpp, commit fc2f82117
//
// CDependencyObject::Enter/EnterImpl/Leave/LeaveImpl. In WinUI these are methods on
// CDependencyObject; in Uno, DependencyObject is an interface on every target, so the
// CDependencyObject layer lives on the DependencyObjectStore that every DependencyObject owns
// (operating on ActualInstance). UIElement's visual Enter walk (UIElement.mux.cs, ported from
// uielement.cpp) calls into this layer exactly where CUIElement::EnterImpl calls
// CDependencyObject::EnterImpl. The enter-property walks live in
// DependencyObjectStore.PropertySystem.mux.cs (ported from PropertySystem.cpp).
//
// Only the lifecycle subset of depends.cpp is ported here; the rest of the file (property
// system, namescope, inheritance context) is NOT PORTED and tracked by the preserved comments
// below. CDependencyObject::SetVisualTree/GetVisualTree are in DependencyObjectExtensions.mux.depends.cs.

#nullable enable

#if UNO_HAS_ENHANCED_LIFECYCLE

using System;
using Uno.UI.Xaml;

namespace Microsoft.UI.Xaml;

public partial class DependencyObjectStore
{
	// MUX Reference: CDependencyObject.h:302 — m_bitFields.fIsProcessingEnterLeave.
	private bool _isProcessingEnterLeave;

	// MUX Reference: CDependencyObject.h — m_bitFields.fLive. For UIElements the live state is
	// UIElement.IsActiveInVisualTree (set by the visual Enter walk); this field carries it for
	// every other DependencyObject so the theme block and (Phase 3) UpdateThemeReference can gate
	// on IsActive like WinUI's CDependencyObject::IsActive().
	private bool _isActive;

	internal bool IsProcessingEnterLeave => _isProcessingEnterLeave;

	/// <summary>
	/// Whether this object is part of the live tree — WinUI's CDependencyObject::IsActive()
	/// (m_bitFields.fLive). UIElements carry the bit on UIElement.IsActiveInVisualTree.
	/// </summary>
	internal bool IsActive => ActualInstance is UIElement uiElement ? uiElement.IsActiveInVisualTree : _isActive;

	// MUX Reference: CDependencyObject ActivateImpl/DeactivateImpl — set/clear m_bitFields.fLive.
	// UIElement's bit (and its Uno-specific Depth reset) is managed by UIElement.ActivateImpl/DeactivateImpl.
	private void ActivateImpl()
	{
		if (ActualInstance is UIElement uiElement)
		{
			uiElement.ActivateImpl();
		}
		else
		{
			_isActive = true;
		}
	}

	private void DeactivateImpl()
	{
		if (ActualInstance is UIElement uiElement)
		{
			uiElement.DeactivateImpl();
		}
		else
		{
			_isActive = false;
		}
	}

	// Causes the object and its "children" to enter scope. If bLive,
	// then the object can now respond to OM requests and perform actions
	// like downloads and animation.
	internal void Enter(DependencyObject? namescopeOwner, EnterParams @params)
	{
		// If IsProcessingEnterLeave is true, then this element is already part of the
		// Enter/Leave walk. This can happen, for instance, if a custom DP's value has
		// been set to some ancestor of this node.
		if (IsProcessingEnterLeave)
		{
			return;
		}
		else
		{
			_isProcessingEnterLeave = true;
		}

		try
		{
			//if (CXamlIslandRoot* xamlIslandRoot = do_pointer_cast<CXamlIslandRoot>(this))
			//{
			//	// The CXamlIslandRoot can enter the tree in a few different ways, and we need to make sure
			//	// that however it enters, we override the params.visualTree with the one from the CXamlIslandRoot.
			//	// CXamlIslandRoot always defines its own visual tree, so we must set it here.  Note that after
			//	// the tree refactoring, this won't be necessary because the XamlIslandRoot won't be in the tree
			//	// anymore.
			//	params.visualTree = xamlIslandRoot->GetVisualTreeNoRef();
			//}

			if (@params.IsLive && @params.VisualTree is not null)
			{
				// As the DO enters the live tree, we call SetVisualTree to remember which one it's associated with
				VisualTreeCache = @params.VisualTree;
			}

			DependencyObject? pAdjustedNamescopeOwner = namescopeOwner;

			// When we copy the EnterParams, reset the pointer to the resource dictionary
			// parent so that descendants don't think they are the direct child of one.
			EnterParams enterParams = @params;
			enterParams.ParentResourceDictionary = null;

			// TODO Uno: NOT PORTED — inherited-properties invalidation (depends.cpp:840-855):
			//if (m_pInheritedProperties != NULL)
			//{
			//	if (m_pInheritedProperties->m_pWriter == this) { m_pInheritedProperties->m_cGenerationCounter = 0; }
			//	else { DisconnectInheritedProperties(); }
			//}

			// TODO Uno: NOT PORTED — standard-namescope-owner adjustment (depends.cpp:857-908):
			// when this object is a namescope owner entering some other scope, WinUI registers its
			// name in the parent namescope, and for permanent owners with already-registered names
			// re-enters with fSkipNameRegistration=TRUE (live) or terminates the non-live walk.

			// TODO Uno: NOT PORTED — popup dual-namescope adjustment (depends.cpp:910-928):
			// a Popup's child receives Enter from both its logical and visual parents' namescopes;
			// the visual-parent Enter resolves the owner via GetStandardNameScopeOwner and caches it
			// on the popup (SetCachedStandardNamescopeOwner).

			// TODO Uno: NOT PORTED — the EnterImpl gate (depends.cpp:930-960). WinUI only runs EnterImpl
			// when there is an adjusted namescope owner or a live enter on a multi-parent-shareable DO
			// (skipping App.xaml parse-time children), and downgrades keyboard-accelerator enters to a
			// dead walk. Uno has no namescope tracking yet, so EnterImpl runs unconditionally.
			EnterImpl(pAdjustedNamescopeOwner, enterParams);
		}
		finally
		{
			_isProcessingEnterLeave = false;
		}
	}

	// Causes the object and its "children" to enter scope. If bLive,
	// then the object can now respond to OM requests and perform actions
	// like downloads and animation.
	//
	// Derived classes are expected to first call <base>::EnterImpl, and
	// then call Enter on any "children".
	internal void EnterImpl(DependencyObject? namescopeOwner, EnterParams @params)
	{
		// Mark the object as in the live tree
		// Enter cannot make a Live object non-live
		// that can only happen in Leave.
		if (@params.IsLive)
		{
			if (!IsActive)
			{
				ActivateImpl();
			}

			//m_checkForResourceOverrides = !!params.fCheckForResourceOverrides;
		}

		// TODO Uno: NOT PORTED — name registration (depends.cpp:986-1001):
		//if (!params.fSkipNameRegistration)
		//{
		//	if (!IsTemplateNamescopeMember())
		//	{
		//		RegisterName(pNamescopeOwner);
		//		RegisterDeferredStandardNameScopeEntries(pNamescopeOwner);
		//	}
		//}
		//
		//if (HasDeferred())
		//{
		//	CDeferredMapping::NotifyEnter(pNamescopeOwner, this, params.fSkipNameRegistration);
		//}

		// Nothing else to do for value types and control/data templates.
		// (Value types cannot be DependencyObjects in Uno; the template check is ported as-is.)
		var owner = ActualInstance;
		if (owner is Controls.ControlTemplate or DataTemplate)
		{
			return;
		}

		// Enumerate all the field-backed properties and enter/invoke as needed.
		// (depends.cpp:1013-1032 — in Uno the store's property enumeration covers WinUI's field-backed
		// CEnterDependencyProperty table; see EnterProperties in DependencyObjectStore.PropertySystem.mux.cs.)
		// TODO Uno: WinUI also runs this walk for dead (non-live) enters — name registration and
		// NeedsInvoke side effects. Uno's walk currently serves theme establishment and live activation
		// only, so it is skipped for dead enters to avoid materializing every DP value on the
		// keyboard-accelerator dead-enter paths.
		if (@params.IsLive)
		{
			EnterProperties(namescopeOwner, @params);
		}

		EnterSparseProperties(pAdjustedNamescopeOwner: namescopeOwner, @params);

		// MUX Reference: depends.cpp:1044-1069 — establish this object's theme from its (logical)
		// inheritance parent now that it is live, before any {ThemeResource} resolves. Runs after the
		// property walks like WinUI: NotifyThemeChanged recursively re-themes the just-entered
		// property values (Theming.cpp:175-248). Element-level theming is a Skia/WASM
		// (UNO_HAS_ENHANCED_LIFECYCLE) feature; native targets support OS + application theme only
		// and intentionally skip this Enter establishment.
		if (IsActive)
		{
			EstablishThemeOnEnterCore(@params);
		}

		//if (params.fIsLive && m_bitFields.fWantsInheritanceContextChanged)
		//{
		//	// We only raise this InheritanceContextChanged if we're entering the live tree because the
		//	// event also acts like a DO.Loaded event for BindingExpression.  This keeps us from adding
		//	// a new internal event
		//	NotifyInheritanceContextChanged();
		//}
	}

	/// <summary>
	/// The theme block of CDependencyObject::EnterImpl (depends.cpp:1044-1069): inherit this
	/// object's theme from its (logical) inheritance parent and re-resolve its theme references.
	/// </summary>
	/// <remarks>
	/// Also invoked without activation for detached UIElements reached through the enter-property
	/// walk (see EnterObjectProperty) — Uno lacks WinUI's IsVisualTreeProperty metadata, so such
	/// elements are theme-established without being live-entered.
	/// </remarks>
	private void EstablishThemeOnEnterCore(in EnterParams @params)
	{
		var owner = ActualInstance;

		// If our theme is different from the parent, make sure we walk the subtree.
		DependencyObject? parent;

		if (owner is FrameworkElement thisAsFe)
		{
			// Get logical parent so popups and flyouts inherit theme changes.
			// MUX: GetInheritanceParentInternal(TRUE /* fLogicalParent */), framework.cpp:3113-3146.
			// In Uno the logical inheritance parent is FrameworkElement.Parent
			// (LogicalParentOverride ?? Store.Parent): a Popup sets its Child's LogicalParentOverride
			// to itself, so the content follows the opener's theme rather than the PopupRoot it is
			// visually reparented under.
			parent = thisAsFe.Parent;
		}
		else
		{
			parent = Parent as DependencyObject;
		}

		var parentTheme = (parent as IDependencyObjectStoreProvider)?.Store.GetTheme() ?? Theme.None;

		// Uno-specific fallback: when the walk reaches a DO whose Store.Parent / LogicalParentOverride
		// isn't set yet (e.g. ContentControl.Content carries no LogicalChild flag, so a
		// CommandBarFlyoutCommandBar assigned as FlyoutPresenter.Content has no logical parent at this
		// point) we use the DP-owner the walk arrived through as the inheritance source. This mirrors
		// WinUI's InheritanceContext — CDependencyObject::SetInheritanceContext sets the property owner
		// as the inherited context when the value goes live, and the theme block then walks that
		// context. Uno hasn't ported per-DP InheritanceContext yet (uno issue #22949), so
		// EnterParams.ThemeInheritanceCaller carries the same data point.
		if (parentTheme == Theme.None && @params.ThemeInheritanceCaller is { } caller)
		{
			parentTheme = caller.GetTheme();
		}

		// MUX: depends.cpp:1060-1068. The WinUI check is `pParent && ...`, but in Uno the fallback
		// above can supply a non-None parentTheme without an actual `parent` object (Content-style DPs
		// that don't propagate Store.Parent), so the inherit branch gates on parentTheme alone.
		if (parentTheme != Theme.None && parentTheme != _theme)
		{
			// MUX: depends.cpp:1062 — inherit the parent theme and walk this subtree.
			// CDependencyObject::NotifyThemeChanged (now hosted on the store for every DO)
			// re-applies a FrameworkElement's own RequestedTheme override via
			// GetRequestedThemeOverride and recursively themes property values and children.
			NotifyThemeChanged(parentTheme);
		}
		else
		{
			// Update theme references to account for new ancestor theme dictionaries.
			UpdateAllThemeReferences(owner, cache: null, ThemeResolution.ResolveOwnerTheme(owner));
		}
	}

	// Causes the object and its properties to leave scope. If bLive,
	// then the object is leaving the "Live" tree, and the object can no
	// longer respond to OM requests related to being Live.   Actions
	// like downloads and animation will be halted.
	internal void Leave(DependencyObject? namescopeOwner, LeaveParams @params)
	{
		// If IsProcessingEnterLeave is true, then this element is already part of the
		// Enter/Leave walk.  This can happen, for instance, if a custom DP's value has
		// been set to some ancestor of this node.
		if (IsProcessingEnterLeave)
		{
			return;
		}
		else
		{
			_isProcessingEnterLeave = true;
		}

		try
		{
			DependencyObject? pAdjustedNamescopeOwner = namescopeOwner;

			// When we copy the LeaveParams, reset the pointer to the resource dictionary
			// parent so that descendants don't think they are the direct child of one.
			LeaveParams leaveParams = @params;
			leaveParams.ParentResourceDictionary = null;

			// It only makes sense to leave a live tree if you are currently live.
			// If this happens, we are likely recovering from a situation where this
			// tree has only partially entered the live tree.
			bool bAdjustedLive = @params.IsLive && IsActive;

			// TODO Uno: NOT PORTED — inherited-properties invalidation (depends.cpp:1110-1125),
			// standard-namescope-owner handling (depends.cpp:1127-1171), popup dual-namescope
			// owner resolution incl. the cached-owner fallback (depends.cpp:1173-1203), and the
			// LeaveImpl gate on namescope owner / multi-parent-shareable DOs (depends.cpp:1205-1227).
			// Uno has no namescope tracking yet, so LeaveImpl runs unconditionally with the
			// adjusted live flag.
			leaveParams.IsLive = bAdjustedLive;
			LeaveImpl(pAdjustedNamescopeOwner, leaveParams);
		}
		finally
		{
			_isProcessingEnterLeave = false;
		}
	}

	// Causes the object and its properties to leave scope. If bLive,
	// then the object is leaving the "Live" tree, and the object can no
	// longer respond to OM requests related to being Live.   Actions
	// like downloads and animation will be halted.
	//
	// Derived classes are expected to first call <base>::LeaveImpl, and
	// then call Leave on any "children".
	//
	// Objects are expected to cleanup all the unshared device resources on their leave from live tree
	// i.e., when params.fIsLive = TRUE. This include primitives,
	// composition nodes, visuals, shape realizations and cache realizations for UIElements.
	// And it includes textures etc, for other objects like brushes and image sources.
	// The recursive leave call for children elements and children properties
	// would do similar cleanup on their final leave. This enables appropriate sharing.
	// Hence an element should not cleanup resources for its
	// child/property in its leave.
	internal void LeaveImpl(DependencyObject? namescopeOwner, LeaveParams @params)
	{
		// Raise InheritanceContextChanged for the live leave.  We need to do this before m_bitFields.fLive is updated.
		// params.fIsLive cannot be used because it is updated before we get here.
		//if (m_bitFields.fLive && m_bitFields.fWantsInheritanceContextChanged)
		//{
		//	NotifyInheritanceContextChanged();
		//}

		// Mark the object as out of tree if the intention of this walk is to notify
		// the element that it is leaving the live tree (as indicated by the bLive parameter.)
		if (@params.IsLive)
		{
			//m_checkForResourceOverrides = !!params.fCheckForResourceOverrides;
			DeactivateImpl();
		}

		// Enumerate all the properties in its class

		// TODO Uno: NOT PORTED — name unregistration (depends.cpp:1275-1280):
		//if (!params.fSkipNameRegistration && !IsTemplateNamescopeMember())
		//{
		//	UnregisterName(pNamescopeOwner);
		//	UnregisterDeferredStandardNameScopeEntries(pNamescopeOwner);
		//}

		// Nothing else to do for value types and control/data templates.
		if (ActualInstance is Controls.ControlTemplate or DataTemplate)
		{
			return;
		}

		// Enumerate all the field-backed properties and leave as needed.
		// (depends.cpp:1292-1309; see LeaveProperties in DependencyObjectStore.PropertySystem.mux.cs.
		// Skipped for dead leaves for the same reason the enter walk is — see EnterImpl.)
		if (@params.IsLive)
		{
			LeaveProperties(namescopeOwner, @params);
		}

		// TODO Uno: NOT PORTED — focus/input cleanup (depends.cpp:1312-1339); Uno handles focus and
		// pointer cleanup of leaving elements through its own visual-tree paths:
		//if (params.fIsLive)
		//{
		//	// If we're currently the focused element, remove ourselves from being focused
		//	const auto contentRoot = VisualTree::GetContentRootForElement(this, LookupOptions::NoFallback);
		//	if (contentRoot != nullptr)
		//	{
		//		CFocusManager * pFocusManager = contentRoot->GetFocusManagerNoRef();
		//
		//		if (pFocusManager && pFocusManager->GetFocusedElementNoRef() == this)
		//		{
		//			pFocusManager->ClearFocus();
		//		}
		//
		//		const auto& akExport = contentRoot->GetAKExport();
		//
		//		if (akExport.IsActive())
		//		{
		//			akExport.RemoveElementFromAKMode(this);
		//		}
		//	}
		//}
		//
		//CInputServices *inputServices = GetContext()->GetInputServices();
		//
		//if (inputServices != nullptr)
		//{
		//	inputServices->ObjectLeavingTree(this);
		//}
	}
}

#endif
