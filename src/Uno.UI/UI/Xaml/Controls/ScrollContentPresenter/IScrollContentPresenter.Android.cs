using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls
{
	internal partial interface IScrollContentPresenter
	{
		bool IsZoomEnabled { get; set; }
		float MinimumZoomScale { get; set; }
		float MaximumZoomScale { get; set; }
		float ZoomScale { get; set; }
		bool BringIntoViewOnFocusChange { get; set; }

		void ScrollTo(int x, int y);

		void SmoothScrollTo(int x, int y);
	}
}
