// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference PopupRoot_Partial.cpp, PopupRoot.cpp, Popup.cpp (in this order), tag winui3/release/1.4.3, commit 685d2bf

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Uno.UI.DirectUI.Components.Theming;
using Uno.UI.Xaml.Core;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml.Automation.Peers;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Windows.UI.Xaml.Controls.Primitives;

internal partial class PopupRoot : Panel
{
	#region PopupRoot_Partial.cpp

	protected override AutomationPeer OnCreateAutomationPeer()
	{
		bool lightDismiss = IsTopmostPopupInLightDismissChain();
		if (lightDismiss)
		{
			return new PopupRootAutomationPeer(this);
		}

		// TODO:MZ: null or base?
		return null;
	}

	private void CloseTopmostPopup() => CoreImports.PopupRoot_CloseTopmostPopup(this, true);

	#endregion

	#region PopupRoot.cpp

	private Popup? GetOpenPopupWithChild(UIElement child, bool checkUnloadingChildToo)
	{
		if (_openPopups != null)
		{
			// Include unloading popups too.
			for (var pNode = _openPopups.First;
				 pNode != null;
				 pNode = pNode.Next)
			{
				Popup popup = pNode.Value;

				if (popup._child == child
					|| checkUnloadingChildToo && popup._unloadingChild == child)
				{
					return popup;
				}
			}
		}

		return null;
	}

	private bool HasOpenOrUnloadingPopups()
	{
		return _openPopups != null && _openPopups.FirstOrDefault() != null;
	}

	/// <summary>
	/// When closing all open popups, we can't just close them in the order that they were opened. With nested
	/// popups, calling Close on the outer popup will also close the inner popup, which calls back to the popup
	/// root and causes reentrancy since we're in the middle of closing the outer popup. So we need to close a
	/// nested popups before closing its parent popup. This method returns open popups in an order that's safe
	/// to iterate over and close.
	/// </summary>
	/// <returns>List of popups in safe closing order.</returns>
	private IList<Popup> GetPopupsInSafeClosingOrder()
	{
		List<Popup> popupsInSafeClosingOrder = new();

		if (_openPopups != null)
		{
			var openPopupNode = _openPopups.First;

			while (openPopupNode != null)
			{
				Popup popup = openPopupNode.Value;
				openPopupNode = openPopupNode.Next;

				// For each open popup, find a safe time to call Close on it. By default the popup is closed in natural
				// order (i.e. the order that it was opened in). But if the popup has popup ancestors that also need to
				// be closed, it must be closed before the ancestors get closed. So default to the index at end of the
				// list, then walk up the ancestor chain. For each popup ancestor, if that ancestor is already in the
				// close list, then we have to insert this popup before that ancestor.
				var insertPopupIterator = popupsInSafeClosingOrder.end();

				for (DependencyObject* ancestor = popup.GetParentFollowPopups();
					// We can early exit if we determine that this popup must be closed before all other popups.
					ancestor != null && insertPopupIterator != popupsInSafeClosingOrder.begin();
					ancestor = ancestor.GetParentFollowPopups())
				{
					if (ancestor.OfTypeByIndex<KnownTypeIndex.Popup>())
					{
						CPopup* ancestorPopup = (CPopup*)(ancestor);
						if (ancestorPopup.IsOpen())
						{
							// We found an open ancestor popup. If it also needs to be closed, then make sure we're
							// closed before that ancestor.
							var ancestorIterator = std.find(popupsInSafeClosingOrder.begin(), popupsInSafeClosingOrder.end(), ancestorPopup);
							if (ancestorIterator != popupsInSafeClosingOrder.end()
								&& ancestorIterator < insertPopupIterator)
							{
								insertPopupIterator = ancestorIterator;
							}
						}
					}
				}

				popupsInSafeClosingOrder.insert(insertPopupIterator, popup);
			}
		}

		return popupsInSafeClosingOrder;
	}

	#endregion

	#region Popup.cpp

	// TODO:MZ: Probably not for destructor
	~PopupRoot() => CloseAllPopupsForTreeReset();

	/// <summary>
	/// Disposes all open popups.
	/// </summary>
	private void CloseAllPopupsForTreeReset()
	{
		if (Children != null)
		{
			// DCompTreeHost has a nolist of elements that are being kept visible, and it will iterate over that
			// list of elements. Children of unloading popups can be kept in this list, and we're about to release
			// them all. Remove the unloading children from this list so that the DCompTreeHost doesn't try to iterate
			// over deleted elements on the next tick.
			//TODO:MZ: Remove all unloading children is not needed
			//(m_pChildren.RemoveAllUnloadingChildren(true, GetDCompTreeHost()));
		}

		if (_openPopups is not null)
		{
			var popupsInSafeClosingOrder = GetPopupsInSafeClosingOrder();
			foreach (Popup popup in popupsInSafeClosingOrder)
			{
				popup.ForceCloseForTreeReset();
			}

			//assert that open popup list is now empty
			MUX_ASSERT(_openPopups.Count == 0);
			_openPopups = null;
		}

		if (m_pDeferredPopups)
		{
			CPopupVector.iterator popupItr = m_pDeferredPopups.begin();
			while (popupItr != m_pDeferredPopups.end())
			{
				ReleaseInterface(*popupItr);
				/*VERIFYHR*/
				(m_pDeferredPopups.erase(popupItr));
			}
			delete m_pDeferredPopups;
			m_pDeferredPopups = NULL;
		}
	}

	private void OnHostWindowPositionChanged()
	{
		if (_openPopups != null)
		{
			for (var node = _openPopups.First; node != null; node = node.Next)
			{
				Popup popup = node.Value;
				FxCallbacks.Popup_OnHostWindowPositionChanged(popup);
			}
		}
	}

	private void OnIslandLostFocus()
	{
		foreach (Popup popup in GetPopupsInSafeClosingOrder())
		{
			FxCallbacks.Popup_OnIslandLostFocus(popup);
		}
	}

