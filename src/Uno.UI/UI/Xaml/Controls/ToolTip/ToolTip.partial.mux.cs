// MUX Reference dxaml\xcp\dxaml\lib\ToolTip_Partial.cpp, tag 5f9e85113
// Contains ported portions of dxaml\xcp\dxaml\lib\ToolTip_Partial.cpp
//
// NOTE: Constants and field declarations live in ToolTip.partial.h.mux.cs
// (port of ToolTip_Partial.h). This file ports method bodies in the order
// they appear in ToolTip_Partial.cpp.

#if __SKIA__

#nullable enable

using System;
using DirectUI;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

public partial class ToolTip : ContentControl
{
	// === WinUI Generated stubs (these come from ToolTip.idl / ToolTip.g.cpp in WinUI;
	// ===  they are not ported from ToolTip_Partial.cpp but Skia needs them because
	// ===  the gated cross-platform ToolTip.cs declared them and the source generator
	// ===  consequently skipped emitting them in Generated/Microsoft.UI.Xaml.Controls/ToolTip.cs.)

	public static DependencyProperty IsOpenProperty { get; } =
		DependencyProperty.Register(
			nameof(IsOpen), typeof(bool),
			typeof(ToolTip),
			new FrameworkPropertyMetadata(default(bool), OnIsOpenChangedStatic));

	public bool IsOpen
	{
		get => (bool)GetValue(IsOpenProperty);
		set => SetValue(IsOpenProperty, value);
	}

	public static DependencyProperty PlacementProperty { get; } =
		DependencyProperty.Register(
			nameof(Placement), typeof(PlacementMode),
			typeof(ToolTip),
			new FrameworkPropertyMetadata(DefaultPlacementMode));

	public PlacementMode Placement
	{
		get => (PlacementMode)GetValue(PlacementProperty);
		set => SetValue(PlacementProperty, value);
	}

#pragma warning disable CS0067 // Event is never used until OnIsOpenChanged dispatches in Phase 2 closeout.
	public event RoutedEventHandler? Opened;

	public event RoutedEventHandler? Closed;
#pragma warning restore CS0067

	// === Port from ToolTip_Partial.cpp in source order ===

	// MUX Reference: ToolTip_Partial.cpp ToolTip() (line 24).
	public ToolTip()
	{
		DefaultStyleKey = typeof(ToolTip);

		m_pToolTipServicePlacementModeOverride = null;
		m_bIsPopupPositioned = false;
		m_bClosing = false;
		m_bInputEventsHookedUp = false;
		m_bIsOpenAsAutomaticToolTip = false;
		m_bCallPerformPlacementAtNextPopupOpen = false;
		m_inputMode = AutomaticToolTipInputMode.None;
		m_isSliderThumbToolTip = false;

		// MUX Reference: ToolTip_Partial.cpp Initialize() (line 92).
		// C# folds the framework Initialize() lifecycle into the constructor since the
		// generated base does not expose a virtual Initialize hook.
		SizeChanged += OnToolTipSizeChanged;
	}

	// MUX Reference: ToolTip_Partial.cpp ~ToolTip() (line 36).
	// The C++ destructor unhooks owner layout-changed and Xaml-island root handlers,
	// and frees the heap-allocated m_pToolTipServicePlacementModeOverride.
	// In C#, GC handles the field; the SerialDisposable revokers (m_ownerLayoutUpdatedToken,
	// m_xamlIslandRoot* handlers) are disposed by their owners. Phase 6 will wire the
	// explicit Unhook* calls below; for now they are no-ops because the hookups have not
	// been ported yet.
#if false
	~ToolTip()
	{
		VERIFYHR(UnhookOwnerLayoutChangedEvent());
		VERIFYHR(UnhookFromXamlIslandRoot());

		delete m_pToolTipServicePlacementModeOverride;
		m_pToolTipServicePlacementModeOverride = NULL;
	}
#endif

	// MUX Reference: ToolTip_Partial.cpp SetOwner (line 45).
	internal void SetOwner(DependencyObject? pNewOwner)
	{
		// TODO Uno (Phase 6): IFC_RETURN(UnhookOwnerLayoutChangedEvent());

		m_wrOwner = null;

		if (pNewOwner is not null)
		{
			m_wrOwner = new WeakReference(pNewOwner);
		}
	}

