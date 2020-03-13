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
	public abstract partial class Layouter
	{
		public static void SetMeasuredDimensions(View view, int width, int height)
		{
			LayouterHelper.SetMeasuredDimensions(view, new object[] { width, height });
		}

		partial void SetDesiredChildSize(View view, Size desiredSize)
		{
			var uiElement = view as UIElement;

			if (uiElement != null)
			{
				uiElement.DesiredSize = desiredSize;
			}
			else
			{
				LayouterHelper.LayoutProperties.SetValue(view, "desiredSize", desiredSize);
			}
		}

		/// <summary>
		/// Provides the desired size of the element, from the last measure phase.
		/// </summary>
		/// <param name="view">The element to get the measured with</param>
		/// <returns>The measured size</returns>
		Size ILayouter.GetDesiredSize(View view)
		{
			return DesiredChildSize(view);
		}

		protected Size DesiredChildSize(View view)
		{
			var uiElement = view as UIElement;

			return uiElement?.DesiredSize ?? LayouterHelper.LayoutProperties.GetValue(view, "desiredSize", () => default(Size));
		}

		protected Size MeasureChildOverride(View view, Size slotSize)
		{
			var widthSpec = ViewHelper.SpecFromLogicalSize(slotSize.Width);
			var heightSpec = ViewHelper.SpecFromLogicalSize(slotSize.Height);

			MeasureChild(view, widthSpec, heightSpec);
			
			var ret = Uno.UI.Controls.BindableView.GetNativeMeasuredDimensionsFast(view)
				.PhysicalToLogicalPixels();

			ret.Width = Math.Min(slotSize.Width, ret.Width);
			ret.Height = Math.Min(slotSize.Height, ret.Height);

			return ret;
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

		protected void ArrangeChildOverride(View view, Rect frame)
		{
			LogArrange(view, frame);

			var physicalFrame = frame.LogicalToPhysicalPixels();

			try
			{
				SetArrangeLogicalSize(view, frame);

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

	public static partial class LayouterExtensions
	{
		public static IEnumerable<View> GetChildren(this Layouter layouter)
		{
			return (layouter.Panel as Android.Views.ViewGroup).GetChildren();
		}
	}
}
