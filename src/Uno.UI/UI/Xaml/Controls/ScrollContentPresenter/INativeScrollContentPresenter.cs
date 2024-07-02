using System;
using System.Linq;

namespace Windows.UI.Xaml.Controls
{
	internal interface INativeScrollContentPresenter
	{
		ScrollBarVisibility HorizontalScrollBarVisibility { get; set; }
		ScrollBarVisibility VerticalScrollBarVisibility { get; set; }

		bool CanHorizontallyScroll { get; set; }
		bool CanVerticallyScroll { get; set; }

		double ExtentWidth { get; set; }
		double ExtentHeight { get; set; }

		object Content { get; set; }

		bool Set(
			double? horizontalOffset = null,
			double? verticalOffset = null,
			float? zoomFactor = null,
			bool disableAnimation = true,
			bool isIntermediate = false);

#if __ANDROID__
		// Padding used by the SIP
		Thickness Padding { get; set; }

		// To avoid massive refactor DO NOT USE, use 'Set' instead
		void SmoothScrollBy(int physicalDeltaX, int physicalDeltaY);
#endif
	}
}
