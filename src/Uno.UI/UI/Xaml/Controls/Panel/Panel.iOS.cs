using CoreGraphics;
using UIKit;

namespace Windows.UI.Xaml.Controls;

partial class Panel
{
	public override void SubviewAdded(UIView uiview)
	{
		base.SubviewAdded(uiview);

		var element = uiview as IFrameworkElement;
		if (element != null)
		{
			OnChildAdded(element);
		}
	}

	partial void OnBorderThicknessChangedPartial(Thickness oldValue, Thickness newValue) => SetNeedsLayout();

	partial void OnPaddingChangedPartial(Thickness oldValue, Thickness newValue) => SetNeedsLayout();

	protected virtual void OnChildrenChanged() => SetNeedsLayout();

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

	public bool HitTestOutsideFrame
	{
		get;
		set;
	}

	public override UIView HitTest(CGPoint point, UIEvent uievent)
	{
		// All touches that are on this view (and not its subviews) are ignored
		return HitTestOutsideFrame ? this.HitTestOutsideFrame(point, uievent) : base.HitTest(point, uievent);
	}

	bool ICustomClippingElement.AllowClippingToLayoutSlot => CornerRadiusInternal == CornerRadius.None;

	bool ICustomClippingElement.ForceClippingToLayoutSlot => false;
}