	// Phase 0 scaffolding shim: Slider.mux.cs calls SetAnchor (Uno's historical name).
	// Per feedback_winui_port_gating_pattern.md we keep the public surface intact and
	// delegate to the faithful SetOwner port. Slider.mux.cs migration to SetOwner can
	// happen later once the rest of the port is validated.
	public void SetAnchor(UIElement element) => SetOwner(element);

	// MUX Reference: ToolTip_Partial.cpp GetContainer (line 61).
	internal FrameworkElement? GetContainer()
	{
		return m_wrContainer?.Target as FrameworkElement;
	}

	// MUX Reference: ToolTip_Partial.cpp SetContainer (line 72).
	internal void SetContainer(FrameworkElement? pNewContainer)
	{
		object? spContainerDataContext = null;

		m_wrContainer = null;

		if (pNewContainer is not null)
		{
			m_wrContainer = new WeakReference(pNewContainer);
			spContainerDataContext = pNewContainer.DataContext;
		}

		// If pNewContainer is NULL, we'll clear the DataContext here.
		this.DataContext = spContainerDataContext;
	}

	// MUX Reference: ToolTip_Partial.cpp HookupParentPopup (line 113).
	internal Popup HookupParentPopup()
	{
		IsTabStop = false;
		IsHitTestVisible = false;

		var spTarget = GetTarget();

		var spPopup = new Popup();

		// Set the Popup's ToolTip owner : This is a hint required by XamlIslandRoots
		// ToolTips use parentless popups.  ToolTip targets point to an element in the
		// XamlIslandRoot's tree and can be walked to find the root for that XamlIslandRoot.
		// The popup can then be rooted under the correct PopupRoot so it displays
		// relative to the island position.
#if false
		auto popup = do_pointer_cast<CPopup>(spPopup->GetHandle());
		if(popup)
		{
			popup->m_toolTipOwnerWeakRef = xref::get_weakref(static_cast<FrameworkElement*>(spTarget.Get())->GetHandle());
		}
#endif
		// TODO Uno (Phase 6): port the m_toolTipOwnerWeakRef hint once XamlIslandRoot
		// support lands on Skia. The XamlIslandRoot routing it enables is gated on
		// Phase 6 hookups.

		// ToolTip sets the location to the out of Xaml Window by using the windowed Popup in Threshold Windows
#if false
		if (CPopup::DoesPlatformSupportWindowedPopup(DXamlCore::GetCurrent()->GetHandle()) &&
			!static_cast<CPopup*>(spPopup.Cast<Popup>()->GetHandle())->IsWindowed())
		{
			// Set the windowed popup to support the render popup at the out of Xaml window
			IFC_RETURN(static_cast<CPopup*>(spPopup.Cast<Popup>()->GetHandle())->SetIsWindowed());
			ASSERT(static_cast<CPopup*>(spPopup.Cast<Popup>()->GetHandle())->IsWindowed());
		}
#endif
		// TODO Uno (Phase 5): port the windowed-popup configuration once Skia exposes
		// the equivalent of CPopup::SetIsWindowed.

		// Don't show the Popup for disabled ToolTips.
		bool bIsEnabled = IsEnabled;
		if (!bIsEnabled)
		{
			spPopup.Opacity = 0;
		}

		if (spTarget is not null)
		{
			// Propagate FlowDirection from placement target to popup
			var spFlowDirectionBinding = new Data.Binding
			{
				Source = spTarget,
				Path = new PropertyPath("FlowDirection"),
			};

			spPopup.SetBinding(FrameworkElement.FlowDirectionProperty, spFlowDirectionBinding);

			// Propagate Language from placement target to popup
			spPopup.Language = spTarget.Language;
		}

		// Listening to the Opened and Closed events lets us guarantee that
		// the popup is actually opened when we perform those functions.

		spPopup.Opened += OnPopupOpened;
		spPopup.Closed += OnPopupClosed;

#if false
		IFC_RETURN(spPopup.Cast<Popup>()->SetOwner(this));
#endif
		// TODO Uno (Phase 5): Popup.SetOwner is internal in WinUI for hit-testing
		// routing back to the owning ToolTip. Uno's Popup does not yet expose this hook.

		return spPopup;
	}

