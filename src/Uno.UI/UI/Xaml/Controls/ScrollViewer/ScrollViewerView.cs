#nullable enable

namespace Microsoft.UI.Xaml.Controls;

public sealed partial class ScrollViewerView
{
	internal ScrollViewerView(double horizontalOffset, double verticalOffset, float zoomFactor)
	{
		HorizontalOffset = horizontalOffset;
		VerticalOffset = verticalOffset;
		ZoomFactor = zoomFactor;
	}

	public double HorizontalOffset { get; }

	public double VerticalOffset { get; }

	public float ZoomFactor { get; }
}
