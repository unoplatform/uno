#if !HAS_UI_TESTS
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Uno.Disposables;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Media;
using CoreGraphics;
using Uno.UI.Xaml.Media;
using Uno.UI.Xaml.Controls;

#if __IOS__
using UIKit;
using _Image = UIKit.UIImage;
#elif __MACOS__
using AppKit;
using _Image = AppKit.NSImage;
#endif

namespace Microsoft.UI.Xaml.Controls;

public partial class Border
{
	private readonly BorderLayerRenderer _borderRenderer;

	public Border()
	{
		_borderRenderer = new BorderLayerRenderer(this);
	}

	partial void OnChildChangedPartial(UIElement previousValue, UIElement newValue)
	{
		previousValue?.RemoveFromSuperview();

		if (newValue != null)
		{
			AddSubview(newValue);
		}

		UpdateBorder();
	}

	protected override void OnAfterArrange()
	{
		base.OnAfterArrange();
		UpdateBorder();
	}

	private void UpdateBorder()
	{
		_borderRenderer.Update();
		if (_borderRenderer.BoundsPath is CGPath updated) // UpdateLayer may return null if there is no update
		{
			BoundsPath = updated;
			BoundsPathUpdated?.Invoke(this, default);
		}

		_borderRenderer.BoundsPath = null;

		this.SetNeedsDisplay();
	}

	partial void OnBorderBrushChangedPartial() => UpdateBorder();

	partial void OnBorderThicknessChangedPartial(Thickness oldValue, Thickness newValue) => UpdateBorder();

	partial void OnPaddingChangedPartial(Thickness oldValue, Thickness newValue) => UpdateBorder();

	partial void OnCornerRadiusUpdatedPartial(CornerRadius oldValue, CornerRadius newValue) => UpdateBorder();

	protected override void OnBackgroundChanged(DependencyPropertyChangedEventArgs args) => UpdateBorder();

	bool ICustomClippingElement.AllowClippingToLayoutSlot => CornerRadius == CornerRadius.None && (!(Child is UIElement ue) || ue.RenderTransform == null);
	bool ICustomClippingElement.ForceClippingToLayoutSlot => false;

	internal event EventHandler BoundsPathUpdated;
	internal CGPath BoundsPath { get; private set; }
}
#endif
