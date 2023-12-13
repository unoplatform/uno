// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\core\core\elements\Popup.cpp and dxaml\xcp\components\elements\Popup.cpp (in this order), tag winui3/release/1.4.3, commit 685d2bf

using System;
using Uno.Extensions;
using Uno.UI.DataBinding;
using Uno.UI.DirectUI.Lib;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Rendering;
using Windows.UI.Xaml.Input;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Windows.UI.Xaml.Controls.Primitives;

partial class Popup
{
#region dxaml\xcp\core\core\elements\Popup.cpp

	//	// Window Prop name to store windowed popup instance in popup's window
	//	static char c_szPopupWindowProp[] = "Xaml_PopupWindow";

	//	// Windowed popup's window class
	//	ATOM CPopup.s_windowedPopupWindowClass = 0;

	//	CCPopup(CCoreServices* pCore)
	//		: CFrameworkElement(pCore)
	//    , m_pChild(null)
	//    , m_fIsOpen(false)
	//    , m_fIsLightDismissEnabled(false)
	//    , m_fIsLightDismiss(false)
	//    , m_fIsContentDialog(false)
	//    , m_fIsSubMenu(false)
	//    , m_fIsApplicationBarService(false)
	//    , m_eHOffset(0.0f)
	//    , m_eVOffset(0.0f)
	//    , m_hAdjustment(0.0f)
    // 	  , m_vAdjustment(0.0f)
    //    , m_calculatedHOffset(0.0f)
    //    , m_calculatedVOffset(0.0f)
	//    , m_cEventListenerCount(0)
	//    , m_pTransformer(null)
	//    , m_fAsyncQueueOnRelease(false)
	//    , m_fRemovingListeners(false)
	//    , m_fMadeChildNamescopeOwner(false)
	//    , m_fIsPrintDirty(false)
	//    , m_wasMarkedAsRedirectionElementOnEnter(false)
	//    , m_wasMarkedAsRedirectionElementOnOpen(false)
	//    , m_isRegisteredOnCore(false)
	//    , m_pChildTransitions(null)
	//    , m_focusStateAfterClosing(FocusState.Unfocused)
	//    , m_savedFocusState(FocusState.Unfocused)
	//    , m_shouldTakeFocus(true)
	//    , m_windowedPopupWindow(null)
	//    , m_isWindowed(false)
	//    , m_everOpened(false)
	//    , m_overlayElement(null)
	//    , m_lightDismissOverlayMode(LightDismissOverlayMode.Off)
	//    , m_isOverlayVisible(false)
	//    , m_disableOverlayIsLightDismissCheck(false)
	//    , m_wasOpenedDuringEngagement(false)
	//    , m_closeIsPending(false)
	//    , m_isClosing(false)
	//    , m_isUnloading(false)
	//    , m_isImplicitClose(false)
	//    , m_skippedCreatingPopupHwnd(false)
	//    , m_registeredWindowPositionChangedHandler(false)
	//	{
	//		m_requiresReleaseOverride = true;
	//	}


	//	CPopup.~Cpublic Popup()
	//	{
	//		/*VERIFYHR*/
	//		(RemoveChild());

	//		if (m_isOverlayVisible)
	//		{
	//			/*VERIFYHR*/
	//			(RemoveOverlayElement());
	//		}

	//		ReleaseInterface(m_pTransformer);

	//		ReleaseInterface(m_pChildTransitions);

	//		if (m_isRegisteredOnCore && GetContext())
	//		{
	//			/*VERIFYHR*/
	//			(GetContext().UnregisterRedirectionElement(this));
	//			m_isRegisteredOnCore = false;
	//		}

	//		if (m_spUIAWindow)
	//		{
	//			m_spUIAWindow.Deinit();
	//		}

	//		if (m_automationProviderRequestedToken.value != 0)
	//		{
	//			/*VERIFYHR*/
	//			VERIFYHR(m_contentIsland->remove_AutomationProviderRequested(m_automationProviderRequestedToken));
    //   		m_automationProviderRequestedToken = { };
	//		}

	//		ReleaseDCompResourcesForWindowedpublic Popup();

	//     DiscardContentExternalBackdropLink();

	//     // The PopupWindowSiteBridge needs to be explicitly IClosable::Close()ed, otherwise it will leak. This is because
	//     // the bridge's underlying hwnd has a ref count on it, and until we destroy that hwnd (via IClosable::Close)) the
	//     // bridge will always be kept alive.
	//     // Bug 43723959 - Remove this once the underlying problem is fixed
	//     EnsureBridgeClosed();
	// }

	// void CPopup::EnsureBridgeClosed()
	// {
	//     if (m_popupWindowBridge)
	//     {
	//         if (m_bridgeClosedToken.value)
	//         {
	//             wrl::ComPtr<mu::IClosableNotifier> closableNotifier;
	//             IFCFAILFAST(m_popupWindowBridge.As(&closableNotifier));

	//             IGNOREHR(closableNotifier->remove_FrameworkClosed(m_bridgeClosedToken));
	//         }

	//         if (WinAppSdk::Containment::IsChangeEnabled<WINAPPSDK_CHANGEID_46437389>())
	//         {
	//             // Release the InputSiteAdapter before closing the bridge so it
	//             // won't be closed already when doing its cleanup.
	//             m_inputSiteAdapter.reset();
	//         }

	//         if (!m_bridgeClosed)
	//         {
	//             wrl::ComPtr<wf::IClosable> closable;
	//             IFCFAILFAST(m_popupWindowBridge.As(&closable));
	//             IFCFAILFAST(closable->Close());
	//         }

	//         m_bridgeClosed = true;
	//     }
	// }

	//	//------------------------------------------------------------------------
	//	//
	//	//  Method: SetValue
	//	//
	//	//  Synopsis:
	//	//      SetValue override for popup
	//	//
	//	//------------------------------------------------------------------------
	//	HRESULT CSetValue(SetValueParams& args)
	//	{
	//		HRESULT hr = S_OK;

	//		if (args.m_pDP.GetIndex() == KnownPropertyIndex.Popup_IsOpen)
	//		{
	//			bool fPrevIsOpen = m_fIsOpen;

	//			(CFrameworkElement.SetValue(args));

	//			if (fPrevIsOpen != m_fIsOpen)
	//			{
	//				if (m_pChild)
	//				{
	//					if (GetStandardNameScopeOwner() == null)
	//					{
	//						if (m_fIsOpen)
	//						{
	//							(AddChildAsNamescopeOwner());
	//						}
	//						else if (ShouldRemoveChildAsNamescopeOwner())
	//						{
	//							(RemoveChildAsNamescopeOwner());
	//						}
	//					}

	//					// Open or Close the popup
	//					hr = m_fIsOpen ? Open() : Close();
	//					(hr);
	//				}

	//				m_fIsOpen? RaisePopupEvent(KnownEventIndex.Popup_Opened) : RaisePopupEvent(KnownEventIndex.Popup_Closed);
	//			}
	//		}
	// else if (args.m_pDP->GetIndex() == KnownPropertyIndex::Popup_SystemBackdrop)
    // {
    //     CSystemBackdrop* newBackdropNoRef = do_pointer_cast<CSystemBackdrop>(args.m_value.AsObject());

    //     IFC(CFrameworkElement::SetValue(args));

    //     // If nothing changed then do nothing. Otherwise we'd call OnTargetDisconnected and OnTargetConnected
    //     // back-to-back on the same SystemBackdrop.
    //     if (m_systemBackdrop.get() != newBackdropNoRef)
    //     {
    //         DirectUI::Popup* popupPeerNoRef = static_cast<DirectUI::Popup*>(GetDXamlPeer());

    //         if (m_systemBackdrop && m_fIsOpen && IsWindowed())
    //         {
    //             SystemBackdrop::InvokeOnTargetDisconnectedFromCore(m_systemBackdrop.get(), popupPeerNoRef);

    //             if (WinAppSdk::Containment::IsChangeEnabled<WINAPPSDK_CHANGEID_46074461>())
    //             {
    //                 // Clean up the existing backdrop link and placement visual immediately. Normally this is done
    //                 // synchronously from lifted Composition, but there are cases where we don't get a call back and the
    //                 // closed backdrop link and PlacementVisual stay around, which prevents us from creating new ones.
    //                 DiscardContentExternalBackdropLink();
    //             }
    //         }

    //         m_systemBackdrop = do_pointer_cast<CSystemBackdrop>(newBackdropNoRef);

    //         //
    //         // Note: We call out to OnTargetConnected here, and it's expected that the SystemBackdrop on the other side
    //         // of this call sets us as the target of a Composition SystemBackdropController. That SystemBackdropController
    //         // will internally create a _system_ EffectBrush and set it on this popup. We should be forwarding that brush
    //         // to a ContentExternalBackdropLink inside this windowed popup, and be attaching the CEBL's PlacementVisual
    //         // in this popup's Visual tree.
    //         //
    //         if (m_systemBackdrop && popupPeerNoRef && m_fIsOpen && IsWindowed())
    //         {
    //             ctl::ComPtr<xaml::IXamlRoot> xamlRoot = XamlRoot::GetForElementStatic(popupPeerNoRef);
    //             SystemBackdrop::InvokeOnTargetConnectedFromCore(m_systemBackdrop.get(), popupPeerNoRef, xamlRoot.Get());

    //             if (WinAppSdk::Containment::IsChangeEnabled<WINAPPSDK_CHANGEID_46074461>())
    //             {
    //                 // Create the backdrop link and placement visual immediately. Normally we wait until we get the call back from
    //                 // Composition with the system brush, but that creates a race condition. Xaml's own layout and rendering will
    //                 // cause us to size the placement visual, so we make sure that it's available right away. Otherwise if Xaml
    //                 // layout and rendering win the race against Composition, we find no visual to size. Composition then comes in
    //                 // to create the link and placement visual, but the placement visual is then stuck at 0 size and we have no
    //                 // visible backdrop.
    //                 EnsureContentExternalBackdropLink();
    //             }
    //         }
    //     }
    // }
    // else
	//		{
	//			if (args.m_pDP.GetIndex() == KnownPropertyIndex.Popup_ChildTransitions)
	//			{
	//				if (args.m_value.AsObject() || args.m_value.Asobject())
	//				{
	//					(UIElement.EnsureLayoutTransitionStorage(m_pChild, null, true));
	//				}
	//				// lifetime of TransitionCollection is manually handled since it has no parent
	//				(SetPeerReferenceToProperty(args.m_pDP, args.m_value));
	//			}

	//			(CFrameworkElement.SetValue(args));

	//			//mark layout and render dirty is popup placement properties have changed
	//			if ((args.m_pDP.GetIndex() == KnownPropertyIndex.Popup_HorizontalOffset
	//				|| args.m_pDP.GetIndex() == KnownPropertyIndex.Popup_VerticalOffset)
	//				&& m_pChild && m_fIsOpen)
	//			{
	//				PopupRoot* pPopupRoot = null;
	//				(GetContext().GetAdjustedPopupRootForElement(this, &pPopupRoot));
	//				if (pPopupRoot)
	//				{
	//					pPopupRoot.InvalidateArrange();
	//				}

	//				// Re-position the window for a windowed popup because the offsets have changed
	//				(PositionAndSizeWindowForWindowedpublic Popup());
	//			}
	//		}

	//	Cleanup:
	//		return hr;
	//	}

	//	HRESULT
	//   COnPropertyChanged(PropertyChangedParams& args)
	//	{
	//		(__super.OnPropertyChanged(args));

	//		switch (args.m_pDP.GetIndex())
	//		{
	//			case KnownPropertyIndex.Popup_Child:
	//			case KnownPropertyIndex.Popup_IsLightDismissEnabled:
	//			case KnownPropertyIndex.Popup_LightDismissOverlayMode:
	//				{
	//					(ReevaluateIsOverlayVisible());
	//				}
	//				break;
	//		}

	//		return S_OK;
	//	}

	// xref_ptr<CTransitionTarget> CPopup::EnsureTransitionTarget()
	// {
	//     ASSERT(IsWindowed());

	//     if (!m_secretTransitionTarget)
	//     {
	//         CREATEPARAMETERS cp(GetContext());
	//         xref_ptr<CDependencyObject> ttDO;
	//         IFCFAILFAST(CTransitionTarget::Create(ttDO.ReleaseAndGetAddressOf(), &cp));
	//         m_secretTransitionTarget = do_pointer_cast<CTransitionTarget>(ttDO.get());
	//     }

	//     return m_secretTransitionTarget;
	// }
	//	// Clean up DComp resources to handle device loss
	//	void CCleanupDeviceRelatedResourcesRecursive(bool cleanupDComp)
	//	{
	//		if (m_pChild != null)
	//		{
	//			// Fix for RS4 bug #14854060:  Crash when cleaning up nested Popups.
	//			// The problem is best explained with a picture and an example.  Take this sample XAML tree:
	//			//
	//			// RootVisual
	//			//    |              \
	//			// PublicRoot         PopupRoot
	//			//    |                   |             \
	//			//   ...             Popup1.Child        Popup2.Child
	//			// Popup1                 |
	//			//                     Popup2
	//			//
	//			// The cleanup walk will first walk to PopupRoot.Popup1.Child.Popup2 and tear down Popup2's CompNode.
	//			// This will also delete all the SpriteVisuals created by Popup2's child subtree.  This is a problem
	//			// because Popup2.Child hasn't been cleaned up yet and is still holding naked SpriteVisual pointers in
	//			// its m_propertyRenderData.  Thus when the cleanup walk visits Popup2.Child later on, we'll crash
	//			// trying to unparent these SpriteVisuals (as they've already been deleted).
	//			// To prevent this, we first jump into Popup.Child and clean it up before cleaning up ourselves.
	//			m_pChild.CleanupDeviceRelatedResourcesRecursive(cleanupDComp);
	//		}

	//		__super.CleanupDeviceRelatedResourcesRecursive(cleanupDComp);

	//     if (cleanupDComp)
    // {
    //     ReleaseDCompResourcesForWindowedPopup();
    // }
	//	}

	//	bool CIsElementOrParentEngaged(DependencyObject* element)
	//	{
	//		DependencyObject* pElement = element;

	//		while (pElement != null)
	//		{
	//			if (pElement.OfTypeByIndex(KnownTypeIndex.Control) && (CControl*)(pElement).IsFocusEngaged())
	//			{
	//				return true;
	//			}
	//			else if (pElement.OfTypeByIndex(KnownTypeIndex.Popup) && (CPopup*)(pElement).WasOpenedDuringEngagement())
	//			{
	//				// If the element lives in a popup that was opened during engagement, we should still consider the element
	//				// engaged, although it may live in different root from the actual engaged element

	//				return true;
	//			}

	//			if (var elementCast = do_pointer_cast<UIElement>(pElement))
	//        {
	//				pElement = elementCast.GetUIElementAdjustedParentInternal(true /*public parents only*/);
	//			}

	//		else
	//			{
	//				pElement = pElement.GetParent();
	//			}
	//		}

	//		return false;
	//	}

	//------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Adds child to the visual tree and itself to the open popup list
	//      maintained by popup root.
	//
	//------------------------------------------------------------------------
	private void Open()
	{
		PopupRoot? pPopupRoot = null;
		FocusManager? pFocusManager = null;
		DependencyObject? pFocusedElement = null;
		ManagedWeakReference pFocusedElementWeakRef;

		if (m_fIsOpen)
		{
			throw new InvalidOperationException("Popup is already open");
		}

		var pPopupRoot = CoreServices.Instance.GetAdjustedPopupRootForElement(this);
		if (CoreServices.Instance.InitializationType == InitializationType.IslandsOnly)
		{
			// When an app is running in an islands-only context, there are times where we can't find
			// a popup root for a given popup.  For example, if an app creates a popup in code and
			// calls Open, we don't know which island it's associated with -- we just fail out here.
			if (pPopupRoot is null)
			{
				throw new InvalidOperationException("Popup.XamlRoot is not set");
			}
		}
		else
		{
			// In other initialization modes we expect to have a PopupRoot.
			MUX_ASSERT(pPopupRoot);
		}

		// If it's been requested that this popup be windowed,
		// then we'll set it to be so at this point before opening.
		IFC_RETURN(SetIsWindowedIfNeeded());

		m_everOpened = true;

		if (GetContext().HasXamlIslands())
		{
			var xamlIslandRoot = do_pointer_cast<CXamlIslandRoot>(pPopupRoot.GetParent());
			if (xamlIslandRoot)
			{
				// Remember the XamlIslandRoot that was the context for the creation of this popup
				SetAssociatedIsland(xamlIslandRoot);
			}
		}

		m_savedFocusState = FocusState.Unfocused;

		if (pPopupRoot is not null)
		{
			m_pChild.SetAssociated(false, null); // clear the association flag, AddChild will set it.

			// before we add this child, the we should set the enter counter correctly.
			// Normally a child would get an enter, which is where set the enter counter. However, in the
			// case of popup, a child is only entered once, even though it is being removed from the visual tree.
			m_pChild.m_enteredTreeCounter = EnteredInThisTick;
			// normal would also be to have a live enter.
			(CTransition.CancelTransitions(m_pChild));  // kills currently running animations, quite likely

			// Cancel animations before flushing pending operations. Canceling an animation requires access to the comp node,
			// which can be destroyed when we finish closing the popup during flush.
			CancelHideAnimationToPrepareForShow();

			FlushPendingKeepVisibleOperations();

			// Neither the Popup nor the child should be in the scene, since they shouldn't be walked unless the Popup is open.
			// The one exception would have been if the child was unloading, but we just cancelled that transition if it existed.
			ASSERT(!IsInPCScene() && !m_pChild.IsInPCScene());

			// Create window for windowed popups. Do this before adding the child to the tree. If the child has a
			// ScrollViewer in its subtree, then the ScrollViewer will look for this windowed popup's hwnd to use for
			// DManip, and the hwnd comes from the popup window bridge.
			if (IsWindowed())
			{
				IFC_RETURN(EnsureWindowForWindowedPopup());
			}

			if (WinAppSdk::Containment::IsChangeEnabled<WINAPPSDK_CHANGEID_46702704>())
			{
				// Initial phase of popup addition to open popup list: just add the popup to the m_pOpenPopups list and add-ref it.
				// This phase is performed *before* the AddChild call just below so that CUIElement::EnterImpl for any 
				// ScrollViewer finds the correct HWND to hand off to DManip through GetElementInputWindow().
				IFC_RETURN(pPopupRoot->StartAdditionToOpenPopupList(this));
			}

			HRESULT hr = pPopupRoot.AddChild(m_pChild);
			if (/*FAILED*/(hr))
			{
				// AddChild failed, set the associated flag back.
				m_pChild.SetAssociated(true, null /* Association owner needed only for shareable, non-parent aware DOs */);
				
				if (WinAppSdk::Containment::IsChangeEnabled<WINAPPSDK_CHANGEID_46702704>())
				{
					// Undo the addition performed by the StartAdditionToOpenPopupList call above.
					IGNOREHR(pPopupRoot->UndoAdditionToOpenPopupList(this));
				}
			}
			(hr);

			if (m_isOverlayVisible)
			{
				(AddOverlayElementToPopupRoot());
			}

			if (WinAppSdk::Containment::IsChangeEnabled<WINAPPSDK_CHANGEID_46702704>())
			{
				// Final phase of popup addition to open popup list: peg managed peer, updates themes and rendering trees.
				IFC_RETURN(pPopupRoot->CompleteAdditionToOpenPopupList(this));
			}
			else
			{
				IFC_RETURN(pPopupRoot->AddToOpenPopupList(this));
			}

			(pPopupRoot.AddToOpenPopupList(this));

			if (IsWindowed())
			{
				IFC_RETURN(ShowWindowForWindowedPopup());
			}

			// Ensure the Popup is marked for composition. If the Popup was already marked we do not need to mark it again.
			if (!m_wasMarkedAsRedirectionElementOnEnter && !m_wasMarkedAsRedirectionElementOnOpen)
			{
				(StartComposition());
				m_wasMarkedAsRedirectionElementOnOpen = true;
			}

			pFocusManager = VisualTree.GetFocusManagerForElement(this);
			if (pFocusManager is not null)
			{
				pFocusedElement = pFocusManager.GetFocusedElementNoRef();

				if (pFocusedElement is not null)
				{
					m_wasOpenedDuringEngagement = IsElementOrParentEngaged(pFocusedElement);
				}
			}

			if (m_fIsLightDismissEnabled || IsFlyout())
			{
				if (m_fIsLightDismissEnabled)
				{
					m_fIsLightDismiss = true;

					// Mark the PopupRoot as dirty to redraw the light dismiss layer.
					UIElement.NWSetContentDirty(pPopupRoot, DirtyFlags.Render);
				}

				if (pFocusedElement is not null && pFocusManager is not null)
				{
					pFocusedElementWeakRef = xref.get_weakref((DependencyObject*)(pFocusedElement));
					m_savedFocusState = pFocusManager.GetRealFocusStateForFocusedElement();
				}

				m_pPreviousFocusWeakRef.reset();

				// We still want to save the previous focus element and the focus state. That is why we are
				// only checking for our state flag at the last step of taking focus.
				//Only set focus if the child element does not have AllowFocusOnInteraction set to false
				if (m_shouldTakeFocus && Focus.FocusSelection.ShouldUpdateFocus(m_pChild, m_savedFocusState))
				{
					InitialFocusSIPSuspender setInitalFocusTrue(pFocusManager);
					(SetFocus(FocusState.Programmatic));
				}

				m_pPreviousFocusWeakRef = std.move(pFocusedElementWeakRef);

				m_savedXYFocusManifolds = pFocusManager.GetXYFocus().ResetManifolds();
			}

			//
			// Opening - Dirties (Render | Bounds)
			//
			UIElement.NWSetContentDirty(this, DirtyFlags.Render | DirtyFlags.Bounds);
			InvalidateChildBounds();

			m_isUnloading = false;
		}

		//Ensure compNode if the public root is canvas. Here is for the case
		//that parentless popups are set shadow before they are opened
		CValue shadowValue;
		(GetValueByIndex(KnownPropertyIndex.UIElement_Shadow, &shadowValue));
		bool isShadowSet = !!(shadowValue.AsObject());
		if (isShadowSet)
		{
			EnsureRootCanvasCompNode();
		}

		DirectUI::Popup* popupPeerNoRef = static_cast<DirectUI::Popup*>(GetDXamlPeer());
		if (m_systemBackdrop && popupPeerNoRef && IsWindowed())
		{
			ctl::ComPtr<xaml::IXamlRoot> xamlRoot = XamlRoot::GetForElementStatic(popupPeerNoRef);
			SystemBackdrop::InvokeOnTargetConnectedFromCore(m_systemBackdrop.get(), popupPeerNoRef, xamlRoot.Get());

			if (WinAppSdk::Containment::IsChangeEnabled<WINAPPSDK_CHANGEID_46074461>())
			{
				// Create the backdrop link and placement visual immediately. Normally we wait until we get the call back from
				// Composition with the system brush, but that creates a race condition. Xaml's own layout and rendering will
				// cause us to size the placement visual, so we make sure that it's available right away. Otherwise if Xaml
				// layout and rendering win the race against Composition, we find no visual to size. Composition then comes in
				// to create the link and placement visual, but the placement visual is then stuck at 0 size and we have no
				// visible backdrop.
				EnsureContentExternalBackdropLink();
			}
		}
	}

