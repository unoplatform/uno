// MUX Reference dxaml\xcp\dxaml\lib\ToolTip_Partial.cpp, tag 5f9e85113
// Contains ported portions of dxaml\xcp\dxaml\lib\ToolTip_Partial.cpp

#if __SKIA__

#nullable enable

using DirectUI;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

public partial class ToolTip : ContentControl
{
	// Default offset for automatic tooltips opened by mouse.
	internal const double DEFAULT_MOUSE_OFFSET = 20;

	// Default offset for automatic tooltips opened by touch.
	internal const double DEFAULT_TOUCH_OFFSET = 44;

	// Top - Default PlacementMode for ToolTips in Jupiter.
	internal const PlacementMode DefaultPlacementMode = PlacementMode.Top;

#pragma warning disable CS0649 // Field is assigned externally (e.g. from Slider.mux.cs) but never read in this scaffolding stub.
#pragma warning disable CS0414 // Field assigned but its value is never used in this scaffolding stub.
#pragma warning disable CS0067 // Event is never used in this scaffolding stub.
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable IDE0052 // Remove unread private members

	internal AutomaticToolTipInputMode m_inputMode;
	internal bool m_isSliderThumbToolTip;

	// Initializes a new instance of the ToolTip class.
	public ToolTip()
	{
		DefaultStyleKey = typeof(ToolTip);
	}

	// Phase 0 scaffolding: public DependencyProperties consumed by external callers
	// (TabView, ColorPickerSlider, etc.). The full faithful port replaces these in
	// subsequent phases - order will be reconciled with C++ source line order
	// (ToolTip_Partial.cpp / ToolTip_Partial.h) as the port lands.

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

	public event RoutedEventHandler? Opened;

	public event RoutedEventHandler? Closed;

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

#pragma warning restore IDE0052
#pragma warning restore IDE0051
#pragma warning restore IDE0044
#pragma warning restore CS0067
#pragma warning restore CS0414
#pragma warning restore CS0649
}

#endif // __SKIA__