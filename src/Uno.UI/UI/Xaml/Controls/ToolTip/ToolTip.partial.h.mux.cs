// MUX Reference dxaml\xcp\dxaml\lib\ToolTip_Partial.h, tag 5f9e85113
// Contains ported field/constant declarations from ToolTip_Partial.h
//
// C++ uses #define for the constants below; we surface them as
// internal compile-time constants on the ToolTip class to match call-site usage
// (e.g. ToolTip.DEFAULT_MOUSE_OFFSET) without polluting a top-level namespace.

#if __SKIA__

#nullable enable

using System;
using DirectUI;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.Disposables;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

public partial class ToolTip : ContentControl
{
	// Used in PlacementMode.Mouse positioning to avoid screen edges.
	internal const double TOOLTIP_TOLERANCE = 2.0;

	// Default offset for automatic tooltips opened by keyboard.
	internal const double DEFAULT_KEYBOARD_OFFSET = 12;

	// Default offset for automatic tooltips opened by mouse.
	internal const double DEFAULT_MOUSE_OFFSET = 20;

	// Default offset for automatic tooltips opened by touch.
	internal const double DEFAULT_TOUCH_OFFSET = 44;

	// The hint is slightly above center in the vertical axis.
	internal const double CONTEXT_MENU_HINT_VERTICAL_OFFSET = -5;

#pragma warning disable CS0067 // Event is declared but never used (placeholder until Phase 2)
#pragma warning disable CS0169 // Field is never used (placeholder until Phase 2)
#pragma warning disable CS0414 // Field is assigned but its value is never used (placeholder)
#pragma warning disable CS0649 // Field is never assigned to (placeholder)
#pragma warning disable IDE0044 // Add readonly modifier (placeholder)
#pragma warning disable IDE0051 // Remove unused private members (placeholder)
#pragma warning disable IDE0052 // Remove unread private members (placeholder)

	// friend class DxamlCoreTestHooks;

	// === private: (ToolTip_Partial.h lines 25-56) ===

	private WeakReference? m_wrOwner;
	private WeakReference? m_wrContainer;
	private WeakReference? m_wrTargetOverride;
	private WeakReference? m_wrPopup;
	private PlacementMode? m_pToolTipServicePlacementModeOverride;
	private bool m_bIsPopupPositioned;
	private bool m_bClosing;
	private bool m_bIsOpenAsAutomaticToolTip;
	private bool m_bCallPerformPlacementAtNextPopupOpen;

	//
	// Bug 19931042: Hyperlink hover shows white box
	//
	// Opening a ToolTip is an asynchronous process, and we start the fade in transition via VSM after getting
	// the Popup.Opened event on some later frame. The OnPopupOpened event handler depends on other state flags to
	// make the VSM state transitions. Specifically, it needs m_bCallPerformPlacementAtNextPopupOpen == true to
	// call PerformPlacement which does the state transition, and PerformPlacement requires !m_bIsPopupPositioned
	// to do the state transition. Rapidly opening and closing the ToolTip can delay the Popup.Opened event enough
	// that these flags get overwritten by the time the first Popup.Opened arrives, at which point we've already
	// set m_bIsPopupPositioned and skip doing the state transition to "Opened". Subsequent Popup.Opened events
	// get ignored because we already cleared the m_bCallPerformPlacementAtNextPopupOpen flag when responding to
	// that first Popup.Opened event. We're then left in the "Closed" state and the ToolTip content never fades in.
	//
	// This counter tracks the number of times we've called CPopup::Open without receiving its Popup.Opened event.
	// If this counter is ever greater than 1, it means we've since closed the ToolTip and reopened it, and we
	// don't do anything until we receive that final Popup.Opened event. At that point we'll be in a consistent
	// state and we'll be able to make the transition to the "Opened" state.
	//
	private int m_pendingPopupOpenEventCount = 0;

	// Mouse placement vertical offset
	private const uint m_mousePlacementVerticalOffset = 11;

	// === public: (ToolTip_Partial.h lines 58-67) ===

	// Uno equivalent of EventRegistrationToken: a SerialDisposable revoker.
	internal readonly SerialDisposable m_ownerPointerEnteredToken = new SerialDisposable();
	internal readonly SerialDisposable m_ownerPointerExitedToken = new SerialDisposable();
	internal readonly SerialDisposable m_ownerPointerCaptureLostToken = new SerialDisposable();
	internal readonly SerialDisposable m_ownerPointerCanceledToken = new SerialDisposable();
	internal readonly SerialDisposable m_ownerGotFocusToken = new SerialDisposable();
	internal readonly SerialDisposable m_ownerLostFocusToken = new SerialDisposable();
	internal bool m_bInputEventsHookedUp;
	internal AutomaticToolTipInputMode m_inputMode;
	internal bool m_isSliderThumbToolTip;

	// Top - Default PlacementMode for ToolTips in Jupiter.
	internal const PlacementMode DefaultPlacementMode = PlacementMode.Top;

	// === private fields (ToolTip_Partial.h lines 272-285) ===

	// Uno: SerialDisposable revokers replace EventRegistrationToken + ComPtr handler pairs.
	private readonly SerialDisposable m_xamlIslandRootPointerMovedHandler = new SerialDisposable();
	private readonly SerialDisposable m_xamlIslandRootKeyDownHandler = new SerialDisposable();
	private readonly SerialDisposable m_xamlIslandRootKeyUpHandler = new SerialDisposable();

	private readonly SerialDisposable m_ownerLayoutUpdatedToken = new SerialDisposable();

	// only Ctrl Down, then Ctrl Up dismiss the ToolTip
	private bool m_lastKeyDownIsControlOnly = false;

	// When ToolTip owner position changed, we should dismiss the tooltip.
	private Rect m_ownerBounds;

	private bool m_isToolTipRequestedThemeOverridden = false;

#pragma warning restore IDE0052
#pragma warning restore IDE0051
#pragma warning restore IDE0044
#pragma warning restore CS0649
#pragma warning restore CS0414
#pragma warning restore CS0169
#pragma warning restore CS0067
}

#endif // __SKIA__