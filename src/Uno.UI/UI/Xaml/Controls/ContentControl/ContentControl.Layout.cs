#if XAMARIN
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.UI;
using Windows.Foundation;

#if XAMARIN_ANDROID
using View = Android.Views.View;
using Font = Android.Graphics.Typeface;
#elif XAMARIN_IOS_UNIFIED
using UIKit;
using View = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
#elif __MACOS__
using AppKit;
using View = AppKit.NSView;
using Color = AppKit.NSColor;
using Font = AppKit.NSFont;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class ContentControl : ICustomClippingElement
	{

		protected override Size MeasureOverride(Size availableSize)
		{
			if(!IsContentPresenterBypassEnabled)
			{
				return base.MeasureOverride(availableSize);
			}

			var padding = Padding;
			var borderThickness = BorderThickness;

			var measuredSize = base.MeasureOverride(
				new Size(
					availableSize.Width - padding.Left - padding.Right - borderThickness.Left - borderThickness.Right,
					availableSize.Height - padding.Top - padding.Bottom - borderThickness.Top - borderThickness.Bottom
				)
			);

			return new Size(
				measuredSize.Width + padding.Left + padding.Right + borderThickness.Left + borderThickness.Right,
				measuredSize.Height + padding.Top + padding.Bottom + borderThickness.Top + borderThickness.Bottom
			);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			if (!IsContentPresenterBypassEnabled)
			{
				base.ArrangeOverride(finalSize);
				return finalSize;
			}

			var child = this.FindFirstChild();

			if (child != null)
			{
				var padding = Padding;
				var borderThickness = BorderThickness;

				var finalRect = new Rect(
					padding.Left + borderThickness.Left,
					padding.Top + borderThickness.Top,
					finalSize.Width - padding.Left - padding.Right - borderThickness.Left - borderThickness.Right,
					finalSize.Height - padding.Top - padding.Bottom - borderThickness.Top - borderThickness.Bottom
				);

				base.ArrangeElement(child, finalRect);
			}

			return finalSize;
		}
		bool ICustomClippingElement.AllowClippingToLayoutSlot => !(Content is UIElement ue) || ue.RenderTransform == null;
		bool ICustomClippingElement.ForceClippingToLayoutSlot => false;
	}
}
#endif
