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

	// === Phase 0 scaffolding methods retained until their corresponding C++ port lands ===

	// Phase 0 scaffolding: invoked by Slider.mux.cs to reposition the slider thumb
	// tooltip while dragging. Full port lives in ToolTip_Partial.cpp::PerformPlacement
	// (line ~830); will be reconciled in Phase 5.
	internal void PerformPlacement(Rect? pTargetRect = null)
	{
		// TODO Uno: Phase 5 will port PerformPlacement faithfully.
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