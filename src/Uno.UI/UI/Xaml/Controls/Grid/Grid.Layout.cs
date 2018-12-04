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

		private List<Column> GetColumns(bool considerStarAsAuto)
		{
			if (ColumnDefinitions.InnerList.Count == 0)
			{
				var sizes = (HorizontalAlignment == HorizontalAlignment.Stretch || !double.IsNaN(Width)) ?
					__singleStarSize :
					__singleAutoSize;

				return sizes.SelectToList(size => CreateInternalColumn(considerStarAsAuto, size));
			}
			else
			{
				return ColumnDefinitions.InnerList
					.SelectToList(cd => CreateInternalColumn(considerStarAsAuto, GridSize.FromGridLength(cd.Width)));
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

		private List<Row> GetRows(bool considerStarAsAuto)
		{
			if (RowDefinitions.InnerList.Count == 0)
			{
				var sizes = (VerticalAlignment == VerticalAlignment.Stretch || !double.IsNaN(Height)) ?
					__singleStarSize :
					__singleAutoSize;

				return sizes.SelectToList(s => CreateInternalRow(considerStarAsAuto, s));
			}
			else
			{
				return RowDefinitions.InnerList
					.SelectToList(rd => CreateInternalRow(considerStarAsAuto, GridSize.FromGridLength(rd.Height)));
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

		private double GetTotalStarSizedWidth(List<Column> columns)
		{
			double sum = 0;

			for (int i = 0; i < columns.Count; i++)
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

		private double GetTotalStarSizedHeight(List<Row> rows)
		{
			double sum = 0;

			for (int i = 0; i < rows.Count; i++)
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

			var columns = GetColumns(considerStarColumnsAsAuto);
			var definedColumns = GetColumns(false);

			var rows = GetRows(considerStarRowsAsAuto);
			var definedRows = GetRows(false);

			Size size;
			if (!TryMeasureUsingFastPath(availableSize, columns, definedColumns, rows, definedRows, out size))
			{
				var measureChild = GetMemoizedMeasureChild();
				var positions = GetPositions();

				// Columns
				double maxMeasuredHeight; //ignored here
				var calculatedPixelColumns = CalculateColumns(
					availableSize,
					positions,
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
					positions,
					measureChild,
					calculatedPixelColumns,
					rows,
					definedRows,
					true,
					out maxMeasuredWidth
				);

				size = new Size(calculatedPixelColumns.Select(cs => cs.MinValue).Sum(), calculatedPixelRows.Select(cs => cs.MinValue).Sum());
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

			var availableSize = finalSize;

			var considerStarColumnsAsAuto = ConsiderStarColumnsAsAuto(availableSize.Width);
			var considerStarRowsAsAuto = ConsiderStarRowsAsAuto(availableSize.Height);

			var columns = GetColumns(considerStarColumnsAsAuto);
			var definedColumns = GetColumns(false);

			var rows = GetRows(considerStarRowsAsAuto);
			var definedRows = GetRows(false);

			if (!TryArrangeUsingFastPath(availableSize, columns, definedColumns, rows, definedRows, positions))
			{
				var measureChild = GetMemoizedMeasureChild();

				// Columns
				double maxMeasuredHeight; //ignored here
				var calculatedPixelColumns = CalculateColumns(
					availableSize,
					positions,
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
					positions,
					measureChild,
					calculatedPixelColumns,
					rows,
					definedRows,
					false,
					out maxMeasuredWidth
				);

				LayoutChildren(calculatedPixelColumns, calculatedPixelRows, positions);
			}

			finalSize.Width += GetHorizontalOffset();
			finalSize.Height += GetVerticalOffset();

			return finalSize;
		}

		private void LayoutChildren(List<DoubleRange> calculatedPixelColumns, List<DoubleRange> calculatedPixelRows, List<ViewPosition> positions)
		{
			var childrenToPositionsMap = positions.ToDictionary(pair => pair.Key, pair => pair.Value);

			// Layout the children
			var offset = GetChildrenOffset();
			foreach (var child in Children)
			{
				var gridPosition = childrenToPositionsMap[child];
				var x = offset.X + calculatedPixelColumns.Take(gridPosition.Column).Select(cs => cs.MinValue).Sum();
				var y = offset.Y + calculatedPixelRows.Take(gridPosition.Row).Select(cs => cs.MinValue).Sum();
				var width = GetSpanSum(gridPosition.Column, gridPosition.ColumnSpan, calculatedPixelColumns.SelectToList(cs => cs.MinValue));
				var height = GetSpanSum(gridPosition.Row, gridPosition.RowSpan, calculatedPixelRows.SelectToList(cs => cs.MinValue));
				var childFrame = new Rect(x, y, width, height);
				ArrangeElement(child, childFrame);
			}
		}

		private List<DoubleRange> CalculateColumns(
			Size availableSize,
			List<ViewPosition> positions,
			Func<View, Size, Size> measureChild,
			List<Column> columns,
			List<Column> definedColumns,
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

			var calculatedPixelColumns = columns
						 .SelectToList(c => c.Width.IsPixelSize ? c.Width.PixelSize.GetValueOrDefault() : 0);

			var allWidths = columns.SelectToList(c => c.Width);
			var definedWidths = definedColumns.SelectToList(c => c.Width);

			var pixelSizeChildrenX = positions
				.WhereToList(pair => IsPixelSize(pair.Value.Column, pair.Value.ColumnSpan, allWidths));

			var autoSizeChildrenX = positions
				.Where(pair => HasAutoSize(pair.Value.Column, pair.Value.ColumnSpan, allWidths))
				//We need to Order By StarSizeComparer to have the stars after the Auto before ordering the Autosizecount
				.OrderBy(pair => StarSizeComparer(pair.Value.Column, pair.Value.ColumnSpan, definedWidths))
				.ThenBy(pair => AutoSizeCount(pair.Value.Column, pair.Value.ColumnSpan, allWidths))
				.ToList();

			var starSizeChildren = positions
				.Except(pixelSizeChildrenX)
				.Except(autoSizeChildrenX)
				.ToList();

			var availaibleWidth = availableSize.Width;

			//If MinWidth property is set on the Grid that is not stretching horizontally we need to take it into account in order to match UWP behavior
			if (MinWidth != 0 && HorizontalAlignment != HorizontalAlignment.Stretch)
			{
				availaibleWidth = MinWidth - GetHorizontalOffset();
			}

			double remainingSpace(int current) => availaibleWidth - calculatedPixelColumns
				.Where((_, i) => i != current)
				.Sum();

			// Pixel size: We only measure these children to set their desired size. 
			// It is not actually required for the columns calculation (because it's constant).
			if (isMeasuring)
			{
				foreach (var pixelChild in pixelSizeChildrenX)
				{
					var size = measureChild(pixelChild.Key, new Size(GetPixelSize(pixelChild.Value.Column, pixelChild.Value.ColumnSpan, allWidths), availableSize.Height));
					maxHeightMeasured = Math.Max(maxHeightMeasured, size.Height);
				}
			}

			// Auto size: This type of measure is always required. 
			// It's the type of size that depends directly on the size of its content.
			foreach (var autoChild in autoSizeChildrenX)
			{
				var gridPosition = autoChild.Value;

				var childAvailableWidth = availaibleWidth - calculatedPixelColumns
					.Except(
						calculatedPixelColumns
							.Skip(gridPosition.Column)
							.Take(gridPosition.ColumnSpan)
					)
					.Sum();

				var childSize = isMeasuring ?
					measureChild(autoChild.Key, new Size(childAvailableWidth, availableSize.Height)) :
					GetElementDesiredSize(autoChild.Key);
				maxHeightMeasured = Math.Max(maxHeightMeasured, childSize.Height);

				var columnSizes = GetSizes(autoChild.Value.Column, autoChild.Value.ColumnSpan, columns.SelectToList(c => c.Width));
				var autoColumns = columnSizes.WhereToList(pair => pair.Value.IsAuto);
				var pixelColumns = columnSizes.WhereToList(pair => pair.Value.IsPixelSize);

				var pixelSize = pixelColumns.Sum(pair => pair.Value.PixelSize.GetValueOrDefault());

				if (autoColumns.Count == 1)
				{
					// The child has only one auto column in its ColumnSpan
					var currentSize = autoColumns.Sum(pair => calculatedPixelColumns[pair.Key]);
					if (childSize.Width - pixelSize > currentSize)
					{

						var index = autoColumns.First().Key;
						calculatedPixelColumns[index] = Math.Max(0, Math.Min(remainingSpace(index), Math.Max(
							childSize.Width - pixelSize,
							calculatedPixelColumns[index] - pixelSize
						)));
					}
				}
				else
				{
					// The child has a ColumnSpan with multiple auto columns
					var currentSize = autoColumns.Sum(pair => calculatedPixelColumns[pair.Key]);
					if (childSize.Width - pixelSize > currentSize)
					{
						// Make the first column bigger
						var index = autoColumns.First().Key;
						var otherColumnsSum = autoColumns.Skip(1).Sum(pair => calculatedPixelColumns[pair.Key]);
						calculatedPixelColumns[index] = Math.Max(0, Math.Min(remainingSpace(index), Math.Max(
							childSize.Width - pixelSize - otherColumnsSum,
							calculatedPixelColumns[index] - pixelSize - otherColumnsSum
						)));
					}
				}
			}

			// Star size: We always measure to set the desired size, but measuring would only be required for the columns calculation when the Star doesn't mean remaining space.
			var usedWidth = calculatedPixelColumns.Sum();
			double remainingWidth = Math.Max(0, availaibleWidth - usedWidth);
			var totalStarSizedWidth = GetTotalStarSizedWidth(columns);
			var defaultStarWidth = remainingWidth / totalStarSizedWidth;
			var starCalculatedPixelColumns = columns
				.SelectToList((c, i) =>
					c.Width.IsStarSize ?
						c.Width.StarSize.GetValueOrDefault() * defaultStarWidth :
						calculatedPixelColumns[i]);

			if (isMeasuring)
			{
				var maxStarWidth = 0.0;

				foreach (var starChild in starSizeChildren)
				{
					var size = measureChild(starChild.Key, new Size(GetSpanSum(starChild.Value.Column, starChild.Value.ColumnSpan, starCalculatedPixelColumns), availableSize.Height));
					maxHeightMeasured = Math.Max(maxHeightMeasured, size.Height);

					var starWidth = size.Width;
					var sizes = allWidths.ToRangeList(starChild.Value.Column, starChild.Value.ColumnSpan);

					for (int i = 0; i < sizes.Count; i++)
					{
						var columnSize = sizes[i];

						if (!columnSize.IsStarSize)
						{
							starWidth -= calculatedPixelColumns[i];
						}
					}

					var stars = sizes.Where(s => s.IsStarSize).Select(s => s.StarSize ?? 0).Sum();

					maxStarWidth = Math.Max(maxStarWidth, starWidth / stars);
				}

				maxStarWidth = Math.Min(defaultStarWidth, maxStarWidth);

				return columns
					.SelectToList((c, i) =>
						c.Width.IsStarSize ?
							new DoubleRange(c.Width.StarSize.GetValueOrDefault() * maxStarWidth, starCalculatedPixelColumns[i]) :
							new DoubleRange(calculatedPixelColumns[i]));
			}
			else
			{
				return starCalculatedPixelColumns.SelectToList(c => new DoubleRange(c));
			}
		}

		private class ViewPosition
		{
			public ViewPosition(View key, GridPosition value)
			{
				Key = key;
				Value = value;
			}

			public readonly View Key;
			public readonly GridPosition Value;
		}

		private List<DoubleRange> CalculateRows(
			Size availableSize,
			List<ViewPosition> positions,
			Func<View, Size, Size> measureChild,
			List<DoubleRange> calculatedColumns,
			List<Row> rows,
			List<Row> definedRows,
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

			var calculatedPixelRows = rows
						 .SelectToList(c => c.Height.IsPixelSize ? c.Height.PixelSize.GetValueOrDefault() : 0);

			var allHeights = rows.SelectToList(c => c.Height);
			var definedHeights = definedRows.SelectToList(c => c.Height);

			var pixelSizeChildrenY = positions
				.WhereToList(pair => IsPixelSize(pair.Value.Row, pair.Value.RowSpan, allHeights));

			var autoSizeChildrenY = positions
				.Where(pair => HasAutoSize(pair.Value.Row, pair.Value.RowSpan, allHeights))
				//We need to Order By StarSizeComparer to have the stars after the Auto before ordering the Autosizecount
				.OrderBy(pair => StarSizeComparer(pair.Value.Row, pair.Value.RowSpan, definedHeights))
				.ThenBy(pair => AutoSizeCount(pair.Value.Row, pair.Value.RowSpan, allHeights))
				.ToList();

			var starSizeChildren = positions
				.Except(pixelSizeChildrenY)
				.Except(autoSizeChildrenY)
				.ToList();

			var availaibleHeight = availableSize.Height;

			//If MinHeight property is set on the Grid that is not stretching vertically we need to take it into account in order to match UWP behavior
			if (MinHeight != 0 && VerticalAlignment != VerticalAlignment.Stretch)
			{
				availaibleHeight = MinHeight - GetVerticalOffset();
			}

			Func<int, double> remainingSpace = (current) => availaibleHeight - calculatedPixelRows
				.Where((_, i) => i != current)
				.Sum();

			if (isMeasuring)
			{
				foreach (var pixelChild in pixelSizeChildrenY)
				{
					var size = measureChild(pixelChild.Key, new Size(
						GetSpanSum(pixelChild.Value.Column, pixelChild.Value.ColumnSpan, calculatedColumns.SelectToList(cs => cs.MaxValue)),
						GetPixelSize(pixelChild.Value.Row, pixelChild.Value.RowSpan, allHeights)
					));
					maxMeasuredWidth = Math.Max(maxMeasuredWidth, size.Width);
				}
			}

			foreach (var autoChild in autoSizeChildrenY)
			{
				var gridPosition = autoChild.Value;
				var width = GetSpanSum(gridPosition.Column, gridPosition.ColumnSpan, calculatedColumns.SelectToList(cs => cs.MaxValue));

				var childAvailableHeight = availaibleHeight - calculatedPixelRows
					.Except(
						calculatedPixelRows
							.Skip(gridPosition.Row)
							.Take(gridPosition.RowSpan)
					)
					.Sum();

				var childSize = isMeasuring ?
					measureChild(autoChild.Key, new Size(width, childAvailableHeight)) :
					GetElementDesiredSize(autoChild.Key);
				maxMeasuredWidth = Math.Max(maxMeasuredWidth, childSize.Width);

				var rowSizes = GetSizes(autoChild.Value.Row, autoChild.Value.RowSpan, rows.SelectToList(c => c.Height));
				var autoRows = rowSizes.WhereToList(pair => pair.Value.IsAuto);
				var pixelRows = rowSizes.WhereToList(pair => pair.Value.IsPixelSize);

				var pixelSize = pixelRows.Sum(pair => pair.Value.PixelSize.GetValueOrDefault());

				if (autoRows.Count == 1)
				{
					var currentSize = autoRows.Sum(pair => calculatedPixelRows[pair.Key]);
					if (childSize.Height - pixelSize > currentSize)
					{

						// The child has only one auto row in is RowSpan
						var index = autoRows.First().Key;
						var remainingSpaceForIndex = remainingSpace(index);
						var childSizeWithActualHeight = childSize.Height - pixelSize;
						var existingHeightAdjusted = calculatedPixelRows[index] - pixelSize;
						var size1 = Math.Max(childSizeWithActualHeight, existingHeightAdjusted);
						var size2 = Math.Min(remainingSpaceForIndex, size1);
						var size3 = Math.Max(0, size2);

						calculatedPixelRows[index] = size3;
					}
				}
				else
				{
					// The child has a RowSpan with multiple auto rows
					var currentSize = autoRows.Sum(pair => calculatedPixelRows[pair.Key]);
					if (childSize.Height - pixelSize > currentSize)
					{
						// Make the first row bigger
						var index = autoRows.First().Key;
						var otherRowsSum = autoRows.Skip(1).Sum(pair => calculatedPixelRows[pair.Key]);
						calculatedPixelRows[index] = Math.Max(0, Math.Min(remainingSpace(index), Math.Max(
							childSize.Height - pixelSize - otherRowsSum,
							calculatedPixelRows[index] - pixelSize - otherRowsSum
						)));
					}
				}
			}

			var usedHeight = calculatedPixelRows.Sum();
			double remainingHeight = Math.Max(0, availaibleHeight - usedHeight);
			var totalStarSizedHeight = GetTotalStarSizedHeight(rows);
			var defaultStarHeight = remainingHeight / totalStarSizedHeight;
			var starCalculatedPixelRows = rows
				.SelectToList((r, i) =>
					r.Height.IsStarSize ?
						r.Height.StarSize.GetValueOrDefault() * defaultStarHeight :
						calculatedPixelRows[i]);

			if (isMeasuring)
			{
				var maxStarHeight = 0.0;

				foreach (var starChild in starSizeChildren)
				{
					var size = measureChild(starChild.Key, new Size(
						GetSpanSum(starChild.Value.Column, starChild.Value.ColumnSpan, calculatedColumns.SelectToList(cs => cs.MaxValue)),
						GetSpanSum(starChild.Value.Row, starChild.Value.RowSpan, starCalculatedPixelRows)
					));
					maxMeasuredWidth = Math.Max(maxMeasuredWidth, size.Width);

					var starHeight = size.Height;
					var sizes = allHeights.ToRangeList(starChild.Value.Row, starChild.Value.RowSpan);

					for (int i = 0; i < sizes.Count; i++)
					{
						var rowSize = sizes[i];

						if (!rowSize.IsStarSize)
						{
							starHeight -= calculatedPixelRows[i];
						}
					}

					var stars = sizes.Where(s => s.IsStarSize).Select(s => s.StarSize ?? 0).Sum();

					maxStarHeight = Math.Max(maxStarHeight, starHeight / stars);
				}

				maxStarHeight = Math.Min(defaultStarHeight, maxStarHeight);

				return rows
					.SelectToList((r, i) =>
						r.Height.IsStarSize ?
							new DoubleRange(r.Height.StarSize.GetValueOrDefault() * maxStarHeight, starCalculatedPixelRows[i]) :
							new DoubleRange(calculatedPixelRows[i]));
			}
			else
			{
				return starCalculatedPixelRows.SelectToList(r => new DoubleRange(r));
			}
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

		private static List<(int Key, GridSize Value)> GetSizes(int index, int span, List<GridSize> sizes)
		{
			int bound = Math.Min(index + span, sizes.Count);
			var output = new List<(int, GridSize)>(span);

			for (int i = index; i < bound; i++)
			{
				output.Add((i, sizes[i]));
			}

			return output;

			// LINQ Query for reference, rewritten to avoid the LINQ cost
			// return sizes
			//	.Select((s, i) => new KeyValuePair<int, Size>(i, s))
			//	.Skip(index)
			//	.Take(span);
		}

		private static bool HasAutoSize(int index, int span, List<GridSize> sizes)
		{
			var cached = sizes.ToRangeList(index, span);

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

		private static int AutoSizeCount(int index, int span, List<GridSize> sizes)
		{
			return sizes.ToRangeList(index, span).Count(s => s.IsAuto);
		}

		/// <summary>
		/// Contains Star returns 1, Doesnt contain star 0
		/// </summary>
		private static int StarSizeComparer(int index, int span, List<GridSize> sizes)
		{
			return Math.Min(1, sizes.ToRangeList(index, span).Count(s => s.IsStarSize));
		}

		private static bool IsPixelSize(int index, int span, List<GridSize> sizes)
		{
			int bound = Math.Min(index + span, sizes.Count);

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

		private static double GetPixelSize(int index, int span, List<GridSize> sizes)
		{
			return GetSpanSum(index, span, sizes.SelectToList(s => s.PixelSize ?? 0));
		}

		private static double GetSpanSum(int index, int span, List<double> sizes)
		{
			double sum = 0;
			int bound = Math.Min(index + span, sizes.Count);

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

		private List<ViewPosition> GetPositions()
			=> Children
				.SelectToList(c =>
					new ViewPosition(c, new GridPosition(Grid.GetColumn(c), Grid.GetRow(c), Grid.GetColumnSpan(c), Grid.GetRowSpan(c)))
				);

		private struct Column
		{
			public GridSize Width { get; private set; }
			public Column(GridSize width)
			{
				Width = width;
			}

			public static Column Auto { get; } = new Column(GridSize.Auto);
		}

		private struct Row
		{
			public GridSize Height { get; private set; }
			public Row(GridSize height)
			{
				Height = height;
			}

			public static Row Auto { get; } = new Row(GridSize.Auto);
		}

		private struct GridSize
		{
			public static GridSize Auto { get { return new GridSize() { PixelSize = double.NaN }; } }
			public static GridSize Star(double coeficient = 1f)
			{
				return new GridSize()
				{
					StarSize = coeficient
				};
			}
			public static GridSize Pixel(double coeficient)
			{
				return new GridSize()
				{
					PixelSize = coeficient
				};
			}

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

			public double? PixelSize;
			public double? StarSize;

			public bool IsAuto { get { return double.IsNaN(PixelSize.GetValueOrDefault()); } }
			public bool IsStarSize { get { return StarSize.HasValue; } }
			public bool IsPixelSize { get { return !IsAuto && !IsStarSize; } }
		}

		private struct DoubleRange
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

			public double MinValue { get; private set; }
			public double MaxValue { get; private set; }
		}
	}

	class GridPosition
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
}
