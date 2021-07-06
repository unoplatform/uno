using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Extensions;
using Uno;
using Uno.Logging;
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

			if (double.IsPositiveInfinity(slotSize.Width) || double.IsPositiveInfinity(slotSize.Height))
			{
				// Bypass Android cache, to ensure the Child's Measure() is actually invoked.
				view.ForceLayout();

				// This could occur when one of the dimension is _Infinite_: Android will cache the
				// value, which is not something we want. Specially when the container is a <StackPanel>.

				// Issue: https://github.com/unoplatform/uno/issues/2879
			}

			MeasureChild(view, widthSpec, heightSpec);
			
			var ret = Uno.UI.Controls.BindableView.GetNativeMeasuredDimensionsFast(view)
				.PhysicalToLogicalPixels();

			return ret.AtMost(slotSize);
		}

		protected abstract void MeasureChild(View view, int widthSpec, int heightSpec);

		private void SetArrangeLogicalSize(View view, Rect frame)
		{
			if (view is UIElement uiElement)
			{
				uiElement.ArrangeLogicalSize = frame;
			}
		}

		private void ResetArrangeLogicalSize(View view)
		{
			if (view is UIElement uiElement)
			{
				uiElement.ArrangeLogicalSize = null;
			}
		}

		private void SetFrameRoundingAdjustment(View view, Rect frame, Rect physicalFrame)
		{
			if (view is UIElement uiElement)
			{
				var physicalWidth = ViewHelper.LogicalToPhysicalPixels(frame.Width);
				var physicalHeight = ViewHelper.LogicalToPhysicalPixels(frame.Height);

				uiElement.FrameRoundingAdjustment = new Size(
					(int)physicalFrame.Width - physicalWidth,
					(int)physicalFrame.Height - physicalHeight);
			}
		}

		protected void ArrangeChildOverride(View view, Rect frame)
		{
			LogArrange(view, frame);

			var physicalFrame = frame.LogicalToPhysicalPixels();

			try
			{
				SetArrangeLogicalSize(view, frame);
				SetFrameRoundingAdjustment(view, frame, physicalFrame);

				view.Layout(
					(int)physicalFrame.Left,
					(int)physicalFrame.Top,
					(int)physicalFrame.Right,
					(int)physicalFrame.Bottom
				);
			}
			finally
			{
				ResetArrangeLogicalSize(view);
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
