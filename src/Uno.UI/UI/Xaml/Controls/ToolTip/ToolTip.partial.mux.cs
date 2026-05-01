// MUX Reference dxaml\xcp\dxaml\lib\ToolTip_Partial.cpp, tag 5f9e85113
// Contains ported portions of dxaml\xcp\dxaml\lib\ToolTip_Partial.cpp
//
// NOTE: Constants and field declarations live in ToolTip.partial.h.mux.cs
// (port of ToolTip_Partial.h). This file ports method bodies in the order
// they appear in ToolTip_Partial.cpp.

#if __SKIA__

#nullable enable

using DirectUI;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

public partial class ToolTip : ContentControl
{
#pragma warning disable IDE0051 // Remove unused private members (placeholder until Phase 2 ports the dispatch wiring)
#pragma warning disable IDE0060 // Remove unused parameter (placeholder)

	// Phase 0 scaffolding: ToolTip_Partial.cpp::ToolTip() (line 24).
	// Phase 2 will replace this body with the faithful port and add the
	// ~ToolTip() destructor (line 36) wiring.
	public ToolTip()
	{
		DefaultStyleKey = typeof(ToolTip);
	}

	// Phase 0 scaffolding: public DependencyProperties consumed by external callers
	// (TabView, ColorPickerSlider, etc.). The Generated NotImplemented stubs in
	// Generated/3.0.0.0/Microsoft.UI.Xaml.Controls/ToolTip.cs skipped IsOpen/Placement
	// because they were declared in the now-gated ToolTip.cs; we re-declare them
	// here for Skia. Phase 2 will replace OnIsOpenChanged with the faithful port.

	public static DependencyProperty IsOpenProperty { get; } =
		DependencyProperty.Register(
			nameof(IsOpen), typeof(bool),
			typeof(ToolTip),
			new FrameworkPropertyMetadata(default(bool), OnIsOpenChanged));

	public bool IsOpen
	{
		get => (bool)GetValue(IsOpenProperty);
		set => SetValue(IsOpenProperty, value);
	}

	private static void OnIsOpenChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		// TODO Uno: Phase 2 will port OnIsOpenChanged from ToolTip_Partial.cpp.
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

#pragma warning disable CS0067 // Event is never used in this scaffolding stub.
	public event RoutedEventHandler? Opened;

	public event RoutedEventHandler? Closed;
#pragma warning restore CS0067

	// Phase 0 scaffolding: invoked by Slider.mux.cs to anchor the slider thumb tooltip
	// to the thumb element. Full port lives in ToolTip_Partial.cpp::SetOwner (line 45)
	// and SetContainer (line 72); will be reconciled in Phase 2.
	public void SetAnchor(UIElement element)
	{
		// TODO Uno: Phase 2 will port SetOwner / SetContainer faithfully.
	}

	// Phase 0 scaffolding: invoked by Slider.mux.cs to reposition the slider thumb
	// tooltip while dragging. Full port lives in ToolTip_Partial.cpp::PerformPlacement
	// (line ~830); will be reconciled in Phase 5.
	internal void PerformPlacement(Rect? pTargetRect = null)
	{
		// TODO Uno: Phase 5 will port PerformPlacement faithfully.
	}

	// Removes the "automatic" flag and clears associated fields.
	//
	// For Slider, the Thumb ToolTip may be opened as an automatic ToolTip by pointer hover.  However, if
	// we click on the Thumb and start to drag, we don't want the ToolTip to disappear after several seconds.
	// Thus, we remove the automatic flag and keep the ToolTip open for Slider to handle.
	[NotImplemented]
	internal void RemoveAutomaticStatusFromOpenToolTip()
	{
		// TODO Uno: Phase 2 will port RemoveAutomaticStatusFromOpenToolTip from
		// ToolTip_Partial.cpp.
	}

#pragma warning restore IDE0060
#pragma warning restore IDE0051
}

#endif // __SKIA__