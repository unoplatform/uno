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

		object Content { get; set; }

		Thickness Padding { get; set; }

		bool Set(
			double? horizontalOffset = null,
			double? verticalOffset = null,
			float? zoomFactor = null,
			bool disableAnimation = true,
			bool isIntermediate = false);

#if __ANDROID__
		// To avoid massive refactor DO NOT USE, use 'Set' instead
		void SmoothScrollBy(int physicalDeltaX, int physicalDeltaY);
#endif
	}
}
