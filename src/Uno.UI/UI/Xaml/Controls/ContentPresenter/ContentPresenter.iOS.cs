using System;
using System.Drawing;
using Uno.Extensions;
using Uno.UI;
using Uno.UI.Views.Controls;
using Uno.UI.DataBinding;
using UIKit;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Declares a Content presenter
/// </summary>
/// <remarks>
/// The content presenter is used for compatibility with WPF concepts,
/// but the ContentSource property is not available, because there are ControlTemplates for now.
/// </remarks>
public partial class ContentPresenter
{
	private void SetUpdateTemplate()
	{
		UpdateContentTemplateRoot();
		SetNeedsLayout();
	}

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
		// If Content is a view it may have already been set as Content somewhere else in certain scenarios, eg virtualizing collections
		if (ContentTemplateRoot.Superview == this)
		{
			ContentTemplateRoot?.RemoveFromSuperview();
		}
	}

	public override void LayoutSubviews()
	{
		base.LayoutSubviews();
		UpdateBorder();
	}

	private void UpdateCornerRadius(CornerRadius radius) => UpdateBorder();

	private void UpdateBorder()
	{
		if (IsLoaded)
		{
			_borderRenderer.UpdateLayer(
				this,
				Background,
				BackgroundSizing,
				BorderThickness,
				BorderBrush,
				CornerRadius,
				null
			);
		}
	}

	partial void ClearBorder() => _borderRenderer.Clear();

	partial void OnPaddingChangedPartial(Thickness oldValue, Thickness newValue)
	{
		UpdateBorder();
	}

	bool ICustomClippingElement.AllowClippingToLayoutSlot => CornerRadius == CornerRadius.None;

	bool ICustomClippingElement.ForceClippingToLayoutSlot => false;
}
