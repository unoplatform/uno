using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	internal partial interface IScrollContentPresenter
	{
		void ScrollTo(double? horizontalOffset, double? verticalOffset, bool disableAnimation);

		bool ForceChangeToCurrentView { get; set; }
	}
}
