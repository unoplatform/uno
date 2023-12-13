// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\core\inc\Popup.h, tag winui3/release/1.4.3, commit 685d2bf

using System;
using Uno.Disposables;
using Uno.UI.DataBinding;
using Windows.Foundation;
using Windows.UI.Xaml.Media.Animation;

namespace Windows.UI.Xaml.Controls.Primitives;

//------------------------------------------------------------------------
//
//  Class: Popup
//
//  Used for UI that needs to appear on a separate "plane" on top of the
//  rest of the XAML content.
//
//  When the popup is opened, its content is added as a child of PopupRoot.
//  PopupRoot manages the popup in its list of open popups, and layout
//  and render of the content occurs as a child of PopupRoot. When the
//  popup is closed, its content is removed as a child of PopupRoot.
//
//  A popup can optionally be marked as windowed. A windowed popup displays
//  its content in a WS_POPUP window, instead of the Jupiter window, so is
//  not clipped to the Jupiter window. This is useful for context menus and
//  tooltips that don't want to be clipped to the Jupiter window. Windowed
//  popups are only supported on Desktop, because OneCore and Phone don't
//  support non-fullscreen windows. Controls that use windowed popups must
//  fallback to non-windowed placement on unsupported platforms.
//    - The popup window is positioned at the same location as the popup, so
//      can use standard hit testing code.
//    - The window does not take window activation or focus, so all keyboard
//      input goes directly to the Jupiter window, and can use the standard
//      focus manager and navigation code.
//    - Pointer input will go to the popup's window and is forwarded to the
//      Jupiter window. Pointer window messages provide the pointer position in
//      screen coordinates. Since the popup window is positioned at the
//      same screen location, the standard input manager code can be used in
//      most cases. Exceptions include PointerRoutedEventArgs.CurrentPoint,
//      which needs to be transformed from the pointer's target window to the
//      root window (see DxamlCore::GetTranslationFromTargetWindowToRootWindow),
//      and initialization of DirectManipulationContainer, which needs to
//      use the popup's window (see InputServices.InitializeDirectManipulationContainer's
//      usage of DependencyObject.GetElementInputWindow.)
//    - Windowed popups must be closed when the Jupiter window is moved, because
//      they will no longer be positioned at the location of the popup element.
//    - For rendering, a windowed popup uses HWWindowedPopupCompTreeNode, which
//      will provide the DComp visual to be set into the window's target (see
//      CPopup::SetRootVisualForWindowedPopupWindow.) HWWindowedPopupCompTreeNode
//      also inserts a placeholder in the main DComp tree, to maintain child
//      ordering with sibling visuals
//    - For UIA support, a windowed popup creates its own CUIAHostWindow, whose
//      root visual is the popup's content, and returns it through the window's
//      WM_GETOBJECT. CUIAHostWindow will use the Jupiter window for screen/client
//      transforms, because the layout of all elements is relative to the Jupiter
//      window -- see CUIAWindow::m_transformWindow.
//    - A windowed popup should be parentless. This is because transforms
//      between the popup and the visual tree root will not be applied while
//      positioning the popup window and setting the DComp visual to the window
//      target.
//    - The windowed popup cannot contain other popups, because nested popups
//      will still be attached to the main visual tree, so will be covered
//      by the windowed popup.
//    - A windowed popup cannot be made windowless, to simplify implementation.
//
//------------------------------------------------------------------------

public partial class Popup
{
	//bool AllowsHandlerWhenNotLive(XINT32 iListenerType, KnownEventIndex eventIndex) const final
	//    {
	//        return true;
	//    }

	//    int ParticipatesInManagedTreeInternal() override
	//{
	//	// Peer has state
	//	return PARTICIPATES_IN_MANAGED_TREE;
	//}

	private bool IsOpen => m_fIsOpen;

	private bool IsFlyout => m_associatedFlyoutWeakRef != null && m_associatedFlyoutWeakRef.Target is not null; // TODO:MZ: Is this check correct?

	private bool IsApplicationBarService => m_fIsApplicationBarService;
	
	private bool IsContentDialog => m_fIsContentDialog;
	
