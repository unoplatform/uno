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
    public partial class Canvas
    {
		protected override Size MeasureOverride(Size availableSize)
		{
			double maxWidth = 0, maxHeight = 0;

			foreach (var child in Children.Where(c => c is DependencyObject))
			{
				var childX = GetLeft(child as DependencyObject);
				var childY = GetTop(child as DependencyObject);

				var measuredSize = MeasureElement(child, new Size(double.PositiveInfinity, double.PositiveInfinity));

				maxHeight = Math.Max(maxHeight, measuredSize.Height + childY);
				maxWidth = Math.Max(maxWidth, measuredSize.Width + childX);
			}

			return new Size(maxWidth, maxHeight);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			foreach (var child in Children.Where(c => c is DependencyObject))
			{
				var desiredSize = GetElementDesiredSize(child);

				var childRect = new Rect
				{
					X = GetLeft(child as DependencyObject),
					Y = GetTop(child as DependencyObject),
					Width = desiredSize.Width,
					Height = desiredSize.Height,
				};

#if __IOS__
				child.Layer.ZPosition = (nfloat)GetZIndex(child as DependencyObject);
#elif __ANDROID__
				if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Lollipop)
				{
					child.SetZ((float)GetZIndex(child as DependencyObject));
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
	}
}
