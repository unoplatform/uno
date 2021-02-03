using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.Foundation;
using Uno.Extensions;
using Microsoft.Extensions.Logging;
using Uno.Logging;

#if __ANDROID__
using _View = Android.Views.View;
#elif __IOS__
using _View = UIKit.UIView;
#elif __MACOS__
using _View = AppKit.NSView;
#elif NETSTANDARD2_0 || NET461
using _View = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class Canvas : ICustomClippingElement
	{
		protected override Size MeasureOverride(Size availableSize)
		{
			double maxWidth = 0, maxHeight = 0;

			MeasureOverridePartial();
			foreach (var child in Children)
			{
				if (child is _View)
				{
					var measuredSize = MeasureElement(child, new Size(double.PositiveInfinity, double.PositiveInfinity));

					maxHeight = Math.Max(maxHeight, measuredSize.Height + GetTop(child as DependencyObject));
					maxWidth = Math.Max(maxWidth, measuredSize.Width + GetLeft(child as DependencyObject));
				}
			}
			// It will not be arranged if always return zero.
			return new Size(maxWidth, maxHeight);
		}

		partial void MeasureOverridePartial();

		protected override Size ArrangeOverride(Size finalSize)
		{
			foreach (var child in Children)
			{
				if (child is _View childView)
				{
					var childAsDO = child as DependencyObject;
					var desiredSize = GetElementDesiredSize(childView);

					var childRect = new Rect
					{
						X = GetLeft(childAsDO),
						Y = GetTop(childAsDO),
						Width = desiredSize.Width,
						Height = desiredSize.Height,
					};

#if __IOS__
					child.Layer.ZPosition = (nfloat)GetZIndex(childAsDO);
#endif

					ArrangeElement(child, childRect);
				}
			}

			return finalSize;
		}

		bool ICustomClippingElement.AllowClippingToLayoutSlot => false;
		bool ICustomClippingElement.ForceClippingToLayoutSlot => false;
	}
}