	// MUX Reference: ToolTip_Partial.cpp OnPropertyChanged2 (line 211).
	// TODO Uno (Phase 2 closeout): Hook into OnPropertyChanged dispatch so IsOpen,
	// HorizontalOffset, VerticalOffset, PlacementRect changes route through the port.
	// For Phase 2 we approximate via the IsOpen DP-callback (OnIsOpenChangedStatic below).

	private static void OnIsOpenChangedStatic(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		// TODO Uno: Phase 2 closeout will wire OnIsOpenChanged faithfully (line 264 of C++).
		// For now this is a no-op; the Phase 3 ToolTipService.OpenAutomaticToolTip path
		// handles popup hosting.
	}

	// MUX Reference: ToolTip_Partial.cpp OnCreateAutomationPeer (line 251).
	protected override AutomationPeer OnCreateAutomationPeer()
	{
		return new ToolTipAutomationPeer(this);
	}

	// MUX Reference: ToolTip_Partial.cpp OnToolTipSizeChanged (referenced from Initialize, body further down in the file).
	// Phase 5 (placement) will replace this stub with the faithful port.
	private void OnToolTipSizeChanged(object sender, SizeChangedEventArgs args)
	{
		// TODO Uno (Phase 5): port the SizeChanged-driven re-placement.
	}

	// MUX Reference: ToolTip_Partial.cpp GetTarget (line 696).
	// help method to get the target from placement override or placement target.
	internal FrameworkElement? GetTarget()
	{
		// If the ToolTipService is opening the ToolTip, then its owner is the placement target,
		// regardless of what the PlacementTarget has been set to.
		var spTarget = m_wrTargetOverride?.Target as FrameworkElement;
		if (spTarget is null)
		{
			var spTargetAsUIElement = PlacementTarget;
			spTarget = spTargetAsUIElement as FrameworkElement;
		}

		return spTarget;
	}

	// === Phase 0 scaffolding methods retained until their corresponding C++ port lands ===

	// Phase 0 scaffolding: invoked by Slider.mux.cs to reposition the slider thumb
	// tooltip while dragging. Full port lives in ToolTip_Partial.cpp::PerformPlacement
	// (line ~830); will be reconciled in Phase 5.
	internal void PerformPlacement(Rect? pTargetRect = null)
	{
		// TODO Uno: Phase 5 will port PerformPlacement faithfully.
	}

	// MUX Reference: ToolTip_Partial.cpp OnPopupOpened (line 1911).
	// Phase 2 closeout will port the AutomationPeer raise + pendingPopupOpenEventCount logic.
	private void OnPopupOpened(object? pUnused1, object pUnused2)
	{
		// TODO Uno: Phase 2 closeout will port OnPopupOpened faithfully.
	}

	// MUX Reference: ToolTip_Partial.cpp OnPopupClosed (line 1945).
	// Phase 2 closeout will port the AutomationPeer raise + m_bIsPopupPositioned reset.
	private void OnPopupClosed(object? pUnused1, object pUnused2)
	{
		// TODO Uno: Phase 2 closeout will port OnPopupClosed faithfully.
		m_bIsPopupPositioned = false;
	}

	// MUX Reference: ToolTip_Partial.cpp RemoveAutomaticStatusFromOpenToolTip (later in file).
	//
	// For Slider, the Thumb ToolTip may be opened as an automatic ToolTip by pointer hover.  However, if
	// we click on the Thumb and start to drag, we don't want the ToolTip to disappear after several seconds.
	// Thus, we remove the automatic flag and keep the ToolTip open for Slider to handle.
	[NotImplemented]
	internal void RemoveAutomaticStatusFromOpenToolTip()
	{
		// TODO Uno: Phase 2 closeout / Phase 3 will port RemoveAutomaticStatusFromOpenToolTip.
	}
}

#endif // __SKIA__