	internal void ForceCloseForTreeReset()
	{
		// PopupRoot.CloseAllPopupsForTreeReset should have cleared out all its unloading children and called
		// FlushPendingKeepVisibleOperations on them before getting here, but PopupRoot's unloading children
		// are the children of popups. The popup itself can still be pending, and needs to be cleaned up here.
		//
		// Note that a popup child's FlushPendingKeepVisibleOperations can call into the popup if the popup had
		// removed the child and placed it in m_unloadingChild, but if the popup itself was closed, then the
		// child is still in m_pChild and FlushPendingKeepVisibleOperations will not propagate up to the popup.
		MUX_ASSERT(m_fIsOpen
			|| (!m_fIsOpen && m_closeIsPending));

		if (m_fIsOpen)
		{
			// Close calls PopupRoot.RemoveFromOpenPopupList, which calls UnpegManagedPeer, which can release
			// the last reference on this CPopup. Take a beforehand to make sure the popup doesn't get deleted
			// in the middle of Close.
			//
			// We don't add this method directly in Close because Close can be called from the popup dtor via
			// RemoveChild. In that case we don't want to resurrect a deleted popup.
			xref_ptr<CPopup> keepAlive(this);
			IFCFAILFAST(keepAlive.PegManagedPeer());

			var scopeGuard = wil.scope_exit([&]

			{
				keepAlive.UnpegManagedPeer();
			});

			m_fIsOpen = false;
			Close(true /* forceClose */);
		}
		else
		{
			FlushPendingKeepVisibleOperations();
		}
	}

	//	//------------------------------------------------------------------------
	//	//
	//	//  Synopsis:
	//	//      Remove child from the visual tree and itself from the open popup list
	//	//      maintained by popup root.
	//	//
	//	//------------------------------------------------------------------------
	//	HRESULT CClose(bool forceCloseforTreeReset)
	//	{
	//		HRESULT hr = S_OK;
	//		PopupRoot* pPopupRoot = null;
	//		bool wasFocused = false;
	//		FocusManager* pFocusManager = null;
	//		UIElementCollection* pChildrenCollection = null;

	//		//Fix bug: 16797776, reentrancy to Close() may occur in
	//		//some rare cases
	//		if (m_isClosing)
	//		{
	//			return S_OK;
	//		}

	//		ReentrancyGuard guard(this);

	//		if (!m_isImplicitClose)
	//		{
	//			DCompTreeHost* dcompTreeHost = GetDCompTreeHost();
	//			dcompTreeHost.RequestKeepAliveForHiddenEventHandlersInSubtree(this);
	//		}

	//		bool keepVisible = !forceCloseforTreeReset && ComputeKeepVisible();

	//		// Can't close an unloading popup. The pending close should be completed first, then the popup should be opened again.
	//		ASSERT(!m_isUnloading);

	//		// Implicit hide animation notes:
	//		// If we detect an implicit hide animation in this subtree, we need to do a number of things
	//		// to properly play the animation.  Those things are controlled by the keepVisible flag.
	//		// When the animation completes, we'll finish closing in FlushPendingKeepVisibleOperations();
	//		if (keepVisible)
	//		{
	//			SetKeepVisible(true);
	//			m_closeIsPending = true;
	//		}

		// if (m_systemBackdrop && IsWindowed())
		//     {
		//         DirectUI::Popup* popupPeerNoRef = static_cast<DirectUI::Popup*>(GetDXamlPeer());
		//         SystemBackdrop::InvokeOnTargetDisconnectedFromCore(m_systemBackdrop.get(), popupPeerNoRef);

		//         if (WinAppSdk::Containment::IsChangeEnabled<WINAPPSDK_CHANGEID_46074461>())
		//         {
		//             // Clean up the existing backdrop link and placement visual immediately. Normally this is done
		//             // synchronously from lifted Composition, but there are cases where we don't get a call back and the
		//             // closed backdrop link and PlacementVisual stay around, which prevents us from creating new ones.
		//             DiscardContentExternalBackdropLink();
		//         }
		//     }

		//     IFC_RETURN(GetContext()->GetAdjustedPopupRootForElement(this, &pPopupRoot));
			
	//		if (pPopupRoot)
	//		{
	//			// normally we would get a live leave, in the case of a popup closing or opening, we do not do that.
	//			CLayoutManager* pLayoutManager = VisualTree.GetLayoutManagerForElement(m_pChild);
	//			if (pLayoutManager)
	//			{
	//				(CTransition.CancelTransitions(m_pChild));  // kills currently running animations, quite likely
	//				m_pChild.m_leftTreeCounter = LeftInThisTick;
	//			}

	//			// Note: even in keepVisible scenarios, we take the child out of the popup root's child collection. That's okay,
	//			// because the popup root renders and hit tests via its list of open popups, and not via its child collection.
	//			//
	//			// If the child has KeepVisible requirements, then this will keep it visible via UIElementCollection.
	//			(pPopupRoot.RemoveChild(m_pChild));

	//			// set the association flag, visual link is no longer present
	//			m_pChild.SetAssociated(true, null /* Association owner needed only for shareable, non-parent aware DOs */);

	//			// For windowed popups, the LTE for the Unload transition caused by the RemoveChild() will be clipped to
	//			// the Jupiter window, although the popup's window's content is not clipped. This causes an ugly flicker
	//			// where the clipped LTE is drawn followed by the unclipped popup window's content. Fix this by canceling
	//			// the Unload transition.
	//			if (IsWindowed())
	//			{
	//				(CTransition.CancelTransitions(m_pChild));
	//			}

	//			// RemoveFromOpenPopupList() will release the last reference to this Popup and will unpeg its managed peer.
	//			// This will cause problems if there are unloading transitions, since render walk will call GetLogicalParentNoRef()
	//			// on the child which will return an invalid pointer to this popup.
	//			// Thus, we need to keep this popup alive until the child finishes unloading.
	//			if (!keepVisible)
	//			{
	//				pChildrenCollection = (UIElementCollection*)(pPopupRoot.GetChildren());
	//				if (pChildrenCollection && pChildrenCollection.IsUnloadingElement(m_pChild))
	//				{
	//					UnloadCleanup* pRemoveLogicToExecute = pChildrenCollection.m_pUnloadingStorage.Get(m_pChild);

	//					*pRemoveLogicToExecute = (UnloadCleanup)(*pRemoveLogicToExecute | UC_ReleaseParentPopup);
	//					(PegManagedPeer());
	//					AddRef();
	//				}
	//			}

	//			if (m_isOverlayVisible)
	//			{
	//				(RemoveOverlayElementFromPopupRoot());
	//			}

	//			if (keepVisible)
	//			{
	//				// Keep this popup in the opened list, but mark it as unloading. It will continue to render.
	//				m_isUnloading = true;
	//			}
	//			else
	//			{
	//				(pPopupRoot.RemoveFromOpenPopupList(this, true));
	//			}

	//			m_wasOpenedDuringEngagement = false;

	//			// If the Popup was marked for composition upon opening, unmark is when it closes.
	//			// This handles parentless Popups that were opened and closed without ever entering the tree.
	//			if (m_wasMarkedAsRedirectionElementOnOpen && !IsActive() && !keepVisible)
	//			{
	//				ASSERT(!m_wasMarkedAsRedirectionElementOnEnter);
	//				(StopComposition());
	//			}

	//			// calls the OnClosed callback function for the Popup_Partial class
	//			// this was done to invoke a reverse change in the close property
	//			CCoreServices* pCore = GetContext();
	//			if (pCore)
	//			{
	//				(FxCallbacks().Popup_OnClosed(this));
	//			}
	//		}

	//		// The Popup needs to be removed from the scene, since it will no longer be walked.
	//		if (!keepVisible)
	//		{
	//			LeavePCScene();
	//		}

	//		if (pPopupRoot && !keepVisible)
	//		{
	//			/// Hide the popup, but only after LeavePCScene is called. This is because HideWindowForWindowedPopup
				// calls ReleaseDCompResourcesForWindowedPopup which would remove visuals from the tree that LeavePCScene
				// needs to access.
				// IFC_RETURN(HideWindowForWindowedPopup());
	//		}

	//		pFocusManager = VisualTree.GetFocusManagerForElement(this);

	//		if (m_fIsLightDismiss || IsFlyout())
	//		{
	//			// If this popup still contains focus, or if nothing now has focus, try to return focus to the item
	//			// that was focused before opening the light-dismiss-enabled popup. If that fails, try to clear FocusManager's focus.
	//			if (pFocusManager &&
	//				(pFocusManager.GetFocusedElementNoRef() == null ||
	//				 pFocusManager.GetFocusedElementNoRef() == this ||
	//				 HasFocusedElementAsChild()))
	//			{
	//				// Determine the FocusState we should use.
	//				FocusState focusState = m_focusStateAfterClosing;
	//				if (focusState == FocusState.Unfocused)
	//				{
	//					// The popup was not light-dismissed, so use the saved FocusState.
	//					focusState = m_savedFocusState;

	//					if (focusState == FocusState.Unfocused)
	//					{
	//						focusState = FocusState.Programmatic;
	//					}
	//				}

	//				// #1. Try to focus the previously focused element that we stored a weakref to.
            // if (m_pPreviousFocusWeakRef)
            // {
            //     CDependencyObject* pPreviousFocus = m_pPreviousFocusWeakRef.lock();
            //     if (pPreviousFocus && FocusProperties::IsFocusable(pPreviousFocus, false /*ignoreOffScreenPosition*/))
            //     {
            //         if (pPreviousFocus->OfTypeByIndex<KnownTypeIndex::UIElement>())
            //         {
            //             // Scenario where proofing menu is light-dismissed and focus is going back to floatie: change focusState to keyboard since the previously focused floatie
            //             // child element has AllowFocusOnInteraction set to false
            //             if (focusState == DirectUI::FocusState::Pointer && FxCallbacks::TextControlFlyout_IsElementChildOfOpenedFlyout(do_pointer_cast<CUIElement>(pPreviousFocus)))
            //             {
            //                 focusState = DirectUI::FocusState::Keyboard;
            //             }

            //             FocusMovement movement{pPreviousFocus, DirectUI::FocusNavigationDirection::None, focusState};
            //             movement.isForLightDismiss = true;
            //             const FocusMovementResult result = pFocusManager->SetFocusedElement(movement);
            //             IFC_RETURN(result.GetHResult());
            //             wasFocused = result.WasMoved();
            //         }
            //         else if (CFocusableHelper::IsFocusableDO(pPreviousFocus))
            //         {
            //             FocusMovement movement{pPreviousFocus, DirectUI::FocusNavigationDirection::None, focusState};
            //             movement.isForLightDismiss = true;
            //             const FocusMovementResult result = pFocusManager->SetFocusedElement(movement);
            //             IFC_RETURN(result.GetHResult());
            //             wasFocused = result.WasMoved();
            //         }
            //     }
            // }

            // // #2. Try to focus the first focusable element
            // if (!wasFocused)
            // {
            //     CDependencyObject* pFirstFocusableElement = static_cast<CDependencyObject*>(pFocusManager->GetFirstFocusableElementFromRoot(FALSE));
            //     if (pFirstFocusableElement)
            //     {
            //         InitialFocusSIPSuspender setInitalFocusTrue(pFocusManager);

            //         FocusMovement movement{pFirstFocusableElement, DirectUI::FocusNavigationDirection::None, focusState};
            //         movement.isForLightDismiss = true;
            //         const FocusMovementResult result = pFocusManager->SetFocusedElement(movement);
            //         IFC_RETURN(result.GetHResult());
            //         wasFocused = result.WasMoved();
            //     }
            // }

            // // #3. Clear focus, so the CPopup doesn't keep focus.
            // if (!wasFocused)
            // {
            //     pFocusManager->ClearFocus();
            // }

            // pFocusManager->GetXYFocus().SetManifolds(m_savedXYFocusManifolds);
            // m_savedXYFocusManifolds.Reset();
	//			}

	//			m_focusStateAfterClosing = FocusState.Unfocused;
	//			m_fIsLightDismiss = false;
	//			m_savedFocusState = FocusState.Unfocused;

	//			// Mark the PopupRoot as dirty to clear the light dismiss layer, if necessary.
	//			if (pPopupRoot != null)
	//			{
	//				UIElement.NWSetContentDirty(pPopupRoot, DirtyFlags.Render);
	//			}
	//		}

	//		//
	//		// Closing - Dirties (Render | Bounds)
	//		//
	//		UIElement.NWSetContentDirty(this, DirtyFlags.Render | DirtyFlags.Bounds);

	//		InvalidateChildBounds();

	//		// The PopupAutomationPeer will be null from calling UIElement.GetOrCreateAutomationPeer() if Popup
	//		// is the closed state and the existing PopupAutomationPeer will invalidate the owner from
	//		// CUIElement::OnCreateAutomationPeer() that shouldn't be referenced from other AutomationPeer.
    
	//		// The below calling SetAPParent(null) ensures the disconnect the relationship between PopupAutomationPeer
	//		// and Popup child's AutomationPeer when Popup is closed.
	//		{
	//			var pChildAP = m_pChild.GetAutomationPeer();

	//			if (pChildAP && pChildAP.HasParent())
	//			{
	//				pChildAP.SetAPParent(null);
	//			}
	//		}

	//	Cleanup:
	//		RRETURN(hr);
	//	}

	//	XBOOL CSkipRenderForLayoutTransition()
	//	{
	//		if (m_pChild == null && !m_unloadingChild)
	//		{
	//			// Case 1:  Since we have no child, there's no content to render in the first place.
	//			return true;
	//		}

	//		if (!IsOpen())
	//		{
	//			// Case 2:  We're not in the Open state, but we may be playing a Hide animation.
	//			// Skip rendering only if we're not playing a Hide animation.
	//			return (!IsKeepVisible());
	//		}

	//		// Case 3:  We're Open and have a child.  Dont skip rendering.
	//		return false;
	//	}

	//	void CFlushPendingKeepVisibleOperations()
	//	{
	//		// The call to RemoveFromOpenPopupList can call UnpegManagedPeer to release the last reference on this CPopup.
	//		// Take a beforehand to make sure this object doesn't get deleted in the middle of the method.
	//		xref_ptr<CPopup> keepAlive(this);

	//		__super.FlushPendingKeepVisibleOperations();

	//		// This popup could have been kept visible because of Visibility="Collapsed", in which case there's no pending
	//		// close operation and there's nothing to do.
	//		if (m_closeIsPending)
	//		{
	//			PopupRoot* popupRoot = null;
	//			IFCFAILFAST(GetContext().GetAdjustedPopupRootForElement(this, &popupRoot));
	//			ASSERT(popupRoot != null);

	//			IFCFAILFAST(popupRoot.RemoveFromOpenPopupList(this, true));
	//			m_closeIsPending = false;

	//			LeavePCScene();

	//			if (m_wasMarkedAsRedirectionElementOnOpen && !IsActive())
	//			{
	//				ASSERT(!m_wasMarkedAsRedirectionElementOnEnter);
	//				IFCFAILFAST(StopComposition());
	//			}

	//			HideWindowForWindowedpublic Popup();
	//		}
	//	}

	//static
//static
// bool CPopup::DoesPlatformSupportWindowedPopup(_In_ CCoreServices* pCore)
// {
//     // CoreWindowSiteBridge doesn't support creating a PopupSiteBridge, which means we can't implement windowed popups
//     // in UWPs. Note that only some tests run in UWP mode. WinUI 3 only runs in islands mode out in the wild.
//     return (pCore->GetInitializationType() == InitializationType::IslandsOnly);
// }

// _Check_return_ HRESULT CPopup::SetIsWindowedIfNeeded()
// {
//     CValue value;
//     IFC_RETURN(GetValueByIndex(KnownPropertyIndex::Popup_ShouldConstrainToRootBounds, &value));

//     if (!value.AsBool() || IsWindowed())
//     {
//         // Before making the popup windowed, run a set of tests that determine if we can correctly compute
//         // the bounds for the popup's HWND.  If these requirements aren't met, don't actually make it windowed.
//         // Note that these requirements are re-evaluated every time the popup is opened, so it's possible for
//         // the popup to toggle between windowed/non-windowed due to these requirements.  However it's not possible
//         // for an app to toggle the popup between windowed/non-windowed via the ShouldConstrainToRootBounds property
//         // (see enforcement of this in Popup::OnPropertyChanged2()).
//         if (MeetsRenderingRequirementsForWindowedPopup())
//         {
//             if (!IsWindowed())
//             {
//                 IFC_RETURN(SetIsWindowed());
//             }
//         }
//         else
//         {
//             m_isWindowed = FALSE;
//         }
//     }

//     return S_OK;
// }

	//	// Set popup to use a window, so its content is not clipped to the Jupiter window.
	//	HRESULT CSetIsWindowed()
	//	{
	//		ASSERT(!m_isWindowed);

	//		CCoreServices* pCore = GetContext();
	//		// Ignore on unsupported platforms, so controls can use CPopup.IsWindowed to determine whether to
	//		// fall back to non-windowed placement.
	//		if (DoesPlatformSupportWindowedPopup(pCore))
	//		{
	//			// Call DXaml peer to hook up window position handlers for light dismiss behavior
	//			if (pCore)
	//			{
	//				if (!m_registeredWindowPositionChangedHandler)
	//				{
	//					(FxCallbacks().Popup_HookupWindowPositionChangedHandler(this));
	//					m_registeredWindowPositionChangedHandler = true;
	//				}
	//			}

