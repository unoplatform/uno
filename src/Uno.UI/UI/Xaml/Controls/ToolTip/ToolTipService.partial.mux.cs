// MUX Reference dxaml\xcp\dxaml\lib\ToolTipService_Partial.cpp, tag 5f9e85113
// Contains ported portions of dxaml\xcp\dxaml\lib\ToolTipService_Partial.cpp

#if __SKIA__

#nullable enable

namespace Microsoft.UI.Xaml.Controls;

public partial class ToolTipService
{
	// Phase 0 scaffolding: the public attached-property registrations in
	// ToolTipService.Properties.cs reference these callbacks. The full faithful
	// port replaces this scaffolding in subsequent phases, mapped 1:1 to
	// ToolTipService_Partial.cpp::OnToolTipChanged (line ~525) and the
	// PlacementProperty registration metadata. Order will be reconciled with
	// C++ source line order as the port lands.

	private static void OnToolTipChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		// TODO Uno: Phase 3 will port the ToolTipService.OnToolTipChanged dispatch
		// (RegisterToolTip / UnregisterToolTip) faithfully from the C++ source.
	}

	private static void OnPlacementChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		// TODO Uno: Phase 3 will mirror the cross-platform Placement-update behavior
		// from ToolTipService_Partial.cpp.
	}

	internal static ToolTip? GetActualToolTipObject(DependencyObject element)
	{
		// TODO Uno: Phase 3 will port GetActualToolTipObjectStatic faithfully from
		// ToolTipService_Partial.cpp (~line 779).
		var toolTip = ToolTipService.GetToolTipReference(element);
		if (toolTip is null)
		{
			toolTip = ToolTipService.GetKeyboardAcceleratorToolTipObject(element);
		}
		return toolTip;
	}

	// MUX Reference: ToolTipService_Partial.cpp EnsureHandlersAttachedToRootElement (later in file).
	// Phase 4 (pointer + focus event handling) will port this faithfully. Currently a stub
	// so OpenPopup compiles.
	internal static void EnsureHandlersAttachedToRootElement(XamlRoot? visualTree)
	{
		// TODO Uno: Phase 4 will port EnsureHandlersAttachedToRootElement.
	}
}

// Phase 0 scaffolding: Slider.mux.cs calls ToolTipPositioning.IsLefthandedUser().
// Full port lives under ToolTipService_Partial.h (lines 331-435) and
// ToolTipService_Partial.cpp; will be reconciled in Phase 5.
internal static class ToolTipPositioning
{
	internal static bool IsLefthandedUser() => false;
}

#endif // __SKIA__