	/// <summary>
	/// PopupRoot only measures the open popups.
	/// </summary>
	/// <param name="availableSize">Available size.</param>
	/// <returns>Desired size.</returns>
	protected override Size MeasureOverride(Size availableSize)
	{
		Size desiredSize = new();

		if (_openPopups is null)
		{
			// no open popups, nothing to do here.
			return desiredSize;
		}

		_availableSizeAtLastMeasure = availableSize;
		Size childConstraint = new(double.PositiveInfinity, double.PositiveInfinity);
		var node = _openPopups.First;

		while (node is not null)
		{
			Popup popup = node.Value;
			if (!popup.IsUnloading())
			{
				if (popup.m_overlayElement != null)
				{
					popup.m_overlayElement.Measure(childConstraint);
				}

				popup.m_pChild.Measure(childConstraint));
			}
			node = node.Next;
		}

		// Open and measure the popups in the deferred list. These popups were open during
		// template expansion while measuring popups that are already open.
		if (_deferredPopups is not null)
		{
			foreach (var popup in _deferredPopups)
			{
				if (popup is not null)
				{
					if (popup.m_pChild && popup.m_fIsOpen)
					{
						popup.Open();

						if (popup.m_overlayElement != null)
						{
							popup.m_overlayElement.Measure(childConstraint);
						}

						popup.m_pChild.Measure(childConstraint);
					}
				}
			}
		}

		return desiredSize;
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		if (_openPopups is null)
		{
			//no open popups, nothing to do here.
			goto Cleanup;
		}

		var node = _openPopups.First;

		while (node is not null)
		{
			Popup popup = node.Value;
			if (!popup.IsUnloading())
			{
				UIElement pChild = popup.m_pChild;

				double x = popup.m_eHOffset;
				double y = popup.m_eVOffset;

				pChild.EnsureLayoutStorage();

				if (popup.m_overlayElement != null)
				{
					Rect contentBounds = default;
					(GetContext().Fx().DXamlCore_GetContentBoundsForElement(this, &contentBounds));
					popup.m_overlayElement.Arrange(new Rect(0, 0, contentBounds.Width, contentBounds.Height)));
				}

				//Arrange at the HorizontalOffset and VerticalOffset specified on the Popup.
				Rect childRect = new(x, y, pChild.GetLayoutStorage().m_desiredSize.width, pChild.GetLayoutStorage().m_desiredSize.height);
				pChild.Arrange(childRect);

				// For windowed popups, the LTE for the Load transition caused by the Arrange() will be clipped to
				// the Jupiter window, although the popup's window's content is not clipped. This causes an ugly flicker
				// where the clipped LTE is drawn followed by the unclipped popup window's content. Fix this by canceling
				// the Unload transition.
				if (popup.IsWindowed())
				{
					(CTransition.CancelTransitions(pChild));
				}
			}

