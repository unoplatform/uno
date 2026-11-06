// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference Popup.cpp (CPopup) and components/elements/Popup.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using DirectUI;
using Uno.Foundation.Extensibility;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.DataBinding;
using Uno.UI.Dispatching;
using Uno.UI.Extensions;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;
using Microsoft.UI.Composition;
using Windows.Graphics;
using Windows.System;
using static Microsoft.UI.Xaml.Controls._Tracing;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class Popup
{
	// TODO Uno: CPopup::~CPopup is WindowedPopupWindow's finalizer. EnsureTransitionTarget, CleanupDeviceRelatedResourcesRecursive: no Skia equivalent.

	// CPopup::SetValue(Popup_IsOpen)
	partial void OnIsOpenChangedPartial(bool oldIsOpen, bool newIsOpen)
	{
		bool fPrevIsOpen = m_fIsOpen;

		m_fIsOpen = newIsOpen;

		if (fPrevIsOpen != m_fIsOpen)
		{
			if (m_pChild is not null)
			{
				// TODO Uno: if (GetStandardNameScopeOwner() == NULL): the namescope owner model is in the open namescope PRs (specs/058).
				if (m_fIsOpen)
				{
					AddChildAsNamescopeOwner();
				}
				else if (ShouldRemoveChildAsNamescopeOwner())
				{
					RemoveChildAsNamescopeOwner();
				}

				// Open or Close the popup
				if (m_fIsOpen)
				{
					Open();
				}
				else
				{
					Close();
				}
			}

			RaisePopupEvent(m_fIsOpen);
		}
	}

	// CPopup::SetValue(Popup_HorizontalOffset, Popup_VerticalOffset)
	private void OnOffsetChanged()
	{
		//mark layout and render dirty is popup placement properties have changed
		if (m_pChild is not null && m_fIsOpen)
		{
			GetAdjustedPopupRoot()?.InvalidateArrange();

			// Re-position the window for a windowed popup because the offsets have changed
			PositionAndSizeWindowForWindowedPopup();
		}
	}

	// CPopup::OnPropertyChanged, called from Popup_Partial's OnPropertyChanged2.
	private void OnPropertyChangedCore(DependencyPropertyChangedEventArgs args)
	{
		if (args.Property == ChildProperty ||
			args.Property == IsLightDismissEnabledProperty ||
			args.Property == LightDismissOverlayModeProperty)
		{
			ReevaluateIsOverlayVisible();
		}
	}

	internal static bool IsElementOrParentEngaged(DependencyObject? element)
	{
		var pElement = element;

		while (pElement is not null)
		{
			if (pElement is Control { IsFocusEngaged: true })
			{
				return true;
			}
			else if (pElement is Popup { WasOpenedDuringEngagement: true })
			{
				// If the element lives in a popup that was opened during engagement, we should still consider the element
				// engaged, although it may live in different root from the actual engaged element

				return true;
			}

			if (pElement is UIElement elementCast)
			{
				pElement = elementCast.GetUIElementAdjustedParentInternal(true /*public parents only*/);
			}
			else
			{
				pElement = pElement.GetParent() as DependencyObject;
			}
		}

		return false;
	}

	/// <summary>
	/// Adds child to the visual tree and itself to the open popup list maintained by popup root.
	/// </summary>
	internal void Open()
	{
		MUX_ASSERT(m_fIsOpen);

#if HAS_UNO
		EnsureXamlRootForOpen();
#endif

		var pPopupRoot = GetAdjustedPopupRoot();

		// When an app is running in an islands-only context, there are times where we can't find
		// a popup root for a given popup.  For example, if an app creates a popup in code and
		// calls Open, we don't know which island it's associated with -- we just fail out here.
		if (pPopupRoot is null)
		{
			// ERROR_POPUP_XAMLROOT_NOT_SET, E_UNEXPECTED
			throw new global::System.Runtime.InteropServices.COMException("Popup.XamlRoot must be set before the popup can be opened.", unchecked((int)0x8000FFFF));
		}

		// If it's been requested that this popup be windowed,
		// then we'll set it to be so at this point before opening.
		SetIsWindowedIfNeeded();

		m_everOpened = true;

		// TODO Uno: SetAssociatedIsland. A popup's XamlRoot is its association, and EnsureWindowForWindowedPopup compares it.

		m_savedFocusState = FocusState.Unfocused;

		if (pPopupRoot is not null)
		{
			var pChild = m_pChild!;

			// TODO Uno: m_pChild->SetAssociated, m_enteredTreeCounter and CTransition::CancelTransitions have no
			// equivalent yet: Uno has neither the association flag nor layout transitions.

			// Cancel animations before flushing pending operations. Canceling an animation requires access to the comp node,
			// which can be destroyed when we finish closing the popup during flush.
			// TODO Uno: CancelHideAnimationToPrepareForShow(): needs UIElement.ShowHide implicit animations, UIElement.Hidden and CUIElement::ComputeKeepVisible.

			FlushPendingKeepVisibleOperations();

			// Create window and ContentIsland for windowed popups. Do this before adding the child to the tree. If the child has a
			// ScrollViewer in its subtree, then the ScrollViewer will look for this windowed popup's IslandInputSite to use for
			// DManip, and the IslandInputSite comes from the popup window bridge's ContentIsland.
			if (IsWindowed)
			{
				// TODO Uno: EnsureDCompResourcesForWindowedPopup(): a window's graphics are set up along with the window.

				// If the window was not created, early-out.
				// TODO Uno: WinUI leaves the popup closed, as its window can only fail when the island is closing.
				// Here it can also fail for lack of a transparent surface, so the popup opens inside the main window.
				if (!EnsureWindowForWindowedPopup())
				{
					m_isWindowed = false;
					m_windowCreationFailed = true;
				}
			}

			// Initial phase of popup addition to open popup list: just add the popup to the m_pOpenPopups list and add-ref it.
			// This phase is performed *before* the AddChild call just below so that CUIElement::EnterImpl for any
			// ScrollViewer finds the correct IslandInputSite to hand off to DManip through GetElementIslandInputSite().
			pPopupRoot.StartAdditionToOpenPopupList(this);

#if HAS_UNO
			// When a popup is reused across different windows (e.g., shared TextCommandBarFlyout), its child may retain stale
			// VisualTreeCache entries from the previous window. WinUI does this in CDependencyObject::EnterImpl.
			if (pPopupRoot.GetVisualTree() is { } targetTree)
			{
				EnsureVisualTreeOnSubtree(pChild, targetTree);
			}

			// The popup root renders and hit tests the child through the popup's transform chain.
			UpdateVisualTransformParent(pPopupRoot);
			pChild.Visual.TransformParent = Visual;

			// A windowed popup's visuals go to its window (hwwalk.cpp, HWWindowedPopupCompTreeNode). So do those of an inline
			// popup nested in one, or the windowed popup would cover them (HWCompTreeNodeWinRT.cpp:3471).
			pChild.Visual.HasOwnSurface = IsWindowed || GetFirstAncestorPopup(windowedPopupOnly: true) is not null;
#endif

			try
			{
				pPopupRoot.Children.Add(pChild);
			}
			catch
			{
				// Undo the addition performed by the StartAdditionToOpenPopupList call above.
				pPopupRoot.UndoAdditionToOpenPopupList(this);
				throw;
			}

			if (m_isOverlayVisible)
			{
				AddOverlayElementToPopupRoot();
			}

			// Final phase of popup addition to open popup list: peg managed peer, updates themes and rendering trees.
			pPopupRoot.CompleteAdditionToOpenPopupList(this);

			if (IsWindowed)
			{
				ShowWindowForWindowedPopup();
			}

			// TODO Uno: StartComposition(), m_wasMarkedAsRedirectionElementOnOpen: no redirection in Uno's visual tree.

			var pFocusManager = VisualTree.GetFocusManagerForElement(this);
			DependencyObject? pFocusedElement = null;
			if (pFocusManager is not null)
			{
				pFocusedElement = pFocusManager.FocusedElement;

				if (pFocusedElement is not null)
				{
					m_wasOpenedDuringEngagement = IsElementOrParentEngaged(pFocusedElement);
				}
			}

			if (IsLightDismissEnabled || IsFlyout)
			{
				if (IsLightDismissEnabled)
				{
					m_fIsLightDismiss = true;

					// Mark the PopupRoot as dirty to redraw the light dismiss layer.
					pPopupRoot.UpdateHitTest();
				}

				ManagedWeakReference? pFocusedElementWeakRef = null;
				if (pFocusedElement is not null && pFocusManager is not null)
				{
					pFocusedElementWeakRef = WeakReferencePool.RentWeakReference(this, pFocusedElement);
					m_savedFocusState = pFocusManager.GetRealFocusStateForFocusedElement();
				}

				m_pPreviousFocusWeakRef = null;

				// We still want to save the previous focus element and the focus state. That is why we are
				// only checking for our state flag at the last step of taking focus.
				//Only set focus if the child element does not have AllowFocusOnInteraction set to false
				if (m_shouldTakeFocus && FocusSelection.ShouldUpdateFocus(pChild, m_savedFocusState))
				{
					// TODO Uno: InitialFocusSIPSuspender(pFocusManager).
					Focus(FocusState.Programmatic);
				}

				m_pPreviousFocusWeakRef = pFocusedElementWeakRef;

				if (pFocusManager is not null)
				{
					m_savedXYFocusManifolds = pFocusManager.XYFocus.ResetManifolds();
				}
			}

			//
			// Opening - Dirties (Render | Bounds)
			//
			// TODO Uno: InvalidateChildBounds(): Uno computes hit-test bounds during the walk, there's nothing to invalidate.

			m_isUnloading = false;
		}

		// Ensure compNode if the public root is canvas. Here is for the case
		// that parentless popups are set shadow before they are opened
		// TODO Uno: EnsureRootCanvasCompNode() has no equivalent; Popup.SystemBackdrop has none either, see ApplyRootRoundedCornerClipToSystemBackdrop.
	}

	internal void ForceCloseForTreeReset()
	{
		// CPopupRoot::CloseAllPopupsForTreeReset should have cleared out all its unloading children and called
		// FlushPendingKeepVisibleOperations on them before getting here, but CPopupRoot's unloading children
		// are the children of popups. The popup itself can still be pending, and needs to be cleaned up here.
		//
		// Note that a popup child's FlushPendingKeepVisibleOperations can call into the popup if the popup had
		// removed the child and placed it in m_unloadingChild, but if the popup itself was closed, then the
		// child is still in m_pChild and FlushPendingKeepVisibleOperations will not propagate up to the popup.
		MUX_ASSERT(m_fIsOpen || (!m_fIsOpen && m_closeIsPending));

		if (m_fIsOpen)
		{
			m_fIsOpen = false;
			Close(true /* forceClose */);

#if HAS_UNO
			// The field and the property are one and the same in WinUI: this doesn't reach Close() again.
			IsOpen = false;

			// As in WinUI, this close raises no Closed. There, a flyout still learns of it from its presenter's Unloaded.
			NativeDispatcher.Main.Enqueue(() => ForceClosed?.Invoke(this, EventArgs.Empty), NativeDispatcherPriority.Normal);
#endif
		}
		else
		{
			FlushPendingKeepVisibleOperations();
		}
	}

	/// <summary>
	/// Remove child from the visual tree and itself from the open popup list maintained by popup root.
	/// </summary>
	internal void Close(bool forceCloseforTreeReset = false)
	{
		//Fix bug: 16797776, reentrancy to Close() may occur in
		//some rare cases
		if (m_isClosing)
		{
			return;
		}

		m_isClosing = true;
		try
		{
			CloseCore(forceCloseforTreeReset);
		}
		finally
		{
			m_isClosing = false;
		}
	}

	private void CloseCore(bool forceCloseforTreeReset)
	{
		m_windowCreationFailed = false;

		bool wasFocused = false;
		var pChild = m_pChild!;

		// TODO Uno: if (!m_isImplicitClose) RequestKeepAliveForHiddenEventHandlersInSubtree(this): needs UIElement.Hidden.

		// TODO Uno: !forceCloseforTreeReset && ComputeKeepVisible() needs implicit Hide animations and UIElement.Hidden.
		bool keepVisible = false;

		// Can't close an unloading popup. The pending close should be completed first, then the popup should be opened again.
		MUX_ASSERT(!m_isUnloading);

		// Implicit hide animation notes:
		// If we detect an implicit hide animation in this subtree, we need to do a number of things
		// to properly play the animation.  Those things are controlled by the keepVisible flag.
		// When the animation completes, we'll finish closing in FlushPendingKeepVisibleOperations();
		if (keepVisible)
		{
			m_closeIsPending = true;
		}

		// TODO Uno: SystemBackdrop::InvokeOnTargetDisconnectedFromCore, DiscardContentExternalBackdropLink, see ApplyRootRoundedCornerClipToSystemBackdrop.

		var pPopupRoot = GetAdjustedPopupRoot();
		if (pPopupRoot is not null)
		{
			// normally we would get a live leave, in the case of a popup closing or opening, we do not do that.
			// TODO Uno: CTransition::CancelTransitions(m_pChild), m_leftTreeCounter: no layout transitions.

			// Note: even in keepVisible scenarios, we take the child out of the popup root's child collection. That's okay,
			// because the popup root renders and hit tests via its list of open popups, and not via its child collection.
			//
			// If the child has KeepVisible requirements, then this will keep it visible via CUIElementCollection.
			pPopupRoot.Children.Remove(pChild);

#if HAS_UNO
			pChild.Visual.TransformParent = null;
			pChild.Visual.HasOwnSurface = false;
			Visual.TransformParent = null;
#endif

			// set the association flag, visual link is no longer present
			// TODO Uno: no association flag.

			// For windowed popups, the LTE for the Unload transition caused by the RemoveChild() will be clipped to
			// the Jupiter window, although the popup's window's content is not clipped. This causes an ugly flicker
			// where the clipped LTE is drawn followed by the unclipped popup window's content. Fix this by canceling
			// the Unload transition.
			// TODO Uno: no layout transitions.

			// RemoveFromOpenPopupList() will release the last reference to this Popup and will unpeg its managed peer.
			// This will cause problems if there are unloading transitions, since render walk will call GetLogicalParentNoRef()
			// on the child which will return an invalid pointer to this popup.
			// Thus, we need to keep this popup alive until the child finishes unloading.
			// TODO Uno: UC_ReleaseParentPopup: no unloading storage.

			if (m_isOverlayVisible)
			{
				RemoveOverlayElementFromPopupRoot();
			}

			if (keepVisible)
			{
				// Keep this popup in the opened list, but mark it as unloading. It will continue to render.
				m_isUnloading = true;
			}
			else
			{
				pPopupRoot.RemoveFromOpenPopupList(this);
			}

			m_wasOpenedDuringEngagement = false;

			// If the Popup was marked for composition upon opening, unmark is when it closes.
			// This handles parentless Popups that were opened and closed without ever entering the tree.
			// TODO Uno: StopComposition(): no redirection in Uno's visual tree.

			// calls the OnClosed callback function for the Popup_Partial class
			// this was done to invoke a reverse change in the close property
			OnClosed();
		}

		// The Popup needs to be removed from the scene, since it will no longer be walked.
		// TODO Uno: LeavePCScene(): Uno's render walk follows the visual tree, the child already left it.

		if (pPopupRoot is not null && !keepVisible)
		{
			// Hide the popup, but only after LeavePCScene is called. This is because HideWindowForWindowedPopup
			// calls ReleaseDCompResourcesForWindowedPopup which would remove visuals from the tree that LeavePCScene
			// needs to access.
			HideWindowForWindowedPopup();
		}

		var pFocusManager = VisualTree.GetFocusManagerForElement(this);

		if (m_fIsLightDismiss || IsFlyout)
		{
			// If this popup still contains focus, or if nothing now has focus, try to return focus to the item
			// that was focused before opening the light-dismiss-enabled popup. If that fails, try to clear FocusManager's focus.
			if (pFocusManager is not null &&
				(pFocusManager.FocusedElement is null ||
				 pFocusManager.FocusedElement == this ||
				 HasFocusedElementAsChild()))
			{
				// Determine the FocusState we should use.
				var focusState = m_focusStateAfterClosing;
				if (focusState == FocusState.Unfocused)
				{
					// The popup was not light-dismissed, so use the saved FocusState.
					focusState = m_savedFocusState;

					if (focusState == FocusState.Unfocused)
					{
						focusState = FocusState.Programmatic;
					}
				}

				// #1. Try to focus the previously focused element that we stored a weakref to.
				if (m_pPreviousFocusWeakRef is not null)
				{
					var pPreviousFocus = m_pPreviousFocusWeakRef.Target as DependencyObject;
					if (pPreviousFocus is not null && FocusProperties.IsFocusable(pPreviousFocus))
					{
						if (pPreviousFocus is UIElement previousFocusElement)
						{
							// Scenario where proofing menu is light-dismissed and focus is going back to floatie: change focusState to keyboard since the previously focused floatie
							// child element has AllowFocusOnInteraction set to false
							if (focusState == FocusState.Pointer && Internal.TextControlFlyoutHelper.IsElementChildOfOpenedFlyout(previousFocusElement))
							{
								focusState = FocusState.Keyboard;
							}

							var movement = new FocusMovement(pPreviousFocus, FocusNavigationDirection.None, focusState) { IsForLightDismiss = true };
							wasFocused = pFocusManager.SetFocusedElement(movement).WasMoved;
						}
						else if (FocusableHelper.IsFocusableDO(pPreviousFocus))
						{
							var movement = new FocusMovement(pPreviousFocus, FocusNavigationDirection.None, focusState) { IsForLightDismiss = true };
							wasFocused = pFocusManager.SetFocusedElement(movement).WasMoved;
						}
					}
				}

				// #2. Try to focus the first focusable element
				if (!wasFocused)
				{
					var pFirstFocusableElement = pFocusManager.GetFirstFocusableElementFromRoot(false);
					if (pFirstFocusableElement is not null)
					{
						// TODO Uno: InitialFocusSIPSuspender(pFocusManager).
						var movement = new FocusMovement(pFirstFocusableElement, FocusNavigationDirection.None, focusState) { IsForLightDismiss = true };
						wasFocused = pFocusManager.SetFocusedElement(movement).WasMoved;
					}
				}

				// #3. Clear focus, so the CPopup doesn't keep focus.
				if (!wasFocused)
				{
					pFocusManager.ClearFocus();
				}

				pFocusManager.XYFocus.SetManifolds(m_savedXYFocusManifolds);
				m_savedXYFocusManifolds.Reset();
			}

			m_focusStateAfterClosing = FocusState.Unfocused;
			m_fIsLightDismiss = false;
			m_savedFocusState = FocusState.Unfocused;

			// Mark the PopupRoot as dirty to clear the light dismiss layer, if necessary.
			pPopupRoot?.UpdateHitTest();
		}

		//
		// Closing - Dirties (Render | Bounds)
		//
		// TODO Uno: InvalidateChildBounds(): Uno computes hit-test bounds during the walk, there's nothing to invalidate.

		// The PopupAutomationPeer will be null from calling UIElement::GetOrCreateAutomationPeer() if Popup
		// is the closed state and the existing PopupAutomationPeer will invalidate the owner from
		// CUIElement::OnCreateAutomationPeer() that shouldn't be referenced from other AutomationPeer.
		// The below calling SetAPParent(null) ensures the disconnect the relationship between PopupAutomationPeer
		// and Popup child's AutomationPeer when Popup is closed.
		{
			var pChildAP = pChild.GetExistingAutomationPeer();

			if (pChildAP is not null && pChildAP.HasParent())
			{
				pChildAP.SetParent(null);
			}
		}
	}

	internal bool WindowedPopupHasFocus()
	{
		MUX_ASSERT(IsWindowed);
		return m_windowedPopupWindow?.Site.HasFocus ?? false;
	}

	// TODO Uno: SkipRenderForLayoutTransition(): no layout transitions.

	internal void FlushPendingKeepVisibleOperations()
	{
		// TODO Uno: __super::FlushPendingKeepVisibleOperations() needs implicit Hide animations on UIElement.

		// This popup could have been kept visible because of Visibility="Collapsed", in which case there's no pending
		// close operation and there's nothing to do.
		if (m_closeIsPending)
		{
			var popupRoot = GetAdjustedPopupRoot();
			MUX_ASSERT(popupRoot is not null);

			popupRoot?.RemoveFromOpenPopupList(this);
			m_closeIsPending = false;

			// TODO Uno: LeavePCScene(), StopComposition().

			HideWindowForWindowedPopup();
		}
	}

	//static
	internal static bool DoesPlatformSupportWindowedPopup()
	{
		// CoreWindowSiteBridge doesn't support creating a PopupSiteBridge, which means we can't implement windowed popups
		// in UWPs. Note that only some tests run in UWP mode. WinUI 3 only runs in islands mode out in the wild.
		// TODO Uno: the equivalent is a host registering IWindowedPopupExtension.
		return GetWindowedPopupExtension() is not null;
	}

	private static IWindowedPopupExtension? s_windowedPopupExtension;

	internal static IWindowedPopupExtension? WindowedPopupExtensionForTesting
	{
		get => GetWindowedPopupExtension();
		set => s_windowedPopupExtension = value;
	}

	internal static bool GetIsRightHandedFromWindowedPopupExtension() => GetWindowedPopupExtension()?.IsRightHanded ?? false;

	private static IWindowedPopupExtension? GetWindowedPopupExtension()
	{
		if (s_windowedPopupExtension is null)
		{
			ApiExtensibility.CreateInstance<IWindowedPopupExtension>(typeof(Popup), out s_windowedPopupExtension);
		}

		return s_windowedPopupExtension;
	}

	private void SetIsWindowedIfNeeded()
	{
		var value = ShouldConstrainToRootBounds;

		if (!value || IsWindowed)
		{
			// Before making the popup windowed, run a set of tests that determine if we can correctly compute
			// the bounds for the popup's island.  If these requirements aren't met, don't actually make it windowed.
			// Note that these requirements are re-evaluated every time the popup is opened, so it's possible for
			// the popup to toggle between windowed/non-windowed due to these requirements.  However it's not possible
			// for an app to toggle the popup between windowed/non-windowed via the ShouldConstrainToRootBounds property
			// (see enforcement of this in Popup::OnPropertyChanged2()).
			if (MeetsRenderingRequirementsForWindowedPopup())
			{
				if (!IsWindowed)
				{
					SetIsWindowed();
				}
			}
			else
			{
				m_isWindowed = false;
			}
		}
	}

	// Set popup to use a window, so its content is not clipped to the Jupiter window.
	internal void SetIsWindowed()
	{
		MUX_ASSERT(!m_isWindowed);

		// Ignore on unsupported platforms, so controls can use CPopup::IsWindowed to determine whether to
		// fall back to non-windowed placement.
		if (DoesPlatformSupportWindowedPopup() && !m_windowCreationFailed)
		{
			// TODO Uno: an unknown root is settled when the popup opens: EnsureWindowForWindowedPopup falls back to in-window.
			if ((XamlRoot ?? m_pChild?.XamlRoot) is not { } xamlRoot || GetWindowedPopupExtension()!.IsSupported(xamlRoot))
			{
				m_isWindowed = true;
			}
		}
	}

	internal void SetPointerCapture() => m_windowedPopupWindow?.Site.SetPointerCapture();

	internal void ReleasePointerCapture() => m_windowedPopupWindow?.Site.ReleasePointerCapture();

	internal bool WindowHasPointerCapture()
	{
		if (m_windowedPopupWindow is { } window)
		{
			return window.Site.HasPointerCapture;
		}

		return false;
	}

	// Create windowed popup's window, if it doesn't exist
	private bool EnsureWindowForWindowedPopup()
	{
		MUX_ASSERT(IsWindowed);

		//
		// It's possible this windowed popup has moved between islands since it last closed. Xaml uses a shared context
		// menu for all TextBoxes, for example, so we need to check that this windowed popup is still anchored to the same
		// island. If not, release all the window-related resources and re-create them.
		//
		var xamlRoot = XamlRoot;

		if (!ReferenceEquals(m_previousXamlRoot, xamlRoot))
		{
			// The PopupWindowSiteBridge needs to be explicitly IClosable::Close()ed, otherwise it will leak. This is because
			// the bridge's underlying hwnd has a ref count on it, and until we destroy that hwnd (via IClosable::Close)) the
			// bridge will always be kept alive.
			// Bug 43723959 - Remove this once the underlying problem is fixed
			EnsureBridgeClosed();

			m_previousXamlRoot = xamlRoot;
		}

		// The window can be gone without the popup knowing, when its owner closed.
		if (m_windowedPopupWindow is { Site.IsClosed: true })
		{
			EnsureBridgeClosed();
		}

		if (m_windowedPopupWindow is null)
		{
			if (xamlRoot is null || GetWindowedPopupExtension() is not { } extension)
			{
				return false;
			}

			// TODO Uno: PopupAnchor_ParentIsland, "Turn on anchoring for the popup so that it follows the top level window":
			// the host owns this, by calling IWindowedPopupContent.OnOwnerBoundsChanged.
			Microsoft.UI.Xaml.Media.CompositionTarget.WindowedPopupSurface surface = new(this);
			if (extension.TryCreateSite(xamlRoot, surface) is not { } site)
			{
				return false;
			}

			surface.Site = site;
			m_windowedPopupWindow = new(site, surface);

			// Force an update of the input offset to make sure it is initialized to something and
			// propogated to the inputsite adapter.
			UpdateTranslationFromContentRoot(default, forceUpdate: true);
		}

		return true;
	}

	// frameworkClosedCallback: "When the bridge unexpectedly closes we should probably do more cleanup".
	// TODO Uno: WinUI's tree reset closes the popup when its island goes away. A site's owner window dying goes the same way here.
	internal void OnWindowedPopupSiteClosed()
	{
		if (m_fIsOpen)
		{
			ForceCloseForTreeReset();
		}
	}

	private void EnsureBridgeClosed()
	{
		if (m_windowedPopupWindow is { } window)
		{
			m_windowedPopupWindow = null;
			window.Close();
		}
	}

	// TODO Uno: GetIslandInputSite, EnsureContentExternalBackdropLink, DiscardContentExternalBackdropLink,
	// GetWindowedPopupRootVisualScaleCompensation, GetPreviousPointerPoint: no input sites, nor a ContentIsland with a
	// rasterization scale of its own.

	internal bool GetShouldShowKeyboardCues()
	{
		// TODO Uno: IInputFocusController3.ShouldShowKeyboardCues is about the island's input focus controller, which has no
		// equivalent, and FlyoutBase only asks in a XamlIslandRoot.
		return false;
	}

	// Show windowed popup's window
	private void ShowWindowForWindowedPopup()
	{
		if (m_windowedPopupWindow is not { } window)
		{
			return;
		}

		XamlRoot?.VisualTree.ContentRoot.CompositionTarget.AddWindowedPopupSurface(window.Surface);
		window.Site.Show();
	}

	// Hide windowed popup's window
	private void HideWindowForWindowedPopup()
	{
		if (m_windowedPopupWindow is { } window)
		{
			window.Site.Hide();

			// In case this popup has a cached point, closes, then opens again, we don't want it to replay that cached
			// point. When the parent island for this popup gets a pointer message, it'll clear the cached point on all
			// _open_ popups. Since this popup is closed, it won't be cleared, so make sure it's not caching a point while
			// it's closed.
			ClearLastPointerPointForReplay();

			// ReleaseDCompResourcesForWindowedPopup
			XamlRoot?.VisualTree.ContentRoot.CompositionTarget.RemoveWindowedPopupSurface(window.Surface);
			window.Surface.DropFrame();

			// Note: We don't close or reset the bridge here. ScrollViewers in the popup subtree have already cached the popup
			// bridge's input hwnd for DManip. We don't currently have a code path for those elements to reset the input hwnd
			// and pick up a new one.
		}
	}

	internal void Reposition() => PositionAndSizeWindowForWindowedPopup();

	// TODO Uno: WindowedPopupInputSiteAdapter's replay. A popup's window hands its input to the owner's pointer source, so
	// there is no cached point per popup, and nothing asks for a replay: Uno has no CCoreServices::RequestReplayPreviousPointerUpdate.
	internal void ClearLastPointerPointForReplay()
	{
	}

	internal bool ReplayPointerUpdate() => false;

	// Position windowed popup's window
	private void PositionAndSizeWindowForWindowedPopup()
	{
		// Ignore if not windowed
		if (m_windowedPopupWindow is not { } window)
		{
			return;
		}

		// It's possible for Reposition to be called on a Popup after it's been closed, or no longer has a child set.  No-op in these cases as well.
		if (!m_fIsOpen || m_pChild is null || window.Site.IsClosed || XamlRoot is not { } xamlRoot)
		{
			return;
		}

		// Popup child's bounds may have changed and the popup's window needs to be
		// sized to the new bounds.
		// TODO Uno: InvalidateChildBounds(), EnsureOuterBounds(): Uno computes bounds during the walk.

		// Get popup's bounds relative to the CoreWindow or Win32 app bounds
		// Note: The returned popupBoundsPhysical will have the plateau zoom scale applied
		var popupBoundsPhysical = GetPhysicalBounds(this, out var scaleX, out var scaleY);

		// Find the offset to the root in physical pixels.  This is either XAML's main CoreWindow,
		// or the associated XamlIslandRoot.
		var rootOffsetPhysical = GetWindowedPopupExtension()!.GetScreenOffset(xamlRoot);
		m_rootOffsetPhysical = rootOffsetPhysical;

		// Compute Popup window's position
		var popupWindowLeft = XcpCeiling(popupBoundsPhysical.X);
		var popupWindowTop = XcpCeiling(popupBoundsPhysical.Y);

		// Note - even if IsRightToLeft() is true, no further adjustments to popupWindowLeft are necessary. The
		// GetPhysicalBounds() call earlier already returned the LTWH rect, so we have the location of the left edge
		// even though the origin refers to the top-right.

		popupWindowLeft += rootOffsetPhysical.X;
		popupWindowTop += rootOffsetPhysical.Y;

		var popupWindowWidth = XcpCeiling(popupBoundsPhysical.Width);
		var popupWindowHeight = XcpCeiling(popupBoundsPhysical.Height);

		RectInt32 contentBounds = new(popupWindowLeft, popupWindowTop, popupWindowWidth, popupWindowHeight);

		if (!ShouldPopupRendersDropShadow())
		{
			// Set position and size of popup's window. Increase size enough to show sub pixel positioned content.

			m_windowedPopupMoveAndResizeRect = contentBounds;

			// TODO Uno: PauseNewDispatch around MoveAndResize. It guards against a third party's window hook pumping messages
			// from WM_WINDOWPOSCHANGED, and a pump here only runs what the host's message loop would. No test shows a problem.
			window.Site.MoveAndResize(m_windowedPopupMoveAndResizeRect, contentBounds);
		}

		//
		// We need to put some more transforms in the tree to make sure the popup content renders at the correct x/y location.
		//
		// TODO Uno: the undo transform and the RTL flip of the original aren't needed. WinUI gives the popup's subtree a tree
		// of Visuals of its own, inside the bridge which carries the popup's offset, so the popup's transforms count twice
		// unless they're undone. Here Visual.TransformParent makes a-b-p-c the total matrix of the popup's child, in root
		// coordinates, and CompositionTarget.WindowedPopupSurface cuts the window's rectangle out of those. It is what
		// the "undo" arrives at. Scales from ancestors and the RTL flip are part of that matrix as well.
		//
		_ = scaleX;
		_ = scaleY;

		AdjustWindowedPopupBoundsForDropShadow(new Rect(popupWindowLeft, popupWindowTop, popupWindowWidth, popupWindowHeight), contentBounds);

		// The system backdrop visual is placed based on the MoveAndResize rect. Update it as well.
		ApplyRootRoundedCornerClipToSystemBackdrop();

		// Tell the windowed popup inputsite adapter the offset of the popup window from the content
		// root. We do this by using the position passed to MoveAndSize (either earlier in this function
		// or updated by AdjustWindowedPopupBoundsForDropShadow), converted to island coords in dips.
		UpdateTranslationFromContentRoot(new global::Windows.Foundation.Point(
			PhysicalPixelsToDips(m_windowedPopupMoveAndResizeRect.X - rootOffsetPhysical.X),
			PhysicalPixelsToDips(m_windowedPopupMoveAndResizeRect.Y - rootOffsetPhysical.Y)));

		// TODO Uno: "Play the MenuPopupThemeTransition, if we have one": no secret transition target, no layout transitions.

		// Note: The Shadow is drawn by HWCompTreeNodeWinRT. There's no work needed aside from the offset adjustment that's already happening.
	}

	//
	// If there is a ContentExternalBackdropLink drawing a system backdrop, and this popup has a rounded corner clip on it,
	// then we also need to put a rounded corner clip on the system backdrop Visual. Otherwise its square corners will be
	// visible behind the translucent shadows in the four corners.
	//
	// TODO Uno: there is no Popup.SystemBackdrop to clip. A DWM system backdrop covers the whole HWND, gutter included, and
	// Win32 has no equivalent of a ContentExternalBackdropLink's placement visual, so the backdrop can't be fitted to the
	// presenter under the shadow. Flyouts draw their acrylic fallback instead.
	//
	private void ApplyRootRoundedCornerClipToSystemBackdrop()
	{
	}

	private Rect GetPhysicalBounds(UIElement element, out float scaleX, out float scaleY)
	{
		Rect physicalBounds;
		var elementForBounds = element;
		var elementForPosition = element;
		Rect oneByOneTransformed = new(0, 0, 1, 1);

		// RuntimeEnabledFeature::WindowedPopupActualBoundsMode is off by default, and nothing turns it on.
		const bool useActualBounds = false;

		if (element is Popup popup)
		{
			elementForBounds = popup.m_pChild!;
			elementForPosition = popup.m_pChild!;
		}

		_ = useActualBounds;

		// The current policy is to compute the bounds of the bridge according to the following:
		// 1) The size of the bridge is based on Popup.Child's unclipped desired size, if set.
		// 2) The position of the bridge is based on Popup.Child's position.
		// This policy has caused numerous bugs and will be harder for public customers to understand.
		// If we can convert all controls to adhere to the "UseActualBounds" policy above, we can delete this code path.
		if (elementForBounds.m_unclippedDesiredSize is { Width: > 0, Height: > 0 } unclippedDesiredSize)
		{
			physicalBounds = TransformToRoot(elementForPosition, new Rect(0, 0, unclippedDesiredSize.Width, unclippedDesiredSize.Height));
			physicalBounds = DipsToPhysicalPixelsRect(physicalBounds);

			oneByOneTransformed = TransformToRoot(elementForPosition, oneByOneTransformed);
		}
		else
		{
			physicalBounds = GetGlobalBounds(elementForBounds);
			physicalBounds = DipsToPhysicalPixelsRect(physicalBounds);
		}

		// Note: These assume that the transform was axis-aligned. We don't support windowed popups with rotations above them.
		scaleX = (float)oneByOneTransformed.Width;
		scaleY = (float)oneByOneTransformed.Height;

		// In addition to the contents in the windowed popup, also union in all the non-windowed popups that are nested
		// inside this windowed popup's. They must live in the same island as the windowed popup so that they won't be
		// covered by the windowed popup, and their bounds weren't included when we bounded the popup. See
		// CUIElement::GenerateChildOuterBounds - we explicitly exclude popups from bounds calculations.
		var popupRoot = GetAdjustedPopupRoot();
		if (popupRoot is not null)
		{
			foreach (var openPopup in popupRoot.GetOpenPopups())
			{
				// We're looking for a non-windowed pop nested directly inside this windowed popup. If there are multiple
				// layers of windowed popups nested inside each other, then the non-windowed popup should live in the
				// innermost windowed popup's island.
				if (!openPopup.IsWindowed)
				{
					var ancestorWindowedPopup = openPopup.GetFirstAncestorPopup(windowedPopupOnly: true);
					if (ancestorWindowedPopup == this && openPopup.m_pChild is { } nestedChild)
					{
						var popupBounds = GetGlobalBounds(nestedChild);
						popupBounds = DipsToPhysicalPixelsRect(popupBounds);
						physicalBounds = RectHelper.Union(physicalBounds, popupBounds);
					}
				}
			}
		}

		return physicalBounds;
	}

	// CUIElement::TransformToVisual(nullptr), which on Skia is this same matrix: a popup's window has to be where the visual draws.
	private static Rect TransformToRoot(UIElement element, Rect rect)
		=> rect.TryTransformBounds(element.Visual.TotalMatrix, out var bounds) ? bounds : default;

	// CUIElement::GetGlobalBounds(ignoreClipping: true) and AdjustBoundingRectToRoot: the bounds of an element and its
	// subtree in root coordinates. A Canvas has a size of zero, and its children outside of it.
	private static Rect GetGlobalBounds(UIElement element)
	{
		var bounds = TransformToRoot(element, new Rect(0, 0, element.ActualSize.X, element.ActualSize.Y));

		foreach (var child in VisualTreeHelper.GetChildren(element))
		{
			if (child is UIElement { Visibility: Visibility.Visible } childElement)
			{
				var childBounds = GetGlobalBounds(childElement);
				if (childBounds is { Width: > 0, Height: > 0 })
				{
					bounds = bounds is { Width: > 0, Height: > 0 } ? RectHelper.Union(bounds, childBounds) : childBounds;
				}
			}
		}

		return bounds;
	}

	// Note: The inflated region will eat clicks on the shadow, rather than have the clicks go through and land on the
	// stuff (e.g. Xaml content in the main tree or content in other windows) under it.
	// TODO Uno: only where the host can't see through its window: it passes the gutter on where it's able to, see
	// Win32WindowedPopupSite. WinUI's own island has no such choice, and what it eats is forwarded to the owner here anyway.
	private void AdjustWindowedPopupBoundsForDropShadow(Rect popupWindowBounds, RectInt32 contentBounds)
	{
		MUX_ASSERT(m_windowedPopupWindow is { Site.IsClosed: false });
		if (ShouldPopupRendersDropShadow())
		{
			var expandedBounds = popupWindowBounds;
			var insets = GetInsetsForDropShadow();
			var scale = GetEffectiveRootScale();

			expandedBounds.X -= (FlowDirection == FlowDirection.RightToLeft ? insets.Right : insets.Left) * scale;
			expandedBounds.Y -= insets.Top * scale;
			expandedBounds.Width += (insets.Left + insets.Right) * scale;
			expandedBounds.Height += (insets.Bottom + insets.Top) * scale;

			m_windowedPopupMoveAndResizeRect = new RectInt32(
				XcpFloor(expandedBounds.X),
				XcpFloor(expandedBounds.Y),
				XcpCeiling(expandedBounds.Width),
				XcpCeiling(expandedBounds.Height));

			// TODO Uno: PauseNewDispatch, see PositionAndSizeWindowForWindowedPopup.
			m_windowedPopupWindow!.Site.MoveAndResize(m_windowedPopupMoveAndResizeRect, contentBounds);
		}
	}

	// Physical positions come out of a chain of float matrices, which puts a whole pixel a rounding error away.
	private static int XcpCeiling(double value) => (int)Math.Ceiling(value - 0.001);

	private static int XcpFloor(double value) => (int)Math.Floor(value + 0.001);

	// CThemeShadow::IsDropShadowMode
	private static bool ShouldPopupRendersDropShadow() => true;

	private Thickness GetInsetsForDropShadow()
	{
		// MUX Reference ThemeShadow.cpp, GetInsetsForWindowedPopup. Small for a ToolTip, Medium otherwise.
		var shadowInsets = IsToolTip
			? new Thickness(4, 1, 4, 8)
			: new Thickness(10, 2, 10, 18);

		//
		// Round the insets according to the root scale. We do this for a couple of reasons:
		//
		//   1. These insets get multiplied by the root scale in AdjustWindowedPopupBoundsForDropShadow.
		//      Rounding them here allows us to calculate integer offsets for the bridge.
		//
		//   2. These insets also factor into the offsets that we set on the island root visual at the end of
		//      PositionAndSizeWindowForWindowedPopup. Those offsets also get multiplied by the root scale,
		//      and we want integers at the end. Otherwise the content in the windowed popup get placed at
		//      a fractional offset, which produces blurry content.
		//
		// We use ceiling to make sure we don't shrink the gutters and clip the shadow.
		//
		var scale = GetEffectiveRootScale();
		if (scale != 1.0)
		{
			shadowInsets.Top = Math.Ceiling(shadowInsets.Top * scale) / scale;
			shadowInsets.Bottom = Math.Ceiling(shadowInsets.Bottom * scale) / scale;
			shadowInsets.Left = Math.Ceiling(shadowInsets.Left * scale) / scale;
			shadowInsets.Right = Math.Ceiling(shadowInsets.Right * scale) / scale;
		}

		return shadowInsets;
	}

	private static bool IsElementScaleOrTranslationOnly(UIElement uielement)
	{
		// CUIElement::GetLocalTransform4x4
		if (uielement.Projection is { })
		{
			// TODO Uno: visuals don't carry a projection in their matrix. Upstream looks at the resulting matrix, so an
			// identity projection stays windowed there, and not here. Transform3D is a stub.
			return false;
		}

		var m = uielement.Visual.LocalMatrix;
		return m is { M12: 0, M13: 0, M14: 0, M21: 0, M23: 0, M24: 0, M31: 0, M32: 0, M34: 0, M44: 1 };
	}

	private bool MeetsRenderingRequirementsForWindowedPopup()
	{
		// Currently, popups can only be windowed if the bounds for the island are of the "simple, rectangular" form.
		// This means only scales/translates for everything from Popup.Child up to the root.
		var isParentedPopup = IsActiveInVisualTree;
		if (isParentedPopup)
		{
			// First, check Popup.Child for scales/translates.
			// Note importantly that at the time this function is called, Popup.Child isn't parented to PopupRoot yet,
			// so we must not rely on GetUIElementAdjustedParentInternal() to hop from Popup.Child to Popup.
			// Hence, we check Popup.Child explicitly here, outside the loop below.
			if (m_pChild is not null && !IsElementScaleOrTranslationOnly(m_pChild))
			{
				return false;
			}

			// Next, check Popup and its ancestors.
			for (UIElement? current = this;
				 current is not null;
				 current = current.GetUIElementAdjustedParentInternal(false))
			{
				if (!IsElementScaleOrTranslationOnly(current))
				{
					return false;
				}
			}
		}

		return true;
	}

	// TODO Uno: SetRootVisualForWindowedPopupWindow, AddAdditionalVisualForWindowedPopupWindow,
	// RemoveAdditionalVisualForWindowedPopupWindow, EnsureWindowedPopupRootVisualTree: the render walk's part, which here is
	// CompositionTarget.WindowedPopupSurface reading TryGetWindowedFrame, and Visual.HasOwnSurface taking the visuals out
	// of the main window's walk.

	/// <summary>
	/// What the window of this popup shows: its rectangle relative to the content root, in physical pixels, the scale, and the
	/// visuals to render, in z-order: the popup's child, then the inline popups nested in it.
	/// </summary>
	internal bool TryGetWindowedFrame(out RectInt32 bounds, out float scale, out List<Composition.Visual> visuals)
	{
		bounds = default;
		scale = GetEffectiveRootScale();
		visuals = null!;

		if (m_pChild is not { } child || m_windowedPopupMoveAndResizeRect is { Width: <= 0 } or { Height: <= 0 })
		{
			return false;
		}

		bounds = new RectInt32(
			m_windowedPopupMoveAndResizeRect.X - m_rootOffsetPhysical.X,
			m_windowedPopupMoveAndResizeRect.Y - m_rootOffsetPhysical.Y,
			m_windowedPopupMoveAndResizeRect.Width,
			m_windowedPopupMoveAndResizeRect.Height);

		visuals = new() { child.Visual };

		if (GetAdjustedPopupRoot() is { } popupRoot)
		{
			// The most recently opened comes first.
			var openPopups = popupRoot.GetOpenPopups();
			for (var i = openPopups.Count - 1; i >= 0; i--)
			{
				var openPopup = openPopups[i];
				if (!openPopup.IsWindowed && openPopup.m_pChild is { } nestedChild && openPopup.GetFirstAncestorPopup(windowedPopupOnly: true) == this)
				{
					visuals.Add(nestedChild.Visual);
				}
			}
		}

		return true;
	}

	// TODO Uno: EnsureUIAWindow, CUIAHostWindow: the host answers WM_GETOBJECT with the peer of IWindowedPopupContent.AutomationRoot,
	// from the owner's automation tree. It transforms against the owner window, as CUIAWindow::m_transformWindow does.

	// TODO Uno: GetPopupPositioningWindow: only serves TextBoxBase's IME and native-window coordinates, which are the owner's here.

	// Converts between logical and physical pixels
	private float GetEffectiveRootScale() => (float)(XamlRoot?.RasterizationScale ?? 1.0);

	// Convert DIPs to Physical Pixels
	private float DipsToPhysicalPixels(float dips) => dips * GetEffectiveRootScale();

	private Rect DipsToPhysicalPixelsRect(Rect rect) => new(
		DipsToPhysicalPixels((float)rect.X),
		DipsToPhysicalPixels((float)rect.Y),
		DipsToPhysicalPixels((float)rect.Width),
		DipsToPhysicalPixels((float)rect.Height));

	// Convert Physical Pixels to DIPs
	private float PhysicalPixelsToDips(float physicalPixels) => physicalPixels / GetEffectiveRootScale();

	// MUX Reference DXamlCore.cpp, CalculateAvailableMonitorRect
	// Calculate the available monitor bounds that is the screen Dips.
	internal static Rect CalculateAvailableMonitorRect(XamlRoot? xamlRoot, Point targetPointClientLogical)
	{
		var availableMonitorRectClientLogicalResult = default(Rect);

		if (xamlRoot is null || GetWindowedPopupExtension() is not { } extension)
		{
			return availableMonitorRectClientLogicalResult;
		}

		var screenOffsetTemp = extension.GetScreenOffset(xamlRoot);

		var scale = (float)xamlRoot.RasterizationScale;
		MUX_ASSERT(scale != 0.0f);

		// VisualTree::ClientLogicalToScreenPhysical
		PointInt32 targetPointScreenPhysicalTemp = new(
			(int)Math.Round(targetPointClientLogical.X * scale) + screenOffsetTemp.X,
			(int)Math.Round(targetPointClientLogical.Y * scale) + screenOffsetTemp.Y);

		// Get the monitor from the nearest target rect
		if (extension.TryGetMonitorBounds(targetPointScreenPhysicalTemp, out var rcMonitor, out var rcWork))
		{
			// TODO Uno: GetInputPaneOccludeRect, which subtracts an input pane: desktops only get here, without one.
			// TODO Uno: VisualTree::ShouldConstrainPopupsToWorkArea, which is DesktopWindowXamlSource's and always true otherwise.
			Rect availableMonitorRectScreenLogical = new(
				rcWork.X / scale,
				rcWork.Y / scale,
				rcWork.Width / scale,
				rcWork.Height / scale);
			_ = rcMonitor;

			availableMonitorRectClientLogicalResult = new Rect(
				availableMonitorRectScreenLogical.X - (screenOffsetTemp.X / scale),
				availableMonitorRectScreenLogical.Y - (screenOffsetTemp.Y / scale),
				availableMonitorRectScreenLogical.Width,
				availableMonitorRectScreenLogical.Height);
		}

		return availableMonitorRectClientLogicalResult;
	}

	private Popup? GetFirstAncestorPopup(bool windowedPopupOnly)
	{
		// MUX Reference dependencyobject.cpp, GetFirstAncestorPopup
		for (var ancestor = GetUIElementAdjustedParentInternal(false);
			 ancestor is not null;
			 ancestor = ancestor.GetUIElementAdjustedParentInternal(false))
		{
			if (ancestor is Popup popup && (!windowedPopupOnly || popup.IsWindowed))
			{
				return popup;
			}
		}

		return null;
	}

	/// <summary>
	/// Owns a popup's window for as long as the popup needs it, so a popup that's no longer referenced takes it along.
	/// </summary>
	private sealed class WindowedPopupWindow(IWindowedPopupSite site, Microsoft.UI.Xaml.Media.CompositionTarget.WindowedPopupSurface surface)
	{
		private int _closed;

		public IWindowedPopupSite Site { get; } = site;

		public Microsoft.UI.Xaml.Media.CompositionTarget.WindowedPopupSurface Surface { get; } = surface;

		public void Close()
		{
			if (Interlocked.Exchange(ref _closed, 1) == 0)
			{
				GC.SuppressFinalize(this);
				Site.Dispose();
			}
		}

		// The window belongs to the UI thread.
		~WindowedPopupWindow() => NativeDispatcher.Main.Enqueue(Close);
	}

	private PopupRoot? GetAdjustedPopupRoot()
		=> XamlRoot?.VisualTree.PopupRoot ?? VisualTree.GetPopupRootForElement(this);

#if HAS_UNO
	private void EnsureXamlRootForOpen()
	{
		var xamlRoot = XamlRoot ?? m_pChild?.XamlRoot ?? WinUICoreServices.Instance.ContentRootCoordinator.Unsafe_IslandsIncompatible_CoreWindowContentRoot?.XamlRoot;

		if (xamlRoot != XamlRoot)
		{
			XamlRoot = xamlRoot;
		}
	}

	/// <summary>
	/// Recursively ensures the <paramref name="element"/> and all its visual children
	/// have their <see cref="UIElement.VisualTreeCache"/> set to <paramref name="targetTree"/>.
	/// Short-circuits when a subtree already has the correct value.
	/// </summary>
	private static void EnsureVisualTreeOnSubtree(UIElement element, VisualTree targetTree)
	{
		if (element.GetVisualTree() == targetTree)
		{
			return;
		}

		element.SetVisualTree(targetTree);

		var count = VisualTreeHelper.GetChildrenCount(element);
		for (var i = 0; i < count; i++)
		{
			if (VisualTreeHelper.GetChild(element, i) is UIElement child)
			{
				EnsureVisualTreeOnSubtree(child, targetTree);
			}
		}
	}

	// A popup outside the live tree has no real ancestors to follow: GetUIElementAdjustedParentInternal treats the
	// popup root as its parent.
	private void UpdateVisualTransformParent(PopupRoot? popupRoot)
	{
		Visual.TransformParent = IsActiveInVisualTree ? null : popupRoot?.Visual;
		ApplyTranslationWithoutArrange();
	}

	// A popup outside the live tree is never arranged, and arranging is what gives an element its Translation.
	private void ApplyTranslationWithoutArrange()
	{
		if (!IsActiveInVisualTree)
		{
			Visual.ArrangeOffset = Translation;
		}
	}

	private protected override void OnTranslationChanged() => ApplyTranslationWithoutArrange();
#endif

	// CPopup::Child
	partial void OnChildChangedPartial(UIElement oldChild, UIElement newChild)
	{
		// TODO Uno: EnsureLayoutTransitionStorage for m_pChildTransitions: ChildTransitions isn't implemented.
		RemoveChild();
		SetChild(newChild);
	}

	/// <summary>
	/// Adds the popup's child to the visual and logical trees and enters it in the right namescope
	/// </summary>
	private void SetChild(UIElement? pChild)
	{
		if (pChild is not null)
		{
			// TODO Uno: WinUI raises AG_E_MANAGED_ELEMENT_ASSOCIATED if pChild->IsAssociated(), then SetAssociated(true, this).
			// In Uno, UIElement.AddChild moves the child out of its current parent when the popup opens.

			// In this case we have a very specific scenario.
			// We have a Popup with ChildTransitions that react to an Unload trigger.
			// We have a non-null child and the Popup is opened.
			// Then we close the Popup and the transition starts playing.
			// While it plays, we set the child of the Popup to null and then we reset it to the same non-null child
			// we had before (for reference, ContentDialog follows this code path when hidden and re-shown).
			// TODO Uno: ClearUCRemoveLogicalParentFlag(pChild): no layout transitions.
		}

		// Set the property value
		MUX_ASSERT(m_pChild is null);

		m_pChild = pChild;

		if (m_pChild is not null)
		{
			// TODO Uno: if (GetStandardNameScopeOwner() == NULL), else m_pChild->Enter(GetStandardNameScopeOwner(), enterParams):
			// the namescope owner model is in the open namescope PRs (specs/058).
			if (m_fIsOpen)
			{
				//if popup does not have a namescope owner, then make the child its own
				//namescope owner. this ensures the name resolution works within the popup tree.
				AddChildAsNamescopeOwner();
			}

			//add to the logical tree
			AddLogicalChild(m_pChild);

			if (m_fIsOpen)
			{
				var pPopupRoot = GetAdjustedPopupRoot();

				// Bug 74739: Detect nested popup in a template scenario. This popup is being created
				// as part of template expansion and the template parent is in a
				// popup. Defer opening this popup till currently open popups have been measured.
				// Attempt to open it here will fail; popup root children collection is locked for layout.
				if (pPopupRoot is { IsOnMeasureStack: true })
				{
					pPopupRoot.AddToDeferredOpenPopupList(this);
				}
				else
				{
					Open();
				}
			}

			// TODO Uno: m_pChild->SetRequiresComposition(ProjectedShadowDefaultReceiver): no projected shadows.
		}

		// Maintain a managed ref on the child to prevent it from being GC'd when the popup is not open.
		// Uno: the Child dependency property holds it.
	}

	/// <summary>
	/// Removes the popup's child from the visual and logical trees and does namescope changes
	/// </summary>
	private void RemoveChild()
	{
		if (m_pChild is not null)
		{
			if (ShouldRemoveChildAsNamescopeOwner())
			{
				RemoveChildAsNamescopeOwner();
			}

			// TODO Uno: else m_pChild->Leave(m_pChild->GetStandardNameScopeOwner(), leaveParams): the namescope PRs.

			// notice how we FIRST close and only then remove logical child.
			// this allows us to use the logical parent as a lookup for transitions.
			if (m_fIsOpen)
			{
				// We're currently removing the child, and the popup shouldn't raise a UIE.Hidden event, so don't call
				// RequestKeepAlive expecting a UIE.Hidden to be raised.
				// TODO Uno: ImplicitCloseGuard: needs UIElement.Hidden.
				Close();
			}

			// TODO Uno: m_unloadingChild, UC_RemoveLogicalParent: needs ComputeKeepVisible and unloading storage.

			RemoveLogicalChild(m_pChild);

			//reset the property value reference
			m_pChild = null;
		}
	}

	private void AddLogicalChild(UIElement child)
	{
		if (child is FrameworkElement childAsFE)
		{
			childAsFE.SetLogicalParent(this);
			childAsFE.RefreshInheritanceParent();
		}

#if HAS_UNO
		PropagateFocusProperties();
#endif
	}

	private void RemoveLogicalChild(UIElement child)
	{
		if (child is FrameworkElement childAsFE && childAsFE.LogicalParentOverride == this)
		{
			childAsFE.SetLogicalParent(null);
			childAsFE.RefreshInheritanceParent();
		}

#if HAS_UNO
		if (child is DependencyObject provider &&
			provider.DataContextPropertyInternal is { } dataContextProperty &&
			provider.ReadLocalValue(dataContextProperty) != DependencyProperty.UnsetValue)
		{
			provider.ClearValue(AllowFocusOnInteractionProperty, DependencyPropertyValuePrecedences.Local);
			provider.ClearValue(AllowFocusWhenDisabledProperty, DependencyPropertyValuePrecedences.Local);
		}
#endif
	}

#if HAS_UNO
	private void PropagateFocusProperties()
	{
		if (Child is DependencyObject provider)
		{
			provider.SetValue(AllowFocusOnInteractionProperty, Uno.UI.Helpers.Boxes.Boxer.Box(AllowFocusOnInteraction), DependencyPropertyValuePrecedences.Local);
			provider.SetValue(AllowFocusWhenDisabledProperty, Uno.UI.Helpers.Boxes.Boxer.Box(AllowFocusWhenDisabled), DependencyPropertyValuePrecedences.Local);
		}
	}
#endif

	// TODO Uno: ClearUCRemoveLogicalParentFlag: no layout transitions.

	// Update the translation that should be applied to windowed popup input
	// so that it can be applied in the coordinate space of the popup's
	// content root.
	// TODO Uno: only the offset is kept, for GetTranslationFromMainWindow. The window forwards its input in the content
	// root's coordinates, so there is no pointer point transform to feed.
	private void UpdateTranslationFromContentRoot(global::Windows.Foundation.Point offset, bool forceUpdate = false)
	{
		if (m_offsetFromMainWindow.X != offset.X ||
			m_offsetFromMainWindow.Y != offset.Y ||
			forceUpdate)
		{
			m_offsetFromMainWindow = offset;
		}
	}

	// Causes the object and its "children" to enter scope. For popup, it calls the base class
	// implementation. Popup's child is entered into popup's namescope if there is one.
	internal override void EnterImpl(EnterParams @params, int depth)
	{
		base.EnterImpl(@params, depth);

		// If the Popup is entering the live tree, ensure that it is marked for composition.
		// If the Popup was already opened (i.e. a 'parentless Popup') then there's no need to mark it again.
		// TODO Uno: StartComposition(): no redirection in Uno's visual tree.

		// TODO Uno: this is the Enter call to register names. if we made the child a namescope owner earlier,
		// then reverse that since the child will now enter popup's namescope (the namescope PRs).

#if HAS_UNO
		// A parentless popup opened and then moved into the live tree starts following its real ancestors.
		if (@params.IsLive && m_fIsOpen)
		{
			Visual.TransformParent = null;
		}
#endif
	}

	// Causes the object and its properties to leave scope. For popup, it calls the base class
	// implementation. If Popup is leaving a namescope, then the child leaves that namescope
	// as well and becomes a namescope owner.
	internal override void LeaveImpl(LeaveParams @params)
	{
		// If the Popup was marked for composition, unmark is when it leaves.
		// This is safe because the Popup cannot be rendered again in this state, since it will also Close() here.
		// TODO Uno: StopComposition(): no redirection in Uno's visual tree.

		base.LeaveImpl(@params);

		// If we're leaving the live tree, close the popup
		if (@params.IsLive && m_fIsOpen)
		{
			var popupRoot = GetAdjustedPopupRoot();
			if (popupRoot is { ShouldDeferClosePopups: true })
			{
				popupRoot.DeferClosePopup(this);
			}
			else
			{
				// We're cleaning up unloading storage, and UIE.Hidden has already been raised. Don't call RequestKeepAlive
				// expecting another UIE.Hidden to be raised.
				// TODO Uno: ImplicitCloseGuard: needs UIElement.Hidden.
				IsOpen = false;
			}
		}

		// TODO Uno: m_pChild->Leave(pNamescopeOwner, params): the namescope PRs.
	}

	// TODO Uno: StartComposition, StopComposition: no redirection in Uno's visual tree.

	/// <summary>
	/// Returns a value indicating whether or not the currently focused element is a child of this Popup.
	/// </summary>
	private bool HasFocusedElementAsChild()
	{
		bool hasFocusedElementAsChild = false;

		var pFocusManager = VisualTree.GetFocusManagerForElement(this);

		if (pFocusManager is not null)
		{
			if (pFocusManager.FocusedElement is UIElement pFocusedElement)
			{
				bool focusedElementIsChildOfThisPopup = false;

				if (m_pChild is not null && pFocusedElement != m_pChild)
				{
					focusedElementIsChildOfThisPopup = m_pChild.IsAncestorOf(pFocusedElement);
				}

				hasFocusedElementAsChild = pFocusedElement == m_pChild || focusedElementIsChildOfThisPopup;
			}
		}

		return hasFocusedElementAsChild;
	}

	// Helper method to add child as a namescope owner. This will be done when popup
	// does not have a namescope owner. It ensures that name resolution works in the
	// popup tree even when popup is created in ether.
	private void AddChildAsNamescopeOwner()
	{
		// TODO Uno: The namescope owner model is in the open namescope PRs (specs/058). FindName walks the visual tree meanwhile.
		// if (m_pChild && !m_pChild->IsStandardNameScopeOwner())
		// {
		//     m_pChild->SetIsStandardNameScopeOwner(TRUE);
		//     m_pChild->SetIsStandardNameScopeMember(TRUE);
		//     GetContext()->GetNameScopeRoot().EnsureNameScope(m_pChild, nullptr);
		//     m_pChild->Enter(m_pChild, enterParams);
		//     m_fMadeChildNamescopeOwner = TRUE;
		// }
	}

	// Helper method to remove popup's child as a namescope owner. This will be done when
	// popup gets a namescope owner. Popup's child should enter the namescope of popup's
	// namescope owner.
	private void RemoveChildAsNamescopeOwner()
	{
		// TODO Uno: The namescope owner model is in the open namescope PRs (specs/058).
		// m_pChild->Leave(m_pChild, leaveParams);
		// GetContext()->RemoveNameScope(m_pChild, Jupiter::NameScoping::NameScopeType::StandardNameScope);
		// m_pChild->SetIsStandardNameScopeOwner(FALSE);
		// m_pChild->SetIsStandardNameScopeMember(FALSE);
		m_fMadeChildNamescopeOwner = false;
	}

	// Returns TRUE if m_bMadeChildNamescopeOwner is set to TRUE
	private bool ShouldRemoveChildAsNamescopeOwner() => m_fMadeChildNamescopeOwner;

	// helper method to raise Popup.Opened and Closed events
	private void RaisePopupEvent(bool isOpened)
	{
		// TODO: Add EventManager.RaiseEvent method and use it here.
		if (isOpened)
		{
			NativeDispatcher.Main.Enqueue(() => Opened?.Invoke(this, true), NativeDispatcherPriority.Normal);
		}
		else
		{
			NativeDispatcher.Main.Enqueue(() => Closed?.Invoke(this, false), NativeDispatcherPriority.Normal);
		}
	}

	// TODO Uno: AsyncRelease, ReleaseOverride, AddEventListener, RemoveEventListener: reference counting, Uno has the GC.
	// TODO Uno: UpdateImplicitStyle, PreChildrenPrintVirtual, MarkInheritedPropertyDirty: no equivalents.

	// BoundsTestInternal, BoundsTestPopup, BoundsTestChildren, GenerateChildOuterBounds: Uno hit tests the popup's child as a
	// child of the popup root, see PopupRoot.ShouldHitTestChild.

	// TODO Uno: LeavePCSceneSubgraph, LeavePCScene: Uno's render walk follows the visual tree.

	/// <summary>
	/// Returns the closest Popup ancestor or NULL if one does not exist.
	/// </summary>
	internal static Popup? GetClosestPopupAncestor(UIElement? pChild)
	{
		var pNode = pChild;

		while (pNode is not null)
		{
			if (pNode is Popup popup)
			{
				return popup;
			}

			pNode = pNode.GetUIElementAdjustedParentInternal(false);
		}

		return null;
	}

	internal static FlyoutBase? GetClosestFlyoutAncestor(UIElement? pUIElement)
	{
		var pNode = pUIElement;

		while (pNode is not null)
		{
			if (pNode is Popup popup)
			{
				if (popup.IsFlyout)
				{
					return popup.AssociatedFlyout;
				}
			}

			pNode = pNode.GetUIElementAdjustedParentInternal(false);
		}

		return null;
	}

	// The ESC key closes the topmost light-dismiss-enabled popup.
	// This handles the case where ESC is pressed while the CPopup has focus.  This does not handle the routed case
	// where some child of a light-dismiss-enabled popup has focus, since its events route to CPopupRoot.
	private void OnKeyDown(object sender, KeyRoutedEventArgs pEventArgs)
	{
		// We will dismiss popup on key down for RTM apps. This is to accommodate IME behavior. IME has some logic
		// tied to ESC key press (such as closing suggestions menu etc) which gets invoked OnKeyDown and they handle that event
		// For light dismiss popups we do not  want ESC key to dismiss the popup if this logic gets executed, which happens in on key down.
		ClosePopupOnKeyEventIfNecessary(pEventArgs);
	}

	// TODO Uno: NWPropagateDirtyFlag: no dirty flags.

	internal FocusState GetSavedFocusState()
		=> m_pPreviousFocusWeakRef is not null ? m_savedFocusState : FocusState.Unfocused;

	/// <summary>
	/// Should PopupRoot notify this popup of app's theme change?
	/// </summary>
	internal bool ShouldPopupRootNotifyThemeChange()
	{
		// An AppBar gets theme change notification from its owner page.
		// A parented popup gets theme change notification from its parent.
		// TODO Uno: no ApplicationBarService (IsApplicationBarService()).
		return this.GetParentInternal(false) is null;
	}

	// TODO Uno: RecursiveInvalidateFontSize: no equivalent.

	/// <summary>
	/// Notify element and its subtree that theme has changed
	/// </summary>
	internal override void NotifyThemeChangedCore(Theme theme, bool forceRefresh)
	{
		// Notify base class that theme has changed
		base.NotifyThemeChangedCore(theme, forceRefresh);

		// Recursively notify element subtree that theme has changed
		if (m_pChild is not null)
		{
			m_pChild.NotifyThemeChanged(theme, forceRefresh);
		}

		if (m_overlayElement is not null)
		{
			// Set the overlay to match the child's theme.  If there is no child,
			// then match the popup's theme.
			var overlayTheme = m_pChild is not null ? m_pChild.GetTheme() : GetTheme();
			m_overlayElement.NotifyThemeChanged(overlayTheme, forceRefresh);
		}
	}

	internal override void UpdateThemeBindings(Data.ResourceUpdateReason updateReason)
	{
		base.UpdateThemeBindings(updateReason);

		// Uno: resource update reasons other than the theme don't go through NotifyThemeChangedCore.
		Application.PropagateResourcesChanged(Child, updateReason);
	}

	private void ClosePopupOnKeyEventIfNecessary(KeyRoutedEventArgs eventArgs)
	{
		if (eventArgs.Key == VirtualKey.Escape)
		{
			eventArgs.Handled = true;

			// Give the popup an opportunity to cancel closing.
			var cancel = false;
			OnClosing(ref cancel);
			if (cancel)
			{
				return;
			}

			m_focusStateAfterClosing = FocusState.Keyboard;
			IsOpen = false;
		}
	}

	internal virtual void OnClosing(ref bool cancel)
	{
		cancel = false;

		// If this popup is associated with a flyout, then give it a chance to cancel closing.
		if (AssociatedFlyout is { } flyout)
		{
			FlyoutBase.OnClosingStatic(flyout, ref cancel);
		}
	}

	// TODO Uno: ReferenceTrackerWalkCore: reference tracking, Uno has the GC.

	private void ReevaluateIsOverlayVisible()
	{
		bool isOverlayVisible = LightDismissOverlayHelper.IsOverlayVisibleForMode(LightDismissOverlayMode);

		// If we don't have a child, then don't show an overlay.
		isOverlayVisible &= m_pChild is not null;

		// Only enable the overlay if the popup is light-dismiss enabled, unless this check is
		// otherwise disabled.
		isOverlayVisible &= IsLightDismissEnabled || DisableOverlayIsLightDismissCheck;

		// Only take action if the value has changed.
		if (isOverlayVisible != m_isOverlayVisible)
		{
			m_isOverlayVisible = isOverlayVisible;

			if (m_isOverlayVisible)
			{
				SetOverlayElement(new Rectangle());
			}
			else
			{
				RemoveOverlayElement();
			}
		}
	}

	private void SetOverlayElement(Rectangle overlayElement)
	{
		MUX_ASSERT(m_isOverlayVisible);
		MUX_ASSERT(overlayElement is not null);
		MUX_ASSERT(m_overlayElement is null);

		m_overlayElement = overlayElement;

#if HAS_UNO
		Microsoft.UI.Xaml.Automation.AutomationProperties.SetAccessibilityView(m_overlayElement!, Microsoft.UI.Xaml.Automation.Peers.AccessibilityView.Raw);
#endif

		SetOverlayThemeBrush("PopupLightDismissOverlayBackground");

		// The overlay element doesn't host any content, so we don't really care about
		// name resolution, so just make itself its own namescope owner.
		// TODO Uno: The namescope owner model is in the open namescope PRs (specs/058).

		m_overlayElement!.SetLogicalParent(this);

		if (m_fIsOpen)
		{
			AddOverlayElementToPopupRoot();
		}
	}

	private void RemoveOverlayElement()
	{
		MUX_ASSERT(m_overlayElement is not null);

		// TODO Uno: The namescope owner model is in the open namescope PRs (specs/058).

		if (m_fIsOpen)
		{
			RemoveOverlayElementFromPopupRoot();
		}

		m_overlayElement!.SetLogicalParent(null);

		m_overlayElement = null;
	}

	private void AddOverlayElementToPopupRoot()
	{
		MUX_ASSERT(m_fIsOpen);
		MUX_ASSERT(m_overlayElement is not null);
		MUX_ASSERT(m_pChild is not null);

		var popupRoot = GetAdjustedPopupRoot();
		MUX_ASSERT(popupRoot is not null);

		if (popupRoot is null)
		{
			return;
		}

		var indexOfChild = popupRoot.Children.IndexOf(m_pChild!);

		if (indexOfChild >= 0)
		{
			popupRoot.Children.Insert(indexOfChild, m_overlayElement!);
		}

		// Set the overlay to match the child's theme.  If there is no child,
		// then match the popup's theme.
		var overlayTheme = m_pChild is not null ? m_pChild.GetTheme() : GetTheme();
		m_overlayElement!.NotifyThemeChanged(overlayTheme, false /*fForceRefresh*/);
	}

	private void RemoveOverlayElementFromPopupRoot()
	{
		MUX_ASSERT(m_overlayElement is not null);

		var popupRoot = GetAdjustedPopupRoot();
		MUX_ASSERT(popupRoot is not null);

		popupRoot?.Children.Remove(m_overlayElement!);
	}

	internal void SetOverlayThemeBrush(string brushKey)
	{
		if (m_overlayElement is null)
		{
			return;
		}

		ResourceResolver.ApplyResource(m_overlayElement, Shape.FillProperty, brushKey, isThemeResourceExtension: true, isHotReloadSupported: true);
	}

	internal DependencyObject? GetOverlayInputPassThroughElement()
	{
		if (IsFlyout)
		{
			return AssociatedFlyout?.OverlayInputPassThroughElement;
		}

		return OverlayInputPassThroughElement;
	}

	internal void SetAssociatedVisualTree(VisualTree? tree)
	{
		tree?.AttachElement(this);
	}

	// TODO Uno: SetAssociatedIsland, OnContentAutomationProviderRequested, SetCachedStandardNamescopeOwner,
	// TODO Uno: GetSystemBackdrop, SetSystemBackdrop, see ApplyRootRoundedCornerClipToSystemBackdrop.

	// components/elements/Popup.cpp
	// TODO Uno: CancelHideAnimationToPrepareForShow, GetPopupOfUnloadingChild, RemoveUnloadingChild: need implicit Hide animations.
}