	private bool IsSubMenu => m_fIsSubMenu;

	private bool IsToolTip => m_pChild is ToolTip;
	
	private bool WasOpenedDuringEngagement() => m_wasOpenedDuringEngagement;
	internal void SetOpenedDuringEngagement(bool value) => m_wasOpenedDuringEngagement = value;

	// We'll treat a popup associated with a sub-menu as light-dismiss
	// because we know that the parent menu of a sub-menu is always light-dismiss.
	internal bool IsSelfOrAncestorLightDismiss() => m_fIsLightDismiss || m_fIsSubMenu;

	private FlyoutBase? GetAssociatedFlyoutNoRef() => m_associatedFlyoutWeakRef?.Target as FlyoutBase;

	//_Check_return_ HRESULT UpdateImplicitStyle(
	//        _In_opt_ CStyle *pOldStyle,
	//	_In_opt_ CStyle *pNewStyle,
	//        bool bForceUpdate,
	//        bool bUpdateChildren = true,
	//        bool isLeavingParentStyle = false
	//        ) final;

	//static CPopup* GetClosestPopupAncestor(_In_opt_ CUIElement *pUIElement);

	//static CFlyoutBase* GetClosestFlyoutAncestor(_In_opt_ CUIElement *pUIElement);

	internal override bool IsFocusable =>
			// We don't check IsActive() because we want the focus logic to also work with unrooted popups,
			// which are never in the live tree.
			m_fIsLightDismiss &&
			IsVisible() &&
			IsEnabled &&
			AreAllAncestorsVisible();

	private void SetFocusStateAfterClosing(FocusState focusState)
	{
		m_focusStateAfterClosing = focusState;
	}

	//// Whether this element should skip rendering when it is the target of a layout transition.
	//bool SkipRenderForLayoutTransition() override;

	private void SetShouldTakeFocus(bool shouldTakeFocus)
	{
		m_shouldTakeFocus = shouldTakeFocus;
	}

	internal void SetFocus(FocusState focusState)
	{
		Focus(focusState, false /*animateIfBringIntoView*/);
	}

	internal bool WasEverOpened() => m_everOpened;

	internal bool IsWindowed() => m_isWindowed;

	private void SetIsOpen(bool value) { m_fIsOpen = value; }

	//CUIAHostWindow* GetUIAWindow() const { return m_spUIAWindow.get(); }

	private UIElement GetChildNoRef() => m_pChild;

	private IExpPopupWindowBridge GetPopupWindowBridgeNoRef() => m_popupWindowBridge;

	ContentIsland GetContentIslandNoRef() { return m_contentIsland.Get(); }
    DesktopSiteBridge GetDesktopBridgeNoRef() { return m_desktopBridge.Get(); }


	private Point GetTranslationFromMainWindow() => m_offsetFromMainWindow;

	//protected:
	//    bool NWSkipRendering() override
	//{
	//	// CPopup no-ops during the regular software render walk.
	//	// It is rendered when the CPopupRoot is walked, after all other elements have rendered.
	//	return true;
	//}

	//_Check_return_ HRESULT PreChildrenPrintVirtual(
	//        _In_ const SharedRenderParams& sharedPrintParams,
	//	_In_ const D2DPrecomputeParams& cp,
	//	_In_ const D2DRenderParams &printParams
	//        ) override;

	//_Check_return_ HRESULT MarkInheritedPropertyDirty(
	//        _In_ const CDependencyProperty* pdp,
	//	_In_ const CValue* pValue) override;

	//void NWPropagateDirtyFlag(DirtyFlags flags) override;

	////-----------------------------------------------------------------------------
	////
	////  Bounds and Hit Testing
	////
	////-----------------------------------------------------------------------------
	//public:
	//    template < typename HitType >
	//	_Check_return_ HRESULT BoundsTestPopup(
	//		_In_ const HitType& target,
	//		_In_ CBoundedHitTestVisitor* pCallback,
	//		_In_opt_ const HitTestParams* hitTestParams,
	//		_In_ bool canHitDisabledElements,
	//		_In_ bool canHitInvisibleElements,
	//		_Out_opt_ BoundsWalkHitResult* pResult

