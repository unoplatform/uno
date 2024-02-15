using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Xaml.Controls;

#if __IOS__
using UIKit;
#else
using AppKit;
#endif

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Declares a Content presenter
/// </summary>
/// <remarks>
/// The content presenter is used for compatibility with WPF concepts,
/// but the ContentSource property is not available, because there are ControlTemplates for now.
/// </remarks>
partial class ContentPresenter
{
	private readonly BorderLayerRenderer _borderRenderer;

	public ContentPresenter()
	{
		_borderRenderer = new BorderLayerRenderer(this);
		InitializeContentPresenter();
	}

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

	private void SetUpdateTemplate()
	{
		UpdateContentTemplateRoot();
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
	
	private void UpdateCornerRadius(CornerRadius radius) => UpdateBorder();
	
	private void UpdateBorder() => _borderRenderer.Update();

	partial void ClearBorder() => _borderRenderer.Clear();

	partial void OnPaddingChangedPartial(Thickness oldValue, Thickness newValue) => UpdateBorder();

	bool ICustomClippingElement.AllowClippingToLayoutSlot => CornerRadius == CornerRadius.None;

	bool ICustomClippingElement.ForceClippingToLayoutSlot => false;
}
