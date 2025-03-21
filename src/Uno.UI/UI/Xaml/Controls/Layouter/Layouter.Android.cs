using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Extensions;
using Uno;
using Uno.Foundation.Logging;
using Uno.Collections;
using Windows.Foundation;

using View = Android.Views.View;
using Font = Android.Graphics.Typeface;
using System.Linq.Expressions;
using Uno.UI;

namespace Windows.UI.Xaml.Controls
{
	abstract partial class Layouter
	{
		public static void SetMeasuredDimensions(View view, int width, int height)
		{
			LayouterHelper.SetMeasuredDimensions(view, new object[] { width, height });
		}

		protected Size MeasureChildOverride(View view, Size slotSize)
		{
			var widthSpec = ViewHelper.SpecFromLogicalSize(slotSize.Width);
			var heightSpec = ViewHelper.SpecFromLogicalSize(slotSize.Height);

			var needsForceLayout =
				(double.IsPositiveInfinity(slotSize.Width) || double.IsPositiveInfinity(slotSize.Height)) ||
				// uno12315: ensure the native measure cache is not used when measure-spec has changed since.
				FeatureConfiguration.FrameworkElement.InvalidateNativeCacheOnRemeasure && (
					view.MeasuredWidth != ViewHelper.LogicalToPhysicalPixels(slotSize.Width) ||
					view.MeasuredHeight != ViewHelper.LogicalToPhysicalPixels(slotSize.Height)
				);

			Uno.UI.Controls.BindableView.TryFastRequestLayout(view, needsForceLayout);

			MeasureChild(view, widthSpec, heightSpec);

			var ret = Uno.UI.Controls.BindableView.GetNativeMeasuredDimensionsFast(view)
				.PhysicalToLogicalPixels();

			return ret.AtMost(slotSize);
		}

		protected abstract void MeasureChild(View view, int widthSpec, int heightSpec);

		protected void ArrangeChildOverride(View view, Rect frame)
		{
			LogArrange(view, frame);

			var elt = view as UIElement;
			var physicalFrame = frame.LogicalToPhysicalPixels();

			try
			{
				elt?.SetFramePriorArrange(frame, physicalFrame);

				view.Layout(
					(int)physicalFrame.Left,
					(int)physicalFrame.Top,
					(int)physicalFrame.Right,
					(int)physicalFrame.Bottom
				);
			}
			finally
			{
				elt?.ResetFramePostArrange();
			}
		}
	}

	internal static partial class LayouterExtensions
	{
		public static IEnumerable<View> GetChildren(this Layouter layouter)
		{
			return (layouter.Panel as Android.Views.ViewGroup).GetChildren();
		}
	}
}
