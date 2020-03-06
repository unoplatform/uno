using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.Foundation;
using Uno.Extensions;
using Microsoft.Extensions.Logging;
using Uno.Logging;

namespace Windows.UI.Xaml.Controls
{
	public partial class Canvas : ICustomClippingElement
	{
		protected override Size MeasureOverride(Size availableSize)
		{
			MeasureOverridePartial();
			// A canvas does not have dimensions and will always return zero even with a chidren collection.
			foreach (var child in Children.Where(c => c is DependencyObject))
			{
				MeasureElement(child, new Size(double.PositiveInfinity, double.PositiveInfinity));
			}
			return new Size(0, 0);
		}

		partial void MeasureOverridePartial();

		protected override Size ArrangeOverride(Size finalSize)
		{
			foreach (var child in Children.Where(c => c is DependencyObject))
			{
				var desiredSize = GetElementDesiredSize(child);
				var childDO = (DependencyObject)child;

				var childRect = new Rect
				{
					X = GetLeft(childDO),
					Y = GetTop(childDO),
					Width = desiredSize.Width,
					Height = desiredSize.Height,
				};

#if __IOS__
				child.Layer.ZPosition = (nfloat)GetZIndex(childDO);
#endif

				ArrangeElement(child, childRect);
			}

			return finalSize;
		}

		bool ICustomClippingElement.AllowClippingToLayoutSlot => false;
		bool ICustomClippingElement.ForceClippingToLayoutSlot => false;
	}
}
