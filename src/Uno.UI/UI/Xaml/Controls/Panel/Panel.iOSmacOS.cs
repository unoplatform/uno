using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Xaml.Controls;
using CoreGraphics;
using Microsoft.UI.Xaml.Media;


#if __IOS__
using UIKit;
using __View = UIKit.UIView;
#elif __MACOS__
using AppKit;
using __View = AppKit.NSView;
#endif

namespace Microsoft.UI.Xaml.Controls;

partial class Panel
{
	private readonly BorderLayerRenderer _borderRenderer;

	public Panel()
	{
		_borderRenderer = new BorderLayerRenderer(this);

		Initialize();
	}

	partial void Initialize();

	partial void OnBorderBrushChangedPartial(Brush oldValue, Brush newValue) => UpdateBorder();

	partial void OnBorderThicknessChangedPartial(Thickness oldValue, Thickness newValue)
	{
		InvalidateMeasure();
		UpdateBorder();
	}

	partial void OnPaddingChangedPartial(Thickness oldValue, Thickness newValue) => InvalidateMeasure();

	protected override void OnBackgroundChanged(DependencyPropertyChangedEventArgs args) => UpdateBorder();

	partial void OnCornerRadiusChangedPartial(CornerRadius oldValue, CornerRadius newValue) => UpdateBorder();

	partial void UpdateBorder() => _borderRenderer.Update(); //TODO: Do we need to pass the image data?

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

	/// <summary>        
	/// Support for the C# collection initializer style.
	/// Allows items to be added like this 
	/// new Panel 
	/// {
	///    new Border()
	/// }
	/// </summary>
	/// <param name="view"></param>
	public
#if __IOS__
		new
#endif
		void Add(__View view)
	{
		Children.Add(view);
	}

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

	bool ICustomClippingElement.AllowClippingToLayoutSlot => CornerRadiusInternal == CornerRadius.None;
	bool ICustomClippingElement.ForceClippingToLayoutSlot => false;
}
