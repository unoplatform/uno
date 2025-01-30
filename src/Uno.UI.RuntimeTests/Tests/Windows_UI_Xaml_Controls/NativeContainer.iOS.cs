using CoreGraphics;
using System;
using System.Collections.Generic;
using System.Text;
using UIKit;
using Windows.Foundation;
using Microsoft.UI.Xaml;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	partial class NativeContainer : UIView
	{
		private void OnContentChanged(UIElement oldValue, UIElement newValue)
		{
			if (oldValue != null)
			{
				oldValue.RemoveFromSuperview();
			}

			if (newValue != null)
			{
				AddSubview(newValue);
			}

			Superview?.SetNeedsLayout();
		}

		public override CGRect Frame
		{
			get => base.Frame;
			set => base.Frame = value;
		}

		public override void LayoutSubviews()
		{
			Content?.Arrange(new Rect(default, Content.DesiredSize));
		}

		public override CGSize SizeThatFits(CGSize size)
		{
			Content?.Measure(size);
			return Content?.DesiredSize ?? default;
		}
	}
}
