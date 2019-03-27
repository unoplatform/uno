using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using Uno.Extensions;
using Uno;
using Uno.UI;
using Windows.Foundation;
using Uno.UI.Extensions;
using System.Runtime.InteropServices;
using Uno.Disposables;

#if XAMARIN_ANDROID
using View = Android.Views.View;
using ViewGroup = Android.Views.ViewGroup;
using Font = Android.Graphics.Typeface;
#elif XAMARIN_IOS_UNIFIED
using UIKit;
using View = UIKit.UIView;
using ViewGroup = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
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
		private static readonly GridSize[] __singleStarSize = { GridSize.Star() };
		private static readonly GridSize[] __singleAutoSize = { GridSize.Auto };

		private Memory<Column> GetColumns(bool considerStarAsAuto)
		{
			if (ColumnDefinitions.InnerList.Count == 0)
			{
				var sizes = (HorizontalAlignment == HorizontalAlignment.Stretch || !double.IsNaN(Width)) ?
					__singleStarSize :
					__singleAutoSize;

				return sizes.SelectToMemory(size => CreateInternalColumn(considerStarAsAuto, size));
			}
			else
			{
				return ColumnDefinitions.InnerList
					.SelectToMemory(cd => CreateInternalColumn(considerStarAsAuto, GridSize.FromGridLength(cd.Width)));
			}
		}

		private static Column CreateInternalColumn(bool considerStarAsAuto, GridSize size)
		{
			if (considerStarAsAuto && size.IsStarSize)
			{
				return Column.Auto;
			}

			return new Column(size);
		}

		private Memory<Row> GetRows(bool considerStarAsAuto)
		{
			if (RowDefinitions.InnerList.Count == 0)
			{
				var sizes = (VerticalAlignment == VerticalAlignment.Stretch || !double.IsNaN(Height)) ?
					__singleStarSize :
					__singleAutoSize;

				return sizes.SelectToMemory(s => CreateInternalRow(considerStarAsAuto, s));
			}
			else
			{
				return RowDefinitions.InnerList
					.SelectToMemory(rd => CreateInternalRow(considerStarAsAuto, GridSize.FromGridLength(rd.Height)));
			}
		}

		private static Row CreateInternalRow(bool considerStarAsAuto, GridSize size)
		{
			if (considerStarAsAuto && size.IsStarSize)
			{
				return Row.Auto;
			}

			return new Row(size);
		}

		private double GetTotalStarSizedWidth(Span<Column> columns)
		{
			double sum = 0;

			for (int i = 0; i < columns.Length; i++)
			{
				var size = columns[i].Width.StarSize;

				if (size.HasValue)
				{
					sum += size.Value;
				}
			}

			return sum;

			// Original LINQ Query, rewritten to avoid the LINQ cost
			// return columns.Sum(c => c.Width.StarSize.GetValueOrDefault());
		}

		private double GetTotalStarSizedHeight(Span<Row> rows)
		{
			double sum = 0;

			for (int i = 0; i < rows.Length; i++)
			{
				var size = rows[i].Height.StarSize;

				if (size.HasValue)
				{
					sum += size.Value;
				}
			}

			return sum;

			// Original LINQ Query, rewritten to avoid the LINQ cost
			// return rows.Sum(c => c.Height.StarSize.GetValueOrDefault());
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			availableSize.Width -= GetHorizontalOffset();
			availableSize.Height -= GetVerticalOffset();

			if (this.Children.Count == 0)
			{
				return default(Size);
			}

			var considerStarColumnsAsAuto = ConsiderStarColumnsAsAuto(availableSize.Width);
			var considerStarRowsAsAuto = ConsiderStarRowsAsAuto(availableSize.Height);

			var columns = GetColumns(considerStarColumnsAsAuto).Span;
			var definedColumns = GetColumns(false).Span;
			var rows = GetRows(considerStarRowsAsAuto).Span;
			var definedRows = GetRows(false).Span;

			Size size;
			if (!TryMeasureUsingFastPath(availableSize, columns, definedColumns, rows, definedRows, out size))
			{
				var measureChild = GetMemoizedMeasureChild();
				var positions = GetPositions();

				using (positions.Subscription)
				{
					// Columns
					double maxMeasuredHeight; //ignored here
					var calculatedPixelColumns = CalculateColumns(
						availableSize,
						positions.Views.Span,
						measureChild,
						columns,
						definedColumns,
						true,
						out maxMeasuredHeight
					);

					// Rows (we need to fully calculate the rows to allow text wrapping)
					double maxMeasuredWidth; //ignored here
					var calculatedPixelRows = CalculateRows(
						availableSize,
						positions.Views.Span,
						measureChild,
						calculatedPixelColumns.Span,
						rows,
						definedRows,
						true,
						out maxMeasuredWidth
					);

					size = new Size(calculatedPixelColumns.Span.Sum(cs => cs.MinValue), calculatedPixelRows.Span.Sum(cs => cs.MinValue));
				}
			}
			size.Width += GetHorizontalOffset();
			size.Height += GetVerticalOffset();

			return size;
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			if (this.Children.Count == 0)
			{
				return finalSize;
			}

			finalSize.Width -= GetHorizontalOffset();
			finalSize.Height -= GetVerticalOffset();

			var positions = GetPositions();

			using (positions.Subscription)
			{
				var availableSize = finalSize;

				var considerStarColumnsAsAuto = ConsiderStarColumnsAsAuto(availableSize.Width);
				var considerStarRowsAsAuto = ConsiderStarRowsAsAuto(availableSize.Height);

				var columns = GetColumns(considerStarColumnsAsAuto).Span;
				var definedColumns = GetColumns(false).Span;
				var rows = GetRows(considerStarRowsAsAuto).Span;
				var definedRows = GetRows(false).Span;

				if (!TryArrangeUsingFastPath(availableSize, columns, definedColumns, rows, definedRows, positions.Views.Span))
				{
					var measureChild = GetMemoizedMeasureChild();

					// Columns
					double maxMeasuredHeight; //ignored here
					var calculatedPixelColumns = CalculateColumns(
						availableSize,
						positions.Views.Span,
						measureChild,
						columns,
						definedColumns,
						false,
						out maxMeasuredHeight
					);

					// Rows
					double maxMeasuredWidth; //ignored here
					var calculatedPixelRows = CalculateRows(
						availableSize,
						positions.Views.Span,
						measureChild,
						calculatedPixelColumns.Span,
						rows,
						definedRows,
						false,
						out maxMeasuredWidth
					);

					LayoutChildren(calculatedPixelColumns.Span, calculatedPixelRows.Span, positions.Views.Span);
				}

				finalSize.Width += GetHorizontalOffset();
				finalSize.Height += GetVerticalOffset();

				return finalSize;
			}
		}

		private void LayoutChildren(Span<DoubleRange> calculatedPixelColumns, Span<DoubleRange> calculatedPixelRows, Span<ViewPosition> positions)
		{
			var childrenToPositionsMap = positions.ToDictionary(pair => pair.Key, pair => pair.Value);

			// Layout the children
			var offset = GetChildrenOffset();
			foreach (var child in Children)
			{
				var gridPosition = childrenToPositionsMap[child];
				var x = offset.X + calculatedPixelColumns.Slice(0, gridPosition.Column).Sum(cs => cs.MinValue);
				var y = offset.Y + calculatedPixelRows.Slice(0, gridPosition.Row).Sum(cs => cs.MinValue);

				Span<double> calculatedPixelColumnsMinValue = stackalloc double[calculatedPixelColumns.Length];
				calculatedPixelColumns.SelectToSpan(calculatedPixelColumnsMinValue, cs => cs.MinValue);

				Span<double> calculatedPixelRowsMinValue = stackalloc double[calculatedPixelRows.Length];
				calculatedPixelRows.SelectToSpan(calculatedPixelRowsMinValue, cs => cs.MinValue);

				var width = GetSpanSum(gridPosition.Column, gridPosition.ColumnSpan, calculatedPixelColumnsMinValue);
				var height = GetSpanSum(gridPosition.Row, gridPosition.RowSpan, calculatedPixelRowsMinValue);
				var childFrame = new Rect(x, y, width, height);
				ArrangeElement(child, childFrame);
			}
		}

		private Memory<DoubleRange> CalculateColumns(
			Size availableSize,
			Span<ViewPosition> positions,
			Func<View, Size, Size> measureChild,
			Span<Column> columns,
			Span<Column> definedColumns,
			bool isMeasuring,
			out double maxHeightMeasured
		)
		{
			// maxHeightMeasured variable is only used for the fast paths calculation
			if (isMeasuring)
			{
				//This is the measure phase. The value will be set when measuring children.
				maxHeightMeasured = 0;
			}
			else
			{
				//This is the arrange phase. We already know the measured size of children.
				var minHeight = Children.Max(child => GetElementDesiredSize(child).Height);

				//If MinHeight property is set on the Grid we need to take it into account in order to match UWP behavior
				if (MinHeight > minHeight)
				{
					minHeight = MinHeight - GetVerticalOffset();
				}

				maxHeightMeasured = minHeight;
			}

			var calculatedPixelColumns = new Memory<double>(new double[columns.Length]);
			columns.SelectToSpan(calculatedPixelColumns.Span, c => c.Width.IsPixelSize ? c.Width.PixelSize.GetValueOrDefault() : 0);

			var allWidths = columns.SelectToMemory(c => c.Width);
			var definedWidths = definedColumns.SelectToMemory(c => c.Width);

			var pixelSizeChildrenX = positions
				.WhereToMemory(pair => IsPixelSize(pair.Value.Column, pair.Value.ColumnSpan, allWidths.Span));

			var autoSizeChildrenX = positions
				.ToArray()
				.Where(pair => HasAutoSize(pair.Value.Column, pair.Value.ColumnSpan, allWidths.Span))
				//We need to Order By StarSizeComparer to have the stars after the Auto before ordering the Autosizecount
				.OrderBy(pair => StarSizeComparer(pair.Value.Column, pair.Value.ColumnSpan, definedWidths.Span))
				.ThenBy(pair => AutoSizeCount(pair.Value.Column, pair.Value.ColumnSpan, allWidths.Span))
				.ToList();

			var starSizeChildren = FindStarSizeChildren(positions, pixelSizeChildrenX, autoSizeChildrenX);

			var availaibleWidth = availableSize.Width;

			//If MinWidth property is set on the Grid that is not stretching horizontally we need to take it into account in order to match UWP behavior
			if (MinWidth != 0 && HorizontalAlignment != HorizontalAlignment.Stretch)
			{
				availaibleWidth = MinWidth - GetHorizontalOffset();
			}

			double remainingSpace(int current, Span<double> pixelColumns) => availaibleWidth - pixelColumns
				.WhereToMemory((_, i) => i != current)
				.Span
				.Sum();

			// Pixel size: We only measure these children to set their desired size. 
			// It is not actually required for the columns calculation (because it's constant).
			if (isMeasuring)
			{
				foreach (var pixelChild in pixelSizeChildrenX.Span)
				{
					var size = measureChild(pixelChild.Key, new Size(GetPixelSize(pixelChild.Value.Column, pixelChild.Value.ColumnSpan, allWidths.Span), availableSize.Height));
					maxHeightMeasured = Math.Max(maxHeightMeasured, size.Height);
				}
			}

			// Auto size: This type of measure is always required. 
			// It's the type of size that depends directly on the size of its content.
			foreach (var autoChild in autoSizeChildrenX)
			{
				var gridPosition = autoChild.Value;

				var childAvailableWidth = availaibleWidth - GetAvailableSizeForPosition(calculatedPixelColumns, gridPosition);

				var childSize = isMeasuring ?
					measureChild(autoChild.Key, new Size(childAvailableWidth, availableSize.Height)) :
					GetElementDesiredSize(autoChild.Key);
				maxHeightMeasured = Math.Max(maxHeightMeasured, childSize.Height);

				Span<GridSizeEntry> columnSizes = stackalloc GridSizeEntry[columns.Length];
				GetSizes(autoChild.Value.Column, autoChild.Value.ColumnSpan, columns, columnSizes);

				var autoColumns = columnSizes.WhereToMemory(pair => pair.Value.IsAuto);
				var pixelColumns = columnSizes.WhereToMemory(pair => pair.Value.IsPixelSize);

				var pixelSize = pixelColumns.Span.Sum(pair => pair.Value.PixelSize.GetValueOrDefault());

				if (autoColumns.Length == 1)
				{
					// The child has only one auto column in its ColumnSpan
					var currentSize = autoColumns.Span.Sum(pair => calculatedPixelColumns.Span[pair.Key]);
					if (childSize.Width - pixelSize > currentSize)
					{

						var index = autoColumns.Span[0].Key;
						calculatedPixelColumns.Span[index] = Math.Max(0, Math.Min(remainingSpace(index, calculatedPixelColumns.Span), Math.Max(
							childSize.Width - pixelSize,
							calculatedPixelColumns.Span[index] - pixelSize
						)));
					}
				}
				else
				{
					// The child has a ColumnSpan with multiple auto columns
					var currentSize = autoColumns.Span.Sum(pair => calculatedPixelColumns.Span[pair.Key]);
					if (childSize.Width - pixelSize > currentSize)
					{
						// Make the first column bigger
						var index = autoColumns.Span[0].Key;
						var otherColumnsSum = autoColumns.Span.Slice(1).Sum(pair => calculatedPixelColumns.Span[pair.Key]);
						calculatedPixelColumns.Span[index] = Math.Max(0, Math.Min(remainingSpace(index, calculatedPixelColumns.Span), Math.Max(
							childSize.Width - pixelSize - otherColumnsSum,
							calculatedPixelColumns.Span[index] - pixelSize - otherColumnsSum
						)));
					}
				}
			}

			// Star size: We always measure to set the desired size, but measuring would only be required for the columns calculation when the Star doesn't mean remaining space.
			var usedWidth = calculatedPixelColumns.Span.Sum();
			double remainingWidth = Math.Max(0, availaibleWidth - usedWidth);
			var totalStarSizedWidth = GetTotalStarSizedWidth(columns);
			var defaultStarWidth = remainingWidth / totalStarSizedWidth;

			var starCalculatedPixelColumns = columns
				.SelectToMemory(
					(c, i) => c.Width.IsStarSize ?
						c.Width.StarSize.GetValueOrDefault() * defaultStarWidth :
						calculatedPixelColumns.Span[i]
				);

			if (isMeasuring)
			{
				var maxStarWidth = 0.0;

				foreach (var starChild in starSizeChildren.Span)
				{
					var size = measureChild(starChild.Key, new Size(GetSpanSum(starChild.Value.Column, starChild.Value.ColumnSpan, starCalculatedPixelColumns.Span), availableSize.Height));
					maxHeightMeasured = Math.Max(maxHeightMeasured, size.Height);

					var starWidth = size.Width;
					var sizes = allWidths.Span.Range(starChild.Value.Column, starChild.Value.ColumnSpan);

					for (int i = 0; i < sizes.Length; i++)
					{
						var columnSize = sizes[i];

						if (!columnSize.IsStarSize)
						{
							starWidth -= calculatedPixelColumns.Span[i];
						}
					}

					var stars = sizes.WhereToMemory(s => s.IsStarSize, s => s.StarSize ?? 0).Span.Sum();

					maxStarWidth = Math.Max(maxStarWidth, starWidth / stars);
				}

				maxStarWidth = Math.Min(defaultStarWidth, maxStarWidth);

				return columns
					.SelectToMemory((c, i) =>
						c.Width.IsStarSize ?
							new DoubleRange(c.Width.StarSize.GetValueOrDefault() * maxStarWidth, starCalculatedPixelColumns.Span[i]) :
							new DoubleRange(calculatedPixelColumns.Span[i]));
			}
			else
			{
				return starCalculatedPixelColumns.Span.SelectToMemory(c => new DoubleRange(c));
			}
		}

		private readonly struct GridSizeEntry
		{
			public readonly int Key;
			public readonly GridSize Value;

			public GridSizeEntry(int i, GridSize column)
			{
				Key = i;
				Value = column;
			}
		}

		private readonly struct ViewPosition
		{
			private readonly GCHandle _key;

			public ViewPosition(GCHandle key, GridPosition value)
			{
				_key = key;
				Value = value;
			}

			public View Key => (View)_key.Target;

			public readonly GridPosition Value;
		}

		private Memory<DoubleRange> CalculateRows(
			Size availableSize,
			Span<ViewPosition> positions,
			Func<View, Size, Size> measureChild,
			Span<DoubleRange> calculatedColumns,
			Span<Row> rows,
			Span<Row> definedRows,
			bool isMeasuring,
			out double maxMeasuredWidth
		)
		{
			// maxMeasuredWidth variable is only used for the fast paths calculation
			if (isMeasuring)
			{
				//This is the measure phase. The value will be set when measuring children.
				maxMeasuredWidth = 0;
			}
			else
			{
				//This is the arrange phase. We already know the measured size of children.
				var minWidth = Children.Max(child => GetElementDesiredSize(child).Width);

				//If MinWidth property is set on the Grid we need to take it into account in order to match UWP behavior
				if (MinWidth > minWidth)
				{
					minWidth = MinWidth - GetHorizontalOffset();
				}

				maxMeasuredWidth = minWidth;
			}

			var calculatedPixelRows = rows.SelectToMemory(c => c.Height.IsPixelSize ? c.Height.PixelSize.GetValueOrDefault() : 0);

			var allHeights = rows.SelectToMemory(c => c.Height);
			var definedHeights = definedRows.SelectToMemory(c => c.Height);

			var pixelSizeChildrenY = positions
				.WhereToMemory(pair => IsPixelSize(pair.Value.Row, pair.Value.RowSpan, allHeights.Span));

			var autoSizeChildrenY = positions
				.ToArray()
				.Where(pair => HasAutoSize(pair.Value.Row, pair.Value.RowSpan, allHeights.Span))
				//We need to Order By StarSizeComparer to have the stars after the Auto before ordering the Autosizecount
				.OrderBy(pair => StarSizeComparer(pair.Value.Row, pair.Value.RowSpan, definedHeights.Span))
				.ThenBy(pair => AutoSizeCount(pair.Value.Row, pair.Value.RowSpan, allHeights.Span))
				.ToList();

			var starSizeChildren = FindStarSizeChildren(positions, pixelSizeChildrenY, autoSizeChildrenY);

			var availaibleHeight = availableSize.Height;

			//If MinHeight property is set on the Grid that is not stretching vertically we need to take it into account in order to match UWP behavior
			if (MinHeight != 0 && VerticalAlignment != VerticalAlignment.Stretch)
			{
				availaibleHeight = MinHeight - GetVerticalOffset();
			}

			double remainingSpace(int current, Span<double> pixelRows) => availaibleHeight - pixelRows
				.WhereToMemory((_, i) => i != current)
				.Span
				.Sum();

			if (isMeasuring)
			{
				foreach (var pixelChild in pixelSizeChildrenY.Span)
				{
					var size = measureChild(pixelChild.Key, new Size(
						GetSpanSum(pixelChild.Value.Column, pixelChild.Value.ColumnSpan, calculatedColumns.SelectToMemory(cs => cs.MaxValue).Span),
						GetPixelSize(pixelChild.Value.Row, pixelChild.Value.RowSpan, allHeights.Span)
					));
					maxMeasuredWidth = Math.Max(maxMeasuredWidth, size.Width);
				}
			}

			foreach (var autoChild in autoSizeChildrenY)
			{
				var gridPosition = autoChild.Value;
				var width = GetSpanSum(gridPosition.Column, gridPosition.ColumnSpan, calculatedColumns.SelectToMemory(cs => cs.MaxValue).Span);

				var childAvailableHeight = availaibleHeight - GetAvailableSizeForPosition(calculatedPixelRows, gridPosition);

				var childSize = isMeasuring ?
					measureChild(autoChild.Key, new Size(width, childAvailableHeight)) :
					GetElementDesiredSize(autoChild.Key);
				maxMeasuredWidth = Math.Max(maxMeasuredWidth, childSize.Width);

				Span<GridSizeEntry> rowSizes = stackalloc GridSizeEntry[rows.Length];
				GetSizes(autoChild.Value.Row, autoChild.Value.RowSpan, rows, rowSizes);

				Span<GridSizeEntry> autoRowsTemp = stackalloc GridSizeEntry[rowSizes.Length];
				var autoRows = rowSizes.WhereToSpan(autoRowsTemp, pair => pair.Value.IsAuto);

				Span<GridSizeEntry> pixelRowsTemp = stackalloc GridSizeEntry[rowSizes.Length];
				var pixelRows = rowSizes.WhereToSpan(pixelRowsTemp, pair => pair.Value.IsPixelSize);

				var pixelSize = pixelRows.Sum(pair => pair.Value.PixelSize.GetValueOrDefault());

				if (autoRows.Length == 1)
				{
					var currentSize = autoRows.Sum(pair => calculatedPixelRows.Span[pair.Key]);
					if (childSize.Height - pixelSize > currentSize)
					{

						// The child has only one auto row in is RowSpan
						var index = autoRows[0].Key;
						var remainingSpaceForIndex = remainingSpace(index, calculatedPixelRows.Span);
						var childSizeWithActualHeight = childSize.Height - pixelSize;
						var existingHeightAdjusted = calculatedPixelRows.Span[index] - pixelSize;
						var size1 = Math.Max(childSizeWithActualHeight, existingHeightAdjusted);
						var size2 = Math.Min(remainingSpaceForIndex, size1);
						var size3 = Math.Max(0, size2);

						calculatedPixelRows.Span[index] = size3;
					}
				}
				else
				{
					// The child has a RowSpan with multiple auto rows
					var currentSize = autoRows.Sum(pair => calculatedPixelRows.Span[pair.Key]);
					if (childSize.Height - pixelSize > currentSize)
					{
						// Make the first row bigger
						var index = autoRows[0].Key;
						var otherRowsSum = autoRows.Slice(1).Sum(pair => calculatedPixelRows.Span[pair.Key]);
						calculatedPixelRows.Span[index] = Math.Max(0, Math.Min(remainingSpace(index, calculatedPixelRows.Span), Math.Max(
							childSize.Height - pixelSize - otherRowsSum,
							calculatedPixelRows.Span[index] - pixelSize - otherRowsSum
						)));
					}
				}
			}

			var usedHeight = calculatedPixelRows.Span.Sum();
			double remainingHeight = Math.Max(0, availaibleHeight - usedHeight);
			var totalStarSizedHeight = GetTotalStarSizedHeight(rows);
			var defaultStarHeight = remainingHeight / totalStarSizedHeight;
			var starCalculatedPixelRows = rows
				.SelectToMemory((r, i) =>
					r.Height.IsStarSize ?
						r.Height.StarSize.GetValueOrDefault() * defaultStarHeight :
						calculatedPixelRows.Span[i]);

			if (isMeasuring)
			{
				var maxStarHeight = 0.0;

				foreach (var starChild in starSizeChildren.Span)
				{
					var size = measureChild(starChild.Key, new Size(
						GetSpanSum(starChild.Value.Column, starChild.Value.ColumnSpan, calculatedColumns.SelectToMemory(cs => cs.MaxValue).Span),
						GetSpanSum(starChild.Value.Row, starChild.Value.RowSpan, starCalculatedPixelRows.Span)
					));
					maxMeasuredWidth = Math.Max(maxMeasuredWidth, size.Width);

					var starHeight = size.Height;
					var sizes = allHeights.Span.Slice(starChild.Value.Row, starChild.Value.RowSpan);

					for (int i = 0; i < sizes.Length; i++)
					{
						var rowSize = sizes[i];

						if (!rowSize.IsStarSize)
						{
							starHeight -= calculatedPixelRows.Span[i];
						}
					}

					var stars = sizes.WhereToMemory(s => s.IsStarSize, s => s.StarSize ?? 0).Span.Sum();

					maxStarHeight = Math.Max(maxStarHeight, starHeight / stars);
				}

				maxStarHeight = Math.Min(defaultStarHeight, maxStarHeight);

				return rows
					.SelectToMemory((r, i) =>
						r.Height.IsStarSize ?
							new DoubleRange(r.Height.StarSize.GetValueOrDefault() * maxStarHeight, starCalculatedPixelRows.Span[i]) :
							new DoubleRange(calculatedPixelRows.Span[i]));
			}
			else
			{
				return starCalculatedPixelRows.Span.SelectToMemory(r => new DoubleRange(r));
			}
		}

		private static double GetAvailableSizeForPosition(Memory<double> calculatedPixel, GridPosition gridPosition)
		{
			var slice = calculatedPixel.Span.Range(gridPosition.Row, gridPosition.RowSpan);
			double result = 0;

			for (int i = 0; i < calculatedPixel.Span.Length; i++)
			{
				var value = calculatedPixel.Span[i];

				if (!slice.Contains(item => item == value))
				{
					result += value;
				}
			}

			return result;
		}

		private static Memory<ViewPosition> FindStarSizeChildren(Span<ViewPosition> positions, Memory<ViewPosition> pixelSizeChildren, List<ViewPosition> autoSizeChildren)
		{
			var res = new Memory<ViewPosition>(new ViewPosition[positions.Length]);
			int count = 0;

			for (int i = 0; i < positions.Length; i++)
			{
				var item = positions[i];

				if (
					!pixelSizeChildren.Span.Contains(c => c.Key == item.Key)
					&& !autoSizeChildren.Any(c => c.Key == item.Key)
				)
				{
					res.Span[count++] = item;
				}
			}

			return res.Slice(0, count);
		}

		// Star sizes revert to auto in the cases where the star sized items are not allowed to stretch.
		// Only exception is when MinHeight is set on the Grid in order to match UWP behavior.
		// In that specific case, star sized items will take at least the entire MinHeight available space
		private bool ConsiderStarRowsAsAuto(double availableHeight)
		{
			var hasMinHeight = MinHeight != 0;
			var hasFixedHeight = !double.IsNaN(Height);
			var isStretch = VerticalAlignment == VerticalAlignment.Stretch;
			var isInsideInfinity = double.IsInfinity(availableHeight);

			return !hasFixedHeight && !((isStretch || hasMinHeight) && !isInsideInfinity);
		}

		// Star sizes revert to auto in the cases where the star sized items are not allowed to stretch.
		// Only exception is when MinWidth is set on the Grid in order to match UWP behavior.
		// In that specific case, star sized items will take at least the entire MinWidth available space
		private bool ConsiderStarColumnsAsAuto(double availableWidth)
		{
			var hasMinWidth = MinWidth != 0;
			var hasFixedWidth = !double.IsNaN(Width);
			var isStretch = HorizontalAlignment == HorizontalAlignment.Stretch;
			var isInsideInfinity = double.IsInfinity(availableWidth);

			return !hasFixedWidth && !((isStretch || hasMinWidth) && !isInsideInfinity);
		}

		private static void GetSizes(int index, int span, Span<Column> sizes, Span<GridSizeEntry> result)
		{
			int bound = Math.Min(index + span, sizes.Length);

			for (int i = index, j = 0; i < bound; i++, j++)
			{
				result[j] = new GridSizeEntry(i, sizes[i].Width);
			}

			// LINQ Query for reference, rewritten to avoid the LINQ cost
			// return sizes
			//	.Select((s, i) => new KeyValuePair<int, Size>(i, s))
			//	.Skip(index)
			//	.Take(span);
		}

		private static void GetSizes(int index, int span, Span<Row> sizes, Span<GridSizeEntry> result)
		{
			int bound = Math.Min(index + span, sizes.Length);

			for (int i = index, j = 0; i < bound; i++, j++)
			{
				result[j] = new GridSizeEntry(i, sizes[i].Height);
			}

			// LINQ Query for reference, rewritten to avoid the LINQ cost
			// return sizes
			//	.Select((s, i) => new KeyValuePair<int, Size>(i, s))
			//	.Skip(index)
			//	.Take(span);
		}

		private static bool HasAutoSize(int index, int span, Span<GridSize> sizes)
		{
			var cached = sizes.Range(index, span);

			// The code of this method is the LINQ unrolled version of the following method,
			// to avoid memory allocations:
			//
			//	return cached.Any(s => s.IsAuto) && !cached.Any(s => s.IsStarSize);

			var hasAuto = false;
			var hasStarSize = false;

			foreach (var s in cached)
			{
				if (s.IsAuto)
				{
					hasAuto = true;
					break;
				}
			}

			foreach (var s in cached)
			{
				if (s.IsStarSize)
				{
					hasStarSize = true;
					break;
				}
			}

			return hasAuto && !hasStarSize;
		}

		private static int AutoSizeCount(int index, int span, Span<GridSize> sizes)
		{
			return sizes.Slice(index, span).Count(s => s.IsAuto);
		}

		/// <summary>
		/// Contains Star returns 1, Doesnt contain star 0
		/// </summary>
		private static int StarSizeComparer(int index, int span, Span<GridSize> sizes)
		{
			return Math.Min(1, sizes.Slice(index, span).Count(s => s.IsStarSize));
		}

		private static bool IsPixelSize(int index, int span, Span<GridSize> sizes)
		{
			int bound = Math.Min(index + span, sizes.Length);

			for (int i = index; i < bound; i++)
			{
				if (!sizes[i].IsPixelSize)
				{
					return false;
				}
			}

			return true;

			// Original LINQ Query, for reference.
			// return sizes.Skip(index).Take(span).All(s => s.IsPixelSize);
		}

		private static double GetPixelSize(int index, int span, Span<GridSize> sizes)
		{
			Span<double> doubleSizes = stackalloc double[sizes.Length];
			sizes.SelectToSpan(doubleSizes, s => s.PixelSize != null ? s.PixelSize.Value : 0.0);

			return GetSpanSum(index, span, doubleSizes);
		}

		private static double GetSpanSum(int index, int span, Span<double> sizes)
		{
			double sum = 0;
			int bound = Math.Min(index + span, sizes.Length);

			for (int i = index; i < bound; i++)
			{
				sum += sizes[i];
			}

			return sum;

			// Original LINQ query, for reference.
			// return sizes.Skip(index).Take(span).Sum();
		}

		private Point GetChildrenOffset()
		{
			var borderThickness = BorderThickness;
			var padding = Padding;

			return new Point(
				borderThickness.Left + padding.Left,
				borderThickness.Top + padding.Top
			);
		}

		private Func<View, Size, Size> GetMemoizedMeasureChild()
		{
			var viewToPreviousMeasure = new Dictionary<View, Tuple<Size, Size>>();

			return Funcs.CreateMemoized<View, Size, Size>((view, availableSize) =>
			{
				Tuple<Size, Size> previousMeasure;
				if (viewToPreviousMeasure.TryGetValue(view, out previousMeasure))
				{
					// if the previous available size is bigger  than the current available size and that
					//    the previous measured  size is smaller that the current available size
					if (previousMeasure.Item1.Width >= availableSize.Width &&
						previousMeasure.Item1.Height >= availableSize.Height &&
						previousMeasure.Item2.Width <= availableSize.Width &&
						previousMeasure.Item2.Height <= availableSize.Height)
					{
						return previousMeasure.Item2;
					}
				}

				var measured = MeasureElement(view, availableSize);
				viewToPreviousMeasure[view] = Tuple.Create(availableSize, measured);
				return measured;
			});
		}

		/// <summary>
		/// This should only be used by fast paths that don't need memoization
		/// </summary>
		private Func<View, Size, Size> GetDirectMeasureChild()
		{
			return MeasureElement;
		}

		private (IDisposable Subscription, Memory<ViewPosition> Views) GetPositions()
		{
			var refs = Children.SelectToArray(c => (View: c, Handle: GCHandle.Alloc(c, GCHandleType.Normal)));

			return (
				Disposable.Create(() => refs.ForEach(c => c.Handle.Free())),
				refs
					.SelectToMemory(c =>
						new ViewPosition(
							c.Handle,
							new GridPosition(
								Grid.GetColumn(c.View),
								Grid.GetRow(c.View),
								Grid.GetColumnSpan(c.View),
								Grid.GetRowSpan(c.View)
							)
						)
					)
			);
		}

		readonly struct Column
		{
			public GridSize Width { get; }
			public Column(GridSize width)
			{
				Width = width;
			}

			public static Column Auto { get; } = new Column(GridSize.Auto);
		}

		readonly struct Row
		{
			public readonly GridSize Height;
			public Row(GridSize height)
			{
				Height = height;
			}

			public static Row Auto { get; } = new Row(GridSize.Auto);
		}

		private readonly struct GridSize
		{
			private readonly bool _hasPixelSize;
			private readonly double _pixelSize;

			private readonly bool _hasStarSize;
			private readonly double _starSize;

			public static GridSize Auto => new GridSize(pixelSize: double.NaN);
			public static GridSize Star(double coeficient = 1f) => new GridSize(starSize: coeficient);
			public static GridSize Pixel(double coeficient) => new GridSize(pixelSize: coeficient);

			public static GridSize FromGridLength(GridLength gridLength)
			{
				switch (gridLength.GridUnitType)
				{
					default:
					case GridUnitType.Auto:
						return Auto;
					case GridUnitType.Pixel:
						return Pixel(gridLength.Value);
					case GridUnitType.Star:
						return Star(gridLength.Value);
				}
			}

			public GridSize(double? pixelSize = null, double? starSize = null)
			{
				if (pixelSize != null)
				{
					_hasPixelSize = true;
					_pixelSize = pixelSize.Value;
				}
				else
				{
					_hasPixelSize = false;
					_pixelSize = double.NaN;
				}

				if (starSize != null)
				{
					_hasStarSize = true;
					_starSize = starSize.Value;
				}
				else
				{
					_hasStarSize = false;
					_starSize = double.NaN;
				}
			}

			public double? PixelSize => _hasPixelSize ? (double?)_pixelSize : null;
			public double? StarSize => _hasStarSize ? (double?)_starSize : null;

			public bool IsAuto => _hasPixelSize && double.IsNaN(_pixelSize);
			public bool IsStarSize => _hasStarSize;
			public bool IsPixelSize => !IsAuto && !IsStarSize;
		}

		private readonly struct DoubleRange
		{
			public DoubleRange(double fixedValue) :
				this(fixedValue, fixedValue)
			{
			}

			public DoubleRange(double minValue, double maxValue)
			{
				MinValue = minValue;
				MaxValue = maxValue;
			}

			public double MinValue { get; }
			public double MaxValue { get; }
		}
	}

	struct GridPosition
	{
		public int Column { get; set; }
		public int Row { get; set; }

		public int ColumnSpan { get; set; }
		public int RowSpan { get; set; }

		public GridPosition(int column = 0, int row = 0, int columnSpan = 1, int rowSpan = 1)
		{
			Column = column;
			Row = row;

			ColumnSpan = columnSpan;
			RowSpan = rowSpan;

			if (ColumnSpan < 1 || RowSpan < 1)
			{
				throw new IndexOutOfRangeException("ColumnSpan and RowSpan should be greater than 0.");
			}
		}

		public override string ToString()
		{
			return "Column={0}, Row={1}, ColumnSpan={2}, RowSpan={3}".InvariantCultureFormat(Column, Row, ColumnSpan, RowSpan);
		}
	}


	public static class Extensions
	{
		public static void SelectToSpan<TIn, TOut>(
			this List<TIn> list,
			Span<TOut> span,
			Func<TIn, TOut> selector
		)
		{
			for (int i = 0; i < list.Count; i++)
			{
				span[i] = selector(list[i]);
			}
		}

		public static void SelectToSpan<TIn, TOut>(
			this Span<TIn> list,
			Span<TOut> span,
			Func<TIn, TOut> selector
		)
		{
			for (int i = 0; i < list.Length; i++)
			{
				span[i] = selector(list[i]);
			}
		}

		public static void SelectToSpan<TIn, TOut>(
			this Span<TIn> list,
			Span<TOut> span,
			Func<TIn, int, TOut> selector
		)
		{
			for (int i = 0; i < list.Length; i++)
			{
				span[i] = selector(list[i], i);
			}
		}

		public static void SelectToSpan<TIn, TOut>(
			this TIn[] list,
			ref Span<TOut> span,
			Func<TIn, TOut> selector
		)
		{
			for (int i = 0; i < list.Length; i++)
			{
				span[i] = selector(list[i]);
			}
		}

		public static Memory<TOut> SelectToMemory<TIn, TOut>(
			this Span<TIn> list,
			Func<TIn, TOut> selector
		)
		{
			var output = new Memory<TOut>(new TOut[list.Length]);
			for (int i = 0; i < list.Length; i++)
			{
				output.Span[i] = selector(list[i]);
			}

			return output;
		}

		public static Memory<TOut> SelectToMemory<TIn, TOut>(
			this Span<TIn> list,
			Func<TIn, int, TOut> selector
		)
		{
			var output = new Memory<TOut>(new TOut[list.Length]);
			for (int i = 0; i < list.Length; i++)
			{
				output.Span[i] = selector(list[i], i);
			}

			return output;
		}

		public static Memory<TOut> SelectToMemory<TIn, TOut>(
			this IList<TIn> list,
			Func<TIn, TOut> selector
		)
		{
			var output = new Memory<TOut>(new TOut[list.Count]);
			for (int i = 0; i < list.Count; i++)
			{
				output.Span[i] = selector(list[i]);
			}

			return output;
		}

		public static Memory<TValue> WhereToMemory<TValue>(
			this Span<TValue> list,
			Func<TValue, bool> filter
		)
		{
			var output = new Memory<TValue>(new TValue[list.Length]);
			int valuesCount = 0;
			for (int i = 0; i < list.Length; i++)
			{
				var value = list[i];

				if (filter(value))
				{
					output.Span[valuesCount++] = value;
				}
			}

			return output.Slice(0, valuesCount);
		}

		public static Memory<TValue> WhereToMemory<TValue>(
			this Span<TValue> list,
			Func<TValue, int, bool> filter
		)
		{
			var output = new Memory<TValue>(new TValue[list.Length]);
			int values = 0;
			for (int i = 0; i < list.Length; i++)
			{
				var value = list[i];

				if (filter(value, i))
				{
					output.Span[values++] = value;
				}
			}

			return output.Slice(0, values);
		}

		public static Memory<TResult> WhereToMemory<TValue, TResult>(
			this Span<TValue> list,
			Func<TValue, bool> filter,
			Func<TValue, TResult> selector
		)
		{
			var output = new Memory<TResult>(new TResult[list.Length]);
			int values = 0;
			for (int i = 0; i < list.Length; i++)
			{
				var value = list[i];

				if (filter(value))
				{
					output.Span[values++] = selector(value);
				}
			}

			return output.Slice(0, values);
		}

		public static Span<TValue> WhereToSpan<TValue>(
			this Span<TValue> list,
			Span<TValue> target,
			Func<TValue, bool> filter
		)
		{
			int values = 0;
			for (int i = 0; i < list.Length; i++)
			{
				var value = list[i];

				if (filter(value))
				{
					target[values++] = value;
				}
			}

			return target.Slice(0, values);
		}

		public static int Count<T>(this Span<T> span, Func<T, bool> predicate)
		{
			int result = 0;
			foreach (var value in span)
			{
				if (predicate(value))
				{
					result++;
				}
			}

			return result;
		}

		public static bool Contains<T>(this Span<T> span, Func<T, bool> predicate)
		{
			foreach (var value in span)
			{
				if (predicate(value))
				{
					return true;
				}
			}

			return false;
		}

		public static Dictionary<TKey, TValue> ToDictionary<TIn, TKey, TValue>(this Span<TIn> span, Func<TIn, TKey> keySelector, Func<TIn, TValue> valueSelector)
		{
			var result = new Dictionary<TKey, TValue>(span.Length);
			foreach (var item in span)
			{
				result.Add(keySelector(item), valueSelector(item));
			}
			return result;
		}

		public static double Sum(this Span<double> span)
		{
			double result = 0;

			foreach (var value in span)
			{
				result += value;
			}

			return result;
		}

		public static double Sum<TIn>(this Span<TIn> span, Func<TIn, double> selector)
		{
			double result = 0;

			foreach (var value in span)
			{
				result += selector(value);
			}

			return result;
		}

		public static Span<TValue> Range<TValue>(this Span<TValue> span, int start, int range)
			=> span.Slice(
				start: Math.Min(span.Length, start),
				length: Math.Min(range, span.Length - start)
			);
	}
}
