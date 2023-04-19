using System;
using Microsoft.UI.Xaml;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.Disposables;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Controls;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Media;

using CoreGraphics;
using AppKit;

namespace Windows.UI.Xaml.Controls;

partial class Panel
{
	public override void DidAddSubview(NSView nsView)
	{
		base.DidAddSubview(nsView);

		var element = nsView as IFrameworkElement;
		if (element != null)
		{
			OnChildAdded(element);
		}
	}

	partial void OnBorderThicknessChangedPartial(Thickness oldValue, Thickness newValue) => InvalidateMeasure();

	partial void OnPaddingChangedPartial(Thickness oldValue, Thickness newValue) => InvalidateMeasure();

	protected virtual void OnChildrenChanged() => InvalidateMeasure();

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

	public bool HitTestOutsideFrame { get; set; }

	public override NSView HitTest(CGPoint point)
	{
		// All touches that are on this view (and not its subviews) are ignored
		return HitTestOutsideFrame ? this.HitTestOutsideFrame(point) : base.HitTest(point);
	}

	bool ICustomClippingElement.AllowClippingToLayoutSlot => CornerRadiusInternal == CornerRadius.None;

	bool ICustomClippingElement.ForceClippingToLayoutSlot => false;
}
