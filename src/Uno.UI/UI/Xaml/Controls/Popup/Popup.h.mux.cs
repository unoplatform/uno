// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference Popup.h, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using Microsoft.UI.Xaml.Shapes;
using Uno.UI.DataBinding;
using Uno.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class Popup
{
	private UIElement? m_pChild;
	private bool m_fIsOpen;
	private bool m_fIsLightDismiss;
	private bool m_fMadeChildNamescopeOwner;
	private bool m_everOpened;

	// Adjustments made to the offset by a popup placed relative to this one, to bring itself into view.
	internal float m_hAdjustment;
	internal float m_vAdjustment;

	// Offsets calculated by SetPositionFromPlacement.
	internal float m_calculatedHOffset;
	internal float m_calculatedVOffset;

	private PopupPlacementMode m_actualPlacement;

	// Pointer to element that has focus before the popup is opened
	private ManagedWeakReference? m_pPreviousFocusWeakRef;
	internal FocusState m_focusStateAfterClosing = FocusState.Unfocused;
	private FocusState m_savedFocusState = FocusState.Unfocused;
	private bool m_shouldTakeFocus = true;
	private XYFocus.Manifolds m_savedXYFocusManifolds = new();

	private ManagedWeakReference? m_associatedFlyoutWeakRef;

	private Rectangle? m_overlayElement;
	private bool m_isOverlayVisible;
	private bool m_wasOpenedDuringEngagement;

	// True when the popup is closing but kept visible for an implicit hide animation.
	private bool m_closeIsPending;
	private bool m_isClosing;
	private bool m_isUnloading;

	private bool m_isWindowed;

	// Stands in for m_desktopPopupSiteBridge and m_popupWindowBridge.
	private WindowedPopupWindow? m_windowedPopupWindow;
	private XamlRoot? m_previousXamlRoot;
	private global::Windows.Graphics.RectInt32 m_windowedPopupMoveAndResizeRect;
	private global::Windows.Graphics.PointInt32 m_rootOffsetPhysical;
	private global::Windows.Foundation.Point m_offsetFromMainWindow;

	internal bool IsSubMenu { get; set; }

	internal bool IsContentDialog { get; set; }

	internal bool IsFlyout => AssociatedFlyout is not null;

	internal FlyoutBase? AssociatedFlyout
	{
		get => m_associatedFlyoutWeakRef?.Target as FlyoutBase;
		set
		{
			WeakReferencePool.ReturnWeakReference(this, m_associatedFlyoutWeakRef);
			m_associatedFlyoutWeakRef = WeakReferencePool.RentWeakReference(this, value);
		}
	}

	internal bool IsOpenInternal => m_fIsOpen;

	internal bool WasEverOpened => m_everOpened;

	internal bool WasOpenedDuringEngagement => m_wasOpenedDuringEngagement;

	internal void SetOpenedDuringEngagement(bool value) => m_wasOpenedDuringEngagement = value;

	internal bool IsSelfOrAncestorLightDismiss => m_fIsLightDismiss || IsSubMenu;

	// Whether the popup currently acts as a light-dismiss surface, which IsLightDismissEnabled alone doesn't tell.
	internal bool IsLightDismiss => m_fIsLightDismiss;

	// m_disableOverlayIsLightDismissCheck (Popup.DisableOverlayIsLightDismissCheck)
	internal bool DisableOverlayIsLightDismissCheck { get; set; }

	internal bool IsUnloading => m_isUnloading;

	internal bool IsWindowed => m_isWindowed;

	internal bool IsToolTip => m_pChild is ToolTip;

	internal FrameworkElement? OverlayElement => m_overlayElement;

	internal void SetShouldTakeFocus(bool value) => m_shouldTakeFocus = value;

	// We don't check IsActive() because we want the focus logic to also work with unrooted popups,
	// which are never in the live tree.
	internal override bool IsFocusable =>
		m_fIsLightDismiss &&
		IsVisible() &&
		IsEnabledInternal() &&
		AreAllAncestorsVisible();

	// Uno: relative to the content root, while WinUI's is in screen coordinates. The rest of the contract is WinUI's:
	// physical pixels, drop shadow gutters included.
	internal global::Windows.Foundation.Rect GetWindowedPopupMoveAndResizeRect() => new(
		m_windowedPopupMoveAndResizeRect.X - m_rootOffsetPhysical.X,
		m_windowedPopupMoveAndResizeRect.Y - m_rootOffsetPhysical.Y,
		m_windowedPopupMoveAndResizeRect.Width,
		m_windowedPopupMoveAndResizeRect.Height);

	internal global::Windows.Foundation.Point GetTranslationFromMainWindow() => m_offsetFromMainWindow;

	internal global::Uno.UI.Xaml.Controls.IWindowedPopupSite? WindowedPopupSite => m_windowedPopupWindow?.Site;

	internal static global::Windows.Graphics.PointInt32 GetScreenOffsetForTesting(XamlRoot xamlRoot) => GetWindowedPopupExtension()?.GetScreenOffset(xamlRoot) ?? default;
}
