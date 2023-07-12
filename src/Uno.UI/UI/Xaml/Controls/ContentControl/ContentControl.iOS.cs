using System;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.DataBinding;

using UIKit;
using CoreGraphics;

namespace Windows.UI.Xaml.Controls
{
	public partial class ContentControl
	{
		partial void RegisterContentTemplateRoot()
		{
			if (Subviews.Length != 0)
			{
				throw new Exception("A Xaml control may not contain more than one child.");
			}

			ContentTemplateRoot.Frame = Bounds;
			ContentTemplateRoot.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
			AddSubview(ContentTemplateRoot);
		}

		partial void UnregisterContentTemplateRoot()
		{
			ContentTemplateRoot?.RemoveFromSuperview();
		}

		public override void SetSuperviewNeedsLayout()
		{
			var actualSuperview = Superview;
			if (!(actualSuperview is FrameworkElement) && actualSuperview?.Superview is ListViewBaseInternalContainer container)
			{
				// Workaround for the fact that the ContentView of a ListViewBaseInternalContainer is a bare UIView. We go over it and 
				// send the request to the container.
				container.SetSuperviewNeedsLayout();
			}
			actualSuperview?.SetNeedsLayout();
		}
	}
}

