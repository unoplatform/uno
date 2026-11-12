#nullable enable

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Geometry of a scroll port, used to mirror it with a native scroller (e.g. the WebAssembly semantic DOM).
/// Offsets, extent and scrollable range are all in zoomed content coordinates.
/// </summary>
internal interface IScrollPortGeometry
{
	double HorizontalOffset { get; }

	double VerticalOffset { get; }

	double ZoomedExtentWidth { get; }

	double ZoomedExtentHeight { get; }

	/// <summary>The range the offsets are clamped to, which is also what automation peers report as scrollable.</summary>
	double ScrollableWidth { get; }

	/// <summary>The range the offsets are clamped to, which is also what automation peers report as scrollable.</summary>
	double ScrollableHeight { get; }

	float ZoomFactor { get; }

	/// <summary>Scrolls to the given offsets, as requested by an external (native) scroller.</summary>
	void ScrollToOffsets(double horizontalOffset, double verticalOffset);
}
