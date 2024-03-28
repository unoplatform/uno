using System;
using Uno.UI.Xaml.Controls;

#if __IOS__
using UIKit;
#else
using AppKit;
#endif

namespace Windows.UI.Xaml.Controls;

partial class ContentPresenter
{
	bool ICustomClippingElement.AllowClippingToLayoutSlot => CornerRadius == CornerRadius.None;
	bool ICustomClippingElement.ForceClippingToLayoutSlot => false;

#if __IOS__
	public override void LayoutSubviews()
	{
		base.LayoutSubviews();
		UpdateBorder();
	}
#else
	public override void Layout()
	{
		base.Layout();
		UpdateBorder();
	}
#endif

	partial void SetUpdateTemplatePartial()
	{
#if __IOS__
		SetNeedsLayout();
#else
		this.InvalidateMeasure();
#endif
	}

	partial void RegisterContentTemplateRoot()
	{
		if (Subviews.Length != 0)
		{
			throw new Exception("A Xaml control may not contain more than one child.");
		}

		ContentTemplateRoot.Frame = Bounds;
#if __IOS__
		ContentTemplateRoot.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
#else
		ContentTemplateRoot.AutoresizingMask = NSViewResizingMask.HeightSizable | NSViewResizingMask.WidthSizable;
#endif
		AddSubview(ContentTemplateRoot);
	}

	partial void UnregisterContentTemplateRoot()
	{
		// If Content is a view it may have already been set as Content somewhere else in certain scenarios, eg virtualizing collections
		if (ContentTemplateRoot.Superview == this)
		{
			ContentTemplateRoot?.RemoveFromSuperview();
		}
	}

}
