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
			// A canvas does not have dimensions and will always return zero even with a chidren collection.
			foreach (var child in Children.Where(c => c is DependencyObject))
			{
				MeasureElement(child, new Size(double.PositiveInfinity, double.PositiveInfinity));
			}
			return new Size(0, 0);
		}

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
#elif __ANDROID__
				if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Lollipop)
				{
					child.SetZ((float)GetZIndex(childDO));
				}
				else
				{
					if (this.Log().IsEnabled(LogLevel.Warning))
					{
						this.Log().Warn("Canvas.ZIndex is not support on Android 4.4 and less. Canvas will arrange its Children in the order they were added.");
					}
				}
#endif

				ArrangeElement(child, childRect);
			}

			return finalSize;
		}

		bool ICustomClippingElement.AllowClippingToLayoutSlot => false;
		bool ICustomClippingElement.ForceClippingToLayoutSlot => false;
	}
}
