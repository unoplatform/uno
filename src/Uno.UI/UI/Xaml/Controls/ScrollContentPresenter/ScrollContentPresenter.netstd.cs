using Uno.Extensions;
using Uno.Logging;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using System.Text;
using Windows.Foundation;
using Uno.UI.Xaml;
using Microsoft.Extensions.Logging;

namespace Windows.UI.Xaml.Controls
{
	public partial class ScrollContentPresenter : ContentPresenter, IScrollContentPresenter
	{
		protected override Size MeasureOverride(Size size)
		{
			if (Content is UIElement child)
			{
				var slotSize = size;

				if (VerticalScrollBarVisibility != ScrollBarVisibility.Disabled)
				{
					slotSize.Height = double.PositiveInfinity;
				}
				if (HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled)
				{
					slotSize.Width = double.PositiveInfinity;
				}

				child.Measure(slotSize);

				return new Size(
					Math.Min(size.Width, child.DesiredSize.Width),
					Math.Min(size.Height, child.DesiredSize.Height)
				);
			}

			return new Size(0, 0);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			if (Content is UIElement child)
			{
				var slotSize = finalSize;
				var desiredChildSize = child.DesiredSize;

				if (VerticalScrollBarVisibility != ScrollBarVisibility.Disabled)
				{
					slotSize.Height = Math.Max(desiredChildSize.Height, finalSize.Height);
				}
				if (HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled)
				{
					slotSize.Width = Math.Max(desiredChildSize.Width, finalSize.Width);
				}

				child.Arrange(new Rect(new Point(0, 0), slotSize));
			}

			return finalSize;
		}

		internal override bool IsViewHit()
		{
			return true;
		}

		void IScrollContentPresenter.OnMinZoomFactorChanged(float newValue)
		{
			MinimumZoomScale = newValue;
		}

		void IScrollContentPresenter.OnMaxZoomFactorChanged(float newValue)
		{
			MaximumZoomScale = newValue;
		}
	}
}
