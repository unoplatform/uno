using System;
using System.Collections.Generic;
using System.Text;
using CoreGraphics;
using Uno.Extensions;
using Microsoft.Extensions.Logging;
using Uno.Logging;
using Windows.Foundation;
using UIKit;

namespace Windows.UI.Xaml.Controls
{
	public sealed partial class ListViewBaseScrollContentPresenter : ContentPresenter, IScrollContentPresenter
	{
		public CGPoint UpperScrollLimit => NativePanel?.UpperScrollLimit ?? CGPoint.Empty;

		public UIEdgeInsets ContentInset
		{
			get => NativePanel?.ContentInset ?? default(UIEdgeInsets);
			set => NativePanel.ContentInset = value;
		}

		public void SetContentOffset(CGPoint contentOffset, bool animated)
		{
			NativePanel?.SetContentOffset(contentOffset, animated);
		}

		public void SetZoomScale(nfloat scale, bool animated)
		{
			NativePanel?.SetZoomScale(scale, animated);
		}

		public Rect MakeVisible(UIElement visual, Rect rectangle)
		{
			return rectangle;
		}
	}
}
