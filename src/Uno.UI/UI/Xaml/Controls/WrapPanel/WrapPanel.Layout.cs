using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno;
using Uno.UI;
using Windows.Foundation;

using Rect = Windows.Foundation.Rect;

#if __ANDROID__
using View = Android.Views.View;
using ViewGroup = Android.Views.ViewGroup;
using Font = Android.Graphics.Typeface;
using Android.Graphics;
#elif __IOS__
using UIKit;
using View = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
using ViewGroup = UIKit.UIView;
#elif __MACOS__
using AppKit;
using View = AppKit.NSView;
using Color = AppKit.NSColor;
using Font = AppKit.NSFont;
using ViewGroup = AppKit.NSView;
#else
using View = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml.Controls
{
	partial class WrapPanel
	{
		protected override Size MeasureOverride(Size availableSize)
		{
			// Variables tracking the size of the current line, the total size
			// measured so far, and the maximum size available to fill.  Note
			// that the line might represent a row or a column depending on the
			// orientation.
			Orientation o = Orientation;

			OrientedSize lineSize = new OrientedSize(o);
			OrientedSize totalSize = new OrientedSize(o);
			OrientedSize maximumSize = new OrientedSize(o, availableSize.Width, availableSize.Height);

			// Determine the constraints for individual items
			double itemWidth = ItemWidth ?? double.NaN;
			double itemHeight = ItemHeight ?? double.NaN;
			bool hasFixedWidth = !double.IsNaN(itemWidth);
			bool hasFixedHeight = !double.IsNaN(itemHeight);
			Size itemSize = new Size(
				hasFixedWidth ? itemWidth : availableSize.Width,
				hasFixedHeight ? itemHeight : availableSize.Height);

			// Measure each of the Children
			foreach (View element in Children)
			{
				// Determine the size of the element
				var desiredSize = MeasureElement(element, itemSize);

				OrientedSize elementSize = new OrientedSize(
					o,
					hasFixedWidth ? itemWidth : desiredSize.Width,
					hasFixedHeight ? itemHeight : desiredSize.Height);

				// If this element falls of the edge of the line
				if (NumericExtensions.IsGreaterThan(lineSize.Direct + elementSize.Direct, maximumSize.Direct))
				{
					// Update the total size with the direct and indirect growth
					// for the current line
					totalSize.Direct = Math.Max(lineSize.Direct, totalSize.Direct);
					totalSize.Indirect += lineSize.Indirect;

					// Move the element to a new line
					lineSize = elementSize;

					// If the current element is larger than the maximum size,
					// place it on a line by itself
					if (NumericExtensions.IsGreaterThan(elementSize.Direct, maximumSize.Direct))
					{
						// Update the total size for the line occupied by this
						// single element
						totalSize.Direct = Math.Max(elementSize.Direct, totalSize.Direct);
						totalSize.Indirect += elementSize.Indirect;

						// Move to a new line
						lineSize = new OrientedSize(o);
					}
				}
				else
				{
					// Otherwise just add the element to the end of the line
					lineSize.Direct += elementSize.Direct;
					lineSize.Indirect = Math.Max(lineSize.Indirect, elementSize.Indirect);
				}
			}

			// Update the total size with the elements on the last line
			totalSize.Direct = Math.Max(lineSize.Direct, totalSize.Direct);
			totalSize.Indirect += lineSize.Indirect;

			// Return the total size required as an un-oriented quantity
			return new Size(totalSize.Width, totalSize.Height);
		}

		protected override Size ArrangeOverride(Size arrangeSize)
		{
			// Variables tracking the size of the current line, and the maximum
			// size available to fill.  Note that the line might represent a row
			// or a column depending on the orientation.
			Orientation o = Orientation;
			OrientedSize lineSize = new OrientedSize(o);
			OrientedSize maximumSize = new OrientedSize(o, arrangeSize.Width, arrangeSize.Height);

			// Determine the constraints for individual items
			double itemWidth = ItemWidth ?? double.NaN;
			double itemHeight = ItemHeight ?? double.NaN;
			bool hasFixedWidth = !double.IsNaN(itemWidth);
			bool hasFixedHeight = !double.IsNaN(itemHeight);
			double indirectOffset = 0;
			double? directDelta = (o == Orientation.Horizontal) ?
				(hasFixedWidth ? (double?)itemWidth : null) :
				(hasFixedHeight ? (double?)itemHeight : null);

			// Measure each of the Children.  We will process the elements one
			// line at a time, just like during measure, but we will wait until
			// we've completed an entire line of elements before arranging them.
			// The lineStart and lineEnd variables track the size of the
			// currently arranged line.
			var children = Children.ToArray();

			int count = children.Length;
			int lineStart = 0;
			for (int lineEnd = 0; lineEnd < count; lineEnd++)
			{
				var element = children[lineEnd];

				var desiredSize = GetElementDesiredSize(element);

				// Get the size of the element
				OrientedSize elementSize = new OrientedSize(
					o,
					hasFixedWidth ? itemWidth : desiredSize.Width,
					hasFixedHeight ? itemHeight : desiredSize.Height);

				// If this element falls of the edge of the line
				if (NumericExtensions.IsGreaterThan(lineSize.Direct + elementSize.Direct, maximumSize.Direct))
				{
					// Then we just completed a line and we should arrange it
					ArrangeLine(children, lineStart, lineEnd, directDelta, indirectOffset, lineSize.Indirect);

					// Move the current element to a new line
					indirectOffset += lineSize.Indirect;
					lineSize = elementSize;

					// If the current element is larger than the maximum size
					if (NumericExtensions.IsGreaterThan(elementSize.Direct, maximumSize.Direct))
					{
						// Arrange the element as a single line
						ArrangeLine(children, lineEnd, ++lineEnd, directDelta, indirectOffset, elementSize.Indirect);

						// Move to a new line
						indirectOffset += lineSize.Indirect;
						lineSize = new OrientedSize(o);
					}

					// Advance the start index to a new line after arranging
					lineStart = lineEnd;
				}
				else
				{
					// Otherwise just add the element to the end of the line
					lineSize.Direct += elementSize.Direct;
					lineSize.Indirect = Math.Max(lineSize.Indirect, elementSize.Indirect);
				}
			}

			// Arrange any elements on the last line
			if (lineStart < count)
			{
				ArrangeLine(children, lineStart, count, directDelta, indirectOffset, lineSize.Indirect);
			}

			return arrangeSize;
		}

		/// <summary>
		/// Arrange a sequence of elements in a single line.
		/// </summary>
		/// <param name="lineStart">
		/// Index of the first element in the sequence to arrange.
		/// </param>
		/// <param name="lineEnd">
		/// Index of the last element in the sequence to arrange.
		/// </param>
		/// <param name="directDelta">
		/// Optional fixed growth in the primary direction.
		/// </param>
		/// <param name="indirectOffset">
		/// Offset of the line in the indirect direction.
		/// </param>
		/// <param name="indirectGrowth">
		/// Shared indirect growth of the elements on this line.
		/// </param>
		private void ArrangeLine(View[] children, int lineStart, int lineEnd, double? directDelta, double indirectOffset, double indirectGrowth)
		{
			double directOffset = 0.0f;

			Orientation o = Orientation;
			bool isHorizontal = o == Orientation.Horizontal;

			for (int index = lineStart; index < lineEnd; index++)
			{
				// Get the size of the element
				View element = children[index];

				var desiredSize = GetElementDesiredSize(element);

				OrientedSize elementSize = new OrientedSize(o, desiredSize.Width, desiredSize.Height);

				// Determine if we should use the element's desired size or the
				// fixed item width or height
				double directGrowth = directDelta != null ?
					directDelta.Value :
					elementSize.Direct;

				// Arrange the element
				Rect bounds = isHorizontal ?
					new Rect(directOffset, indirectOffset, directGrowth, indirectGrowth) :
					new Rect(indirectOffset, directOffset, indirectGrowth, directGrowth);

				ArrangeElement(element, bounds);

				directOffset += directGrowth;
			}
		}
	}
}
