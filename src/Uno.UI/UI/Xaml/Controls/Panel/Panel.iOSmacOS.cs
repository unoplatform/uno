using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Xaml.Controls;
using CoreGraphics;
using Windows.UI.Xaml.Media;


#if __IOS__
using UIKit;
using __View = UIKit.UIView;
#elif __MACOS__
using AppKit;
using __View = AppKit.NSView;
#endif

namespace Windows.UI.Xaml.Controls;

partial class Panel
{
	bool ICustomClippingElement.AllowClippingToLayoutSlot => CornerRadiusInternal == CornerRadius.None;
	bool ICustomClippingElement.ForceClippingToLayoutSlot => false;

	protected override void OnAfterArrange()
	{
		base.OnAfterArrange();

		//We trigger all layoutUpdated animations
		_transitionHelper?.LayoutUpdatedTransition();
	}

	protected override void OnBeforeArrange()
	{
		base.OnBeforeArrange();

		//We set childrens position for the animations before the arrange
		_transitionHelper?.SetInitialChildrenPositions();

		UpdateBorder();
	}

#if __IOS__
	public override void SubviewAdded(__View uiview)
	{
		base.SubviewAdded(uiview);

		var element = uiview as IFrameworkElement;
		if (element != null)
		{
			OnChildAdded(element);
		}
	}
#else
	public override void DidAddSubview(__View nsView)
	{
		base.DidAddSubview(nsView);

		var element = nsView as IFrameworkElement;
		if (element != null)
		{
			OnChildAdded(element);
		}
	}
#endif

	public bool HitTestOutsideFrame
	{
		get;
		set;
	}

#if __IOS__
	// All touches that are on this view (and not its subviews) are ignored
	public override __View HitTest(CGPoint point, UIEvent uievent)
	{
		return HitTestOutsideFrame ? this.HitTestOutsideFrame(point, uievent) : base.HitTest(point, uievent);
	}
#else
	// All touches that are on this view (and not its subviews) are ignored
	public override __View HitTest(CGPoint point) =>
		HitTestOutsideFrame ? this.HitTestOutsideFrame(point) : base.HitTest(point);
#endif
}
