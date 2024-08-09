using System;
using Windows.Foundation;
using Windows.UI.Xaml;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls.Primitives
{
	public partial class ColumnMajorUniformToLargestGridLayout : NonVirtualizingLayout
	{
		private int m_actualColumnCount = 1;
		private Size m_largestChildSize = Size.Empty;

		//Testhooks helpers, only function while m_testHooksEnabled == true
		private bool m_testHooksEnabled = false;

		private int m_rows = -1;

		private int m_columns = -1;

		private int m_largerColumns = -1;

		protected internal override Size MeasureOverride(NonVirtualizingLayoutContext context, Size availableSize)
		{
			var children = context.Children;
			if (children != null)
			{
				Size GetLargestChildSize()
				{
					var largestChildWidth = 0.0;
					var largestChildHeight = 0.0;
					foreach (var child in children)
					{
						child.Measure(availableSize);
						var desiredSize = child.DesiredSize;
						if (desiredSize.Width > largestChildWidth)
						{
							largestChildWidth = desiredSize.Width;
						}
						if (desiredSize.Height > largestChildHeight)
						{
							largestChildHeight = desiredSize.Height;
						}
					}
					return new Size(largestChildWidth, largestChildHeight);
				}

				m_largestChildSize = GetLargestChildSize();

				m_actualColumnCount = CalculateColumns(children.Count, m_largestChildSize.Width, availableSize.Width);
				var maxItemsPerColumn = (int)(Math.Ceiling((double)(children.Count) / (double)(m_actualColumnCount)));
				return new Size(
					(m_largestChildSize.Width * m_actualColumnCount) +
					((float)(ColumnSpacing) * (m_actualColumnCount - 1)),
					(m_largestChildSize.Height * maxItemsPerColumn) +
					((float)(RowSpacing) * (maxItemsPerColumn - 1))
				);
			}
			return new Size(0, 0);
		}

		protected internal override Size ArrangeOverride(NonVirtualizingLayoutContext context, Size finalSize)
		{
			var children = context.Children;
			if (children != null)
			{
				var itemCount = children.Count;
				var minitemsPerColumn = (int)(Math.Floor((float)(itemCount) / m_actualColumnCount));
				var numberOfColumnsWithExtraElements = (int)(itemCount % (int)(m_actualColumnCount));

				var columnSpacing = (float)(ColumnSpacing);
				var rowSpacing = (float)(RowSpacing);

				var horizontalOffset = 0.0;
				var verticalOffset = 0.0;
				var index = 0;
				var column = 0;
				foreach (var child in children)
				{
					var desiredSize = child.DesiredSize;
					child.Arrange(new Rect(horizontalOffset, verticalOffset, desiredSize.Width, desiredSize.Height));
					if (column < numberOfColumnsWithExtraElements)
					{
						if (index % (minitemsPerColumn + 1) == minitemsPerColumn)
						{
							horizontalOffset += m_largestChildSize.Width + columnSpacing;
							verticalOffset = 0.0;
							column++;
						}
						else
						{
							verticalOffset += m_largestChildSize.Height + rowSpacing;
						}
					}
					else
					{
						var indexAfterExtraLargeColumns = index - (numberOfColumnsWithExtraElements * (minitemsPerColumn + 1));
						if (indexAfterExtraLargeColumns % minitemsPerColumn == minitemsPerColumn - 1)
						{
							horizontalOffset += m_largestChildSize.Width + columnSpacing;
							verticalOffset = 0.0;
							column++;
						}
						else
						{
							verticalOffset += m_largestChildSize.Height + rowSpacing;
						}
					}
					index++;
				}

				if (m_testHooksEnabled)
				{
					//Testhooks setup
					if (m_largerColumns != numberOfColumnsWithExtraElements ||
						m_columns != column ||
						m_rows != minitemsPerColumn)
					{
						m_largerColumns = numberOfColumnsWithExtraElements;
						m_columns = column;
						m_rows = minitemsPerColumn;

						LayoutChanged?.Invoke(this, null);
					}
				}
			}
			return finalSize;
		}

		void OnColumnSpacingPropertyChanged(DependencyPropertyChangedEventArgs args)
		{
			InvalidateMeasure();
		}

		void OnRowSpacingPropertyChanged(DependencyPropertyChangedEventArgs args)
		{
			InvalidateMeasure();
		}

		void OnMaxColumnsPropertyChanged(DependencyPropertyChangedEventArgs args)
		{
			InvalidateMeasure();
		}

		int CalculateColumns(int childCount, double maxItemWidth, double availableWidth)
		{
			/*
			--------------------------------------------------------------
			|      |-----------|-----------| | widthNeededForExtraColumn |
			|                                |                           |
			|      |------|    |------|      | ColumnSpacing             |
			| |----|      |----|      |----| | maxItemWidth              |
			|  O RB        O RB        O RB  |                           |
			--------------------------------------------------------------
			*/

			// Every column execpt the first takes this ammount of space to fit on screen.
			var widthNeededForExtraColumn = ColumnSpacing + maxItemWidth;
			// The number of columns from data and api ignoring available space
			var requestedColumnCount = Math.Min(MaxColumns, childCount);

			// If columns can be added with effectively 0 extra space return as many columns as needed.
			if (widthNeededForExtraColumn < float.Epsilon)
			{
				return requestedColumnCount;
			}

			var extraWidthAfterFirstColumn = availableWidth - maxItemWidth;
			var maxExtraColumns = Math.Max(0.0, Math.Floor(extraWidthAfterFirstColumn / widthNeededForExtraColumn));

			// The smaller of number of columns from data and api and
			// the number of columns the available space can support
			var effectiveColumnCount = Math.Min((double)(requestedColumnCount), maxExtraColumns + 1);
			// return 1 even if there isn't any data
			return Math.Max(1, (int)(effectiveColumnCount));
		}

		private void ValidateGreaterThanZero(int value)
		{
			if (value <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(value));
			}
		}

		//Testhooks helpers, only function while m_testHooksEnabled == true
		internal void SetTestHooksEnabled(bool enabled)
		{
			m_testHooksEnabled = enabled;
		}

		internal int GetRows()
		{
			return m_rows;
		}

		internal int GetColumns()
		{
			return m_columns;
		}

		internal int GetLargerColumns()
		{
			return m_largerColumns;
		}

		internal event TypedEventHandler<ColumnMajorUniformToLargestGridLayout, object> LayoutChanged;
	}
}