	//			m_isWindowed = true;
	//		}

	//		return S_OK;
	//	}

	//	void CSetPointerCapture()
	//	{
	//		IFCFAILFAST(m_inputSiteAdapter.SetPointerCapture());
	//	}

	//	void CReleasePointerCapture()
	//	{
	//		IFCFAILFAST(m_inputSiteAdapter.ReleasePointerCapture());
	//	}

// bool CPopup::HasPointerCapture() const
// {
//     if (m_inputSiteAdapter)
//     {
//         return m_inputSiteAdapter->HasPointerCapture();
//     }

//     return false;
// }

// // Return window handle for test hook and automation
// HWND CPopup::GetWindowHandle()
// {
//     return m_windowedPopupWindow;
// }

// // Create windowed popup's window, if it doesn't exit
// _Check_return_ HRESULT CPopup::EnsureWindowForWindowedPopup()
// {
//     auto core = GetContext();

//     ASSERT(IsWindowed());

//     if (core->GetHostSite()->IsWindowDestroyed())
//     {
//         // Xaml is shutting down. Trying to create a child hwnd of the destroyed Xaml hwnd will hit an error. Just no-op.
//         m_skippedCreatingPopupHwnd = true;
//     }
//     else
//     {
//         //
//         // It's possible this windowed popup has moved between islands since it last closed. Xaml uses a shared context
//         // menu for all TextBoxes, for example, so we need to check that this windowed popup is still anchored to the same
//         // island. If not, release all the window-related resources and re-create them.
//         //
//         CXamlIslandRoot* island = GetAssociatedXamlIslandNoRef();
//         if (m_previousXamlIslandHwnd != island->GetPositioningHWND())
//         {
//             // The PopupWindowSiteBridge needs to be explicitly IClosable::Close()ed, otherwise it will leak. This is because
//             // the bridge's underlying hwnd has a ref count on it, and until we destroy that hwnd (via IClosable::Close)) the
//             // bridge will always be kept alive.
//             // Bug 43723959 - Remove this once the underlying problem is fixed
//             EnsureBridgeClosed();

//             m_contentIsland.Reset();
//             if (WinAppSdk::Containment::IsChangeEnabled<WINAPPSDK_CHANGEID_46437389>())
//             {
//                 m_inputSiteAdapter.reset();
//             }
//             m_popupWindowBridge.Reset();
//             m_desktopBridge.Reset();
//             m_windowedPopupWindow = NULL;
//             if (!WinAppSdk::Containment::IsChangeEnabled<WINAPPSDK_CHANGEID_46437389>())
//             {
//                 m_inputSiteAdapter.reset();
//             }

//             m_previousXamlIslandHwnd = island->GetPositioningHWND();
//         }

//         if (!m_popupWindowBridge)
//         {
//             // PopupSiteBridges can't be created from CoreWindowSiteBridge, only from DesktopChildSiteBridge. That means
//             // windowed popups aren't supported for UWPs (see DoesPlatformSupportWindowedPopup). The lack of support comes
//             // from lack of features for top-level window moving, monitor tracking, and light dismiss behavior in UWPs.
//             ASSERT(island != nullptr);

//             wrl::ComPtr<ixp::IDesktopChildSiteBridge> desktopChildSiteBridge { island->GetDesktopContentBridgeNoRef() };
//             wrl::ComPtr<ixp::IDesktopSiteBridge2> contentSiteBridge;
//             IFCFAILFAST(desktopChildSiteBridge.As(&contentSiteBridge));
//             IFC_RETURN(contentSiteBridge->TryCreatePopupSiteBridge(m_popupWindowBridge.ReleaseAndGetAddressOf()));

//             // Retrieve and cache the windowed popup hwnd.
//             ABI::Microsoft::UI::WindowId windowId;
//             IFCFAILFAST(m_popupWindowBridge.As(&m_desktopBridge));
//             IFC_RETURN(m_desktopBridge->get_WindowId(&windowId));
//             IFC_RETURN(Windowing_GetWindowFromWindowId(windowId, &m_windowedPopupWindow));

//             // Ensure that the window text is correct for UIA.
//             xstring_ptr strPopupHostUIAName;
//             IFC_RETURN(core->GetBrowserHost()->GetLocalizedResourceString(UIA_WINDOWED_POPUP_HOST, &strPopupHostUIAName));
//             if (!SetWindowText(m_windowedPopupWindow, strPopupHostUIAName.GetBuffer()))
//             {
//                 IFC_RETURN(HResultFromKnownLastError());
//             }

//             {
//                 wrl::ComPtr<mu::IClosableNotifier> closableNotifier;
//                 IFCFAILFAST(m_popupWindowBridge.As(&closableNotifier));

//                 // It's safe to capture "this" here becasue we ensure the event is unsubscribed in CPopup's dtor.
//                 auto frameworkClosedCallback = [this]() -> HRESULT
//                 {
//                     // http://task.ms/45244384 Simplify the shutdown process of DesktopWindowXamlSource and Popups...
//                     // TODO: When the bridge unexpectedly closes we should probably do more cleanup, but this event subscription
//                     // was added late in 1.4 and we wanted to reduce risk of regression.
//                     this->m_bridgeClosed = true;
//                     return S_OK;
//                 };

//                 IFCFAILFAST(closableNotifier->add_FrameworkClosed(
//                     WRLHelper::MakeAgileCallback<mu::IClosableNotifierHandler>(frameworkClosedCallback).Get(),
//                     &m_bridgeClosedToken));
//             }

//         }
//     }

//     return S_OK;
// }

// _Check_return_ HRESULT CPopup::GetScreenOffsetFromOwner(_Out_ XPOINTF_COORDS* offset)
// {
//     ABI::Microsoft::UI::WindowId windowId;
//     IFCFAILFAST(m_desktopBridge->get_WindowId(&windowId));

//     HWND popupHwnd;
//     Windowing_GetWindowFromWindowId(windowId, &popupHwnd);

//     POINT origin = {0, 0};
//     ::ClientToScreen(popupHwnd, &origin);

//     // Get the popup owner hwnd so that we can get the windowed popup offsets in relation to
//     // its owner which we assume has the same origin as the main window.
//     // Note: Currently this does not work for getting the correct window when running as uap.
//     HWND popupOwnerHwnd = ::GetWindow(popupHwnd, GW_OWNER);
//     if (!popupOwnerHwnd)
//     {
//         IFCW32_RETURN(FALSE);
//     }

//     POINT ownerOrigin = {0, 0};
//     ::ClientToScreen(popupOwnerHwnd, &ownerOrigin);

//     // ::ClientToScreen doesn't account for the display scale. That's on the popup bridge.
//     offset->isPhysicalPixels = true;
//     offset->x = static_cast<float>(origin.x - ownerOrigin.x);
//     offset->y = static_cast<float>(origin.y - ownerOrigin.y);

//     return S_OK;
// }

// // Ensure that DComp resources are created for windowed popup
// _Check_return_ HRESULT CPopup::EnsureDCompResourcesForWindowedPopup()
// {
//     if (GetContext()->HasXamlIslands())
//     {
//         CFlyoutBase* flyoutBase = GetAssociatedFlyoutNoRef();
//         if (flyoutBase)
//         {
//             SetAssociatedIsland(flyoutBase->GetIslandContext());
//         }
//     }

//     if (m_popupWindowBridge && !m_contentIsland)
//     {
//         wrl::ComPtr<ixp::IContentIslandStatics> contentStatics;
//         IFC_RETURN(wf::GetActivationFactory(wrl::Wrappers::HStringReference(
//             RuntimeClass_Microsoft_UI_Content_ContentIsland).Get(), &contentStatics));

//         const auto& core = GetContext();
//         CWindowRenderTarget* renderTargetNoRef = core->NWGetWindowRenderTarget();
//         DCompTreeHost* dcompTreeHostNoRef = renderTargetNoRef->GetDCompTreeHost();
//         ixp::ICompositor* compositorNoRef = dcompTreeHostNoRef->GetCompositor();

//         wrl::ComPtr<ixp::IContainerVisual> containerVisual;
//         IFC_RETURN(compositorNoRef->CreateContainerVisual(&containerVisual));
//         wrl::ComPtr<ixp::IVisual> visual;
//         IFC_RETURN(containerVisual.As(&visual));
//         IFC_RETURN(contentStatics->Create(visual.Get(), &m_contentIsland));

//         IFC_RETURN(m_contentIsland->add_AutomationProviderRequested(WRLHelper::MakeAgileCallback<wf::ITypedEventHandler<
//             ixp::ContentIsland*,
//             ixp::ContentIslandAutomationProviderRequestedEventArgs*>>([&](
//                 ixp::IContentIsland* content,
//                 ixp::IContentIslandAutomationProviderRequestedEventArgs* args) -> HRESULT
//                 {
//                     return OnContentAutomationProviderRequested(content, args);
//                 }).Get(),
//                 &m_automationProviderRequestedToken));

//         m_inputSiteAdapter = std::make_unique<WindowedPopupInputSiteAdapter>();
//         CContentRoot* contentRoot = VisualTree::GetContentRootForElement(this);
//         m_inputSiteAdapter->Initialize(this, m_contentIsland.Get(), contentRoot, DirectUI::DXamlServices::GetCurrentJupiterWindow());

//         // Force an update of the input offset to make sure it is initialized to something and
//         // propogated to the inputsite adapter.
//         IFC_RETURN(UpdateTranslationFromContentRoot({}, /*forceUpdate*/ true));

//         IFC_RETURN(m_desktopBridge->Connect(m_contentIsland.Get()));
//     }

//     return S_OK;
// }

// void CPopup::EnsureContentExternalBackdropLink()
// {
//     if (!m_backdropLink)
//     {
//         DCompTreeHost* dcompTreeHostNoRef = GetDCompTreeHost();
//         ixp::ICompositor* compositorNoRef = dcompTreeHostNoRef->GetCompositor();

//         wrl::ComPtr<ixp::IContentExternalBackdropLinkStatics> backdropLinkStatics;
//         IFCFAILFAST(wf::GetActivationFactory(wrl_wrappers::HStringReference(
//             RuntimeClass_Microsoft_UI_Content_ContentExternalBackdropLink).Get(), &backdropLinkStatics));
//         IFCFAILFAST(backdropLinkStatics->Create(compositorNoRef, m_backdropLink.ReleaseAndGetAddressOf()));

//         IFCFAILFAST(m_backdropLink->put_ExternalBackdropBorderMode(ixp::CompositionBorderMode::CompositionBorderMode_Soft));

//         IFCFAILFAST(m_backdropLink->get_PlacementVisual(m_systemBackdropPlacementVisual.ReleaseAndGetAddressOf()));
//         DCompTreeHost::SetTagIfEnabled(m_systemBackdropPlacementVisual.Get(), VisualDebugTags::WindowedPopup_SystemBackdropPlacementVisual);

//         // If there's already an animation root visual, then the windowed popup is already open. Add the system backdrop
//         // placement Visual to the tree of Visuals at the root of the popup.
//         if (m_animationRootVisual)
//         {
//             wrl::ComPtr<ixp::IContainerVisual> animationCV;
//             IFCFAILFAST(m_animationRootVisual.As(&animationCV));

//             wrl::ComPtr<ixp::IVisualCollection> visualChildren;
//             IFCFAILFAST(animationCV->get_Children(&visualChildren))

//             IFCFAILFAST(visualChildren->InsertAtBottom(m_systemBackdropPlacementVisual.Get()));
//         }
//     }
// }

// void CPopup::DiscardContentExternalBackdropLink()
// {
//     if (m_backdropLink)
//     {
//         if (m_animationRootVisual)
//         {
//             wrl::ComPtr<ixp::IContainerVisual> animationCV;
//             IFCFAILFAST(m_animationRootVisual.As(&animationCV));

//             wrl::ComPtr<ixp::IVisualCollection> visualChildren;
//             IFCFAILFAST(animationCV->get_Children(&visualChildren))

//             IFCFAILFAST(visualChildren->Remove(m_systemBackdropPlacementVisual.Get()));
//         }

//         m_systemBackdropPlacementVisual.Reset();

//         m_backdropLink.Reset();
//     }
// }

// float CPopup::GetWindowedPopupRasterizationScale() const
// {
//     float rasterizationScale;
//     IFCFAILFAST(m_contentIsland->get_RasterizationScale(&rasterizationScale));
//     return rasterizationScale;
// }

// wrl::ComPtr<ixp::IPointerPoint> CPopup::GetPreviousPointerPoint()
// {
//     return m_inputSiteAdapter->GetPreviousPointerPoint();
// }

// // Release DComp resources associated with windowed popup
// void CPopup::ReleaseDCompResourcesForWindowedPopup()
// {
//     if (m_publicRootVisual)
//     {
//        m_publicRootVisual.Reset();
//     }

//     if (m_contentIsland)
//     {
//         wrl::ComPtr<ixp::IContentIsland2> contentIsland2;
//         IFCFAILFAST(m_contentIsland.As(&contentIsland2));
//         contentIsland2->put_Root(nullptr);
//     }

//     m_contentIslandRootVisual.Reset();
//     m_animationRootVisual.Reset();
//     m_windowedPopupDebugVisual.Reset();

//     m_secretTransitionTarget.reset();
//     m_entranceAnimationExpression.Reset();

//     // Don't clear out the ContentExternalBackdropLink. It's free to continue existing as long as we have a
//     // SystemBackdrop set on the popup. If the popup reopens, we'll reuse the link and reparent the placement visual. If
//     // the SystemBackdrop is removed in the meantime, we'll clear out the link then.

//     // Note: We don't close or reset the bridge here. ScrollViewers in the popup subtree have already cached the popup
//     // bridge's hwnd for DManip. We don't currently have a code path for those elements to reset the hwnd and pick up a new one.
// }

// // Show windowed popup's window
// _Check_return_ HRESULT
// CPopup::ShowWindowForWindowedPopup()
// {
//     IFCEXPECT_RETURN(m_popupWindowBridge);

//     if (m_popupWindowBridge)
//     {
//         IFC_RETURN(EnsureDCompResourcesForWindowedPopup());

//         m_desktopBridge->Show();
//     }

//     return S_OK;
// }

// // Hide windowed popup's window
// _Check_return_ HRESULT
// CPopup::HideWindowForWindowedPopup()
// {
//     if (m_popupWindowBridge)
//     {
//         m_desktopBridge->Hide();

//         // In case this popup has a cached point, closes, then opens again, we don't want it to replay that cached
//         // point. When the parent island for this popup gets a pointer message, it'll clear the cached point on all
//         // _open_ popups. Since this popup is closed, it won't be cleared, so make sure it's not caching a point while
//         // it's closed.
//         ClearLastPointerPointForReplay();

//         ReleaseDCompResourcesForWindowedPopup();
//     }

//     return S_OK; // RRETURN_REMOVAL
// }

// _Check_return_ HRESULT CPopup::Reposition()
// {
//     IFC_RETURN(PositionAndSizeWindowForWindowedPopup());

//     return S_OK;
// }

// void CPopup::ClearLastPointerPointForReplay()
// {
//     if (m_inputSiteAdapter != nullptr)
//     {
//         return m_inputSiteAdapter->ClearLastPointerPointForReplay();
//     }
// }

// bool CPopup::ReplayPointerUpdate()
// {
//     if (m_inputSiteAdapter != nullptr)
//     {
//         return m_inputSiteAdapter->ReplayPointerUpdate();
//     }

//     return false;
// }

// // Position windowed popup's window
// _Check_return_ HRESULT CPopup::PositionAndSizeWindowForWindowedPopup()
// {
//     // Ignore if not windowed
//     if (!m_windowedPopupWindow)
//     {
//         return S_OK;
//     }

//     // It's possible for Reposition to be called on a Popup after it's been closed, or no longer has a child set.  No-op in these cases as well.
//     if (!IsOpen() || m_pChild == nullptr || m_bridgeClosed)
//     {
//         return S_OK;
//     }

//     // Popup child's bounds may have changed and the popup's window needs to be
//     // sized to the new bounds.
//     InvalidateChildBounds();
//     IFC_RETURN(EnsureOuterBounds(nullptr));

//     // Get popup's bounds relative to the CoreWindow or Win32 app bounds
//     // Note: The returned popupBounds will have the plateau zoom scale applied
//     XRECTF popupBoundsPhysical = {};
//     IFC_RETURN(GetPhysicalBounds(this, &popupBoundsPhysical));

//     // Find the offset to the root in physical pixels.  This is either XAML's main CoreWindow,
//     // or the associated XamlIsland.
//     POINT rootOffsetPhysical = {};
//     if (CXamlIslandRoot* island = GetAssociatedXamlIslandNoRef())
//     {
//         // If this popup is attached to a XamlIsland, get the screen offset from the XamlIsland,
//         // it knows its own screen offset.
//         rootOffsetPhysical = island->GetScreenOffset();
//     }
//     else
//     {
//         XamlOneCoreTransforms::FailFastIfEnabled(); // Due to ClientToScreen.  12768041 tracks using island-based windowed popups in XamlOneCoreTransforms mode

//         // Get the bounds of the CoreWindow
//         const HWND hwndJupiter = static_cast<HWND>(GetContext()->GetHostSite()->GetXcpControlWindow());

//         rootOffsetPhysical = { 0, 0 };
//         ::ClientToScreen(hwndJupiter, &rootOffsetPhysical);
//     }

//     // Compute Popup window's position
//     int popupWindowLeft = 0;
//     int popupWindowTop = 0;

//     popupWindowLeft = XcpCeiling(popupBoundsPhysical.X);
//     popupWindowTop = XcpCeiling(popupBoundsPhysical.Y);

//     // Note - even if IsRightToLeft() is true, no further adjustments to popupWindowLeft are necessary. The
//     // GetPhysicalBounds() call earlier already returned the LTWH rect, so we have the location of the left edge
//     // even though the origin refers to the top-right.

//     popupWindowLeft += rootOffsetPhysical.x;
//     popupWindowTop += rootOffsetPhysical.y;

//     int popupWindowWidth = XcpCeiling(popupBoundsPhysical.Width);
//     int popupWindowHeight = XcpCeiling(popupBoundsPhysical.Height);

//     if (!ShouldPopupRendersDropShadow())
//     {
//         // Set position and size of popup's window. Increase size enough to show sub pixel positioned content.

//         m_windowedPopupMoveAndResizeRect.X = popupWindowLeft;
//         m_windowedPopupMoveAndResizeRect.Y = popupWindowTop;
//         m_windowedPopupMoveAndResizeRect.Width = popupWindowWidth;
//         m_windowedPopupMoveAndResizeRect.Height = popupWindowHeight;

//         IFC_RETURN(m_desktopBridge->MoveAndResize(m_windowedPopupMoveAndResizeRect));
//     }

//     //
//     // We need to put some more transforms in the tree to make sure the popup content renders at the correct x/y location.
//     //
//     // For a tree that looks like:
//     //  <root>
//     //      <a>
//     //          <b>
//     //              <Popup p>
//     //                  <c>
//     //                      <d />
//     //                  </c>
//     //              </Popup>
//     //          </b>
//     //      </a>
//     //  </root>
//     //
//     // Popup p's content c should render with the transform a-b-p-c. This is also the offset that should be applied to the
//     // popup's hwnd. Xaml rendering code will automatically produce a comp node (and a Composition Visual) for the popup
//     // element, with its transform p already set, and a visual for the popup content with its transform c set on it.
//     // If we just put these visuals inside the hwnd, we'll end up with the transform (a-b-p-c)-p-c. This double counts
//     // two transforms and produces the wrong result. So we apply an undo transform under the hwnd to make sure the net
//     // transform is what we want. We'll have
//     //
//     //  (a-b-p-c)-(c'-p')-(p)-(c) = a-b-p-c
//     //   ^         ^       ^   ^
//     //   |         |       |   +- transform on the Popup's child element
//     //   |         |       |
//     //   |         |       +- transform on the Popup's comp node
//     //   |         |
//     //   |         +- undo transform on the hidden root visual
//     //   |
//     //   +- applied by the hwnd
//     //
//     // where c' and p' are inverse transforms of c and p
//     //

//     bool isParentedPopup = IsActive();
//     float undoX = 0;
//     float undoY = 0;
//     if (isParentedPopup)
//     {
//         //
//         // For parented windowed popups, the undo transform is a function of the transforms on both Popup and Popup.Child.
//         // Here we manually apply only the 2D portions of these transforms.  If there are 3D transforms with depth present,
//         // the popup cannot be windowed via the checks done in MeetsRenderingRequirementsForWindowedPopup(), but we will respect
//         // the 2D portions of any 3D transforms that are present (Translation.X and Translation.Y).
//         //
//         // Note that the undo transform does not always correspond to the hwnd's position. Consider a scenario there's an
//         // RTL popup inside an LTR parent. The popup's child lines up with the popup:
//         //
//         //  o-------------------------------------+
//         //  | LTR popup parent                    |
//         //  |                                     |
//         //  |        +------------------------o   |
//         //  |        | RTL popup, popup child |   |
//         //  |        |                        |   |
//         //  |<--15-->|<--------- 50 --------->|   |
//         //  |        |                        |   |
//         //  +--------|                        |---+
//         //           |                        |
//         //           +------------------------+
//         //
//         //      "o" is the origin of each element.
//         //
//         // Here, the RTL popup has its origin at the top-right corner, but the hwnd that we create for it is positioned by
//         // its top-left corner. So even though the transform from the popup to its parent is 65px, we position the hwnd at
//         // 15. We do this using GetPhysicalBounds to transform an entire rect up the tree, then taking its top-left corner.
//         //
//         // Meanwhile, the popup child's undo transform must have -65, because that's transform applied by the popup's comp
//         // node. Here the undo transform doesn't match the hwnd position.
//         //
//         // Here, we also put an in-place flip (scaleX = -1, dX = 50) on m_contentIslandRootVisual to make its content
//         // render RTL inside the LTR hwnd (see further below as we're computing the transform for m_contentIslandRootVisual).
//         //

//         // First transform [0,0] up from Popup.Child to Popup
//         CMILMatrix4x4 childLocalTransform4x4 = m_pChild->GetLocalTransform4x4();
//         CMILMatrix childTransform2D = childLocalTransform4x4.Extract2DComponents();
//         XPOINTF topLeft = {0, 0};
//         childTransform2D.Transform(topLeft, topLeft);

//         // Next, transform the result from above from Popup to its parent
//         CMILMatrix4x4 localTransform4x4 = GetLocalTransform4x4();
//         CMILMatrix transform2D = localTransform4x4.Extract2DComponents();
//         transform2D.Transform(topLeft, topLeft);

//         undoX = -topLeft.x;
//         undoY = -topLeft.y;
//     }
//     else
//     {
//         undoX = PhysicalPixelsToDips(-popupBoundsPhysical.X);
//         undoY = PhysicalPixelsToDips(-popupBoundsPhysical.Y);
//     }

//     // Adjust for drop shadow's top and left insets
//     if (ShouldPopupRendersDropShadow())
//     {
//         XTHICKNESS insets = GetInsetsForDropShadow();
//         undoX += (isParentedPopup && IsRightToLeft()) ? -insets.left : insets.left;
//         undoY += insets.top;
//     }

//     if (m_contentIslandRootVisual)
//     {
//         // Set premultiplied "undo" transform on the window's root visual
//         CMILMatrix rootTransform(true);
//         rootTransform.AppendTranslation(undoX, undoY);

//         if (isParentedPopup && IsRightToLeft())
//         {
//             // For parented windowed popups, we need to manually apply the flip transform.
//             // In this case we can't inherit the flip transform from ancestor UIElement since windowed popups have a dedicated visual tree.
//             // This means we don't need to compare our flow direction to our parent's flow direction, instead we simply detect RTL and apply
//             // the flip transform.
//             // Note that this logic only applies to parented popups.  Unparented windowed popups apply the flip transform at Popup.Child
//             // (see CUIElement::GetShouldFlipRTL()).
//             CMILMatrix flipTransform(true);
//             flipTransform.SetM11(-1);
//             flipTransform.SetDx(PhysicalPixelsToDips(popupBoundsPhysical.Width));
//             rootTransform.Append(flipTransform);
//         }
//         wfn::Matrix4x4 rootTransform4x4;
//         rootTransform.ToMatrix4x4(&rootTransform4x4);
//         IFCFAILFAST(m_contentIslandRootVisual->put_TransformMatrix(rootTransform4x4));
//     }

//     if (m_windowedPopupDebugVisual)
//     {
//         // Update size and apply "redo" of the "undo" transform on the debug visual as well.
//         constexpr float inflate = 1000.0f; // size to inflate the visual on all sides to ensure this visual covers the full window (clipped at the edges)
//         IFCFAILFAST(m_windowedPopupDebugVisual->put_Size({ PhysicalPixelsToDips(popupBoundsPhysical.Width)+inflate*2, PhysicalPixelsToDips(popupBoundsPhysical.Height)+inflate*2 }));
//         wfn::Vector3 redo = { -undoX-inflate, -undoY-inflate, 0 };
//         IFCFAILFAST(m_windowedPopupDebugVisual->put_Offset(redo));
//     }

//     XRECTF popupWindowBounds =
//     {
//         static_cast<float>(popupWindowLeft),
//         static_cast<float>(popupWindowTop),
//         static_cast<float>(popupWindowWidth),
//         static_cast<float>(popupWindowHeight),
//     };
//     IFC_RETURN(AdjustWindowedPopupBoundsForDropShadow(&popupWindowBounds));

//     // Tell the windowed popup inputsite adapter the offset of the popup window from the content
//     // root. We do this by using the position passed to MoveAndSize (either earlier in this function
//     // or updated by AdjustWindowedPopupBoundsForDropShadow), converted to island coords in dips.
//     wf::Point transformedOffset{
//         PhysicalPixelsToDips(static_cast<float>(m_windowedPopupMoveAndResizeRect.X - rootOffsetPhysical.x)),
//         PhysicalPixelsToDips(static_cast<float>(m_windowedPopupMoveAndResizeRect.Y - rootOffsetPhysical.y))};

//     IFC_RETURN(UpdateTranslationFromContentRoot(transformedOffset, /*forceUpdate*/ false));

//     // Play the MenuPopupThemeTransition, if we have one
//     if (m_animationRootVisual && m_secretTransitionTarget && m_secretTransitionTarget->IsDirty())
//     {
//         if (m_secretTransitionTarget->m_pxf)
//         {
//             const auto& core = GetContext();
//             CWindowRenderTarget* renderTargetNoRef = core->NWGetWindowRenderTarget();
//             DCompTreeHost* dcompTreeHostNoRef = renderTargetNoRef->GetDCompTreeHost();
//             ixp::ICompositor* compositorNoRef = dcompTreeHostNoRef->GetCompositor();

//             WinRTExpressionConversionContext context(compositorNoRef);
//             m_entranceAnimationExpression = m_secretTransitionTarget->m_pxf->CreateSimpleExpression(&context, false /* use2D */);
//             m_secretTransitionTarget->m_pxf->UpdateSimpleExpression(&context, m_entranceAnimationExpression.Get());
//             wrl::ComPtr<ixp::ICompositionAnimation> ca;
//             IFCFAILFAST(m_entranceAnimationExpression.As(&ca));

//             wrl::ComPtr<WUComp::ICompositionObject> visualICO;
//             IFCFAILFAST(m_animationRootVisual.As(&visualICO));
//             IFCFAILFAST(visualICO->StartAnimation(wrl_wrappers::HStringReference(L"TransformMatrix").Get(), ca.Get()));
//         }

//         // We're skipping clip transforms because they aren't important. The windowed popup implicitly clips to window
//         // bounds, so even without any additional clipping things will look right.

//         m_secretTransitionTarget->Clean();
//     }

//     return S_OK;

//     // Note: The Shadow is drawn by HWCompTreeNodeWinRT. There's no work needed aside from the offset adjustment that's already happening.
// }

// //
// // If there is a ContentExternalBackdropLink drawing a system backdrop, and this popup has a rounded corner clip on it,
// // then we also need to put a rounded corner clip on the system backdrop Visual. Otherwise its square corners will be
// // visible behind the translucent shadows in the four corners.
// //
// // We clip the system backdrop Visual by putting a rounded corner clip on the ContentExternalOutputLink's
// // PlacementVisual. The lifted Compositor will then copy this clip over to the system backdrop system Visual.
// //
// // We use a heuristic to find the rounded corner clip - walk down the tree as long as it doesn't branch and all the
// // elements have the full size of the CPopup. Intersect all the rounded corner clips that we encounter along the way.
// // When we're done walking, apply the result on the CEBL's PlacementVisual.
// //
// void CPopup::ApplyRootRoundedCornerClipToSystemBackdrop()
// {
//     // We're doing this to put a rounded corner clip on the ContentExternalBackdropLink's PlacementVisual. If we don't
//     // have a system backdrop applied, there is no PlacementVisual, and we can skip all this.
//     if (m_systemBackdropPlacementVisual)
//     {
//         CUIElement* currentElementNoRef = m_pChild;
//         const float width = currentElementNoRef->GetActualWidth();
//         const float height = currentElementNoRef->GetActualHeight();
//         XCORNERRADIUS collectedCorners = {0, 0, 0, 0};

//         //
//         // Also look for elements with a uniform border. If we find one, we'll reduce the size of the backdrop to not
//         // overlap with the border. Xaml's MenuFlyout control templates assume no overlap between the translucent border
//         // and the desktop Acrylic backdrop. Otherwise the border will look too dark when blended with the backdrop as
//         // well as the shadow around the popup.
//         //
//         // Note that we strictly look for uniform borders to keep things simple, because it's what Xaml control
//         // templates use. If there's a custom template with borders of different sizes, we won't bother detect it or
//         // reducing the size of the backdrop. Uneven borders add another challenge - the rounded corners that we collect
//         // will need to be reduced by the thickness of the border, and we can end up with corners that have different x
//         // and y radii from uneven borders.
//         float collectedBorderThickness = 0;

//         // Walk down the popup UIElement tree, as long as it doesn't branch and the elements have the same size as the
//         // popup's direct child. Collect the corner radii along the way (if there's a radius that's bigger than what we've
//         // seen so far, then record it). Once we're done walking, apply the corner radius on the system backdrop Visual.
//         while (currentElementNoRef)
//         {
//             // We'll walk down the tree as long as the layout size of the element matches the whole size of the popup child
//             const float epsilon = 0.0001f;
//             if (DoubleUtil::AreWithinTolerance(currentElementNoRef->GetActualWidth(), width, epsilon)
//                 && DoubleUtil::AreWithinTolerance(currentElementNoRef->GetActualHeight(), height, epsilon))
//             {
//                 CFrameworkElement* frameworkElement = static_cast<CFrameworkElement*>(currentElementNoRef);
//                 XCORNERRADIUS corners = frameworkElement->GetCornerRadius();

//                 if (corners.topLeft > collectedCorners.topLeft)
//                 {
//                     collectedCorners.topLeft = corners.topLeft;
//                 }
//                 if (corners.topRight > collectedCorners.topRight)
//                 {
//                     collectedCorners.topRight = corners.topRight;
//                 }
//                 if (corners.bottomLeft > collectedCorners.bottomLeft)
//                 {
//                     collectedCorners.bottomLeft = corners.bottomLeft;
//                 }
//                 if (corners.bottomRight > collectedCorners.bottomRight)
//                 {
//                     collectedCorners.bottomRight = corners.bottomRight;
//                 }

//                 // Check that there is a brush - Xaml templates don't have any border thicknesses with a null brush
//                 if (frameworkElement->GetBorderBrush())
//                 {
//                     XTHICKNESS borderThickness = frameworkElement->GetBorderThickness();
//                     if (collectedBorderThickness == 0   // Xaml templates don't have nested border thicknesses, so only gathering 1 is enough
//                         && borderThickness.left == borderThickness.top
//                         && borderThickness.left == borderThickness.right
//                         && borderThickness.left == borderThickness.bottom)
//                     {
//                         collectedBorderThickness = borderThickness.left;
//                     }
//                 }

//                 // Continue as long as there's a single child.
//                 XUINT32 childCount;
//                 CUIElement **ppUIElements;
//                 currentElementNoRef->GetChildrenInRenderOrder(&ppUIElements, &childCount);
//                 if (childCount == 1)
//                 {
//                     currentElementNoRef = ppUIElements[0];
//                 }
//                 else
//                 {
//                     break;
//                 }
//             }
//             else
//             {
//                 break;
//             }
//         }

//         const auto& core = GetContext();
//         CWindowRenderTarget* renderTargetNoRef = core->NWGetWindowRenderTarget();
//         DCompTreeHost* dcompTreeHostNoRef = renderTargetNoRef->GetDCompTreeHost();
//         ixp::ICompositor* compositorNoRef = dcompTreeHostNoRef->GetCompositor();

//         wrl::ComPtr<ixp::ICompositor7> compositor7;
//         VERIFYHR(compositorNoRef->QueryInterface(IID_PPV_ARGS(&compositor7)));

//         wrl::ComPtr<ixp::IRectangleClip> rectangleClip;
//         IFCFAILFAST(compositor7->CreateRectangleClip(rectangleClip.ReleaseAndGetAddressOf()));

//         // The offset of the ContentExternalBackdropLink's PlacementVisual should be relative to the
//         // PopupWindowContentBridge where the PlacementVisual is hosted, but in practice it's relative to the
//         // DesktopChildContentBridge that created the windowed popup. So we get the main island's offset and subtract
//         // the difference.
//         CXamlIslandRoot* island = GetAssociatedXamlIslandNoRef();
//         POINT rootOffsetPhysical = island->GetScreenOffset();
//         XTHICKNESS insets = GetInsetsForDropShadow();
//         const float x = PhysicalPixelsToDips(static_cast<float>(m_windowedPopupMoveAndResizeRect.X - rootOffsetPhysical.x)) + insets.left;
//         const float y = PhysicalPixelsToDips(static_cast<float>(m_windowedPopupMoveAndResizeRect.Y - rootOffsetPhysical.y)) + insets.top;

//         IFCFAILFAST(rectangleClip->put_Left(collectedBorderThickness));
//         IFCFAILFAST(rectangleClip->put_Top(collectedBorderThickness));
//         IFCFAILFAST(rectangleClip->put_Right(width - collectedBorderThickness));
//         IFCFAILFAST(rectangleClip->put_Bottom(height - collectedBorderThickness));

//         // In Xaml, the corner radius is defined to be the middle of the stroke (i.e. half the border thickness extends
//         // to either side). See CGeometryBuilder::CalculateRoundedCornersRectangle. So we have to bring the rounded
//         // corner from the middle of the stroke to the inside of the stroke, which is half the stroke thickness.
//         collectedCorners.topLeft = std::max(0.0f, collectedCorners.topLeft - collectedBorderThickness/2);
//         collectedCorners.topRight = std::max(0.0f, collectedCorners.topRight - collectedBorderThickness/2);
//         collectedCorners.bottomLeft = std::max(0.0f, collectedCorners.bottomLeft - collectedBorderThickness/2);
//         collectedCorners.bottomRight = std::max(0.0f, collectedCorners.bottomRight - collectedBorderThickness/2);

//         IFCFAILFAST(rectangleClip->put_TopLeftRadius({collectedCorners.topLeft, collectedCorners.topLeft}));
//         IFCFAILFAST(rectangleClip->put_TopRightRadius({collectedCorners.topRight, collectedCorners.topRight}));
//         IFCFAILFAST(rectangleClip->put_BottomLeftRadius({collectedCorners.bottomLeft, collectedCorners.bottomLeft}));
//         IFCFAILFAST(rectangleClip->put_BottomRightRadius({collectedCorners.bottomRight, collectedCorners.bottomRight}));

//         IFCFAILFAST(m_systemBackdropPlacementVisual->put_Size({width, height}));
//         IFCFAILFAST(m_systemBackdropPlacementVisual->put_Offset({x, y}));

//         wrl::ComPtr<ixp::ICompositionClip> clip;
//         IFCFAILFAST(rectangleClip.As(&clip));
//         IFCFAILFAST(m_systemBackdropPlacementVisual->put_Clip(clip.Get()));
//     }
// }

// _Check_return_ HRESULT CPopup::GetPhysicalBounds(_In_ CUIElement* element, _Out_ XRECTF* physicalBounds)
// {
//     *physicalBounds = {};
//     CUIElement* elementForBounds = element;
//     CUIElement* elementForPosition = element;
//     float left = 0;
//     float top = 0;
//     bool useActualBounds = RuntimeFeatureBehavior::GetRuntimeEnabledFeatureDetector()->IsFeatureEnabled(RuntimeFeatureBehavior::RuntimeEnabledFeature::WindowedPopupActualBoundsMode);

//     if (auto popup = do_pointer_cast<CPopup>(element))
//     {
//         elementForBounds = popup->m_pChild;

//         if (useActualBounds)
//         {
//             elementForPosition = popup;

//             // Popup.HorizontalOffset/VerticalOffset are considered part of the Popup's position.
//             left = m_eHOffset + m_hAdjustment + m_calculatedHOffset;
//             top = m_eVOffset + m_vAdjustment + m_calculatedVOffset;
//         }
//         else
//         {
//             elementForPosition = popup->m_pChild;
//         }
//     }

//     if (useActualBounds)
//     {
//         // With the "UseActualBounds" policy we compute the bounds of the HWND according to the following:
//         // 1) The size of the HWND is based on Popup.Child's ActualWidth/Height
//         // 2) The position of the HWND is based on the position of Popup.
//         // We hope that for 19H1 this can be the policy we ship with.  However the policy is currently turned off by default
//         // as multiple controls need to change to work correctly with the policy.
//         xref_ptr<CGeneralTransform> transform;
//         IFC_RETURN(elementForPosition->TransformToVisual(nullptr, &transform));
//         IFC_RETURN(transform->TransformRect({ left, top, elementForBounds->GetActualWidth(), elementForBounds->GetActualHeight() }, physicalBounds));
//         DipsToPhysicalPixelsRect(physicalBounds);
//     }
//     else
//     {
//         // The current policy is to compute the bounds of the HWND according to the following:
//         // 1) The size of the HWND is based on Popup.Child's unclipped desired size, if set.
//         // 2) The position of the HWND is based on Popup.Child's position.
//         // This policy has caused numerous bugs and will be harder for public customers to understand.
//         // If we can convert all controls to adhere to the "UseActualBounds" policy above, we can delete this code path.
//         if (elementForBounds->HasLayoutStorage() &&
//             elementForBounds->UnclippedDesiredSize.width > 0 &&
//             elementForBounds->UnclippedDesiredSize.height > 0)
//         {
//             xref_ptr<CGeneralTransform> transform;
//             IFC_RETURN(elementForPosition->TransformToVisual(nullptr, &transform));
//             IFC_RETURN(transform->TransformRect({ 0, 0, elementForBounds->UnclippedDesiredSize.width, elementForBounds->UnclippedDesiredSize.height }, physicalBounds));
//             DipsToPhysicalPixelsRect(physicalBounds);
//         }
//         else
//         {
//             XRECTF_RB physicalBoundsRb = {};
//             IFC_RETURN(elementForBounds->GetGlobalBounds(&physicalBoundsRb, TRUE /* ignoreClipping */));
//             *physicalBounds = ToXRectF(physicalBoundsRb);
//             IFC_RETURN(elementForBounds->AdjustBoundingRectToRoot(physicalBounds));
//         }
//     }

//     // In addition to the contents in the windowed popup, also union in all the non-windowed popups that are nested
//     // inside this windowed popup's. They must live in the same island as the windowed popup so that they won't be
//     // covered by the windowed popup, and their bounds weren't included when we bounded the popup. See
//     // CUIElement::GenerateChildOuterBounds - we explicitly exclude popups from bounds calculations.
//     CPopupRoot* popupRoot = NULL;
//     IFC_RETURN(GetContext()->GetAdjustedPopupRootForElement(this, &popupRoot));
//     FAIL_FAST_ASSERT(popupRoot);
//     FAIL_FAST_ASSERT(popupRoot->HasOpenOrUnloadingPopups());
//     std::vector<CPopup*> openPopups = popupRoot->GetOpenPopupList(true /* includeUnloadingPopups */);
//     for (CPopup* popup : openPopups)
//     {
//         // We're looking for a non-windowed pop nested directly inside this windowed popup. If there are multiple
//         // layers of windowed popups nested inside each other, then the non-windowed popup should live in the
//         // innermost windowed popup's island.
//         if (!popup->IsWindowed())
//         {
//             CPopup* ancestorWindowedPopup = popup->GetFirstAncestorPopup(true /* windowedOnly */);
//             if (ancestorWindowedPopup == this)
//             {
//                 XRECTF_RB physicalBoundsRb = {};
//                 IFC_RETURN(popup->GetGlobalBounds(&physicalBoundsRb, TRUE /* ignoreClipping */));
//                 XRECTF popupBounds = ToXRectF(physicalBoundsRb);
//                 IFC_RETURN(popup->AdjustBoundingRectToRoot(&popupBounds));
//                 UnionRectF(physicalBounds, &popupBounds);
//             }
//         }
//     }

//     return S_OK;
// }

// // Note: The inflated region will eat clicks on the shadow, rather than have the clicks go through and land on the
// // stuff (e.g. Xaml content in the main tree or content in other windows) under it.
// _Check_return_ HRESULT CPopup::AdjustWindowedPopupBoundsForDropShadow(_In_ const XRECTF* popupWindowBounds)
// {
//     ASSERT(!m_bridgeClosed);
//     if (ShouldPopupRendersDropShadow())
//     {
//         XRECTF expandedBounds = *popupWindowBounds;
//         XTHICKNESS insets = GetInsetsForDropShadow();
//         XFLOAT scale = GetEffectiveRootScale();

//         expandedBounds.X -= (IsRightToLeft() ? insets.right : insets.left) * scale;
//         expandedBounds.Y -= insets.top * scale;
//         expandedBounds.Width += (insets.left + insets.right) * scale;
//         expandedBounds.Height += (insets.bottom + insets.top) * scale;

//         m_windowedPopupMoveAndResizeRect.X = XcpFloor(expandedBounds.X);
//         m_windowedPopupMoveAndResizeRect.Y = XcpFloor(expandedBounds.Y);
//         m_windowedPopupMoveAndResizeRect.Width = XcpCeiling(expandedBounds.Width);
//         m_windowedPopupMoveAndResizeRect.Height = XcpCeiling(expandedBounds.Height);

//         IFC_RETURN(m_desktopBridge->MoveAndResize(m_windowedPopupMoveAndResizeRect));
//     }

//     return S_OK;
// }

// bool CPopup::ShouldPopupRendersDropShadow() const
// {
//     return CThemeShadow::IsDropShadowMode();
// }

// XTHICKNESS CPopup::GetInsetsForDropShadow()
// {
//     XTHICKNESS shadowInsets = CThemeShadow::GetInsetsForWindowedPopup(
//         IsToolTip() ? CThemeShadow::DropShadowDepthClass::Small
//         : CThemeShadow::DropShadowDepthClass::Medium);

//     //
//     // Round the insets according to the root scale. We do this for a couple of reasons:
//     //
//     //   1. These insets get multiplied by the root scale in AdjustWindowedPopupBoundsForDropShadow.
//     //      Rounding them here allows us to calculate integer offsets for the hwnd.
//     //
//     //   2. These insets also factor into the offsets that we set on the hwnd root visual at the end of
//     //      PositionAndSizeWindowForWindowedPopup. Those offsets also get multiplied by the root scale,
//     //      and we want integers at the end. Otherwise the content in the windowed popup get placed at
//     //      a fractional offset, which produces blurry content.
//     //
//     // We use ceiling to make sure we don't shrink the gutters and clip the shadow.
//     //
//     XFLOAT scale = GetEffectiveRootScale();
//     if (scale != 1.0)
//     {
//         shadowInsets.top = XcpCeiling(shadowInsets.top * scale) / scale;
//         shadowInsets.bottom = XcpCeiling(shadowInsets.bottom * scale) / scale;
//         shadowInsets.left = XcpCeiling(shadowInsets.left * scale) / scale;
//         shadowInsets.right = XcpCeiling(shadowInsets.right * scale) / scale;
//     }

//     return shadowInsets;
// }

// static bool IsElementScaleOrTranslationOnly(_In_ CUIElement* uielement)
// {
//     CMILMatrix4x4 localTransform4x4 = uielement->GetLocalTransform4x4();
//     return localTransform4x4.IsScaleOrTranslationOnly();
// }

// bool CPopup::MeetsRenderingRequirementsForWindowedPopup()
// {
//     // Currently, popups can only be windowed if the bounds for the HWND are of the "simple, rectangular" form.
//     // This means only scales/translates for everything from Popup.Child up to the root.
//     bool isParentedPopup = IsActive();
//     if (isParentedPopup)
//     {
//         // First, check Popup.Child for scales/translates.
//         // Note importantly that at the time this function is called, Popup.Child isn't parented to PopupRoot yet,
//         // so we must not rely on GetUIElementAdjustedParentInternal() to hop from Popup.Child to Popup.
//         // Hence, we check Popup.Child explicitly here, outside the loop below.
//         if (m_pChild != nullptr && !IsElementScaleOrTranslationOnly(m_pChild))
//         {
//             return false;
//         }

//         // Next, check Popup and its ancestors.
//         for (CUIElement* current = this;
//              current != nullptr;
//              current = current->GetUIElementAdjustedParentInternal(FALSE))
//         {
//             if (!IsElementScaleOrTranslationOnly(current))
//             {
//                 return false;
//             }
//         }
//     }

//     return true;
// }

// ixp::IVisual* CPopup::GetWindowRootVisual_TestHook()
// {
//     return m_contentIslandRootVisual.Get();
// }

// //
// // Set DComp visual as windowed popup's window content. Called as part of the render walk.
// //
// _Check_return_ HRESULT CPopup::SetRootVisualForWindowedPopupWindow(_In_ ixp::IVisual* popupRootDCompVisual)
// {
//     IFC_RETURN(EnsureDCompResourcesForWindowedPopup());

//     EnsureWindowedPopupRootVisualTree();

//     // When the PopupRoot loses a comp node (e.g. because it had the last XamlLight detached from it), we'll reparent
//     // the comp nodes for all open popups. If an open windowed popup gets reparented, we'll call back here to make sure
//     // the root visual is still attached. There's nothing to do if it still is.
//     if (!m_publicRootVisual)
//     {
//         // We should always be setting something here. Nulling out the root visual is done by ReleaseDCompResourcesForWindowedPopup.
//         ASSERT(popupRootDCompVisual);

//         // Store popup's root visual to handle popup offset changes
//         m_publicRootVisual = popupRootDCompVisual;
//         DCompTreeHost::SetTagIfEnabled(m_publicRootVisual.Get(), VisualDebugTags::WindowedPopup_PublicRootVisual);

//         wrl::ComPtr<ixp::IContainerVisual> containerVisual;
//         IFCFAILFAST(m_animationRootVisual.As(&containerVisual))

//         wrl::ComPtr<ixp::IVisualCollection> visualChildren;
//         IFCFAILFAST(containerVisual->get_Children(&visualChildren))
//         IFCFAILFAST(visualChildren->InsertAtTop(m_publicRootVisual.Get()));

//         // Position & size window
//         IFC_RETURN(PositionAndSizeWindowForWindowedPopup());

//         // If we have a system backdrop, update the rounded corner clip. Note that to be completely correct, we need to
//         // update the rounded corner clip whenever something under this Popup is dirtied for rendering, because rounded
//         // corner clips in the subtree can change after the popup opens. In practice none of our controls do that, so we
//         // do the simple thing and update the clip once when the public root is assigned.
//         ApplyRootRoundedCornerClipToSystemBackdrop();
//     }
//     else
//     {
//         // The windowed popup visual should never change to a different object once assigned. It'll get released when
//         // the windowed popup closes, then get reassigned from null once it reopens.
//         ASSERT(m_publicRootVisual.Get() == popupRootDCompVisual);
//     }

//     return S_OK;
// }

// // Used for inline popups nested inside windowed popups
// void CPopup::AddAdditionalVisualForWindowedPopupWindow(_In_ ixp::IVisual* popupVisual)
// {
//     EnsureWindowedPopupRootVisualTree();

//     wrl::ComPtr<ixp::IContainerVisual> containerVisual;
//     IFCFAILFAST(m_animationRootVisual.As(&containerVisual));

//     wrl::ComPtr<ixp::IVisualCollection> visualChildren;
//     IFCFAILFAST(containerVisual->get_Children(&visualChildren));
//     IFCFAILFAST(visualChildren->InsertAtTop(popupVisual));
// }

// // Used for inline popups nested inside windowed popups
// void CPopup::RemoveAdditionalVisualForWindowedPopupWindow(_In_ ixp::IVisual* popupVisual)
// {
//     if (m_animationRootVisual)
//     {
//         wrl::ComPtr<ixp::IContainerVisual> containerVisual;
//         IFCFAILFAST(m_animationRootVisual.As(&containerVisual));

//         wrl::ComPtr<ixp::IVisualCollection> visualChildren;
//         IFCFAILFAST(containerVisual->get_Children(&visualChildren));
//         IFCFAILFAST(visualChildren->Remove(popupVisual));
//     }
// }

// //
// // Builds the tree of Visuals at the root of the windowed popup tree. These Visuals don't correspond to any UIElements.
// // The tree structure is:
// //
// //  <!-- This gets set as the ContentIsland.Root -->
// //  <ContentIslandRoot>
// //
// //      <!-- This is an optional Visual that puts a transparent green layer to help see the bounds of the island/popup
// //           window. -->
// //      <DebugVisual />
// //
// //      <!-- This Visual holds entrance animations, so that they'll apply to the backdrop, content, and shadow. -->
// //      <AnimationRoot>
// //
// //          <!-- This Visual is the ContentExternalBackdropLink's PlacementVisual. Its position/size/clip are copied to
// //               the system Visual displaying the actual backdrop by the lifted Compositor. -->
// //          <SystemBackdropPlacementVisual />
// //
// //          <!-- This is a Visual corresponding to the Popup.Child UIElement. It's the prepend visual for its comp node. -->
// //          <PublicRootVisual>
// //
// //              <!-- Note that the shadow for the CPopup lives under the UIElement visual.
// //                   See HWCompTreeNodeWinRT::EnsureDropShadowVisual. -->
// //              <Shadow />
// //
// //              ...
// //
// //          </PublicRootVisual>
// //
// //      </AnimationRoot>
// //
// //  <ContentIslandRoot>
// //
// void CPopup::EnsureWindowedPopupRootVisualTree()
// {
//     if (!m_contentIslandRootVisual)
//     {
//         const auto& core = GetContext();
//         CWindowRenderTarget* renderTargetNoRef = core->NWGetWindowRenderTarget();
//         DCompTreeHost* dcompTreeHostNoRef = renderTargetNoRef->GetDCompTreeHost();
//         ixp::ICompositor* compositorNoRef = dcompTreeHostNoRef->GetCompositor();

//         wrl::ComPtr<ixp::IContainerVisual> windowCV;
//         wrl::ComPtr<ixp::IVisual> windowV;
//         IFCFAILFAST(compositorNoRef->CreateContainerVisual(windowCV.ReleaseAndGetAddressOf()));
//         IFCFAILFAST(windowCV.As(&windowV));
//         m_contentIslandRootVisual = windowV;
//         DCompTreeHost::SetTagIfEnabled(m_contentIslandRootVisual.Get(), VisualDebugTags::WindowedPopup_ContentIslandRootVisual);

//         wrl::ComPtr<ixp::IContainerVisual> animationCV;
//         wrl::ComPtr<ixp::IVisual> animationV;
//         IFCFAILFAST(compositorNoRef->CreateContainerVisual(animationCV.ReleaseAndGetAddressOf()));
//         IFCFAILFAST(animationCV.As(&animationV));
//         m_animationRootVisual = animationV;
//         DCompTreeHost::SetTagIfEnabled(m_animationRootVisual.Get(), VisualDebugTags::WindowedPopup_AnimationRootVisual);

//         // If we have a system backdrop placement visual, then a system backdrop was already set. Parent the placement
//         // visual in the tree of Visuals at the root of the tree now. We weren't able to parent it when the backdrop
//         // link was created because the tree of Visuals at the root didn't exist yet.
//         if (m_systemBackdropPlacementVisual)
//         {
//             wrl::ComPtr<ixp::IVisualCollection> animationVisualChildren;
//             IFCFAILFAST(animationCV->get_Children(&animationVisualChildren))
//             IFCFAILFAST(animationVisualChildren->InsertAtBottom(m_systemBackdropPlacementVisual.Get()));
//         }

//         wrl::ComPtr<ixp::IVisualCollection> visualChildren;
//         IFCFAILFAST(windowCV->get_Children(visualChildren.ReleaseAndGetAddressOf()));
//         IFCFAILFAST(visualChildren->InsertAtTop(animationV.Get()));

//         if (RuntimeFeatureBehavior::GetRuntimeEnabledFeatureDetector()->IsFeatureEnabled(RuntimeFeatureBehavior::RuntimeEnabledFeature::EnableWindowedPopupDebugVisual))
//         {
//             // Create a "debug" visual for windowed popups.  This visual will be sized to the size of the HWND and will draw
//             // a semi-transparent green color on top of all the content inside the Popup.
//             // This debug visual allows us to see exactly where the HWND is on screen and is helpful in debugging cases where
//             // either the HWND bounds or the "undo transform" was not computed correctly.
//             wrl::ComPtr<ixp::ICompositionColorBrush> debugColor_cb;
//             IFCFAILFAST(compositorNoRef->CreateColorBrush(&debugColor_cb));
//             IFCFAILFAST(debugColor_cb->put_Color(ColorUtils::GetWUColor(0x8800ff00)));

//             wrl::ComPtr<ixp::ICompositionBrush> debugColor_b;
//             IFCFAILFAST(debugColor_cb.As(&debugColor_b));

//             wrl::ComPtr<ixp::ISpriteVisual> debugVisual;
//             IFCFAILFAST(compositorNoRef->CreateSpriteVisual(&debugVisual));
//             IFCFAILFAST(debugVisual->put_Brush(debugColor_b.Get()));
//             m_windowedPopupDebugVisual.Reset();
//             IFCFAILFAST(debugVisual.As(&m_windowedPopupDebugVisual));
//             DCompTreeHost::SetTagIfEnabled(m_windowedPopupDebugVisual.Get(), VisualDebugTags::WindowedPopup_DebugVisual);

//             IFCFAILFAST(windowCV->get_Children(&visualChildren))
//             visualChildren->InsertAtBottom(m_windowedPopupDebugVisual.Get());
//         }

//         wrl::ComPtr<ixp::IContentIsland2> contentIsland2;
//         IFCFAILFAST(m_contentIsland.As(&contentIsland2));
//         contentIsland2->put_Root(m_contentIslandRootVisual.Get());
//     }
// }

// void CPopup::EnsureUIAWindow()
// {
//     if (m_spUIAWindow == nullptr && m_windowedPopupWindow != nullptr)
//     {
//         auto core = GetContext();

//         UIAHostEnvironmentInfo uiaInfo;
//         if (CXamlIslandRoot* island = GetAssociatedXamlIslandNoRef())
//         {
//             uiaInfo = UIAHostEnvironmentInfo(island);
//         }
//         else
//         {
//             // Use Jupiter window here because all elements are positioned relative to it for hit testing
//             uiaInfo = UIAHostEnvironmentInfo(m_windowedPopupWindow, static_cast<HWND>(core->GetHostSite()->GetXcpControlWindow()));
//         }

//         IFCFAILFAST(CUIAHostWindow::Create(
//             uiaInfo,
//             core->GetHostSite(),
//             this,
//             m_spUIAWindow.ReleaseAndGetAddressOf()));
//     }
// }

// // If popup is windowed, return popup's window. Else, return Jupiter window.
// HWND CPopup::GetPopupWindow()
// {
//     // Note: We can't assume that the popup hwnd exists. We won't create one of someone opens a windowed popup
//     // after Xaml has shut down and has destroyed the main hwnd.
//     if (IsWindowed() && IsOpen() && m_windowedPopupWindow)
//     {
//         return m_windowedPopupWindow;
//     }
//     return static_cast<HWND>(GetContext()->GetHostSite()->GetXcpControlWindow());
// }

// // Converts between logical and physical pixels
// float CPopup::GetEffectiveRootScale()
// {
//     const auto scale = RootScale::GetRasterizationScaleForElement(this);
//     return scale;
// }

// // Convert DIPs to Physical Pixels
// float CPopup::DipsToPhysicalPixels(_In_ float dips)
// {
//     return dips * GetEffectiveRootScale();
// }

// void CPopup::DipsToPhysicalPixelsRect(_Inout_ XRECTF* rect)
// {
//     rect->X = DipsToPhysicalPixels(rect->X);
//     rect->Y = DipsToPhysicalPixels(rect->Y);
//     rect->Width = DipsToPhysicalPixels(rect->Width);
//     rect->Height = DipsToPhysicalPixels(rect->Height);
// }

// // Convert Physical Pixels to DIPs
// float CPopup::PhysicalPixelsToDips(_In_ float physicalPixels)
// {
//     return physicalPixels / GetEffectiveRootScale();
// }

// //------------------------------------------------------------------------
// //
// //  Synopsis:
// //      Return the popup root associated with this popup.
// //
// //------------------------------------------------------------------------
// CPopupRoot* CPopup::GetAssociatedPopupRootNoRef()
// {
//     CXamlIslandRoot* islandRoot = GetAssociatedXamlIslandNoRef();
//     if (islandRoot != nullptr)
//     {
//         return islandRoot->GetPopupRoot();
//     }
//     else
//     {
//         const CRootVisual *pRootVisual = VisualTree::GetRootForElement(this);
//         if (NULL != pRootVisual)
//         {
//             return pRootVisual->GetAssociatedPopupRootNoRef();
//         }
//         else
//         {
//             return GetContext()->GetMainPopupRoot();
//         }
//     }
// }

// CXamlIslandRoot* CPopup::GetAssociatedXamlIslandNoRef()
// {
//     if (auto visualTree = VisualTree::GetForElementNoRef(this))
//     {
//         return do_pointer_cast<CXamlIslandRoot>(visualTree->GetXamlIslandRootForElement(this));
//     }

//     return nullptr;
// }

// CDependencyObject* CPopup::GetToolTipOwner()
// {
//     if(!IsToolTip()) return nullptr;

//     return m_toolTipOwnerWeakRef.lock_noref();
// }

// //-------------------------------------------------------------------------
// //
// //  Synopsis:   The property method for Popup.Child
// //
// //-------------------------------------------------------------------------
// _Check_return_ HRESULT
// CPopup::Child(
//     _In_ CDependencyObject* pObject,
//     _In_ XUINT32 cArgs,
//     _Inout_updates_(cArgs) CValue* pArgs,
//     _In_opt_ IInspectable* pValueOuter,
//     _Out_ CValue* pResult)
// {
//     CPopup* pThis = NULL;
//     CUIElement* pNewChild = NULL;

//     IFC_RETURN(DoPointerCast(pThis, pObject));
//     IFC_RETURN(pThis && cArgs <= 1 ? S_OK : E_INVALIDARG);

//     if (cArgs == 0)
//     {
//         IFCEXPECT_RETURN(pResult);

//         pResult->SetObjectAddRef(pThis->m_pChild);
//     }
//     else
//     {
//         IFC_RETURN((pArgs->GetType() == valueObject || pArgs->GetType() == valueNull) ? S_OK : E_INVALIDARG);

//         if(pArgs->AsObject())
//         {
//             pNewChild = do_pointer_cast<CUIElement>(pArgs->AsObject());
//             IFC_RETURN(pNewChild ? S_OK: E_INVALIDARG);
//         }

//         if (pThis->m_pChildTransitions)
//         {
//             IFC_RETURN(CUIElement::EnsureLayoutTransitionStorage(pNewChild, NULL, TRUE));
//         }

//         IFC_RETURN(pThis->SetChild(pNewChild));
//     }

//     return S_OK;
// }

// //------------------------------------------------------------------------
// //
// //  Synopsis:
// //    Adds the popup's child to the visual and logical trees and enters it in the
// //    right namescope
// //
// //------------------------------------------------------------------------
// _Check_return_ HRESULT CPopup::SetChild(
//     _In_ CUIElement* pChild
//     )
// {
//     HRESULT hr = S_OK;
//     CPopupRoot *pPopupRoot = NULL;
//     bool fPegged = false;
//     CValue valueChild;

//     IFC(RemoveChild());

//     if (pChild)
//     {
//         if(pChild->IsAssociated())
//         {
//             IFC(SetAndOriginateError(E_NER_INVALID_OPERATION, ManagedError, AG_E_MANAGED_ELEMENT_ASSOCIATED));
//         }
//         pChild->SetAssociated(true, this);

//         // In this case we have a very specific scenario.
//         // We have a Popup with ChildTransitions that react to an Unload trigger.
//         // We have a non-null child and the Popup is opened.
//         // Then we close the Popup and the transition starts playing.
//         // While it plays, we set the child of the Popup to null and then we reset it to the same non-null child
//         // we had before (for reference, ContentDialog follows this code path when hidden and re-shown).
//         // If CancelTransitions hasn't been called yet (e.g. OnTransitionCompleted hasn't fired before this happens),
//         // the new child will still be an unloading element, so when CancelTransitions does get called,
//         // RemoveUnloadedElement will read the UC_RemoveLogicalParent flag that was set in CPopup::RemoveChild
//         // and unhook the logical parent from this child, even though in reality it just got loaded back into the visual tree,
//         // so the logical parent-child relationship is now in a bad state.
//         // To prevent this from happening, we clear the UC_RemoveLogicalParent flag when the logical parent
//         // of the new child is this popup already, meaning the logical parent-child relationship shouldn't be broken.
//         if (pChild->GetLogicalParentNoRef() == this)
//         {
//             ClearUCRemoveLogicalParentFlag(pChild);
//         }
//     }

//     // Set the property value
//     ASSERT(m_pChild == NULL);
//     {
//         // m_pChild is walked for GC, so take GC lock before it is changed
//         AutoReentrantReferenceLock lock(DXamlServices::GetPeerTableHost());

//         // Bug 43647291: [Watson Failure] caused by NULL_CLASS_PTR_READ_c0000005_Microsoft.UI.Xaml.dll!CPopup::Close
//         // We have crashes where m_pChild is null when CPopup::Close goes to get the child's automation peer. Except
//         // CPopup::Close uses m_pChild earlier to call SetAssociated on it, and we didn't crash then, which suggests
//         // that something in the Close call is nulling out m_pChild between the two times it's referenced. This
//         // failfast will help us catch the place that's doing that.
//         if (m_isClosing && !pChild)
//         {
//             IFCFAILFAST(E_UNEXPECTED);
//         }

//         m_pChild = pChild;
//     }
//     AddRefInterface(m_pChild);

//     if (m_pChild)
//     {
//         EnterParams enterParams(
//             /*isLive*/                FALSE, //this is just for name registration
//             /*skipNameRegistration*/  FALSE,
//             /*coercedIsEnabled*/      GetCoercedIsEnabled(),
//             /*useLayoutRounding*/     EnterParams::UseLayoutRoundingDefault,
//             /*visualTree*/            VisualTree::GetForElementNoRef(this, LookupOptions::NoFallback)
//         );

//         if (GetStandardNameScopeOwner() == NULL)
//         {
//             if (m_fIsOpen)
//             {
//                 //if popup does not have a namescope owner, then make the child its own
//                 //namescope owner. this ensures the name resolution works within the popup tree.
//                 IFC(AddChildAsNamescopeOwner());
//             }
//         }
//         else
//         {
//             //if popup has a namescope owner, then the child should be part of that namescope
//             IFC(m_pChild->Enter(GetStandardNameScopeOwner(), enterParams));
//         }

//         //add to the logical tree
//         IFC(AddLogicalChild(m_pChild));

//         if (m_fIsOpen)
//         {
//             IFC(GetContext()->GetAdjustedPopupRootForElement(this, &pPopupRoot));

//             // Bug 74739: Detect nested popup in a template scenario. This popup is being created
//             // as part of template expansion and the template parent is in a
//             // popup. Defer opening this popup till currently open popups have been measured.
//             // Attempt to open it here will fail; popup root children collection is locked for layout.
//             if (IsParsing() && pPopupRoot && pPopupRoot->GetIsOnMeasureStack())
//             {
//                 IFC(pPopupRoot->AddToDeferredOpenPopupList(this));
//             }
//             else
//             {
//                 IFC(Open());
//             }
//         }

//         CUIElement::NWSetContentAndBoundsDirty(this, DirtyFlags::None);

//         // Create the managed peer for child immediately and hold it until complete the
//         // processing SetPeerReferenceToProperty that ensure hold the managed peer.
//         IFC(m_pChild->EnsurePeerAndTryPeg(&fPegged));

//         // Note:  DropShadow mode relies on this code to create a CompNode so Popup.Child can cast Popup's shadow
//         IFC(m_pChild->SetRequiresComposition(CompositionRequirement::ProjectedShadowDefaultReceiver, IndependentAnimationType::None));
//     }

//     // Maintain a managed ref on the child to prevent it from being GC'd when the popup is not open.
//     valueChild.WrapObjectNoRef(m_pChild);
//     IFC(SetPeerReferenceToProperty(GetPropertyByIndexInline(KnownPropertyIndex::Popup_Child), valueChild));

//     // If this popup is open, then setting a child would have attached the child to the CPopupRoot's children collection,
//     // which should have updated the 3D depth flags under the CPopupRoot. We also need to update the 3D depth flags in
//     // the main tree.
//     UpdateHas3DDepthInSubtree();

// Cleanup:
//     if (fPegged)
//     {
//         ASSERT(m_pChild);
//         m_pChild->UnpegManagedPeer();
//     }
//     RRETURN(hr);
// }

// //------------------------------------------------------------------------
// //
// //  Synopsis:
// //    Removes the popup's child from the visual and logical trees and does namescope
// //    changes
// //
// //------------------------------------------------------------------------
// _Check_return_ HRESULT
// CPopup::RemoveChild()
// {
//     CPopupRoot *pPopupRoot = NULL;
//     CUIElementCollection* pPopupRootChildrenCollection = NULL;

//     if (m_pChild != nullptr)
//     {
//         if (m_pChild->RequiresComposition())
//         {
//             m_pChild->UnsetRequiresComposition(CompositionRequirement::ProjectedShadowDefaultReceiver, IndependentAnimationType::None);
//         }

//         if (ShouldRemoveChildAsNamescopeOwner())
//         {
//             IFC_RETURN(RemoveChildAsNamescopeOwner());
//         }
//         else
//         {
//             LeaveParams leaveParams;
//             leaveParams.fIsLive= FALSE; //non-live leave as we are only changing the namescope owner here
//             leaveParams.fSkipNameRegistration = FALSE;
//             leaveParams.fCoercedIsEnabled = GetCoercedIsEnabled();
//             leaveParams.fVisualTreeBeingReset = FALSE;
//             IFC_RETURN(m_pChild->Leave(m_pChild->GetStandardNameScopeOwner(), leaveParams));
//         }

//         // notice how we FIRST close and only then remove logical child.
//         // this allows us to use the logical parent as a lookup for transitions.
//         if (m_fIsOpen)
//         {
//             // We're currently removing the child, and the popup shouldn't raise a UIE.Hidden event, so don't call
//             // RequestKeepAlive expecting a UIE.Hidden to be raised.
//             ImplicitCloseGuard guard(this);
//             IFC_RETURN(Close());
//         }

//         IFC_RETURN(GetContext()->GetAdjustedPopupRootForElement(this, &pPopupRoot));

//         if (pPopupRoot)
//         {
//             pPopupRootChildrenCollection = static_cast<CUIElementCollection*>(pPopupRoot->GetChildren());
//         }

//         bool keepChildVisible = m_fIsOpen && m_pChild->ComputeKeepVisible();

//         if (keepChildVisible)
//         {
//             m_unloadingChild = m_pChild;
//             m_pChild->SetKeepVisible(true);
//             // No need to call RequestKeepAliveForHiddenEventHandlersInSubtree on the child. If this popup is open, then
//             // Close() would have removed the child from the popup root's child collection, which would have called
//             // RequestKeepAliveForHiddenEventHandlersInSubtree on it.
//         }

//         if (pPopupRootChildrenCollection && pPopupRootChildrenCollection->IsUnloadingElement(m_pChild))
//         {
//             // need to defer this
//             UnloadCleanup* pRemoveLogicToExecute = pPopupRootChildrenCollection->m_pUnloadingStorage->Get(m_pChild);

//             *pRemoveLogicToExecute = (UnloadCleanup) (*pRemoveLogicToExecute | UC_RemoveLogicalParent);
//             m_pChild->AddRef();    // take a ref, which will be released on the unload action of the child
//         }
//         else if (!keepChildVisible)
//         {
//             // If the child is kept visible, then we'll take care of its logical parent in FlushPendingKeepVisibleOperations.
//             RemoveLogicalChild(m_pChild);
//         }

//         // If we're keeping the old child visible, leave it associated with this popup. If the child gets added anywhere
//         // else, it will encounter an association failure and CUIElement::OnAssociationFailure will know to unset it as
//         // this popup's unloading child first. Otherwise it will unassociate in its own CUIElement::FlushPendingKeepVisibleOperations.
//         if (!keepChildVisible)
//         {
//             m_pChild->SetAssociated(false, nullptr);
//         }

//         //reset the property value reference
//         CUIElement* pChild = m_pChild;
//         {
//             // m_pChild is walked for GC, so take GC lock before it is changed
//             AutoReentrantReferenceLock lock(DXamlServices::GetPeerTableHost());

//             // Bug 43647291: [Watson Failure] caused by NULL_CLASS_PTR_READ_c0000005_Microsoft.UI.Xaml.dll!CPopup::Close
//             // We have crashes where m_pChild is null when CPopup::Close goes to get the child's automation peer. Except
//             // CPopup::Close uses m_pChild earlier to call SetAssociated on it, and we didn't crash then, which suggests
//             // that something in the Close call is nulling out m_pChild between the two times it's referenced. This
//             // failfast will help us catch the place that's doing that.
//             if (m_isClosing)
//             {
//                 IFCFAILFAST(E_UNEXPECTED);
//             }

//             m_pChild = nullptr;
//         }
//         ReleaseInterface(pChild);

//         // If this popup was open, then removing the child closed it and removed the child from the CPopupRoot's
//         // children collection, which should have updated the 3D depth flags under the CPopupRoot. We also need
//         // to update the 3D depth flags in the main tree.
//         UpdateHas3DDepthInSubtree();

//         CUIElement::NWSetContentAndBoundsDirty(this, DirtyFlags::None);
//     }

//     return S_OK;
// }

// //------------------------------------------------------------------------
// //
// //  Synopsis:
// //    Clears the UC_RemoveLogicalParent flag so that the specified element
// //    does not get unhooked from its logical parent during Unload Cleanup.
// //
// //------------------------------------------------------------------------
// _Check_return_ HRESULT
// CPopup::ClearUCRemoveLogicalParentFlag(
//     _In_ CUIElement* pChild
//     )
// {
//     CPopupRoot *pPopupRoot = NULL;
//     CUIElementCollection* pChildrenCollection = NULL;

//     IFC_RETURN(GetContext()->GetAdjustedPopupRootForElement(this, &pPopupRoot));

//     if (pPopupRoot)
//     {
//         pChildrenCollection = static_cast<CUIElementCollection*>(pPopupRoot->GetChildren());
//     }

//     if (pChildrenCollection && pChildrenCollection->IsUnloadingElement(pChild))
//     {
//         // Remove any deferred UC_RemoveLogicalParent logic.
//         UnloadCleanup* pRemoveLogicToExecute = pChildrenCollection->m_pUnloadingStorage->Get(pChild);

//         if (*pRemoveLogicToExecute & UC_RemoveLogicalParent)
//         {
//             *pRemoveLogicToExecute = (UnloadCleanup) (*pRemoveLogicToExecute & ~UC_RemoveLogicalParent);

//             // Release the reference we take when we subscribe for UC_RemoveLogicalParent.
//             ReleaseInterfaceNoNULL(pChild);
//         }
//     }

//     return S_OK;
// }

// //------------------------------------------------------------------------
// //
// //  Synopsis:
// //  Update the translation that should be applied to windowed popup input
// //  so that it can be applied in the coordinate space of the popup's
// //  content root.
// //
// //------------------------------------------------------------------------
// _Check_return_ HRESULT
// CPopup::UpdateTranslationFromContentRoot(const wf::Point& offset, bool forceUpdate)
// {
//     IFCEXPECT_RETURN(m_inputSiteAdapter);

//     if (m_offsetFromMainWindow.X != offset.X ||
//         m_offsetFromMainWindow.Y != offset.Y ||
//         forceUpdate)
//     {
//         m_offsetFromMainWindow.X = offset.X;
//         m_offsetFromMainWindow.Y = offset.Y;

//         auto dxamlCore = DXamlCore::GetCurrent();
//         auto coreServices = dxamlCore->GetHandle();
//         CREATEPARAMETERS cp(coreServices);

//         xref_ptr<CGeneralTransform> generalTransform;
//         CMILMatrix matTransform(/*fInitialize*/ true);
//         xref_ptr<CMatrix> matrix;
//         xref_ptr<CMatrixTransform> matrixTransform;

//         IFC_RETURN(CMatrixTransform::Create(reinterpret_cast<CDependencyObject**>(matrixTransform.ReleaseAndGetAddressOf()), &cp));
//         IFC_RETURN(CMatrix::Create(reinterpret_cast<CDependencyObject**>(matrix.ReleaseAndGetAddressOf()), &cp));

//         IFC_RETURN(matrixTransform->SetValueByKnownIndex(KnownPropertyIndex::MatrixTransform_Matrix, matrix.get()));

//         matrix->m_matrix = matTransform;
//         generalTransform = std::move(matrixTransform);

//         ctl::ComPtr<DirectUI::DependencyObject> spTransformPeer;
//         IFC_RETURN(dxamlCore->GetPeer(generalTransform.get(), &spTransformPeer));
//         ctl::ComPtr<xaml_media::IGeneralTransform> spTransform;
//         IFC_RETURN(spTransformPeer.As(&spTransform));

//         IFC_RETURN(m_inputSiteAdapter->SetTransformFromContentRoot(spTransform.Get(), &m_offsetFromMainWindow));
//     }

//     return S_OK;
// }

// //------------------------------------------------------------------------
// //
// //  Method: EnterImpl
// //
// //  Synopsis:
// //      Causes the object and its "children" to enter scope. For popup, it calls the base class
// //      implementation. Popup's child is entered into popup's namescope if there is one.
// //
// //------------------------------------------------------------------------
// _Check_return_ HRESULT
// CPopup::EnterImpl(_In_ CDependencyObject *pNamescopeOwner, EnterParams params)
// {
//     bool bOldFlowDirection = IsRightToLeft();

//     IFC_RETURN(CFrameworkElement::EnterImpl(pNamescopeOwner, params));

//     // If the Popup is entering the live tree, ensure that it is marked for composition.
//     // If the Popup was already opened (i.e. a 'parentless Popup') then there's no need to mark it again.
//     if (params.fIsLive && !m_wasMarkedAsRedirectionElementOnOpen)
//     {
//         ASSERT(!m_wasMarkedAsRedirectionElementOnEnter);
//         IFC_RETURN(StartComposition());

//         m_wasMarkedAsRedirectionElementOnEnter = TRUE;
//     }

//     if(m_pChild && ((pNamescopeOwner && !params.fSkipNameRegistration) || (bOldFlowDirection != IsRightToLeft())) )
//     {
//         if(pNamescopeOwner && !params.fSkipNameRegistration)
//         {
//             // this is the Enter call to register names. if we made the child a namescope owner earlier,
//             // then reverse that since the child will now enter popup's namescope.
//             if(ShouldRemoveChildAsNamescopeOwner())
//             {
//                 //the Child no longer needs to a namescope owner as Popup has a namescope owner now
//                 IFC_RETURN(RemoveChildAsNamescopeOwner());
//             }
//         }

//         params.fIsLive = FALSE; //live enter is done when the child enters the popup root. this is only for name registration or propagating FlowDirection
//         IFC_RETURN(m_pChild->Enter(pNamescopeOwner, params));
//     }

//     return S_OK;
// }

// //------------------------------------------------------------------------
// //
// //  Synopsis:
// //      Causes the object and its properties to leave scope. For popup, it calls the base class
// //      implementation. If Popup is leaving a namescope, then the child leaves that namescope
// //      as well and becomes a namescope owner.
// //
// //------------------------------------------------------------------------
// _Check_return_ HRESULT
// CPopup::LeaveImpl(_In_ CDependencyObject *pNamescopeOwner, LeaveParams params)
// {
//     // If the Popup was marked for composition, unmark is when it leaves.
//     // This is safe because the Popup cannot be rendered again in this state, since it will also Close() here.
//     if (params.fIsLive)
//     {
//         // This case handles Popups in the live tree, and parentless Popups that were opened and moved into
//         // the live tree afterwards. In both cases, closing the Popup does not unset the composition state, only
//         // leaving the tree does.
//         ASSERT(m_wasMarkedAsRedirectionElementOnEnter ^ m_wasMarkedAsRedirectionElementOnOpen);
//         ASSERT(IsActive());
//         IFC_RETURN(StopComposition());
//     }

//     IFC_RETURN(CFrameworkElement::LeaveImpl(pNamescopeOwner, params));

//     // If we're leaving the live tree, close the popup
//     if (params.fIsLive)
//     {
//         // We're cleaning up unloading storage, and UIE.Hidden has already been raised. Don't call RequestKeepAlive
//         // expecting another UIE.Hidden to be raised.
//         ImplicitCloseGuard guard(this);
//         IFC_RETURN(SetValueByKnownIndex(KnownPropertyIndex::Popup_IsOpen, FALSE));
//     }

//     if (m_pChild && pNamescopeOwner && !params.fSkipNameRegistration)
//     {
//         //leave the current namescope
//         params.fIsLive = FALSE; //not leaving the live tree
//         IFC_RETURN(m_pChild->Leave(pNamescopeOwner, params));
//     }

//     // We don't need to hit-test if we left the tree
//     ReleaseInterface(m_pTransformer);

//     return S_OK;
// }

// //------------------------------------------------------------------------
// //
// //  Synopsis:
// //      Helper to properly manage registering the Popup for composition.
// //
// //------------------------------------------------------------------------
// _Check_return_ HRESULT
// CPopup::StartComposition()
// {
//     // Mark the Popup as needing composition for redirection.
//     IFC_RETURN(SetRequiresComposition(
//         CompositionRequirement::RedirectionElement,
//         IndependentAnimationType::None
//         ));

//     ASSERT(!m_isRegisteredOnCore);
//     IFC_RETURN(GetContext()->RegisterRedirectionElement(this));
//     m_isRegisteredOnCore = TRUE;

//     return S_OK;
// }

// //------------------------------------------------------------------------
// //
// //  Synopsis:
// //      Helper to properly manage unregistering a Popup for composition when
// //      it is no longer walkable from the PopupRoot.
// //
// //------------------------------------------------------------------------
// _Check_return_ HRESULT
// CPopup::StopComposition()
// {
//     // Unmark the Popup itself as needing composition for redirection.
//     UnsetRequiresComposition(
//         CompositionRequirement::RedirectionElement,
//         IndependentAnimationType::None
//         );

//     ASSERT(m_isRegisteredOnCore);
//     IFC_RETURN(GetContext()->UnregisterRedirectionElement(this));
//     m_isRegisteredOnCore = FALSE;

//     m_wasMarkedAsRedirectionElementOnEnter = FALSE;
//     m_wasMarkedAsRedirectionElementOnOpen = FALSE;

//     return S_OK;
// }

// //------------------------------------------------------------------------
// //
// //  Synopsis:
// //      Returns a value indicating whether or not the currently focused element is a child
// //      of this Popup.
// //
// //------------------------------------------------------------------------
// bool
// CPopup::HasFocusedElementAsChild()
// {
//     CFocusManager* pFocusManager = NULL;
//     bool hasFocusedElementAsChild = false;

//     pFocusManager = VisualTree::GetFocusManagerForElement(this);

//     if (pFocusManager != NULL)
//     {
//         CUIElement *pFocusedElement = do_pointer_cast<CUIElement>(pFocusManager->GetFocusedElementNoRef());

//         if (pFocusedElement != NULL)
//         {
//             bool focusedElementIsChildOfThisPopup = false;

//             if (m_pChild != NULL && pFocusedElement != m_pChild)
//             {
//                 focusedElementIsChildOfThisPopup = m_pChild->IsAncestorOf(pFocusedElement);
//             }

//             hasFocusedElementAsChild = pFocusedElement == m_pChild || focusedElementIsChildOfThisPopup;
//         }
//     }

//     return hasFocusedElementAsChild;
// }

// //------------------------------------------------------------------------
// //
// //  Method: AddChildAsNamescopeOwner
// //
// //  Synopsis:
// //      Helper method to add child as a namescope owner. This will be done when popup
// //      does not have a namescope owner. It ensures that name resolution works in the
// //      popup tree even when popup is created in ether.
// //
// //------------------------------------------------------------------------
// _Check_return_ HRESULT CPopup::AddChildAsNamescopeOwner()
// {
//     if(m_pChild && !m_pChild->IsStandardNameScopeOwner())
//     {
//         EnterParams enterParams(
//             /*isLive*/                FALSE, //this is just for name registration
//             /*skipNameRegistration*/  FALSE,
//             /*coercedIsEnabled*/      GetCoercedIsEnabled(),
//             /*useLayoutRounding*/     EnterParams::UseLayoutRoundingDefault,
//             /*visualTree*/            VisualTree::GetForElementNoRef(this, LookupOptions::NoFallback)
//         );

//         m_pChild->SetIsStandardNameScopeOwner(TRUE);
//         m_pChild->SetIsStandardNameScopeMember(TRUE);

//         GetContext()->GetNameScopeRoot().EnsureNameScope(m_pChild, nullptr);

//         IFC_RETURN(m_pChild->Enter(m_pChild, enterParams));
//         m_fMadeChildNamescopeOwner = TRUE;
//     }

//     return S_OK;
// }


// // Helper method to remove popup's child as a namescope owner. This will be done when
// // popup gets a namescope owner. Popup's child should enter the namescope of popup's
// // namescope owner.
// _Check_return_ HRESULT CPopup::RemoveChildAsNamescopeOwner()
// {
//     if(m_pChild)
//     {
//         LeaveParams leaveParams;
//         leaveParams.fIsLive= FALSE; //non-live leave as we are only changing the namescope owner here
//         leaveParams.fSkipNameRegistration = FALSE;
//         leaveParams.fCoercedIsEnabled = GetCoercedIsEnabled();
//         leaveParams.fVisualTreeBeingReset = FALSE;

//         IFC_RETURN(m_pChild->Leave(m_pChild, leaveParams));

//         IFC_RETURN(GetContext()->RemoveNameScope(m_pChild, Jupiter::NameScoping::NameScopeType::StandardNameScope));
//         m_pChild->SetIsStandardNameScopeOwner(FALSE);
//         m_pChild->SetIsStandardNameScopeMember(FALSE);
//         m_fMadeChildNamescopeOwner = FALSE;
//     }

//     return S_OK;
// }


// //------------------------------------------------------------------------
// //
// // Method: ShouldRemoveChildAsNamescopeOwner
// //
// // Synopsis:
// //      Returns TRUE if m_bMadeChildNamescopeOwner is set to TRUE
// //
// //------------------------------------------------------------------------
// bool CPopup::ShouldRemoveChildAsNamescopeOwner()
// {
//     return m_fMadeChildNamescopeOwner;
// }

// //------------------------------------------------------------------------
// //
// //  Method: AsyncRelease
// //
// //  Synopsis:
// //     adds popup to the CLR native peer release queue for deferred deletion. It is used
// //     when Popup wants to self-destruct in its instance method.
// //------------------------------------------------------------------------
// void CPopup::AsyncRelease()
// {
//     m_fAsyncQueueOnRelease = true;
//     Release();
//     m_fAsyncQueueOnRelease = false;
// }


// //------------------------------------------------------------------------
// //
// //  Method: RaisePopupEvent
// //
// //  Synopsis:
// //     helper method to raise Popup.Opened and Closed events
// //
// //------------------------------------------------------------------------

// void CPopup::RaisePopupEvent(EventHandle hEvent)
// {
//     CEventManager* pEventManager = GetContext()->GetEventManager();

//     if(pEventManager)
//     {
//         pEventManager->Raise(hEvent, TRUE, this, NULL);
//     }
// }


// //------------------------------------------------------------------------
// //
// //  Method:   Release
// //
// //  Synopsis:
// //      Making sure that all registered event handlers are removed and references from
// //      event manager to the popup are released. This is necessary as popup might not
// //      be a part of the tree and hence, this cleanup will not be done normally in Leave.
// //
// //------------------------------------------------------------------------
// void CPopup::ReleaseOverride()
// {
//     if(m_cEventListenerCount > 0
//         && GetRefCount() == (m_cEventListenerCount+1)
//         && !m_fRemovingListeners)
//     {
//         //setting a flag to prevent reentrancy
//         m_fRemovingListeners = TRUE;
//         IGNOREHR(CUIElement::RemoveAllEventListeners(false /* leaveUIEShownHiddenEventListenersAttached */));
//         m_fRemovingListeners = FALSE;
//         m_cEventListenerCount = 0;
//     }
// }


// //------------------------------------------------------------------------
// //
// //  Method: AddEventListener
// //
// //  Synopsis:
// //      Override to base AddEventListener. Maintains a count of listeners added
// //
// //------------------------------------------------------------------------
// _Check_return_
// HRESULT
// CPopup::AddEventListener(
//     _In_ EventHandle hEvent,
//     _In_ CValue *pValue,
//     _In_ XINT32 iListenerType,
//     _Out_opt_ CValue *pResult,
//     _In_ bool fHandledEventsToo)
// {
//     IFC_RETURN(CFrameworkElement::AddEventListener(hEvent, pValue, iListenerType, pResult, fHandledEventsToo));

//     ++m_cEventListenerCount;

//     return S_OK;
// }


// //------------------------------------------------------------------------
// //
// //  Method: RemoveEventListener
// //
// //  Synopsis:
// //      Override to base RemoveEventListener. Decrements the count of listeners added
// //
// //------------------------------------------------------------------------
// _Check_return_
// HRESULT
// CPopup::RemoveEventListener(
//     _In_ EventHandle hEvent,
//     _In_ CValue *pValue)
// {
//     // Sometimes we released all event handlers in CPopup::Release, before
//     // a client of this object removes one of its handlers. We don't want
//     // to fail in that case, but just no-op.
//     if (m_cEventListenerCount > 0)
//     {
//         m_fRemovingListeners = TRUE;
//         IFC_RETURN(CFrameworkElement::RemoveEventListener(hEvent, pValue));
//         m_fRemovingListeners = FALSE;

//         --m_cEventListenerCount;
//     }

//     return S_OK;
// }

// //------------------------------------------------------------------------
// //
// //  Synopsis:
// //      Resets implicit style and forwards value to the child.
// //
// //------------------------------------------------------------------------
// _Check_return_ HRESULT
// CPopup::UpdateImplicitStyle(
//     _In_opt_ CStyle *pOldStyle,
//     _In_opt_ CStyle *pNewStyle,
//     bool bForceUpdate,
//     bool bUpdateChildren,
//     bool isLeavingParentStyle
//     )
// {
//     IFC_RETURN(CFrameworkElement::UpdateImplicitStyle(pOldStyle, pNewStyle, bForceUpdate, bUpdateChildren, isLeavingParentStyle));

//     // If Popup is open, forward the implicit style update to the child. If the popup is closed, then
//     // there is no need to forward the notification. When popup is opened next, child will get added
//     // to the visual tree and the Enter walk will update the implicit style.
//     if(m_pChild && m_fIsOpen)
//     {
//         IFC_RETURN(m_pChild->UpdateImplicitStyle(pOldStyle, pNewStyle, bForceUpdate, bUpdateChildren));
//     }

//     return S_OK;
// }

// //------------------------------------------------------------------------
// //
// //  Method: PreChildrenPrintVirtual
// //
// //  Synopsis:
// //      Print override for Popup. Set a flag to indicate this popup should be printed.
// //
// //------------------------------------------------------------------------
// _Check_return_ HRESULT
// CPopup::PreChildrenPrintVirtual(
//     _In_ const SharedRenderParams& sharedPrintParams,
//     _In_ const D2DPrecomputeParams& cp,
//     _In_ const D2DRenderParams &printParams
//     )
// {
//     if (m_pChild && IsOpen())
//     {
//         m_fIsPrintDirty = TRUE;
//     }
//     RRETURN(S_OK);
// }

// //------------------------------------------------------------------------
// //
// //  Synopsis:
// //      Walk the element and its children finding elements that intersect
// //      with the given point.
// //
// //------------------------------------------------------------------------
// _Check_return_ HRESULT
// CPopup::BoundsTestInternal(
//     _In_ const XPOINTF& /*target*/,
//     _In_ CBoundedHitTestVisitor* /*pCallback*/,
//     _In_opt_ const HitTestParams* /*hitTestParams*/,
//     _In_ bool /*canHitDisabledElements*/,
//     _In_ bool /*canHitInvisibleElements*/,
//     _Out_opt_ BoundsWalkHitResult* pResult
//     )
// {
//     HRESULT hr = S_OK;

//     //
//     // Intentionally do not test the element with this API. Popups
//     // can only be bounds tested using the BoundsTestPopup API
//     // to ensure parented and non-parented popups are treated
//     // uniformly.
//     //

//     if (pResult)
//     {
//         *pResult = BoundsWalkHitResult::Continue;
//     }

//     RRETURN(hr);
// }

// //------------------------------------------------------------------------
// //
// //  Synopsis:
// //      Walk the element and its children finding elements that intersect
// //      with the given polygon.
// //
// //------------------------------------------------------------------------
// _Check_return_ HRESULT
// CPopup::BoundsTestInternal(
//     _In_ const HitTestPolygon& /*target*/,
//     _In_ CBoundedHitTestVisitor* /*pCallback*/,
//     _In_opt_ const HitTestParams* /*hitTestParams*/,
//     _In_ bool /*canHitDisabledElements*/,
//     _In_ bool /*canHitInvisibleElements*/,
//     _Out_opt_ BoundsWalkHitResult* pResult
//     )
// {
//     HRESULT hr = S_OK;

//     //
//     // Intentionally do not test the element with this API. Popups
//     // can only be bounds tested using the BoundsTestPopup API
//     // to ensure parented and non-parented popups are treated
//     // uniformly.
//     //

//     if (pResult)
//     {
//         *pResult = BoundsWalkHitResult::Continue;
//     }

//     RRETURN(hr);
// }

// //------------------------------------------------------------------------
// //
// //  Synopsis:
// //      Walk the element and its children finding elements that intersect
// //      with the given point/polygon.
// //
// //------------------------------------------------------------------------
// template <typename HitType>
// _Check_return_ HRESULT
// CPopup::BoundsTestPopup(
//     _In_ const HitType& target,
//     _In_ CBoundedHitTestVisitor* pCallback,
//     _In_opt_ const HitTestParams *hitTestParams,
//     _In_ bool canHitDisabledElements,
//     _In_ bool canHitInvisibleElements,
//     _Out_opt_ BoundsWalkHitResult* pResult
//     )
// {
//     // Skip the Popup::BoundsTestInternal override (called during the regular tree walk) and
//     // call into the real implementation.  BoundsTestPopup is only called from the PopupRoot,
//     // which is where we actually want to test the Popups from.
//     RRETURN(CUIElement::BoundsTestInternal(target, pCallback, hitTestParams, canHitDisabledElements, canHitInvisibleElements, pResult));
// }

// //------------------------------------------------------------------------
// //
// //  Synopsis:
// //      Walk the children of the element finding elements that intersect
// //      with the given point.
// //
// //------------------------------------------------------------------------
// _Check_return_ HRESULT
// CPopup::BoundsTestChildren(
//     _In_ const XPOINTF& target,
//     _In_ CBoundedHitTestVisitor* pCallback,
//     _In_opt_ const HitTestParams *hitTestParams,
//     _In_ bool canHitDisabledElements,
//     _In_ bool canHitInvisibleElements,
//     _Out_opt_ BoundsWalkHitResult* pResult
//     )
// {
//     BoundsWalkHitResult childHitResult = BoundsWalkHitResult::Continue;

//     if (m_pChild != NULL)
//     {
//         IFC_RETURN(m_pChild->BoundsTestInternal(target, pCallback, hitTestParams, canHitDisabledElements, canHitInvisibleElements, &childHitResult));
//     }

//     if (pResult)
//     {
//         *pResult = childHitResult;
//     }

//     return S_OK;
// }

// //------------------------------------------------------------------------
// //
// //  Synopsis:
// //      Walk the children of the element finding elements that intersect
// //      with the given polygon.
// //
// //------------------------------------------------------------------------
// _Check_return_ HRESULT
// CPopup::BoundsTestChildren(
//     _In_ const HitTestPolygon& target,
//     _In_ CBoundedHitTestVisitor* pCallback,
//     _In_opt_ const HitTestParams *hitTestParams,
//     _In_ bool canHitDisabledElements,
//     _In_ bool canHitInvisibleElements,
//     _Out_opt_ BoundsWalkHitResult* pResult
//     )
// {
//     BoundsWalkHitResult childHitResult = BoundsWalkHitResult::Continue;

//     if (m_pChild != NULL)
//     {
//         IFC_RETURN(m_pChild->BoundsTestInternal(target, pCallback, hitTestParams, canHitDisabledElements, canHitInvisibleElements, &childHitResult));
//     }

//     if (pResult)
//     {
//         *pResult = childHitResult;
//     }

//     return S_OK;
// }

// //------------------------------------------------------------------------
// //
// //  Synopsis:
// //      Get the combined outer bounds of all children.
// //
// //------------------------------------------------------------------------
// _Check_return_ HRESULT
// CPopup::GenerateChildOuterBounds(
//     _In_opt_ HitTestParams *hitTestParams,
//     _Out_ XRECTF_RB* pBounds
//     )
// {
//     if (m_pChild != NULL)
//     {
//         IFC_RETURN(m_pChild->GetOuterBounds(hitTestParams, pBounds));
//     }
//     else
//     {
//         EmptyRectF(pBounds);
//     }

//     return S_OK;
// }

// //------------------------------------------------------------------------
// //
// //  Synopsis:
// //      Removes the render data for this element's subgraph from the scene.
// //      This is called when the PopupRoot leaves the scene, or when the Popup closes.
// //
// //------------------------------------------------------------------------
// void CPopup::LeavePCSceneSubgraph()
// {
//     // This could have been called after a popup finishes unloading, in which case the child might be null.
//     if (m_pChild != nullptr)
//     {
//         // The PopupRoot walked to the Popup, and the Popup walks to its child.
//         m_pChild->LeavePCSceneRecursive();
//     }

//     if (m_overlayElement)
//     {
//         m_overlayElement->LeavePCSceneRecursive();
//     }
// }

// // Leave scene because popup is being closed
// void CPopup::LeavePCScene()
// {
//     LeavePCSceneRecursive();

//     // If this is a nested popup and is closed because of an ancestor popup's closure, it is removed from the scene
//     // in CUIElement::LeaveImpl, but its m_pChild may not be removed from the scene. So the child can continue to be rendered
//     // after the popups close. This scenario occurs when nested popups are closed in a different order than innermost-first,
//     // which can occur when the XAML window deactivation handler closes popups.
//     //
//     // When the outermost popup closes, CPopup::SetValue(Popup_IsOpen) removes its m_pChild as namescope owner if needed,
//     // calls CPopup::Close which removes m_pChild from the popup root, causing a live-LeaveImpl on m_pChild, and then
//     // does LeavePCSceneRecursive on the popup itself. The LeaveImpl will stop if it hits a nested popup's child because
//     // the namescope owner has been removed. If the innermost popup is closed first, everything works correctly.
//     //
//     // Specifically:
//     // 1. The nested popup's CUIElement::LeaveImpl calls EnsurePropertyRenderData(RWT_None), which removes the
//     // popup from the scene, but not its m_pChild.
//     // 2. The nested CPopup::Close's pPopupRoot->RemoveChild(m_pChild) doesn't do a live-LeaveImpl on m_pChild
//     // because the ancestor popup has called RemoveChildAsNamescopeOwner during its closure. (See CDependencyObject::Leave.)
//     // 3. Since the nested popup was already removed from the scene during 1, CUIElement::LeavePCSceneRecursive
//     // doesn't call LeavePCSceneSubgraph to remove m_pChild from the scene.
//     //
//     // So, remove m_pChild's render data if m_pChild is still in the scene.
//     //
//     // With non-nested popups, or when the innermost popup is closed first, the namescope owner exists, so live-LeaveImpl
//     // is called on the popup child, which will remove m_pChild from the scene. Then CPopup::Close's call
//     // to LeavePCSceneRecursive will remove the popup from the scene.

//     if (m_pChild && m_pChild->IsInPCScene())
//     {
//         m_pChild->LeavePCSceneRecursive();
//     }

//     if (m_overlayElement && m_overlayElement->IsInPCScene())
//     {
//         m_overlayElement->LeavePCSceneRecursive();
//     }
// }

// //------------------------------------------------------------------------
// //
// //  Synopsis:
// //      Mark state as dirty when an inherited property changes.
// //
// //------------------------------------------------------------------------
// _Check_return_ HRESULT CPopup::MarkInheritedPropertyDirty(
//     _In_ const CDependencyProperty* pdp,
//     _In_ const CValue* pValue)
// {
//     IFC_RETURN(CFrameworkElement::MarkInheritedPropertyDirty(pdp, pValue));
//     if (m_pChild && IsOpen())
//     {
//         IFC_RETURN(m_pChild->MarkInheritedPropertyDirty(pdp, pValue));
//     }

//     return S_OK;
// }

// //------------------------------------------------------------------------
// //
// //  Synopsis:
// //      Returns the closest Popup ancestor or NULL if one does not exist.
// //
// //------------------------------------------------------------------------
// CPopup *CPopup::GetClosestPopupAncestor(_In_opt_ CUIElement *pChild)
// {
//     CUIElement *pNode = pChild;

//     while (pNode)
//     {
//         if (pNode->OfTypeByIndex<KnownTypeIndex::Popup>())
//         {
//             return static_cast<CPopup *>(pNode);
//         }

//         pNode = pNode->GetUIElementAdjustedParentInternal(FALSE);
//     }

//     return NULL;
// }

// CFlyoutBase *CPopup::GetClosestFlyoutAncestor(_In_opt_ CUIElement *pUIElement)
// {
//     CUIElement *pNode = pUIElement;

//     while (pNode)
//     {
//         if (pNode->OfTypeByIndex<KnownTypeIndex::Popup>())
//         {
//             CPopup* popup = static_cast<CPopup*>(pNode);
//             if (popup->IsFlyout())
//             {
//                 return popup->GetAssociatedFlyoutNoRef();
//             }
//         }

//         pNode = pNode->GetUIElementAdjustedParentInternal(FALSE);
//     }

//     return nullptr;
// }

// // The ESC key closes the topmost light-dismiss-enabled popup.
// // This handles the case where ESC is pressed while the CPopup has focus.  This does not handle the routed case
// // where some child of a light-dismiss-enabled popup has focus, since its events route to CPopupRoot.
// _Check_return_ HRESULT CPopup::OnKeyDown(
//     _In_ CEventArgs* pEventArgs)
// {
//     // We will dismiss popup on key down for RTM apps. This is to accommodate IME behavior. IME has some logic
//     // tied to ESC key press (such as closing suggestions menu etc) which gets invoked OnKeyDown and they handle that event
//     // For light dismiss popups we do not  want ESC key to dismiss the popup if this logic gets executed, which happens in on key down.
//     return ClosePopupOnKeyEventIfNecessary(static_cast<CKeyEventArgs*>(pEventArgs));
// }

// //------------------------------------------------------------------------
// //
// //  Synopsis:
// //      This method propagates dirty flags to the parent DO via its stored dirty flag function.
// //      Overridden to handle propagation to the PopupRoot in addition to the main tree,
// //      since Popups do not render as part of their actual parent's subgraph.
// //
// //------------------------------------------------------------------------
// void
// CPopup::NWPropagateDirtyFlag(
//     DirtyFlags flags
//     )
// {
// #if DBG
//     DbgCheckElementDirtyFlags(flags);
// #endif

//     // The Popup itself might be the target of a LayoutTransition. In that case, it will not
//     // be rendered by the PopupRoot, but will be rendered by an LTE instead, so that's where
//     // the dirty flag propagation should go.
//     if (IsHiddenForLayoutTransition())
//     {
//         NWPropagateDirtyFlagForLayoutTransitions(flags);
//     }
//     else
//     {
//         CPopupRoot* pPopupRoot = NULL;
//         IGNOREHR(GetContext()->GetAdjustedPopupRootForElement(this, &pPopupRoot));
//         if (pPopupRoot)
//         {
//             NWSetSubgraphDirty(pPopupRoot, flags);
//         }
//     }

//     // Also propagate dirty flag up the direct ancestor chain as the render walk must
//     // first walk this part of the tree to ensure proper updating of certain CUIElement flags
//     // (currently limited to m_isRedirectedChildOfSwapChainPanel flag) before proceeding to the PopupRoot .
//     CDependencyObject::NWPropagateDirtyFlag(flags);
// }

// DirectUI::FocusState CPopup::GetSavedFocusState()
// {
//     return (m_pPreviousFocusWeakRef) ? m_savedFocusState : DirectUI::FocusState::Unfocused;
// }

// //------------------------------------------------------------------------
// //
// //  Synopsis:
// //      Should PopupRoot notify this popup of app's theme change?
// //
// //------------------------------------------------------------------------

// bool CPopup::ShouldPopupRootNotifyThemeChange()
// {
//     // An AppBar gets theme change notification from its owner page.
//     // A parented popup gets theme change notification from its parent.
//     if (IsApplicationBarService() || GetParentInternal(false))
//     {
//         return false;
//     }
//     else
//     {
//         return true;
//     }
// }

// //------------------------------------------------------------------------
// //
// //  Synopsis:
// //      Pass the FontSize invalidation down to the child.
// //
// //------------------------------------------------------------------------

// _Check_return_ HRESULT
// CPopup::RecursiveInvalidateFontSize()
// {
//     if (m_pChild && IsOpen())
//     {
//         IFC_RETURN(m_pChild->RecursiveInvalidateFontSize());
//     }

//     return S_OK;
// }

// //------------------------------------------------------------------------
// //
// //  Synopsis:
// //      Notify element and its subtree that theme has changed
// //
// //------------------------------------------------------------------------
// _Check_return_ HRESULT
// CPopup::NotifyThemeChangedCore(_In_ Theming::Theme theme, _In_ bool fForceRefresh)
// {
//     // Notify base class that theme has changed
//     IFC_RETURN(CFrameworkElement::NotifyThemeChangedCore(theme, fForceRefresh));

//      // Recursively notify element subtree that theme has changed
//     if (m_pChild)
//     {
//         IFC_RETURN(m_pChild->NotifyThemeChanged(theme, fForceRefresh));
//     }

//     if (m_overlayElement)
//     {
//         // Set the overlay to match the child's theme.  If there is no child,
//         // then match the popup's theme.
//         auto overlayTheme = m_pChild ? m_pChild->GetTheme() : GetTheme();
//         IFC_RETURN(m_overlayElement->NotifyThemeChanged(overlayTheme, fForceRefresh));
//     }

//     return S_OK;
// }

// _Check_return_ HRESULT
// CPopup::ClosePopupOnKeyEventIfNecessary(_In_ CKeyEventArgs* eventArgs)
// {
//     if (eventArgs->m_platformKeyCode == VK_ESCAPE)
//     {
//         eventArgs->m_bHandled = TRUE;

//         // Give the popup an opportunity to cancel closing.
//         bool cancel = false;
//         IFC_RETURN(OnClosing(&cancel));
//         if (cancel)
//         {
//             return S_OK;
//         }

//         m_focusStateAfterClosing = DirectUI::FocusState::Keyboard;
//         IFC_RETURN(SetValueByKnownIndex(KnownPropertyIndex::Popup_IsOpen, FALSE));
//     }

//     return S_OK;
// }

// _Check_return_ HRESULT
// CPopup::OnClosing(_Out_ bool* cancel)
// {
//     *cancel = false;

//     // If this popup is associated with a flyout, then give it a chance to cancel closing.
//     if (IsFlyout())
//     {
//         IFC_RETURN(FxCallbacks::FlyoutBase_OnClosing(m_associatedFlyoutWeakRef.lock_noref(), cancel));
//     }

//     return S_OK;
// }

// //------------------------------------------------------------------------
// //
// // GC's ReferenceTrackerWalk. Walk children
// // Called on GC's thread
// //
// //------------------------------------------------------------------------

// bool CPopup::ReferenceTrackerWalkCore(
//     _In_ DirectUI::EReferenceTrackerWalkType walkType,
//     _In_ bool isRoot,
//     _In_ bool shouldWalkPeer)
// {
//     bool walked = CDependencyObject::ReferenceTrackerWalkCore(walkType, isRoot, shouldWalkPeer);

//     if (walked)
//     {
//         if (m_pChild)
//         {
//             m_pChild->ReferenceTrackerWalk(
//                         walkType,
//                         false,  //isRoot
//                         true);  //shouldWalkPeer
//         }
//     }

//     return walked;
// }

// _Check_return_ HRESULT
// CPopup::ReevaluateIsOverlayVisible()
// {
//     bool isOverlayVisible = LightDismissOverlayHelper::IsOverlayVisibleForMode(static_cast<xaml_controls::LightDismissOverlayMode>(m_lightDismissOverlayMode));

//     // If we don't have a child, then don't show an overlay.
//     isOverlayVisible &= (m_pChild != nullptr);

//     // Only enable the overlay if the popup is light-dismiss enabled, unless this check is
//     // otherwise disabled.
//     isOverlayVisible &= (m_fIsLightDismissEnabled || m_disableOverlayIsLightDismissCheck);

//     // Only take action if the value has changed.
//     if (isOverlayVisible != m_isOverlayVisible)
//     {
//         m_isOverlayVisible = isOverlayVisible;

//         if (m_isOverlayVisible)
//         {
//             CREATEPARAMETERS cp(GetContext());
//             xref_ptr<CFrameworkElement> overlayElement;
//             IFC_RETURN(CRectangle::Create(reinterpret_cast<CDependencyObject**>(overlayElement.ReleaseAndGetAddressOf()), &cp));

//             IFC_RETURN(SetOverlayElement(overlayElement.get()));
//         }
//         else
//         {
//             IFC_RETURN(RemoveOverlayElement());
//         }
//     }

//     return S_OK;
// }

// _Check_return_ HRESULT
// CPopup::SetOverlayElement(_In_ CFrameworkElement* overlayElement)
// {
//     ASSERT(m_isOverlayVisible);
//     ASSERT(overlayElement != nullptr);
//     ASSERT(m_overlayElement == nullptr);

//     SetInterface(m_overlayElement, overlayElement);

//     IFC_RETURN(m_overlayElement->PegManagedPeer());

//     DECLARE_CONST_STRING_IN_FUNCTION_SCOPE(themeBrushKey, L"PopupLightDismissOverlayBackground");
//     IFC_RETURN(SetOverlayThemeBrush(themeBrushKey));

//     EnterParams enterParams(
//         /*isLive*/                FALSE, //this is just for name registration
//         /*skipNameRegistration*/  FALSE,
//         /*coercedIsEnabled*/      GetCoercedIsEnabled(),
//         /*useLayoutRounding*/     EnterParams::UseLayoutRoundingDefault,
//         /*visualTree*/            VisualTree::GetForElementNoRef(this, LookupOptions::NoFallback)
//         );

//     // The overlay element doesn't host any content, so we don't really care about
//     // name resolution, so just make itself its own namescope owner.
//     m_overlayElement->SetIsStandardNameScopeOwner(TRUE);
//     m_overlayElement->SetIsStandardNameScopeMember(TRUE);
//     GetContext()->GetNameScopeRoot().EnsureNameScope(m_overlayElement, nullptr);

//     IFC_RETURN(m_overlayElement->Enter(m_overlayElement, enterParams));

//     IFC_RETURN(AddLogicalChild(m_overlayElement));

//     if (m_fIsOpen)
//     {
//         IFC_RETURN(AddOverlayElementToPopupRoot());
//     }

//     return S_OK;
// }

// _Check_return_ HRESULT
// CPopup::RemoveOverlayElement()
// {
//     ASSERT(m_overlayElement != nullptr);

//     LeaveParams leaveParams(
//         /*isLive*/                  FALSE, // non-live leave as we are only changing the namescope owner here.
//         /*skipNameRegistration*/    FALSE,
//         /*coercedIsEnabled*/        GetCoercedIsEnabled(),
//         /*visualTreeReset*/         FALSE
//         );

//     IFC_RETURN(m_overlayElement->Leave(GetStandardNameScopeOwner(), leaveParams));

//     IFC_RETURN(GetContext()->RemoveNameScope(m_overlayElement, Jupiter::NameScoping::NameScopeType::StandardNameScope));
//     m_overlayElement->SetIsStandardNameScopeOwner(FALSE);
//     m_overlayElement->SetIsStandardNameScopeMember(FALSE);

//     if (m_fIsOpen)
//     {
//         IFC_RETURN(RemoveOverlayElementFromPopupRoot());
//     }

//     RemoveLogicalChild(m_overlayElement);

//     m_overlayElement->UnpegManagedPeer();

//     ReleaseInterface(m_overlayElement);

//     return S_OK;
// }

// _Check_return_ HRESULT
// CPopup::AddOverlayElementToPopupRoot()
// {
//     ASSERT(m_fIsOpen);
//     ASSERT(m_overlayElement != nullptr);
//     ASSERT(m_pChild != nullptr);

//     CPopupRoot* popupRoot = nullptr;
//     IFC_RETURN(GetContext()->GetAdjustedPopupRootForElement(this, &popupRoot));
//     ASSERT(popupRoot);

//     XINT32 indexOfChild = 0;
//     IFC_RETURN(popupRoot->GetChildren()->IndexOf(m_pChild, &indexOfChild));

//     IFC_RETURN(popupRoot->InsertChild(indexOfChild, m_overlayElement));

//     // Set the overlay to match the child's theme.  If there is no child,
//     // then match the popup's theme.
//     auto overlayTheme = m_pChild ? m_pChild->GetTheme() : GetTheme();
//     IFC_RETURN(m_overlayElement->NotifyThemeChanged(overlayTheme, FALSE /*fForceRefresh*/));

//     return S_OK;
// }

// _Check_return_ HRESULT
// CPopup::RemoveOverlayElementFromPopupRoot()
// {
//     ASSERT(m_overlayElement != nullptr);
//     ASSERT(m_pChild != nullptr);

//     CPopupRoot* popupRoot = nullptr;

//     IFC_RETURN(GetContext()->GetAdjustedPopupRootForElement(this, &popupRoot));
//     ASSERT(popupRoot);

//     IFC_RETURN(popupRoot->RemoveChild(m_overlayElement));

//     return S_OK;
// }

// _Check_return_ HRESULT
// CPopup::SetOverlayThemeBrush(_In_ const xstring_ptr& brushKey)
// {
//     ASSERT(m_overlayElement);

//     auto core = GetContext();
//     auto dictionary = core->GetThemeResources();

//     CDependencyObject* initialValueNoRef = nullptr;
//     IFC_RETURN(dictionary->GetKeyNoRef(brushKey, &initialValueNoRef));

//     CREATEPARAMETERS cp(core);
//     xref_ptr<CThemeResourceExtension> themeResourceExtension;
//     IFC_RETURN(CThemeResourceExtension::Create(
//         reinterpret_cast<CDependencyObject **>(themeResourceExtension.ReleaseAndGetAddressOf()),
//         &cp));

//     themeResourceExtension->m_strResourceKey = brushKey;

//     IFC_RETURN(themeResourceExtension->SetInitialValueAndTargetDictionary(initialValueNoRef, dictionary));

//     IFC_RETURN(themeResourceExtension->SetThemeResourceBinding(
//         m_overlayElement,
//         DirectUI::MetadataAPI::GetPropertyByIndex(KnownPropertyIndex::Shape_Fill))
//         );

//     return S_OK;
// }

// //------------------------------------------------------------------------
// //
// //  Synopsis:
// //      Set and Get the AssociatedFlyout to interact between Popup and Flyout
// //
// //------------------------------------------------------------------------
// _Check_return_ HRESULT
// CPopup::AssociatedFlyout(
//     _In_ CDependencyObject *pObject,
//     _In_ XUINT32 cArgs,
//     _Inout_updates_(cArgs) CValue *pArgs,
//     _In_opt_ IInspectable* pValueOuter,
//     _Out_ CValue *pResult)
// {
//     CPopup* pPopup = nullptr;

//     if (!pObject || !pObject->OfTypeByIndex<KnownTypeIndex::Popup>())
//     {
//         IFC_RETURN(E_INVALIDARG);
//     }

//     IFC_RETURN(DoPointerCast(pPopup, pObject));

//     if (cArgs == 0)
//     {
//         // Getting the Associated Flyout
//         pResult->SetObjectAddRef(pPopup->m_associatedFlyoutWeakRef.lock_noref());
//     }
//     else if (cArgs == 1 && pArgs->GetType() == valueObject && pArgs->AsObject()->OfTypeByIndex<KnownTypeIndex::FlyoutBase>())
//     {
//         // Setter(Associated Flyout) must be called once from the FlyoutBase
//         if (pPopup->m_associatedFlyoutWeakRef.lock_noref() == nullptr)
//         {
//             CFlyoutBase* pFlyoutBase;

//             IFC_RETURN(DoPointerCast(pFlyoutBase, *pArgs));
//             pPopup->m_associatedFlyoutWeakRef = xref::get_weakref(static_cast<CFlyoutBase*>(pFlyoutBase));
//         }
//         else
//         {
//             // Not allow to set the AssociatedFlyout multiple
//             IFC_RETURN(E_FAIL);
//         }
//     }
//     else
//     {
//         IFC_RETURN(E_INVALIDARG);
//     }

//     return S_OK;
// }

// _Check_return_ HRESULT
// CPopup::GetOverlayInputPassThroughElementNoRef(_Outptr_ CDependencyObject **ppElementNoRef)
// {
//     *ppElementNoRef = nullptr;
//     CDependencyObject *overlayInputPassThroughElement = nullptr;

//     if (IsFlyout())
//     {
//         CFlyoutBase *flyoutBase = m_associatedFlyoutWeakRef.lock_noref();

//         if (flyoutBase != nullptr)
//         {
//             CValue value;
//             IFC_RETURN(flyoutBase->GetValueByIndex(KnownPropertyIndex::FlyoutBase_OverlayInputPassThroughElement, &value));
//             IFC_RETURN(DirectUI::CValueBoxer::UnwrapWeakRef(
//                 &value,
//                 DirectUI::MetadataAPI::GetDependencyPropertyByIndex(KnownPropertyIndex::FlyoutBase_OverlayInputPassThroughElement),
//                 &overlayInputPassThroughElement));
//         }
//     }
//     else
//     {
//         CValue value;
//         IFC_RETURN(GetValueByIndex(KnownPropertyIndex::Popup_OverlayInputPassThroughElement, &value));
//         IFC_RETURN(DirectUI::CValueBoxer::UnwrapWeakRef(
//             &value,
//             DirectUI::MetadataAPI::GetDependencyPropertyByIndex(KnownPropertyIndex::Popup_OverlayInputPassThroughElement),
//             &overlayInputPassThroughElement));
//     }

//     *ppElementNoRef = overlayInputPassThroughElement;

//     return S_OK;
// }

// void CPopup::SetAssociatedVisualTree(_In_opt_ VisualTree* tree)
// {
//     if(tree)
//     {
//         VERIFYHR(tree->AttachElement(this));
//     }
// }

// void CPopup::SetAssociatedIsland(_In_opt_ CDependencyObject* root)
// {
//     if (CRootVisual* rootVisual = do_pointer_cast<CRootVisual>(root))
//     {
//         SetAssociatedVisualTree(rootVisual->GetAssociatedVisualTree());
//     }
//     else if (CXamlIslandRoot* xamlIsland = do_pointer_cast<CXamlIslandRoot>(root))
//     {
//         SetAssociatedVisualTree(xamlIsland->GetVisualTreeNoRef());
//     }
// }

// _Check_return_ HRESULT
// CPopup::OnContentAutomationProviderRequested(
//     _In_ ixp::IContentIsland* content,
//     _In_ ixp::IContentIslandAutomationProviderRequestedEventArgs* e)
// {
//     EnsureUIAWindow();

//     IFC_RETURN(e->put_AutomationProvider(m_spUIAWindow.get()));
//     IFC_RETURN(e->put_Handled(true));

//     return S_OK;
// }

// void CPopup::SetCachedStandardNamescopeOwner(_In_ CDependencyObject* obj)
// {
//     if (obj)
//     {
//         m_cachedNamescopeOwner = xref::get_weakref(obj);
//     }
//     else
//     {
//         m_cachedNamescopeOwner.reset();
//     }
// }

// CDependencyObject* CPopup::GetCachedStandardNamescopeOwnerNoRef()
// {
//     return m_cachedNamescopeOwner.lock_noref();
// }

// // Microsoft::UI::Composition::ICompositionSupportsSystemBackdrop implementation
// _Check_return_ HRESULT CPopup::GetSystemBackdrop(_Outptr_result_maybenull_ RealWUComp::ICompositionBrush** systemBackdropBrush)
// {
//     if (m_backdropLink)
//     {
//         wrl::ComPtr<ixp::ICompositionSupportsSystemBackdrop> icssb;
//         IFC_RETURN(m_backdropLink.As(&icssb));
//         IFC_RETURN(icssb->get_SystemBackdrop(systemBackdropBrush));
//     }
//     else
//     {
//         *systemBackdropBrush = nullptr;
//     }

//     return S_OK;
// }

// _Check_return_ HRESULT CPopup::SetSystemBackdrop(_In_opt_ RealWUComp::ICompositionBrush* systemBackdropBrush)
// {
//     if (systemBackdropBrush)
//     {
//         EnsureContentExternalBackdropLink();

//         wrl::ComPtr<ixp::ICompositionSupportsSystemBackdrop> icssb;
//         IFC_RETURN(m_backdropLink.As(&icssb));
//         IFC_RETURN(icssb->put_SystemBackdrop(systemBackdropBrush));
//     }
//     else
//     {
//         DiscardContentExternalBackdropLink();
//     }

//     return S_OK;
// }

#endregion

#region dxaml\xcp\components\elements\Popup.cpp