	//		);

	//protected:
	//    _Check_return_ HRESULT BoundsTestInternal(
	//        _In_ const XPOINTF& target,
	//		_In_ CBoundedHitTestVisitor* pCallback,
	//		_In_opt_ const HitTestParams *hitTestParams,
	//		_In_ bool canHitDisabledElements,
	//		_In_ bool canHitInvisibleElements,
	//		_Out_opt_ BoundsWalkHitResult* pResult
	//        ) final;

	//_Check_return_ HRESULT BoundsTestInternal(
	//        _In_ const HitTestPolygon& target,
	//	_In_ CBoundedHitTestVisitor* pCallback,
	//	_In_opt_ const HitTestParams *hitTestParams,
	//	_In_ bool canHitDisabledElements,
	//	_In_ bool canHitInvisibleElements,
	//	_Out_opt_ BoundsWalkHitResult* pResult
	//        ) final;

	//_Check_return_ HRESULT BoundsTestChildren(
	//        _In_ const XPOINTF& target,
	//	_In_ CBoundedHitTestVisitor* pCallback,
	//	_In_opt_ const HitTestParams *hitTestParams,
	//	_In_ bool canHitDisabledElements,
	//	_In_ bool canHitInvisibleElements,
	//	_Out_opt_ BoundsWalkHitResult* pResult
	//        ) final;

	//_Check_return_ HRESULT BoundsTestChildren(
	//        _In_ const HitTestPolygon& target,
	//	_In_ CBoundedHitTestVisitor* pCallback,
	//	_In_opt_ const HitTestParams *hitTestParams,
	//	_In_ bool canHitDisabledElements,
	//	_In_ bool canHitInvisibleElements,
	//	_Out_opt_ BoundsWalkHitResult* pResult
	//        ) final;

	//_Check_return_ HRESULT GenerateChildOuterBounds(
	//        _In_opt_ HitTestParams *hitTestParams,
	//	_Out_ XRECTF_RB* pBounds
	//        ) override;

	//bool IgnoresInheritedClips() override
	//{ return true; }

	// When the popup is open, its child is parented directly to the popup root.
	private UIElement m_pChild;
	private TransitionCollection m_pChildTransitions;
	private FrameworkElement m_overlayElement;

	// When the previous child needs to be kept visible as a result of changing Popup.Child, we store the
	// old child here. This allows it to keep rendering while it animates.
	private UIElement m_unloadingChild;

	// Owner associated with this popup instance
	// Required for popups hosting ToolTips, which have no proper parent
	private ManagedWeakReference m_toolTipOwnerWeakRef;

	// Storage for Popup.HorizontalOffset/VerticalOffset
	// Note carefully that these properties are NOT considered part of the Popup's layout offset or local transform.
	// These properties must be pushed into Popup.Child because of parentless popups.  In this scenario,
	// layout jumps directly to Popup.Child.  This means these properties are applied as layout properties
	// on Popup.Child, and subsequently get picked up as part of Popup.Child's local transform in the RenderWalk.
	private double m_eHOffset;
	private double m_eVOffset;

	    // In order to accommodate relatively-positioned popups, we'll shift absolutely-positioned popups out of the way
    // in the case where the former type of popup can't fit on either side of its placement target.  We don't want to
    // muck with values the app can set, so instead of modifying the above values, we'll add separate offsets that we
    // exclusively own to the above.
    private double m_hAdjustment;
    private double m_vAdjustment;

    // When the popup is opened and both Placement and PlacementTarget have been set, we'll calculate the
    // horizontal offset and vertical offset needed to place the popup at the right location to honor those
    // values.  These will be added to the values of HorizontalOffset and VerticalOffset and the above adjustments
    // to give us the final xy-position at which we'll place the popup.
    private double m_calculatedHOffset;
    private double m_calculatedVOffset;

	// m_fIsLightDismiss is only set when opening the popup if m_fIsLightDismissEnabled is TRUE at this time,
	// and is cleared when closing the popup.  If a popup is opened as light-dismiss-enabled, it will remain
	// light-dismiss-enabled as long as it is shown.  We don't want to handle changing light-dismiss-enabled
	// state for open popups.
	internal bool m_fIsLightDismiss;

