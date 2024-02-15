using CoreGraphics;
using UIKit;
using Uno.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

public partial class Panel
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

	partial void OnBorderBrushChangedPartial(Brush oldValue, Brush newValue) => UpdateBorder();

	partial void OnBorderThicknessChangedPartial(Thickness oldValue, Thickness newValue)
	{
		SetNeedsLayout();
		UpdateBorder();
	}

	partial void OnPaddingChangedPartial(Thickness oldValue, Thickness newValue)
	{
		SetNeedsLayout();
	}

	protected override void OnBackgroundChanged(DependencyPropertyChangedEventArgs args) => UpdateBorder();

	partial void OnCornerRadiusChangedPartial(CornerRadius oldValue, CornerRadius newValue) => UpdateBorder();

	partial void UpdateBorder() => _borderRenderer.Update(); //TODO: Do we need to pass the image data?

	protected virtual void OnChildrenChanged()
	{
		SetNeedsLayout();
	}

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

	/// <summary>        
	/// Support for the C# collection initializer style.
	/// Allows items to be added like this 
	/// new Panel 
	/// {
	///    new Border()
	/// }
	/// </summary>
	/// <param name="view"></param>
	public new void Add(UIView view)
	{
		Children.Add(view);
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
