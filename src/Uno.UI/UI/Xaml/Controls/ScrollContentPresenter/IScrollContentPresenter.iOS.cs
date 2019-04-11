using System;
using System.Collections.Generic;
using System.Text;
using CoreGraphics;
using UIKit;

namespace Windows.UI.Xaml.Controls
{
	internal partial interface IScrollContentPresenter : IUIScrollView
	{
		UIEdgeInsets ContentInset { get; set; }
	}
}