	private LightDismissOverlayMode m_lightDismissOverlayMode;

	private bool m_fAsyncQueueOnRelease;

	// Normally, the overlay is only shown for light-dismiss popups, but for controls that roll their own
	// light-dismiss logic (and therefore configure their popup's to not be light-dismiss) we still want
	// to re-use the popup's overlay code.  This flag provides a mechanism to disable the check for
	// IsLightDismissEnabled.
	private bool m_disableOverlayIsLightDismissCheck;
	private bool m_fIsOpen;
	private bool m_fIsContentDialog;
	private bool m_fIsSubMenu;

	// Is this the ApplicationBarService popup, which is used to host app bars?
	private bool m_fIsApplicationBarService;

	// m_fIsLightDismissEnabled can be set at any time when the customer sets IsLightDismissEnabled.
	private bool m_fIsLightDismissEnabled;

	// Holds a value indicating whether or not the popup has been placed in its own window.
	private bool m_isWindowed;

	 // Remember the last island that opened this popup. A popup can move between islands between closing and reopening.
    // Xaml uses a single shared context menu for all its TextBoxes, for example. If this popup is associated with a
    // different island then the content bridge and input site adapter needs to be re-created.
    private HWND m_previousXamlIslandHwnd;

    // The Popup.SystemBackdrop set on this popup. We will call its OnTargetDisconnected when the SystemBackdrop
    // property changes. Note that by the time we get to the CPopup::SetValue override, the new value has already
    // been set, so we can't do a GetValue lookup for the previous value, and we have to cache it ourselves.
    private SystemBackdrop m_systemBackdrop;

	private struct ReentrancyGuard : IDisposable //TODO:MZ:Can it be done this way?
	{
		private Popup m_popup;
		
		public ReentrancyGuard(Popup popup)
		{
			m_popup = popup;
			m_popup.m_isClosing = true;
		}

		public void Dispose()
		{
			m_popup.m_isClosing = false;
		}
	}

	private bool m_fRemovingListeners;
	private bool m_fMadeChildNamescopeOwner;
	private bool m_fIsPrintDirty;
	private bool m_wasMarkedAsRedirectionElementOnEnter;
	private bool m_wasMarkedAsRedirectionElementOnOpen;
	private bool m_isRegisteredOnCore;
	private bool m_shouldTakeFocus;
	//// Was this popup ever opened?
	private bool m_everOpened;

	// If this popup is being kept visible, then there might be a pending close operation that needs to complete when
	// it's no longer being kept visible. It might also be kept visible because it has VIsibility="Collapsed", in which
	// case there's nothing to do when it's no longer being kept visible anymore.
	private bool m_closeIsPending;

	private bool m_isClosing;

	// Unloading popups are kept in the CPopupRoot's list of open popups, but aren't considered open. They're in that
	// list so they can continue rendering and play a close animation. They're marked by this flag.
	private bool m_isUnloading;

	// Xaml implicitly closes popups in situations like changing the child or un-parenting from the tree. These close
	// operations shouldn't cause UIE.Hidden to be raised and shouldn't call RequestKeepAlive expecting a UIE.Hidden
	// later. This flag guards against that.
	private bool m_isImplicitClose;

	// Debug flag. If someone opens a windowed popup after the main Xaml window has been destroyed, we'll no-op instead
	// of trying to create an HWND for it. In these cases Xaml is shutting down.
	private bool m_skippedCreatingPopupHwnd;

	private bool m_registeredWindowPositionChangedHandler;

	private int m_cEventListenerCount;
	private ITransformer m_pTransformer;

	// Maintain a weak reference to the object that was focused before opening a light-dismiss-enabled popup.
	// When the light-dismiss-enabled popup is closed, return focus to that object.
	private ManagedWeakReference m_pPreviousFocusWeakRef;

	// When opening light-dismiss popup, retain the type of focus that was on the last focused element.
	private FocusState m_savedFocusState;