			node = node.Next;
		}

	Cleanup:
		return finalSize;
	}

	/// <summary>
	/// Adds popup to the list of open popups. Also maintains a strong on the popup.
	/// this is to ensure that the native peer does not go away if it loses all other native
	/// refs, as long as it is open.
	/// </summary>
	/// <param name="popup">Popup</param>
	/// <exception cref="ArgumentNullException">Popup may not be null.</exception>
	private void AddToOpenPopupList(Popup popup)
	{
		if (popup is null)
		{
			throw new ArgumentNullException(nameof(popup));
		}

		MUX_ASSERT(popup.m_pChild && popup.m_fIsOpen);

		if (_openPopups is null)
		{
			_openPopups = new();
		}

		// The popup must not be unloading. If it is, the caller must finish unloading it first.
		MUX_ASSERT(!ContainsOpenOrUnloadingPopup(popup));

		_openPopups.AddLast(popup); // TODO:MZ: AddLast or AddFirst?
									//maintain a on popup so that it wont go away if it does not have
									//any other native ref.
		popup.AddRef();

		// Open popup should not be GC'd. Hold a on the managed peer.
		popup.PegManagedPeer();

		// If app's theme has changed, notify popup
		if (_hasThemeChanged && popup.ShouldPopupRootNotifyThemeChange())
		{
			popup.NotifyThemeChanged(GetContext().GetFrameworkTheming().GetTheme());
		}

		//
		// Child Added - Dirties (Render | Bounds)
		//
		UIElement.NWSetSubgraphDirty(this, DirtyFlags.Render | DirtyFlags.Bounds);

		// We parent CPopup.m_pChild to the CPopupRoot as part of opening a popup, which calls ComputeDepthInOpenPopups
		// via UIElementCollection.OnChildrenChanged, but that wasn't good enough because the CPopup itself hadn't been
		// added to the list of open popups yet. So we make another explicit call here. Note that CPopupRoot renders based
		// on its list of open popups, rather than its UIElement children, so we'll compute depth the same way.

		// 19H1 Bug #20099678 - if 3D is set on the Popup subtree while the Popup is closed, and after Popup.Child is set,
		// the "has depth in subtree" flag won't propagate to Popup or PopupRoot, resulting in broken hit-testing within the Popup.
		// The fix is to first update Popup, then update PopupRoot, here as we're opening the Popup.
		// This somewhat manual sequence guarantees we forcefully recompute the state for both Popup and PopupRoot.
		popup.UpdateHas3DDepthInSubtree();
		UpdateHas3DDepthInSubtree();
	}

	private bool ContainsOpenOrUnloadingPopup(Popup popup)
	{
		if (_openPopups != null)
		{
			var node = _openPopups.First;

			while (node != null)
			{
				if (node.Value == popup)
				{
					return true;
				}
				node = node.Next;
			}
		}

		return false;
	}

	private bool ReplayPointerUpdate()
	{
		int popupsThatReplayedPointerUpdate = 0;
		if (_openPopups != null)
		{
			for (var node = _openPopups.First; node != null; node = node.Next)
			{
				var popup = node.Value;
				if (popup && !popup.IsUnloading())
				{
					bool popupPointerUpdateReplayed = popup.ReplayPointerUpdate();
					if (popupPointerUpdateReplayed)
					{
						popupsThatReplayedPointerUpdate++;
					}
				}
			}

			MUX_ASSERT(popupsThatReplayedPointerUpdate <= 1);
		}

		return popupsThatReplayedPointerUpdate != 0;
	}

	private void RefreshWindowedPopupsPositions()
	{
		if (_openPopups != null)
		{
			for (var node = _openPopups.First; node != null; node = node.Next)
			{
				Popup popup = node.Value;
				if (popup && !popup.IsUnloading() && popup.IsWindowed())
				{
					popup.Reposition();
				}
			}
		}
	}

	/// <summary>
	/// Removes popup from the open popup list and releases the on it.
	/// </summary>
	/// <param name="popup">Popup.</param>
	/// <param name="asyncRelease">Request asynchronous release.</param>
	/// <exception cref="ArgumentNullException">Popup may not be null.</exception>
	private void RemoveFromOpenPopupList(Popup popup, bool asyncRelease)
	{
		if (popup is null)
		{
			throw new ArgumentNullException(nameof(popup));
		}

		if (_openPopups != null
			// The releases that we do in this method could be the final release on the popup, which deletes it. CPopup's dtor
			// calls RemoveChild, which can call CPopup.Close, which calls back here, which can cause us to release the already
			// deleted popup again. Make sure that the popup is actually in the list before releasing it to prevent this reentrancy.
			&& ContainsOpenOrUnloadingPopup(popup)
			)
		{
			_openPopups.Remove(popup);

			if (asyncRelease)
			{
				//async release as this method can be called from a Popup instance method.
				popup.AsyncRelease();
			}
			else
			{
				popup.Release();
			}

			// Popup is closing, we dont need to keep the peer alive anymore.
			popup.UnpegManagedPeer();

			//
			// Child removed - Dirties (Render | Bounds)
			//
			UIElement.NWSetSubgraphDirty(this, DirtyFlags.Render | DirtyFlags.Bounds);

			// We remove CPopup.m_pChild from the CPopupRoot as part of closing a popup, which calls ComputeDepthInOpenPopups
			// via UIElementCollection.OnChildrenChanged, but that wasn't good enough because the CPopup itself hadn't been
			// removed to the list of open popups yet. So we make another explicit call here. Note that CPopupRoot renders based
			// on its list of open popups, rather than its UIElement children, so we'll compute depth the same way.
			UpdateHas3DDepthInSubtree();
		}
	}

	/// <summary>
	/// Add popup to the deferred list. This is for popups opened during template expansion.
	/// These will be opened by popup root after it is done with layout for currently open popups.
	/// </summary>
	/// <param name="popup">Popup.</param>
	/// <exception cref="ArgumentNullException">Popup parameter is required.</exception>
	private void AddToDeferredOpenPopupList(Popup popup)
	{
		if (popup is null)
		{
			throw new ArgumentNullException(nameof(popup));
		}

		if (_deferredPopups is null)
		{
			_deferredPopups = new PopupVector();
		}

		_deferredPopups.AddLast(popup);
		//maintain a on popup so that it wont go away if it does not have
		//any other native ref.
		popup.AddRef();
	}

	/// <summary>
	/// Walk the children of the element finding elements that intersect with the given point.
	/// </summary>
	/// <param name="target">Target point.</param>
	/// <param name="pCallback">Callback.</param>
	/// <param name="hitTestParams">Hit test params.</param>
	/// <param name="canHitDisabledElements">Can hit disabled?</param>
	/// <param name="canHitInvisibleElements">Can hit invisible?</param>
	/// <returns>Hit test result.</returns>
	private BoundsWalkHitResult BoundsTestChildren(
		ref Point target,
		BoundedHitTestVisitor pCallback,
		HitTestParams hitTestParams,
		bool canHitDisabledElements,
		bool canHitInvisibleElements) =>
		BoundsTestChildrenImpl(target, pCallback, hitTestParams, null /*pSubRoot*/, canHitDisabledElements, canHitInvisibleElements, pResult);

	/// <summary>
	/// Walk the children of the element finding elements that intersect
	//  with the given polygon.	
	/// </summary>
	/// <param name="target">Target point.</param>
	/// <param name="pCallback">Callback.</param>
	/// <param name="hitTestParams">Hit test params.</param>
	/// <param name="canHitDisabledElements">Can hit disabled?</param>
	/// <param name="canHitInvisibleElements">Can hit invisible?</param>
	/// <returns>Hit test result.</returns>
	private BoundsWalkHitResult BoundsTestChildren(
		HitTestPolygon target,
		BoundedHitTestVisitor pCallback,
		HitTestParams* hitTestParams,
		bool canHitDisabledElements,
		bool canHitInvisibleElements) =>
		BoundsTestChildrenImpl(target, pCallback, hitTestParams, null /*pSubRoot*/, canHitDisabledElements, canHitInvisibleElements, pResult));

//BoundsTestChildrenImpl(
//	  HitType& target,
//	 CBoundedHitTestVisitor* pCallback,
//	  HitTestParams* hitTestParams,
//	 UIElement* pSubTreeRoot,  // Used to support explicit hit testing against a subtree (i.e. VisualTreeHelper.FindElementsInHostCoordinates)
//	 bool canHitDisabledElements,
//	 bool canHitInvisibleElements,
//	_Out_opt_ BoundsWalkHitResult* pResult

//	)
//	{
//		HRESULT hr = S_OK;
//		BoundsWalkHitResult hitResult = BoundsWalkHitResult.Continue;
//		BoundsWalkHitResult childHitResult = BoundsWalkHitResult.Continue;
//		CPopup* pSubTreeRootPopup = CGetClosestPopupAncestor(pSubTreeRoot);

