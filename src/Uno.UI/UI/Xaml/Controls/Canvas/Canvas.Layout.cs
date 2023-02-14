using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.Foundation;
using Uno.Extensions;

using Uno.Foundation.Logging;

#if __ANDROID__
using _View = Android.Views.View;
#elif __IOS__
using _View = UIKit.UIView;
using ObjCRuntime;
#elif __MACOS__
using _View = AppKit.NSView;
using ObjCRuntime;
#elif UNO_REFERENCE_API || NET461
using _View = Microsoft.UI.Xaml.UIElement;
#endif

namespace Microsoft.UI.Xaml.Controls
{
	public partial class Canvas : ICustomClippingElement
	{
		protected override Size MeasureOverride(Size availableSize)
		{
			MeasureOverridePartial();
			// A canvas does not have dimensions and will always return zero even with a children collection.
			foreach (var child in Children)
			{
				if (child is _View)
				{
					MeasureElement(child, new Size(double.PositiveInfinity, double.PositiveInfinity));
				}
			}
			return new Size(0, 0);
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