	// ImplicitCloseGuard::ImplicitCloseGuard(_In_ CPopup* popup)
	//     : m_popupNoRef(popup)
	// {
	//     m_popupNoRef->m_isImplicitClose = TRUE;
	// }

	// ImplicitCloseGuard::~ImplicitCloseGuard()
	// {
	//     m_popupNoRef->m_isImplicitClose = FALSE;
	// }

	// void CPopup::CancelHideAnimationToPrepareForShow()
	// {
	//     CancelImplicitAnimation(ImplicitAnimationType::Hide);

	//     // Open() cancels not only hide animations on the popup, but on its child as well.
	//     if (m_pChild != nullptr)
	//     {
	//         m_pChild->CancelImplicitAnimation(ImplicitAnimationType::Hide);
	//     }

	//     if (m_unloadingChild)
	//     {
	//         m_unloadingChild->CancelImplicitAnimation(ImplicitAnimationType::Hide);
	//     }
	// }

	// bool CPopup::IsUnloading() const
	// {
	//     return m_isUnloading;
	// }

	// CPopup* CPopup::GetPopupOfUnloadingChild(_In_ CUIElement* child)
	// {
	//     CDependencyObject* logicalParent = child->GetLogicalParentNoRef();
	//     if (logicalParent != nullptr && logicalParent->OfTypeByIndex<KnownTypeIndex::Popup>())
	//     {
	//         CPopup* parentPopup = static_cast<CPopup*>(logicalParent);
	//         if (parentPopup->m_unloadingChild == child)
	//         {
	//             return parentPopup;
	//         }
	//     }
	//     return nullptr;
	// }

	// void CPopup::RemoveUnloadingChild()
	// {
	//     RemoveLogicalChild(m_unloadingChild);
	//     m_unloadingChild->SetAssociated(false, nullptr);
	//     m_unloadingChild = nullptr;
	// }
}