//		HitTestParams newParams(hitTestParams);

//		if (m_pOpenPopups != NULL)
//		{
//			var core = GetContext();

//			// Test bounds in opened order.
//			for (CXcpList<CPopup>.XCPListNode* pNode = m_pOpenPopups.GetHead(); pNode != NULL && flags_enum.is_set(childHitResult, BoundsWalkHitResult.Continue); pNode = pNode.m_pNext)
//			{
//				CPopup* pPopup = pNode.m_pData;
//				if (!pPopup.IsUnloading())
//				{
//					UIElement* pPopupParent = pPopup.GetUIElementAdjustedParentInternal(false);

//					if (pPopupParent != null)
//					{
//						//
//						// Only test against this popup if:
//						//    A) We are not restricting to a subtree OR
//						//    B) This popup contains the subtree root OR
//						//    C) The subtree contains this popup.
//						//

//						XBOOL isSubTreeInPopup = (pSubTreeRootPopup == pPopup);
//						XBOOL isPopupInSubTree = (pSubTreeRoot != NULL && pPopupParent.IsInUIElementsAdjustedVisualSubTree(pSubTreeRoot));

//						if (pSubTreeRoot == NULL || isSubTreeInPopup || isPopupInSubTree)
//						{
//							HitType popupTarget;
//							XBOOL transformSucceeded = false;

//							XBOOL shouldStartFromSubTreeRoot = (pSubTreeRoot != NULL && !isPopupInSubTree);

//							if (shouldStartFromSubTreeRoot)
//							{
//								transformSucceeded = pSubTreeRoot.PrepareHitTestParamsStartingHere(newParams, popupTarget);
//							}
//							else
//							{
//								transformSucceeded = pPopup.PrepareHitTestParamsStartingHere(newParams, popupTarget);
//							}

//							if (transformSucceeded)
//							{
//								if (shouldStartFromSubTreeRoot)
//								{
//									if (pSubTreeRoot.OfTypeByIndex<KnownTypeIndex.Popup>())
//									{
//										// If pSubTreeRoot is a popup, then its BoundsTestInternal is no-oped out. We want to call into the real implementation in BoundsTestPopup.
//										CPopup* popup = (CPopup*)(pSubTreeRoot);
//										(popup.BoundsTestPopup(popupTarget, pCallback, &newParams, canHitDisabledElements, canHitInvisibleElements, &childHitResult));
//									}
//									else
//									{
//										(pSubTreeRoot.BoundsTestInternal(popupTarget, pCallback, &newParams, canHitDisabledElements, canHitInvisibleElements, &childHitResult));
//									}
//								}
//								else
//								{
//									(pPopup.BoundsTestPopup(popupTarget, pCallback, &newParams, canHitDisabledElements, canHitInvisibleElements, &childHitResult));
//								}

//								// If any child wanted to include its parent chain, copy the flag.
//								if (flags_enum.is_set(childHitResult, BoundsWalkHitResult.IncludeParents))
//								{
//									hitResult = flags_enum = hitResult, BoundsWalkHitResult.IncludeParents;
//								}
//							}
//						}

//						// Check for light dismiss.
//						// If a drag and drop operation is in progress, we allow it to hit test through the light dismiss layer.
//						if (pPopup.m_fIsLightDismiss &&
//							flags_enum.is_set(childHitResult, BoundsWalkHitResult.Continue) &&
//							!core.Fx().DXamlCore_IsWinRTDndOperationInProgress())
//						{
//							(pCallback.OnElementHit(this, target, false /*hitPostChildren*/, &childHitResult));
//						}
//					}
//				}
//			}

//			// If the child element wanted the bounds walk to stop, remove the continue flag
//			// from the result.
//			if (!flags_enum.is_set(childHitResult, BoundsWalkHitResult.Continue))
//			{
//				hitResult = flags_enum.unset(hitResult, BoundsWalkHitResult.Continue);
//			}
//		}

//		if (pResult != NULL)
//		{
//			*pResult = hitResult;
//		}

//	Cleanup:
//		RRETURN(hr);
//	}

//	//------------------------------------------------------------------------
//	//
//	//  Synopsis:
//	//      Public hit test entry point for hit testing against all popups held
//	//      inside this popup root. Only called as a result of a call to
//	//      VisualTreeHelper.FindElementsInHostCoordinates.
//	//
//	//  Notes:
//	//      Target is in absolute coordinates (relative to RootVisual).
//	//
//	//------------------------------------------------------------------------
//	template<typename HitType>
//	 HRESULT
//CPopupRoot.HitTestPopupsImpl(
//	  HitType& target,
//      HitTestParams* hitTestParams,

//	 bool canHitMultipleElements,
//	 UIElement *pSubTreeRoot,
//     bool canHitDisabledElements,

//	 bool canHitInvisibleElements,
//	 CHitTestResults *pHitElements
//    )
//{
//    BoundsWalkHitResult hitResult = BoundsWalkHitResult.Continue;

//	CBoundedHitTestVisitor Visitor(pHitElements, canHitMultipleElements);

//	HitTestParams newParams(hitTestParams);

//	// TODO: HitTest: Consider only updating layout during Tick.
//	(UpdateLayout());

//    // Ensure all bounds in the subgraph are up-to-date for hit-testing.
//    (EnsureOuterBounds(&newParams));

//    // Any active LayoutTransitions in this element's TransitionRoot draw on top of its children, so test them first.
//    CTransitionRoot* transitionRootNoRef = GetLocalTransitionRoot(false /*ensureTransitionRoot*/);
//    if (transitionRootNoRef)
//    {
//        (transitionRootNoRef.BoundsTestInternal(target, &Visitor, &newParams, canHitDisabledElements, canHitInvisibleElements, &hitResult));
//    }

//    if (!flags_enum.is_set(hitResult, BoundsWalkHitResult.Continue))
//    {
//        return S_OK;
//    }