	//// When opening a flyout/light-dismiss popup, retain the horizontal and vertical manifolds
	//Focus::XYFocus::Manifolds m_savedXYFocusManifolds;

	// When closing a light-dismiss-enabled popup, remember which input mode caused us to close, and
	// set this as the new focus state for m_pPreviousFocusWeakRef.
	internal FocusState m_focusStateAfterClosing;

	// Windowed popup's HWND
	private HWND m_windowedPopupWindow;

	// Class registration for Windowed popup's HWND
	private static ATOM s_windowedPopupWindowClass;

	// See banner of CPopup::EnsureWindowedPopupRootVisualTree for the full tree.
    wrl::ComPtr<ixp::IVisual> m_contentIslandRootVisual;
    wrl::ComPtr<ixp::IVisual> m_windowedPopupDebugVisual;
    wrl::ComPtr<ixp::IVisual> m_animationRootVisual;
    wrl::ComPtr<ixp::IContentExternalBackdropLink> m_backdropLink;
    wrl::ComPtr<ixp::IVisual> m_systemBackdropPlacementVisual;
    wgr::RectInt32 m_windowedPopupMoveAndResizeRect = {};

    // The prepend visual from Popup.Child's comp node
    // This is the topmost Visual in the popup's tree that corresponds to a UIElement. Everything above this is an
    // internal Visual that handles some Popup feature. See banner of CPopup::EnsureWindowedPopupRootVisualTree for the
    // full tree. This corresponds to the "public root" concept in the main tree, so we name it the same.
    wrl::ComPtr<ixp::IVisual> m_publicRootVisual;

	// Window root DComp visual for windowed popup. It is the real root visual and
	// parent of m_spPopupRootDCompositionVisual below.
	Microsoft::WRL::ComPtr<WUComp::IVisual> m_spWindowRootVisual;
	Microsoft::WRL::ComPtr<WUComp::IVisual> m_windowedPopupDebugVisual;

	//// Windowed popup's root DComp visual
	//Microsoft::WRL::ComPtr<WUComp::IVisual> m_spPopupRootDCompositionVisual;

	//// Windowed popup's UIA support
	//xref_ptr<CUIAHostWindow> m_spUIAWindow;

	private bool m_isOverlayVisible;

	private bool m_wasOpenedDuringEngagement;

	// AssociatedFlyout uses the weak reference to prevent the reference cycle
	// between the associated FlyoutBase and Popup
	private ManagedWeakReference m_associatedFlyoutWeakRef;

	// wrl::ComPtr<ixp::IPopupWindowSiteBridge> m_popupWindowBridge;
    // wrl::ComPtr<ixp::IDesktopSiteBridge> m_desktopBridge;
    // wrl::ComPtr<ixp::IContentIsland> m_contentIsland;
    // std::unique_ptr<WindowedPopupInputSiteAdapter> m_inputSiteAdapter{nullptr};
    // wf::Point m_offsetFromMainWindow{};
    // EventRegistrationToken m_automationProviderRequestedToken = {};
    // EventRegistrationToken m_bridgeClosedToken {};

    // // True if the m_popupWindowBridge/m_desktopBridge has been closed unexpectedly.
    // bool m_bridgeClosed {false};

    // // This is temporary fix to ensure that a popup children leave the tree correctly.
    // // http://osgvsowi/19548424 - Remove this field and find a better fix
    // xref::weakref_ptr<CDependencyObject> m_cachedNamescopeOwner;

    // // Note: We use a "secret" CTransitionTarget, rather than a real one connected through the
    // // UIElement_TransitionTarget property. A real TransitionTarget will be detected and picked up by the render walk,
    // // which then results in a TransitionClip Visual created in the comp node. We don't have a size set, so a
    // // TransitionClip (with insets of 0) will just clip out the entire popup. Use a secret CTransitionTarget instead
    // // and pick up its expression explicitly in popup code, rather than rely on the render walk.
    // xref_ptr<CTransitionTarget> m_secretTransitionTarget;
    // wrl::ComPtr<ixp::IExpressionAnimation> m_entranceAnimationExpression;
}
