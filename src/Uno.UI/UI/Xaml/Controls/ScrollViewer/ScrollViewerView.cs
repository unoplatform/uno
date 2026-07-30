// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference winrtgeneratedclasses/ScrollViewerView.g.h, commit 5f9e85113

namespace Microsoft.UI.Xaml.Controls
{
	// Represents a snapshot of the current view (offsets + zoom factor) reported by
	// ScrollViewer's ViewChanging event. Plain value carrier — no behavior.
	public partial class ScrollViewerView
	{
#if __SKIA__
		// On Skia we provide a real implementation. The generated NotImplemented-stub
		// remains for the other platforms.
		internal ScrollViewerView() { }

		internal ScrollViewerView(double horizontalOffset, double verticalOffset, float zoomFactor)
		{
			HorizontalOffset = horizontalOffset;
			VerticalOffset = verticalOffset;
			ZoomFactor = zoomFactor;
		}

		public double HorizontalOffset { get; internal set; }

		public double VerticalOffset { get; internal set; }

		public float ZoomFactor { get; internal set; } = 1.0f;
#endif
	}
}