//    // We don't need to transform from the root visual down to the popup root. We'll do that before walking into the popup.
//    // Proceed with hit testing directly.
//    (BoundsTestChildrenImpl(target, &Visitor, &newParams, pSubTreeRoot, canHitDisabledElements, canHitInvisibleElements, null /*pResult*/));

//return S_OK;
//}

////------------------------------------------------------------------------
////
////  Synopsis:
////      Public hit test entry point for hit testing against all popups held
////      inside this popup root. Only called as a result of a call to
////      VisualTreeHelper.FindElementsInHostCoordinates.
////
////  Notes:
////      Target is in absolute coordinates (relative to RootVisual).
////
////------------------------------------------------------------------------
// HRESULT
//CPopupRoot.HitTestPopups(
//	  XPOINTF& target,
//	  HitTestParams* hitTestParams,
//	 bool canHitMultipleElements,
//	 UIElement* pSubTreeRoot,
//	 bool canHitDisabledElements,
//	 bool canHitInvisibleElements,
//	 CHitTestResults* pHitElements
//	)
//{
//	RRETURN(HitTestPopupsImpl(target, hitTestParams, canHitMultipleElements, pSubTreeRoot, canHitDisabledElements, canHitInvisibleElements, pHitElements));
//}

////------------------------------------------------------------------------
////
////  Synopsis:
////      Public hit test entry point for hit testing against all popups held
////      inside this popup root. Only called as a result of a call to
////      VisualTreeHelper.FindElementsInHostCoordinates.
////
////  Notes:
////      Target is in absolute coordinates (relative to RootVisual).
////
////------------------------------------------------------------------------
//HRESULT
//CPopupRoot.HitTestPopups(
//	 HitTestPolygon& target,
//	 HitTestParams* hitTestParams,
//	bool canHitMultipleElements,
//	UIElement* pSubTreeRoot,
//	bool canHitDisabledElements,
//	bool canHitInvisibleElements,
//	CHitTestResults* pHitElements
//   )
//{
//	RRETURN(HitTestPopupsImpl(target, hitTestParams, canHitMultipleElements, pSubTreeRoot, canHitDisabledElements, canHitInvisibleElements, pHitElements));
//}

////------------------------------------------------------------------------
////
////  Synopsis:
////      Get the combined outer bounds of all children.
////
////------------------------------------------------------------------------
//HRESULT
//CPopupRoot.GenerateChildOuterBounds(
//	HitTestParams* hitTestParams,
//   out Rect_RB* pBounds
//   )
//{
//	HRESULT hr = S_OK;
//	XBOOL includeLightDismissCanvasBounds = false;

//	EmptyRectF(pBounds);

//	if (m_pOpenPopups != NULL)
//	{
//		for (CXcpList<CPopup>.XCPListNode* pNode = m_pOpenPopups.GetHead(); pNode != null; pNode = pNode.m_pNext)
//		{
//			Rect_RB childBounds = default;
//			CPopup* pPopup = pNode.m_pData;

//			if (!pPopup.IsUnloading() && pPopup.IsVisible() && pPopup.AreAllAncestorsVisible())
//			{
//				UIElement* popupParent = pPopup.GetUIElementAdjustedParentInternal(false);

//				if (popupParent != null)
//				{
//					// TODO: HWPC: Need a better way to raise dirtiness from child to Popup.
//					// Popups do not get notified when their child's bounds change, because the visual
//					// parent of the Popup's child is the PopupRoot, not the Popup itself.
//					// Assume the Popup's bounds are always dirty, and force them to be recalculated
//					// from the child.
//					pPopup.InvalidateChildBounds();

//					(pPopup.GetOuterBounds(hitTestParams, &childBounds)); // TODO: Enable Transform3D.

//					// Transform bounds through the popups parent chain to get the world space bounds where
//					// the Popup is positioned.
//					(TransformInnerToOuterChain(
//						popupParent,
//						GetUIElementAdjustedParentInternal(false),
//						&childBounds,
//						&childBounds));
//				}
//			}

//			UnionRectF(pBounds, &childBounds);

//			includeLightDismissCanvasBounds |= pPopup.m_fIsLightDismiss;
//		}

//		if (includeLightDismissCanvasBounds)
//		{
//			Rect_RB lightDismissCanvasBounds = { 0, 0, m_availableSizeAtLastMeasure.width, m_availableSizeAtLastMeasure.height };
//			UnionRectF(pBounds, &lightDismissCanvasBounds);
//		}
//	}

//Cleanup:
//	RRETURN(hr);
//}

////------------------------------------------------------------------------
////
////  Synopsis:
////      Prints the popups that are marked for printing.
////
////------------------------------------------------------------------------
//HRESULT
//CPopupRoot.PrintChildren(
//	 SharedRenderParams& sharedPrintParams,
//	 D2DPrecomputeParams& cp,
//	 D2DRenderParams& printParams

//   )
//{
//	HRESULT hr = S_OK;
//	CXcpList<CPopup>* pChildren = NULL;

//	if (m_pOpenPopups)
//	{
//		//get the FIFO ordered list of open popups
//		pChildren = new CXcpList<CPopup>;
//		m_pOpenPopups.GetReverse(pChildren);

//		for (CXcpList<CPopup>.XCPListNode* pNode = pChildren.GetHead(); pNode != NULL; pNode = pNode.m_pNext)
//		{
//			CPopup* pPopup = pNode.m_pData;

//			IFCEXPECT(pPopup && pPopup.m_pChild);
//			// Unloading popups print too
//			if (pPopup.m_fIsPrintDirty)
//			{
//				CMILMatrix popupTransform(true);
//		SharedRenderParams mySharedPrintParams(sharedPrintParams);
//		UIElement* pElement = pPopup;

