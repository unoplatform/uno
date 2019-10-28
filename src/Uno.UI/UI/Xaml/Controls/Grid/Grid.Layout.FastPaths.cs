using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using Uno.UI;
using Windows.Foundation;
using Uno.Collections;

#if XAMARIN_ANDROID
using View = Android.Views.View;
using ViewGroup = Android.Views.ViewGroup;
using Font = Android.Graphics.Typeface;
using Android.Graphics;
#elif XAMARIN_IOS_UNIFIED
using UIKit;
using View = UIKit.UIView;
using ViewGroup = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
#elif XAMARIN_IOS
using View = MonoTouch.UIKit.UIView;
using ViewGroup = MonoTouch.UIKit.UIView;
using Color = MonoTouch.UIKit.UIColor;
using Font = MonoTouch.UIKit.UIFont;
#elif __MACOS__
using AppKit;
using View = AppKit.NSView;
using ViewGroup = AppKit.NSView;
using Color = AppKit.NSColor;
using Font = AppKit.NSFont;
#else
using View = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml.Controls
{
	partial class Grid
	{
		/// <summary>
		/// This method measures the a grid using known patterns (fast paths).
		/// For example, a 1x1 Grid can be measure as a SuperpositionPanel. 
		/// </summary>
		/// <param name="size">The measured size if a fast path was used, [0,0] otherwise</param>
		/// <returns>True if a fast path was used, false otherwise</returns>
		private bool TryMeasureUsingFastPath(Size availableSize, Span<Column> columns, Span<Column> definedColumns, Span<Row> rows, Span<Row> definedRows, out Size size)
		{
			var columnCount = columns.Length;
			var rowCount = rows.Length;

			if (columnCount == 1 && rowCount == 1)
			{
				// 1x1
				size = MeasureSuperpositionPanel(availableSize, columns[0], rows[0]);
				return true;
			}
			else if (rowCount == 1)
			{
				// Nx1
				size = MeasureHorizontalGrid(availableSize, columns, definedColumns, rows[0]);
				return true;
			}
			else if (columnCount == 1)
			{
				// 1xN
				size = MeasureVerticalGrid(availableSize, columns[0], rows, definedRows);
				return true;
			}

			size = default(Size);
			return false;
		}

		/// <summary>
		/// Measure the grid knowing it has only one column (1xN grid)
		/// </summary>
		private Size MeasureVerticalGrid(Size availableSize, Column column, Span<Row> rows, Span<Row> definedRows)
		{
			var size = default(Size);
			if (column.Width.IsPixelSize)
			{
				var pixelWidth = column.Width.PixelSize.Value;
				availableSize.Width = pixelWidth;
				size.Width = pixelWidth;
			}

			var positions = GetPositions(1, rows.Length);

			using (positions.Subscription)
			{
				var measureChild = GetDirectMeasureChild();

				// One simulated column
				Span<DoubleRange> columns = stackalloc DoubleRange[] { new DoubleRange(availableSize.Width) };

				_calculatedRows = CalculateRows(
					availableSize,
					positions.Views.Span,
					measureChild,
					columns,
					rows,
					definedRows,
					true,
					out var maxMeasuredWidth);

				size = new Size(
					Math.Max(size.Width, maxMeasuredWidth),
					_calculatedRows.Span.Sum(r => r.MinValue)
				);

				//If MinWidth property is set on the Grid we need to take it into account in order to match UWP behavior
				var minWidth = size.Width;

				if (MinWidth != 0 && MinWidth > size.Width)
				{
					minWidth = MinWidth - BorderAndPaddingSize.Width;
				}

				size = new Size(
					Math.Min(availableSize.Width, minWidth),
					Math.Min(availableSize.Height, size.Height)
				);

				return size;
			}
		}

		/// <summary>
		/// Measure the grid knowing it has only one row (Nx1 grid)
		/// </summary>
		private Size MeasureHorizontalGrid(Size availableSize, Span<Column> columns, Span<Column> definedColumns, Row row)
		{
			var size = default(Size);
			if (row.Height.IsPixelSize)
			{
				var pixelHeight = row.Height.PixelSize.Value;
				availableSize.Height = pixelHeight;
				size.Height = pixelHeight;
			}

			var positions = GetPositions(columns.Length, 1);
			using (positions.Subscription)
			{
				var measureChild = GetDirectMeasureChild();

				_calculatedColumns = CalculateColumns(
					availableSize,
					positions.Views.Span,
					measureChild,
					columns,
					definedColumns,
					true,
					out var maxHeightMeasured);

				size = new Size(
					_calculatedColumns.Span.Sum(r => r.MinValue),
					Math.Max(size.Height, maxHeightMeasured)
				);

				//If MinHeight property is set on the Grid we need to take it into account in order to match UWP behavior
				var minHeight = size.Height;

				if (MinHeight != 0 && MinHeight > size.Height)
				{
					minHeight = MinHeight - BorderAndPaddingSize.Height;
				}

				size = new Size(
					Math.Min(availableSize.Width, size.Width),
					Math.Min(availableSize.Height, minHeight)
				);

				return size;
			}
		}

		/// <summary>
		/// Measure the grid as if it was a SuperpositionPanel (1x1 grid)
		/// </summary>
		private Size MeasureSuperpositionPanel(Size availableSize, Column column, Row row)
		{
			var size = default(Size);
			if (column.Width.IsPixelSize)
			{
				var pixelWidth = column.Width.PixelSize.Value;
				availableSize.Width = pixelWidth;
				size.Width = pixelWidth;
			}
			if (row.Height.IsPixelSize)
			{
				var pixelHeight = row.Height.PixelSize.Value;
				availableSize.Height = pixelHeight;
				size.Height = pixelHeight;
			}

			foreach (var child in Children)
			{
				var childSize = MeasureElement(child, availableSize);
				size = new Size(
					Math.Max(childSize.Width, size.Width),
					Math.Max(childSize.Height, size.Height)
				);
			}
			size = new Size(
				Math.Min(availableSize.Width, size.Width),
				Math.Min(availableSize.Height, size.Height)
			);
			return size;
		}

		private bool TryArrangeUsingFastPath(Size finalSize, Span<Column> columns, Span<Column> definedColumns, Span<Row> rows, Span<Row> definedRows, Span<ViewPosition> positions)
		{
			var columnCount = columns.Length;
			var rowCount = rows.Length;
			if (columnCount == 1 && rowCount == 1)
			{
				// 1x1
				ArrangeSuperpositionPanel(finalSize, columns[0], rows[0]);
				return true;
			}
			else if (rowCount == 1)
			{
				// Nx1
				ArrangeHorizontalGrid(finalSize, columns, definedColumns, rows[0], positions);
				return true;
			}
			else if (columnCount == 1)
			{
				// 1xN
				ArrangeVerticalGrid(finalSize, columns[0], rows, definedRows, positions);
				return true;
			}

			return false;
		}

		/// <summary>
		/// Arranges the children knowing the grid is 1xN
		/// </summary>
		private void ArrangeVerticalGrid(Size finalSize, Column column, Span<Row> rows, Span<Row> definedRows, Span<ViewPosition> positions)
		{
			if (column.Width.IsPixelSize)
			{
				finalSize.Width = column.Width.PixelSize.Value;
			}

			var measureChild = GetDirectMeasureChild();

			// One simulated column
			Span<DoubleRange> columns = stackalloc DoubleRange[] { new DoubleRange(finalSize.Width) };

			double maxWidthMeasured;
			_calculatedRows = CalculateRows(finalSize, positions, measureChild, columns, rows, definedRows, false, out maxWidthMeasured);

			Span<DoubleRange> calculatedColumns
				= stackalloc DoubleRange[] { column.Width.IsAuto ? new DoubleRange(maxWidthMeasured) : new DoubleRange(finalSize.Width) };

			LayoutChildren(calculatedColumns, _calculatedRows.Span, positions);
		}

		/// <summary>
		/// Arranges the children knowing the grid is Nx1
		/// </summary>
		private void ArrangeHorizontalGrid(Size finalSize, Span<Column> columns, Span<Column> definedColumns, Row row, Span<ViewPosition> positions)
		{
			if (row.Height.IsPixelSize)
			{
				finalSize.Height = row.Height.PixelSize.Value;
			}

			var measureChild = GetDirectMeasureChild();

			double maxHeightMeasured;
			_calculatedColumns = CalculateColumns(finalSize, positions, measureChild, columns, definedColumns, false, out maxHeightMeasured);

			Span<DoubleRange> calculatedRows = stackalloc DoubleRange[] {
				new DoubleRange(row.Height.IsAuto ? maxHeightMeasured : finalSize.Height)
			};

			LayoutChildren(_calculatedColumns.Span, calculatedRows, positions);
		}

		/// <summary>
		/// Arranges the children knowing the grid is 1x1
		/// </summary>
		private void ArrangeSuperpositionPanel(Size finalSize, Column column, Row row)
		{
			if (column.Width.IsPixelSize)
			{
				finalSize.Width = column.Width.PixelSize.Value;
			}
			if (row.Height.IsPixelSize)
			{
				finalSize.Height = row.Height.PixelSize.Value;
			}

			var isMeasureRequired = column.Width.IsAuto || row.Height.IsAuto;
			if (isMeasureRequired)
			{
				finalSize = MeasureChildren(finalSize, column.Width.IsAuto, row.Height.IsAuto);
			}

			var offset = GetChildrenOffset();
			foreach (var child in Children)
			{
				var childFrame = new Foundation.Rect(
					offset.X,
					offset.Y,
					finalSize.Width,
					finalSize.Height
				);
				ArrangeElement(child, childFrame);
			}
		}

		/// <summary>
		/// Measure the children knowing the grid is 1x1
		/// </summary>
		private Size MeasureChildren(Size availableSize, bool isAutoWidth, bool isAutoHeight)
		{
			var borderAndPaddingSize = BorderAndPaddingSize;

			var minSize = default(Size);
			var minWidth = MinWidth - borderAndPaddingSize.Width;
			var minHeight = MinHeight - borderAndPaddingSize.Height;

			//If MinWidth or MinHeight properties are set on the Grid we need to take them into account
			if (MinWidth != 0 && MinHeight != 0)
			{
				minSize = new Size(minWidth, minHeight);
			}
			else if (MinWidth != 0)
			{
				minSize = new Size(minWidth, 0);
			}
			else if (MinHeight != 0)
			{
				minSize = new Size(0, minHeight);
			}

			foreach (var child in Children)
			{
				var childSize = MeasureElement(child, availableSize);
				if (isAutoWidth)
				{
					minSize.Width = Math.Max(minSize.Width, childSize.Width);
				}
				if (isAutoHeight)
				{
					minSize.Height = Math.Max(minSize.Height, childSize.Height);
				}
			}

			if (!isAutoWidth)
			{
				minSize.Width = availableSize.Width;
			}
			if (!isAutoHeight)
			{
				minSize.Height = availableSize.Height;
			}

			return minSize;
		}
	}
}
