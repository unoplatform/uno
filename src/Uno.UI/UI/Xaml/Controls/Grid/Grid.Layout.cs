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
using Uno.Collections;
using Microsoft.Extensions.Logging;

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

		private Memory<DoubleRange> _calculatedRows;
		private Memory<DoubleRange> _calculatedColumns;

		private Memory<Column> GetColumns(bool considerStarAsAuto)
		{
			if (ColumnDefinitions.InnerList.Count == 0)
			{
				var sizes = (HorizontalAlignment == HorizontalAlignment.Stretch || !double.IsNaN(Width)) ?
					__singleStarSize :
					__singleAutoSize;

				return sizes.SelectToMemory(size => CreateInternalColumn(considerStarAsAuto, size, minWidth: 0, maxWidth: double.PositiveInfinity));
			}
			else
			{
				return ColumnDefinitions.InnerList
					.SelectToMemory(cd => CreateInternalColumn(considerStarAsAuto, GridSize.FromGridLength(cd.Width), cd.MinWidth, cd.MaxWidth));
			}
		}

		private static Column CreateInternalColumn(bool considerStarAsAuto, GridSize size, double minWidth, double maxWidth)
		{
			if (considerStarAsAuto && size.IsStarSize)
			{
				return new Column(GridSize.Auto, minWidth, maxWidth);
			}

			return new Column(size, minWidth, maxWidth);
		}

		private Memory<Row> GetRows(bool considerStarAsAuto)
		{
			if (RowDefinitions.InnerList.Count == 0)
			{
				var sizes = (VerticalAlignment == VerticalAlignment.Stretch || !double.IsNaN(Height)) ?
					__singleStarSize :
					__singleAutoSize;

				return sizes.SelectToMemory(s => CreateInternalRow(considerStarAsAuto, s, minHeight: 0, maxHeight: double.PositiveInfinity));
			}
			else
			{
				return RowDefinitions.InnerList
					.SelectToMemory(rd => CreateInternalRow(considerStarAsAuto, GridSize.FromGridLength(rd.Height), rd.MinHeight, rd.MaxHeight));
			}
		}

		private static Row CreateInternalRow(bool considerStarAsAuto, GridSize size, double minHeight, double maxHeight)
		{
			if (considerStarAsAuto && size.IsStarSize)
			{
				return new Row(GridSize.Auto, minHeight, maxHeight);
			}

			return new Row(size, minHeight, maxHeight);
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
			if (this.Children.Count == 0)
			{
				return default(Size);
			}

			var borderAndPaddingSize = BorderAndPaddingSize;
			availableSize = availableSize.Subtract(borderAndPaddingSize);

			var definedColumns = GetColumns(false).Span;
			var definedRows = GetRows(false).Span;

			var spacingSize = new Size(ColumnSpacing * (definedColumns.Length - 1), RowSpacing * (definedRows.Length - 1));
			availableSize.Subtract(spacingSize);

			var considerStarColumnsAsAuto = ConsiderStarColumnsAsAuto(availableSize.Width);
			var considerStarRowsAsAuto = ConsiderStarRowsAsAuto(availableSize.Height);

			var columns = GetColumns(considerStarColumnsAsAuto).Span;
			var rows = GetRows(considerStarRowsAsAuto).Span;

			Size size;
			if (!TryMeasureUsingFastPath(availableSize, columns, definedColumns, rows, definedRows, out size))
			{
				var measureChild = GetMemoizedMeasureChild();
				var positions = GetPositions(columns.Length, rows.Length);

				using (positions.Subscription)
				{
					// Columns
					_calculatedColumns = CalculateColumns(
						availableSize,
						positions.Views.Span,
						measureChild,
						columns,
						definedColumns,
						true,
						out var _
					);

					// Rows (we need to fully calculate the rows to allow text wrapping)
					_calculatedRows = CalculateRows(
						availableSize,
						positions.Views.Span,
						measureChild,
						_calculatedColumns.Span,
						rows,
						definedRows,
						true,
						out var maxMeasuredWidth
					);

					size = new Size(_calculatedColumns.Span.Sum(cs => cs.MinValue), _calculatedRows.Span.Sum(cs => cs.MinValue));
				}
			}
			return size.Add(borderAndPaddingSize)
				.Add(spacingSize);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			if (this.Children.Count == 0)
			{
				return finalSize;
			}

			var borderAndPaddingSize = BorderAndPaddingSize;

			var definedColumns = GetColumns(false).Span;
			var definedRows = GetRows(false).Span;

			var spacingSize = new Size(ColumnSpacing * (definedColumns.Length - 1), RowSpacing * (definedRows.Length - 1));

			var availableSize = finalSize
				.Subtract(borderAndPaddingSize)
				.Subtract(spacingSize);

			var considerStarColumnsAsAuto = ConsiderStarColumnsAsAuto(availableSize.Width);
			var considerStarRowsAsAuto = ConsiderStarRowsAsAuto(availableSize.Height);

			var columns = GetColumns(considerStarColumnsAsAuto).Span;
			var rows = GetRows(considerStarRowsAsAuto).Span;

			var positions = GetPositions(columns.Length, rows.Length);

			using (positions.Subscription)
			{

				if (!TryArrangeUsingFastPath(availableSize, columns, definedColumns, rows, definedRows, positions.Views.Span))
				{
					var measureChild = GetMemoizedMeasureChild();

					// Columns
					double maxMeasuredHeight; //ignored here
					_calculatedColumns = CalculateColumns(
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
					_calculatedRows = CalculateRows(
						availableSize,
						positions.Views.Span,
						measureChild,
						_calculatedColumns.Span,
						rows,
						definedRows,
						false,
						out maxMeasuredWidth
					);

					LayoutChildren(_calculatedColumns.Span, _calculatedRows.Span, positions.Views.Span);
				}

				return finalSize;
			}
		}

		private void LayoutChildren(Span<DoubleRange> calculatedPixelColumns, Span<DoubleRange> calculatedPixelRows, Span<ViewPosition> positions)
		{
			var childrenToPositionsMap = positions.ToDictionary(pair => pair.Key, pair => pair.Value);

			var rowSpacing = RowSpacing;
			var columnSpacing = ColumnSpacing;

			// Layout the children
			var offset = GetChildrenOffset();
			foreach (var child in Children)
			{
				var gridPosition = childrenToPositionsMap[child];
				var x = offset.X + calculatedPixelColumns.SliceClamped(0, gridPosition.Column).Sum(cs => cs.MinValue);
				var y = offset.Y + calculatedPixelRows.SliceClamped(0, gridPosition.Row).Sum(cs => cs.MinValue);

				x += columnSpacing * gridPosition.Column;
				y += rowSpacing * gridPosition.Row;

				Span<double> calculatedPixelColumnsMinValue = stackalloc double[calculatedPixelColumns.Length];
				calculatedPixelColumns.SelectToSpan(calculatedPixelColumnsMinValue, cs => cs.MinValue);

				Span<double> calculatedPixelRowsMinValue = stackalloc double[calculatedPixelRows.Length];
				calculatedPixelRows.SelectToSpan(calculatedPixelRowsMinValue, cs => cs.MinValue);

				var width = GetSpanSum(gridPosition.Column, gridPosition.ColumnSpan, calculatedPixelColumnsMinValue);
				var height = GetSpanSum(gridPosition.Row, gridPosition.RowSpan, calculatedPixelRowsMinValue);

				width += GetAdjustmentForSpacing(gridPosition.ColumnSpan, columnSpacing);
				height += GetAdjustmentForSpacing(gridPosition.RowSpan, rowSpacing);

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
			var borderAndPaddingSize = BorderAndPaddingSize;

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
					minHeight = MinHeight - borderAndPaddingSize.Height;
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

			var availableWidth = availableSize.Width;

			//If MinWidth property is set on the Grid that is not stretching horizontally we need to take it into account in order to match UWP behavior
			if (MinWidth != 0 && HorizontalAlignment != HorizontalAlignment.Stretch)
			{
				availableWidth = MinWidth - borderAndPaddingSize.Width;
			}

			double remainingSpace(int current, Span<double> pixelColumns) => availableWidth - pixelColumns
				.WhereToMemory((_, i) => i != current)
				.Span
				.Sum();

			// Pixel size: We only measure these children to set their desired size. 
			// It is not actually required for the columns calculation (because it's constant).
			if (isMeasuring)
			{
				foreach (var pixelChild in pixelSizeChildrenX.Span)
				{
					var availablePixelWidth = GetPixelSize(pixelChild.Value.Column, pixelChild.Value.ColumnSpan, allWidths.Span);
					if (pixelChild.Value.ColumnSpan > 1)
					{
						availablePixelWidth += GetAdjustmentForSpacing(pixelChild.Value.ColumnSpan, ColumnSpacing);
					}
					var size = measureChild(pixelChild.Key, new Size(availablePixelWidth, availableSize.Height));
					maxHeightMeasured = Math.Max(maxHeightMeasured, size.Height);
				}
			}

			// Auto size: This type of measure is always required. 
			// It's the type of size that depends directly on the size of its content.
			foreach (var autoChild in autoSizeChildrenX)
			{
				var gridPosition = autoChild.Value;

				var childAvailableWidth = availableWidth - GetAvailableSizeForPosition(calculatedPixelColumns, gridPosition);

				if (gridPosition.ColumnSpan > 1)
				{
					childAvailableWidth += GetAdjustmentForSpacing(gridPosition.ColumnSpan, ColumnSpacing);
				}

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

			// Auto columns should take up the larger of their measured width and their MinWidth
			for (int i = 0; i < columns.Length; i++)
			{
				if (columns[i].Width.IsAuto)
				{
					calculatedPixelColumns.Span[i] = Math.Max(calculatedPixelColumns.Span[i], columns[i].MinWidth);
				}
			}

			// Star size: We always measure to set the desired size, but measuring would only be required for the columns calculation when the Star doesn't mean remaining space.
			var usedWidth = calculatedPixelColumns.Span.Sum();
			var initialRemainingWidth = Math.Max(0, availableWidth - usedWidth);
			var initialTotalStarSizedWidth = GetTotalStarSizedWidth(columns);

			var remainingWidth = initialRemainingWidth;
			var totalStarSizedWidth = initialTotalStarSizedWidth;
			var unitStarWidth = totalStarSizedWidth != 0 ? remainingWidth / totalStarSizedWidth : 0;

			// We want to run at least one iteration. We don't expect more iterations than the number of star children (but allow margin for programmer error).
			var maxTries = starSizeChildren.Span.Length * 2 + 1;
			var previousRemainingWidth = remainingWidth;

			/// Try to set the star column widths. If there are no MinWidths or MaxWidths in effect, this should only take a single pass.
			/// 
			/// If the actual width consumed is less than was available, this means that at least one column MinWidth kicked in. Conversely,
			/// if it's greater than what was available, a MaxWidth must have kicked in.
			/// 
			/// In either case, we exclude the constrained columns and recalculate for the remaining 'true' star-sized columns. The new unitStarWidth may cause
			/// more Min/MaxWidths to kick in, so we repeat until the values stabilize.
			Memory<double> starCalculatedPixelColumns = null;
			for (int i = 0; i <= maxTries; i++)
			{
				var maxWidthColumnsWidth = 0d;
				var minWidthColumnsWidth = 0d;
				var maxWidthColumnsStarWidth = 0d;
				var minWidthColumnsStarWidth = 0d;

				starCalculatedPixelColumns = columns
					.SelectToMemory(
						(c, j) =>
						{
							if (!c.Width.IsStarSize)
							{
								return calculatedPixelColumns.Span[j];
							}

							var starSize = c.Width.StarSize.GetValueOrDefault();
							var baseWidth = starSize * unitStarWidth;
							var width = MathEx.Clamp(baseWidth, c.MinWidth, c.MaxWidth);

							if (width > baseWidth)
							{
								minWidthColumnsWidth += width;
								minWidthColumnsStarWidth += starSize;
							}
							else if (width < baseWidth)
							{
								maxWidthColumnsWidth += width;
								maxWidthColumnsStarWidth += starSize;
							}

							remainingWidth -= width;

							return width;
						}
					);

				if (MathEx.ApproxEqual(remainingWidth, 0) || //Exactly the remaining width has been used, we're done
					MathEx.ApproxEqual(remainingWidth, previousRemainingWidth) //Adding/removing width hasn't made any difference, we're done. (Perhaps all columns are constrained)
				)
				{
					break;
				}
				else
				{
					double totalWidth;
					if (remainingWidth > 0)
					{
						//On net we used less width than was available, ie MaxWidths kicked in. We need to calculate the star width with those columns excluded.
						totalWidth = initialRemainingWidth - maxWidthColumnsWidth;
						totalStarSizedWidth = initialTotalStarSizedWidth - maxWidthColumnsStarWidth;
					}
					else
					{
						//On net we used more width than was available, ie MinWidths kicked in. We need to calculate the star width with those columns excluded.
						totalWidth = initialRemainingWidth - minWidthColumnsWidth;
						totalStarSizedWidth = initialTotalStarSizedWidth - minWidthColumnsStarWidth;
					}

					if (totalStarSizedWidth != 0)
					{
						unitStarWidth = totalWidth / totalStarSizedWidth;
					}

					previousRemainingWidth = remainingWidth;
					remainingWidth = initialRemainingWidth;
				}

				if (i == maxTries && maxTries > 1)
				{
					if (this.Log().IsEnabled(LogLevel.Warning))
					{
						this.Log().LogWarning($"Star measurements failed to stabilize after {i} tries.");
					}
#if NET461
					throw new InvalidOperationException($"Star measurements failed to stabilize after {i} tries.");
#endif
				}
			}

			if (isMeasuring)
			{
				var maxStarWidth = 0.0;

				foreach (var starChild in starSizeChildren.Span)
				{
					var availableStarWidth = GetSpanSum(starChild.Value.Column, starChild.Value.ColumnSpan, starCalculatedPixelColumns.Span);
					if (starChild.Value.ColumnSpan > 1)
					{
						availableStarWidth += GetAdjustmentForSpacing(starChild.Value.ColumnSpan, ColumnSpacing);
					}
					var size = measureChild(starChild.Key, new Size(availableStarWidth, availableSize.Height));
					maxHeightMeasured = Math.Max(maxHeightMeasured, size.Height);

					var starWidth = size.Width;
					var sizes = allWidths.Span.SliceClamped(starChild.Value.Column, starChild.Value.ColumnSpan);

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

				maxStarWidth = Math.Min(unitStarWidth, maxStarWidth);

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
			var borderAndPaddingSize = BorderAndPaddingSize;

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
					minWidth = MinWidth - borderAndPaddingSize.Width;
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

			var availableHeight = availableSize.Height;

			//If MinHeight property is set on the Grid that is not stretching vertically we need to take it into account in order to match UWP behavior
			if (MinHeight != 0 && VerticalAlignment != VerticalAlignment.Stretch)
			{
				availableHeight = MinHeight - borderAndPaddingSize.Height;
			}

			double remainingSpace(int current, Span<double> pixelRows) => availableHeight - pixelRows
				.WhereToMemory((_, i) => i != current)
				.Span
				.Sum();

			if (isMeasuring)
			{
				foreach (var pixelChild in pixelSizeChildrenY.Span)
				{
					var availablePixelHeight = GetPixelSize(pixelChild.Value.Row, pixelChild.Value.RowSpan, allHeights.Span);
					if (pixelChild.Value.RowSpan > 1)
					{
						availablePixelHeight += GetAdjustmentForSpacing(pixelChild.Value.RowSpan, RowSpacing);
					}
					var size = measureChild(pixelChild.Key, new Size(
						GetSpanSum(pixelChild.Value.Column, pixelChild.Value.ColumnSpan, calculatedColumns.SelectToMemory(cs => cs.MaxValue).Span),
						availablePixelHeight
					));
					maxMeasuredWidth = Math.Max(maxMeasuredWidth, size.Width);
				}
			}

			foreach (var autoChild in autoSizeChildrenY)
			{
				var gridPosition = autoChild.Value;
				var width = GetSpanSum(gridPosition.Column, gridPosition.ColumnSpan, calculatedColumns.SelectToMemory(cs => cs.MaxValue).Span);

				var childAvailableHeight = availableHeight - GetAvailableSizeForPosition(calculatedPixelRows, gridPosition);

				if (gridPosition.RowSpan > 1)
				{
					childAvailableHeight += GetAdjustmentForSpacing(gridPosition.RowSpan, RowSpacing);
				}

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

			// Auto rows should take up the larger of their measured height and their MinHeight
			for (int i = 0; i < rows.Length; i++)
			{
				if (rows[i].Height.IsAuto)
				{
					calculatedPixelRows.Span[i] = Math.Max(calculatedPixelRows.Span[i], rows[i].MinHeight);
				}
			}

			// Star size: We always measure to set the desired size, but measuring would only be required for the rows calculation when the Star doesn't mean remaining space.
			var usedHeight = calculatedPixelRows.Span.Sum();
			var initialRemainingHeight = Math.Max(0, availableHeight - usedHeight);
			var initialTotalStarSizedHeight = GetTotalStarSizedHeight(rows);

			var remainingHeight = initialRemainingHeight;
			var totalStarSizedHeight = initialTotalStarSizedHeight;
			var unitStarHeight = remainingHeight / totalStarSizedHeight;

			// We want to run at least one iteration. We don't expect more iterations than the number of star children (but allow margin for programmer error).
			var maxTries = starSizeChildren.Span.Length * 2 + 1;
			var previousRemainingHeight = remainingHeight;

			/// Try to set the star row heights. If there are no MinHeights or MaxHeights in effect, this should only take a single pass.
			/// 
			/// If the actual height consumed is less than was available, this means that at least one row MinHeight kicked in. Conversely,
			/// if it's greater than what was available, a MaxHeight must have kicked in.
			/// 
			/// In either case, we exclude the constrained rows and recalculate for the remaining 'true' star-sized rows. The new unitStarHeight may cause
			/// more Min/MaxHeights to kick in, so we repeat until the values stabilize.
			Memory<double> starCalculatedPixelRows = null;
			for (int i = 0; i <= maxTries; i++)
			{
				var maxHeightRowsHeight = 0d;
				var minHeightRowsHeight = 0d;
				var maxHeightRowsStarHeight = 0d;
				var minHeightRowsStarHeight = 0d;

				starCalculatedPixelRows = rows
					.SelectToMemory(
						(c, j) =>
						{
							if (!c.Height.IsStarSize)
							{
								return calculatedPixelRows.Span[j];
							}

							var starSize = c.Height.StarSize.GetValueOrDefault();
							var baseHeight = starSize * unitStarHeight;
							var height = MathEx.Clamp(baseHeight, c.MinHeight, c.MaxHeight);

							if (height > baseHeight)
							{
								minHeightRowsHeight += height;
								minHeightRowsStarHeight += starSize;
							}
							else if (height < baseHeight)
							{
								maxHeightRowsHeight += height;
								maxHeightRowsStarHeight += starSize;
							}

							remainingHeight -= height;

							return height;
						}
					);

				if (MathEx.ApproxEqual(remainingHeight, 0) || //Exactly the remaining height has been used, we're done
					MathEx.ApproxEqual(remainingHeight, previousRemainingHeight) //Adding/removing height hasn't made any difference, we're done. (Perhaps all rows are constrained)
				)
				{
					break;
				}
				else
				{
					double totalHeight;
					if (remainingHeight > 0)
					{
						//On net we used less height than was available, ie MaxHeights kicked in. We need to calculate the star height with those rows excluded.
						totalHeight = initialRemainingHeight - maxHeightRowsHeight;
						totalStarSizedHeight = initialTotalStarSizedHeight - maxHeightRowsStarHeight;
					}
					else
					{
						//On net we used more height than was available, ie MinHeights kicked in. We need to calculate the star height with those rows excluded.
						totalHeight = initialRemainingHeight - minHeightRowsHeight;
						totalStarSizedHeight = initialTotalStarSizedHeight - minHeightRowsStarHeight;
					}

					if (totalStarSizedHeight != 0)
					{
						unitStarHeight = totalHeight / totalStarSizedHeight;
					}

					previousRemainingHeight = remainingHeight;
					remainingHeight = initialRemainingHeight;
				}

				if (i == maxTries && maxTries > 1)
				{
					if (this.Log().IsEnabled(LogLevel.Warning))
					{
						this.Log().LogWarning($"Star measurements failed to stabilize after {i} tries.");
					}
#if NET461
					throw new InvalidOperationException($"Star measurements failed to stabilize after {i} tries.");
#endif
				}
			}

			if (isMeasuring)
			{
				var maxStarHeight = 0.0;

				foreach (var starChild in starSizeChildren.Span)
				{
					var availableStarHeight = GetSpanSum(starChild.Value.Row, starChild.Value.RowSpan, starCalculatedPixelRows.Span);
					if (starChild.Value.RowSpan > 1)
					{
						availableStarHeight += GetAdjustmentForSpacing(starChild.Value.RowSpan, RowSpacing);
					}
					var size = measureChild(starChild.Key, new Size(
						GetSpanSum(starChild.Value.Column, starChild.Value.ColumnSpan, calculatedColumns.SelectToMemory(cs => cs.MaxValue).Span),
						availableStarHeight
					));
					maxMeasuredWidth = Math.Max(maxMeasuredWidth, size.Width);

					var starHeight = size.Height;
					var sizes = allHeights.Span.SliceClamped(starChild.Value.Row, starChild.Value.RowSpan);

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

				maxStarHeight = Math.Min(unitStarHeight, maxStarHeight);

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
			var slice = calculatedPixel.Span.SliceClamped(gridPosition.Row, gridPosition.RowSpan);
			double result = 0;

			for (int i = 0; i < calculatedPixel.Span.Length; i++)
			{
				var value = calculatedPixel.Span[i];

				if (!slice.Any(item => item == value))
				{
					result += value;
				}
			}

			return result;
		}

		/// <summary>
		/// Gets adjustment to available child size if it extends across multiple rows (columns) and Row(Column)Spacing is non-zero. (Intuitively,
		/// the child straddles the intermediate spacing.)
		/// </summary>
		private static double GetAdjustmentForSpacing(int rowOrColumnSpan, double spacing) => spacing * (rowOrColumnSpan - 1);

		private static Memory<ViewPosition> FindStarSizeChildren(Span<ViewPosition> positions, Memory<ViewPosition> pixelSizeChildren, List<ViewPosition> autoSizeChildren)
		{
			var res = new Memory<ViewPosition>(new ViewPosition[positions.Length]);
			int count = 0;

			for (int i = 0; i < positions.Length; i++)
			{
				var item = positions[i];

				if (
					!pixelSizeChildren.Span.Any(c => c.Key == item.Key)
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
			var cached = sizes.SliceClamped(index, span);

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
			return sizes.SliceClamped(index, span).Count(s => s.IsAuto);
		}

		/// <summary>
		/// Contains Star returns 1, Doesn't contain star 0
		/// </summary>
		private static int StarSizeComparer(int index, int span, Span<GridSize> sizes)
		{
			return Math.Min(1, sizes.SliceClamped(index, span).Count(s => s.IsStarSize));
		}

		/// <summary>
		/// True if all rows/columns for the provided starting point + row/columnSpan have pixel definitions, false if any are star or auto.
		/// </summary>
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
			var viewToPreviousMeasure = new Dictionary<View, (Size available, Size measured)>();

			return Funcs.CreateMemoized<View, Size, Size>((view, availableSize) =>
			{
				(Size available, Size measured) previousMeasure;
				if (viewToPreviousMeasure.TryGetValue(view, out previousMeasure))
				{
					// if the previous available size is bigger  than the current available size and that
					//    the previous measured  size is smaller that the current available size
					if (previousMeasure.available.Width >= availableSize.Width &&
						previousMeasure.available.Height >= availableSize.Height &&
						previousMeasure.measured.Width <= availableSize.Width &&
						previousMeasure.measured.Height <= availableSize.Height)
					{
						return previousMeasure.measured;
					}
				}

				var m = MeasureElement(view, availableSize);
				viewToPreviousMeasure[view] = (availableSize, m);
				return m;
			});
		}

		/// <summary>
		/// This should only be used by fast paths that don't need memoization
		/// </summary>
		private Func<View, Size, Size> GetDirectMeasureChild()
		{
			return MeasureElement;
		}

		internal double GetActualWidth(ColumnDefinition columnDefinition)
		{
			var i = ColumnDefinitions.IndexOf(columnDefinition);
			if (i < 0 || i >= _calculatedColumns.Length)
			{
				return 0d;
			}

			return _calculatedColumns.Span[i].MinValue;
		}

		internal double GetActualHeight(RowDefinition rowDefinition)
		{
			var i = RowDefinitions.IndexOf(rowDefinition);
			if (i < 0 || i >= _calculatedRows.Length)
			{
				return 0d;
			}

			return _calculatedRows.Span[i].MinValue;
		}

		private (IDisposable Subscription, Memory<ViewPosition> Views) GetPositions(int numberOfColumns, int numberOfRows)
		{
			var refs = Children.SelectToArray(c => (View: c, Handle: GCHandle.Alloc(c, GCHandleType.Normal)));

			return (
				Disposable.Create(() => refs.ForEach(c => c.Handle.Free())),
				refs
					.SelectToMemory(c =>
					{
						return MapViewToGridPosition(c);
					})
			);

			ViewPosition MapViewToGridPosition((View View, GCHandle Handle) c)
			{
				var column = Grid.GetColumn(c.View);
				var columnSpan = Grid.GetColumnSpan(c.View);

				if (column == 0)
				{
					// Ok: nothing to check
				}
				else if (column < 0)
				{
					column = 0;
				}
				else if (column >= numberOfColumns)
				{
					column = numberOfColumns - 1;
				}

				if (columnSpan == 1)
				{
					// Ok: nothing to check
				}
				else if (columnSpan < 1)
				{
					columnSpan = 1;
				}
				else if (column + columnSpan > numberOfColumns)
				{
					columnSpan = numberOfColumns - column;
				}

				var row = Grid.GetRow(c.View);
				var rowSpan = Grid.GetRowSpan(c.View);

				if (row == 0)
				{
					// Ok: nothing to check
				}
				else if (row < 0)
				{
					row = 0;
				}
				else if (row >= numberOfRows)
				{
					row = numberOfRows - 1;
				}

				if (rowSpan == 1)
				{
					// Ok: nothing to check
				}
				else if (rowSpan < 1)
				{
					rowSpan = 1;
				}
				else if (row + rowSpan > numberOfRows)
				{
					rowSpan = numberOfRows - row;
				}

				return new ViewPosition(
					c.Handle,
					new GridPosition(
						column,
						row,
						columnSpan,
						rowSpan
					)
				);
			}
		}

		readonly struct Column
		{
			public GridSize Width { get; }
			public double MinWidth { get; }
			public double MaxWidth { get; }
			public Column(GridSize width, double minWidth, double maxWidth)
			{
				Width = width;
				MinWidth = minWidth;
				MaxWidth = maxWidth;

			}
		}

		readonly struct Row
		{
			public GridSize Height { get; }
			public double MinHeight { get; }
			public double MaxHeight { get; }
			public Row(GridSize height, double minHeight, double maxHeight)
			{
				Height = height;
				MinHeight = minHeight;
				MaxHeight = maxHeight;
			}
		}

		private readonly struct GridSize
		{
			private readonly bool _hasPixelSize;
			private readonly double _pixelSize;

			private readonly bool _hasStarSize;
			private readonly double _starSize;

			public static GridSize Auto => new GridSize(pixelSize: double.NaN);
			public static GridSize Star(double coefficient = 1f) => new GridSize(starSize: coefficient);
			public static GridSize Pixel(double coefficient) => new GridSize(pixelSize: coefficient);

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

	internal struct GridPosition
	{
		public int Column { get; }
		public int Row { get; }

		public int ColumnSpan { get; }
		public int RowSpan { get; }

		internal GridPosition(int column, int row, int columnSpan, int rowSpan)
		{
			Column = column;
			Row = row;

			ColumnSpan = columnSpan;
			RowSpan = rowSpan;
		}

		public override string ToString()
		{
			return "Column={0}, Row={1}, ColumnSpan={2}, RowSpan={3}".InvariantCultureFormat(Column, Row, ColumnSpan, RowSpan);
		}
	}
}