//		// Get the transform from the popup to the root of the tree.
//		while (pElement != NULL)
//		{
//			CMILMatrix matLocal(false);
//			IGNORERESULT(pElement.GetLocalTransform(TransformRetrievalOptions.None, &matLocal));
//			popupTransform.Add(matLocal);
//			pElement = do_pointer_cast<UIElement>(pElement.GetUIElementAdjustedParentInternal());
//		}

//		if (!popupTransform.IsIdentity())
//		{
//			// Append the transform to the existing transform in print params.
//			if (sharedPrintParams.pCurrentTransform)
//			{
//				popupTransform.Add(*sharedPrintParams.pCurrentTransform);
//			}
//			mySharedPrintParams.pCurrentTransform = &popupTransform;
//		}

//		// Print the child.
//		pPopup.m_fIsPrintDirty = false;
//		(pNode.m_pData.m_pChild.Print(mySharedPrintParams, cp, printParams));
//	}
//}
//    }
//Cleanup:
//if (pChildren)
//{
//	pChildren.Clean(false);
//	delete pChildren;
//}
//RRETURN(hr);
//}

	/// <summary>
	/// Clears the _isPrintDirty flag on all open popups.
	/// </summary>
	/// <exception cref="InvalidOperationException">When null Popup is encountered.</exception>
	private void ClearPrintDirtyFlagOnOpenPopups()
	{
		if (_openPopups is not null)
		{
			// Unloading popups get cleared too
			for (var node = _openPopups.First; node != null; node = node.Next)
			{
				var popup = node.Value;

				if (popup is null)
				{
					throw new InvalidOperationException("Popup must not be null");
				}

				popup._isPrintDirty = false;
			}
		}
	}

	////------------------------------------------------------------------------
	////
	////  Method:   GetOpenPopups
	////
	////  Synopsis:
	////       Gets all open popups, rooted and unrooted.
	////
	////------------------------------------------------------------------------

	//HRESULT
	//CPopupRoot.GetOpenPopups(out Xint* pnCount, out result_buffer_(* pnCount) CPopup ***pppPopups )
	//{
	//	HRESULT hr = S_OK;

	//	CXcpList<CPopup>.XCPListNode* pNode = NULL;
	//	Xint count = 0;
	//	CPopup** ppResults = NULL;

	//	if (!m_pOpenPopups)
	//	{
	//		// no open popups, nothing to do here.
	//		pnCount = 0;
	//		*pppPopups = NULL;
	//		goto Cleanup;
	//	}

	//	pNode = m_pOpenPopups.GetHead();

	//	// get count
	//	while (pNode && count < XINT32_MAX)
	//	{
	//		if (!pNode.m_pData.IsUnloading())
	//		{
	//			count++;
	//		}

	//		pNode = pNode.m_pNext;
	//	}

	//	if (count > 0)
	//	{
	//		pNode = m_pOpenPopups.GetHead();
	//		ppResults = new CPopup*[count];

	//		for (Xint i = 0; i < count && pNode; pNode = pNode.m_pNext)
	//		{
	//			if (!pNode.m_pData.IsUnloading())
	//			{
	//				ppResults[i] = pNode.m_pData;
	//				AddRefInterface(pNode.m_pData);
	//				i++;
	//			}
	//		}
	//	}

	//	*pnCount = count;
	//	*pppPopups = ppResults;
	//	ppResults = NULL;
	//Cleanup:
	//	delete[] ppResults;
	//	RRETURN(hr);
	//}

	////------------------------------------------------------------------------
	////
	////  Method: GetTransitionForChildElementNoAddRef
	////
	////  Synopsis: Children of panel should inherit the child transitions
	////            of its parents.
	////  Note: In the case of this panel being an itemshost we need to
	////  return the itemscontrols child transitions.
	////  Finally note the special casing for contentcontrol.
	////
	////------------------------------------------------------------------------
	//CTransitionCollection* CPopupRoot.GetTransitionsForChildElementNoAddRef(UIElement* pChild)
	//{
	//	// a popup root is a strange beast. It might be the visual parent, but it is not flowing any of the
	//	// transitions. The childtransitions are coming from the popup itself that hosts this pChild.
	//	CPopup* pLogicalParent = do_pointer_cast<CPopup>(pChild.GetLogicalParentNoRef());
	//	if (pLogicalParent && pChild != (UIElement*)(pLogicalParent.m_overlayElement))
	//	{
	//		return pLogicalParent.m_pChildTransitions;
	//	}
	//	return NULL;

	//}

	//HRESULT
	//CPopupRoot.HitTestLocalInternal(
	//	 XPOINTF& target,
	//   out XBOOL* pHit
	//   )
	//{
	//	RRETURN(HitTestLocalInternalImpl(target, pHit));
	//}

	//HRESULT
	//CPopupRoot.HitTestLocalInternal(
	//	 HitTestPolygon& target,
	//   out XBOOL* pHit
	//   )
	//{
	//	RRETURN(HitTestLocalInternalImpl(target, pHit));
	//}

	//template < typename HitType >
	// HRESULT CPopupRoot.HitTestLocalInternalImpl(
	//	  HitType & target,
	//	out XBOOL* pHit)
	//{
	//	HRESULT hr = S_OK;
	//	XBOOL isHit = false;

	//	if (m_pOpenPopups && !m_isRootHitTestingSuppressed)
	//	{
	//		// Hit testing doesn't run on unloading popups.
	//		for (CXcpList<CPopup>.XCPListNode* pNode = m_pOpenPopups.GetHead(); pNode != null; pNode = pNode.m_pNext)
	//		{
	//			CPopup* pPopup = pNode.m_pData;
	//			IFCEXPECT(pPopup);
	//			if (!pPopup.IsUnloading() && pPopup.m_fIsLightDismiss)
	//			{
	//				isHit = true;
	//				break;
	//			}
	//		}
	//	}

	//Cleanup:
	//	*pHit = isHit;
	//	RRETURN(hr);
	//}

	////------------------------------------------------------------------------
	////
	////  Synopsis:
	////      Leaves the CPopupRoot and all its popup children.
	////      The base impl of Leave doesn't walk to the open Popups because they aren't children of the PopupRoot,
	////      and it doesn't call the recursive version of LeavePCScene.
	////
	////------------------------------------------------------------------------
	//HRESULT
	//CPopupRoot.LeaveImpl(DependencyObject* pNamescopeOwner, LeaveParams params)
	//{
	//	HRESULT hr = S_OK;

	//	(CFrameworkElement.LeaveImpl(pNamescopeOwner, params));

	//	if (params.fIsLive)
	//    {
	//		LeavePCSceneSubgraph();
	//	}

	//Cleanup:
	//	RRETURN(hr);
	//}

	////------------------------------------------------------------------------
	////
	////  Synopsis:
	////      Removes the render data for this element's subgraph from the scene.
	////
	////------------------------------------------------------------------------
	//void CPopupRoot.LeavePCSceneSubgraph()
	//{
	//	CleanupAndLeaveSubgraphHelper(true /* forLeave */, false /* cleanupDComp */);
	//}

	/// <summary>
	/// The virtual method which does the tree walk to clean up all
	/// the device related resources like brushes, textures,
	/// primitive composition data etc. in this subgraph.
	/// </summary>
	/// <param name="cleanupDComp">Cleanup.</param>
	private void CleanupDeviceRelatedResourcesRecursive(bool cleanupDComp)
	{
		Panel.CleanupDeviceRelatedResourcesRecursive(cleanupDComp);
		CleanupAndLeaveSubgraphHelper(false /* forLeave */, cleanupDComp);
	}

	//----------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Helper method to do the subgraph walk for device related cleanup
	//      and for leaving pc scene.
	//
	//-----------------------------------------------------------------------------
	private void CleanupAndLeaveSubgraphHelper(bool forLeave, bool cleanupDComp)
	{
		// TODO: INCWALK: The pattern here needs to be kept in sync with the render walk, should be cleaned up
		CXcpList<CPopup>* pPopupRootChildren = null;

		if (m_pOpenPopups is not null)
		{
			pPopupRootChildren = new CXcpList<CPopup>;

			// Get the FIFO ordered list
			m_pOpenPopups.GetReverse(pPopupRootChildren);

			// Includes unloading popups too.
			for (CXcpList<CPopup>.XCPListNode* pNode = pPopupRootChildren.GetHead(); pNode != NULL; pNode = pNode.m_pNext)
			{
				if (forLeave)
				{
					pNode.m_pData.LeavePCSceneRecursive();
				}
				else
				{
					pNode.m_pData.CleanupDeviceRelatedResourcesRecursive(cleanupDComp);
				}
			}
		}

		if (pPopupRootChildren)
		{
			pPopupRootChildren.Clean(false);
			SAFE_DELETE(pPopupRootChildren);
		}
	}

	internal bool CloseTopmostPopup(
		FocusState focusStateAfterClosing,
		PopupFilter filter)
	{
		bool didCloseAPopup = false;

		var popupNoRef = GetTopmostPopup(filter);
		if (popupNoRef != null)
		{
			// Give the popup an opportunity to cancel closing.
			popupNoRef.OnClosing(out var cancel);
			if (cancel)
			{
				return false;
			}

			// Take a for the duration of the SetValue call below, which could otherwise release
			// the last reference to the popup in the middle of the call, where the "this" pointer is
			// still expected to be valid.
			Popup popup = popupNoRef;

			popup.m_focusStateAfterClosing = focusStateAfterClosing;
			popup.SetValue(Popup.IsOpenProperty, false);
			didCloseAPopup = true;
		}

		return didCloseAPopup;
	}

	/// <summary>
	/// Handler for PointerPressed on the CPopupRoot, for light dismiss Popups.
	/// </summary>
	/// <param name="eventArgs">Event args.</param>
	private void OnPointerPressed(EventArgs eventArgs)
	{
		// Make sure we are the original source.  We do not want to handle PointerPressed on the Popup itself.
		RoutedEventArgs routedEventArgs = (RoutedEventArgs)eventArgs;
		if (routedEventArgs.m_pSource == this)
		{
			bool didCloseAPopup = CloseTopmostPopup(FocusState.Pointer, PopupFilter.LightDismissOnly);
			routedEventArgs.Handled = didCloseAPopup;
		}
	}

	/// <summary>
	/// The ESC key closes the topmost light-dismiss-enabled popup.
	/// Handling must be done by PopupRoot because the popups reparent their children to be under PopupRoot,
	/// so routed events from beneanth the popups route to PopupRoot and skip the popups themselves.
	/// </summary>
	/// <param name="pEventArgs">Event args.</param>
	private void OnKeyDown(EventArgs eventArgs)
	{
		KeyEventArgs pKeyEventArgs = (KeyEventArgs)eventArgs;
		if (pKeyEventArgs.m_platformKeyCode == VirtualKey.Escape)
		{
			bool didCloseAPopup = CloseTopmostPopup(FocusState.Keyboard, PopupFilter.LightDismissOrFlyout);
			pKeyEventArgs.Handled = didCloseAPopup;
		}
	}

	private Popup? GetTopmostPopup(PopupFilter filter)
	{
		if (_openPopups is not null)
		{
			// Find the most recently opened Popup that has light dismiss enabled.
			// Unloading popups don't count.
			for (var node = _openPopups.First; node != null; node = node.Next)
			{
				Popup popup = node.Value;
				if (!popup.IsUnloading())
				{
					if (filter == PopupFilter.All ||
						(filter == PopupFilter.LightDismissOrFlyout && popup.IsFlyout()) ||
						popup.m_fIsLightDismiss)
					{
						return popup;
					}
				}
			}
		}

		return null;
	}

	internal bool IsTopmostPopupInLightDismissChain()
	{
		if (_openPopups is not null)
		{
			// Find the most recently opened Popup and check that has light dismiss enabled,
			// or which has a parent that has light dismiss enabled.
			// Unloading popups don't count.
			for (var node = _openPopups.First; node != null; node = node.Next)
			{
				var popup = node.Value;
				if (!popup.IsUnloading())
				{
					return popup.IsSelfOrAncestorLightDismiss();
				}
			}
		}

		return false;
	}

	private DependencyObject? GetTopmostPopupInLightDismissChain()
	{
		Popup? result = null;

		if (_openPopups is not null)
		{
			// Return recently opened Popup if it has light dismiss enabled,
			// or if it has a parent that has light dismiss enabled.
			// Unloading popups don't count.
			for (var node = _openPopups.First; node != null; node = node.Next)
			{
				Popup popup = node.Value;
				if (popup.IsUnloading())
				{
					if (popup.IsSelfOrAncestorLightDismiss())
					{
						result = popup;
					}
					break;
				}
			}
		}

		return result;
	}


	/// <summary>
	/// If the element is an open popup, return that popup
	/// </summary>
	/// <param name="element">Element.</param>
	/// <returns>Popup that contains the element or null.</returns>
	private Popup? GetOpenPopupForElement(UIElement element)
	{
		Popup? result = null;

		// Early exit when there are no open popups
		var popupRoot = VisualTree.GetPopupRootForElement(element);
		if (popupRoot == null || !popupRoot.HasOpenOrUnloadingPopups())
		{
			return result;
		}

		// If the element is in a popup, GetRootOfPopupSubTree returns the popup's subtree root.
		var popupSubtree = element.GetRootOfPopupSubTree();
		if (popupSubtree is not null)
		{
			// The logical parent of the subtree root is the popup
			var popup = popupSubtree.GetLogicalParentNoRef();
			if (popup != null && popup.IsOpen && !popup.IsUnloading())
			{
				result = popup;
			}
		}

		return result;
	}

	/// <summary>
	/// Notify open popups that app's theme has changed
	/// </summary>
	/// <param name="theme">Theme.</param>
	/// <param name="forceRefresh">Whether theme refresh should be forced.</param>
	private void NotifyThemeChanged(Theme theme, bool forceRefresh)
	{
		// An open popup's content is set as child of PopupRoot, and we
		// want to prevent that child from getting the PopupRoot's theme
		// in CDepedencyObject.EnterImpl. So the PopupRoot's theme is never
		// changed. Instead, when a popup is opened, it will propagate the
		// theme to its content.
		MUX_ASSERT(GetTheme() == Theme.None);
		_hasThemeChanged = true;

		if (_openPopups is null)
		{
			//No open popups
			return;
		}

		// Notify open popups
		var node = _openPopups.First;

		while (node is not null)
		{
			Popup popup = node.Value;

			// Notification propagates to unloading popups too
			if (popup.ShouldPopupRootNotifyThemeChange())
			{
				popup.NotifyThemeChanged(theme, forceRefresh);
			}

			node = node.Next;
		}
	}

	private void ClearWasOpenedDuringEngagementOnAllOpenPopups()
	{
		if (_openPopups is null)
		{
			return;
		}

		var node = _openPopups.First;

		// Includes unloading popups too
		while (node is not null)
		{
			node.Value.SetOpenedDuringEngagement(false);
			node = node.Next;
		}
	}

	//std.vector<DependencyObject*> CPopupRoot.GetPopupChildrenOpenedDuringEngagement(
	//	 DependencyObject* element)
	//{
	//	int openPopupsCount = 0;
	//	CPopup** openedPopups = null;
	//	std.vector<DependencyObject*> popupChildrenDuringEngagement;

	//	CPopupRoot* popupRoot = null;
	//	VisualTree.GetPopupRootForElementNoRef(element, &popupRoot);

	//	if (popupRoot == null || /*FAILED*/(popupRoot.GetOpenPopups(&openPopupsCount, &openedPopups)))
	//	{
	//		return std.vector<DependencyObject*>();
	//	}

	//	popupChildrenDuringEngagement.reserve(openPopupsCount);

	//	for (int index = 0; index < openPopupsCount; index++)
	//	{
	//		xref_ptr<CPopup> popup;
	//		popup.attach(openedPopups[index]);
	//		if (popup && popup.WasOpenedDuringEngagement())
	//		{
	//			popupChildrenDuringEngagement.push_back(popup.m_pChild);
	//		}
	//	}

	//	delete[] openedPopups;

	//	return popupChildrenDuringEngagement;
	//}

	//// Returns true if any child of this element has depth, or depth in their subtree.
	//bool CPopupRoot.ComputeDepthInOpenPopups()
	//{
	//	if (m_pOpenPopups != null)
	//	{
	//		// Look for depth in opened popups. Hit testing doesn't run on unloading popups, so ignore those.
	//		for (var node = m_pOpenPopups.GetHead(); node != null; node = node.m_pNext)
	//		{
	//			CPopup* popup = node.m_pData;

	//			if (!popup.IsUnloading())
	//			{
	//				// If there's an LTE targeting a child element, we need to look at whether that LTE has 3D depth as well.
	//				// See LTE comment in UIElement.PropagateDepthInSubtree for the scenario.
	//				if (popup.Has3DDepthOnSelfOrSubtreeOrLTETargetingSelf())
	//				{
	//					return true; // Short circuit if any open popup has depth
	//				}
	//			}
	//		}
	//	}

	//	// Look for depth in layout transition elements.
	//	CTransitionRoot* transitionRootNoRef = GetLocalTransitionRoot(false /*ensureTransitionRoot*/);
	//	if (transitionRootNoRef)
	//	{
	//		if (transitionRootNoRef.Has3DDepthOnSelfOrSubtree())
	//		{
	//			return true;
	//		}
	//	}

	//	return false;
	//}

	#endregion
}
