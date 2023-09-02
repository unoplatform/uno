using System;
using System.Collections.Generic;
using System.Text;
using CoreGraphics;

using _EdgeInsets = UIKit.UIEdgeInsets;

namespace Windows.UI.Xaml.Controls
{
	internal partial interface IScrollContentPresenter : IUIScrollView
	{
		_EdgeInsets ContentInset { get; set; }
	}
}
