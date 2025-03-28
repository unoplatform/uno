using System;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.DataBinding;

using AppKit;
using CoreGraphics;

namespace Windows.UI.Xaml.Controls
{
	public partial class ContentControl
	{

		public override void ViewDidMoveToSuperview()
		{
			base.ViewDidMoveToSuperview();
		}

		partial void RegisterContentTemplateRoot()
		{
			if (Subviews.Length != 0)
			{
				throw new Exception("A Xaml control may not contain more than one child.");
			}

			ContentTemplateRoot.Frame = Bounds;
			ContentTemplateRoot.AutoresizingMask = NSViewResizingMask.WidthSizable | NSViewResizingMask.HeightSizable;
			AddSubview(ContentTemplateRoot);
		}

		partial void UnregisterContentTemplateRoot()
		{
			ContentTemplateRoot?.RemoveFromSuperview();
		}

		public override void SetSuperviewNeedsLayout()
		{
			var actualSuperview = Superview;
			actualSuperview?.SetNeedsLayout();
		}
	}
}

