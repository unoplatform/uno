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
#elif UNO_REFERENCE_API || IS_UNIT_TESTS
using _View = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class Canvas
#if !__CROSSRUNTIME__ && !IS_UNIT_TESTS
		: ICustomClippingElement
#endif
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
					var childAsUIElement = child as UIElement;
					var desiredSize = GetElementDesiredSize(childView);

					var childRect = new Rect
					{
						X = GetLeft(childAsUIElement),
						Y = GetTop(childAsUIElement),
						Width = desiredSize.Width,
						Height = desiredSize.Height,
					};

#if __IOS__
					child.Layer.ZPosition = (nfloat)GetZIndex(childAsUIElement);
#endif

					ArrangeElement(child, childRect);
				}
			}

			return finalSize;
		}

#if __SKIA__ || __WASM__
		private protected override Rect? GetClipRect(bool needsClipToSlot, Point visualOffset, Rect finalRect, Size maxSize, Thickness margin) => null;
#elif !__NETSTD_REFERENCE__ && !IS_UNIT_TESTS
		bool ICustomClippingElement.AllowClippingToLayoutSlot => false;
		bool ICustomClippingElement.ForceClippingToLayoutSlot => false;
#endif
	}
}
