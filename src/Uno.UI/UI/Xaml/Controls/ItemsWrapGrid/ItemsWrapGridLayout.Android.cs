using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using AndroidX.RecyclerView.Widget;
using Android.Views;
using Uno.Extensions;
using Uno.UI;
using Windows.UI.Xaml.Controls.Primitives;
using Uno.Foundation.Logging;

using Windows.Foundation;
using Uno.UI.Extensions;

using Size = Windows.Foundation.Size;

namespace Windows.UI.Xaml.Controls
{
	/// <summary>
	/// A native layout which implements <see cref="ItemsWrapGrid"/> behaviour.
	/// </summary>
	internal partial class ItemsWrapGridLayout : VirtualizingPanelLayout
	{
		//These properties are set to the dimensions of the first materialised item, and used if ItemWidth/ItemHeight are set to auto
		private int? _implicitItemWidth;
		private int? _implicitItemHeight;

		private protected override Line CreateLine(GeneratorDirection direction,
			int extentOffset,
			int breadthOffset,
			int availableBreadth,
			RecyclerView.Recycler recycler,
			RecyclerView.State state,
			Uno.UI.IndexPath nextVisibleItem,
			bool isNewGroup
		)
		{
			var itemsInLine = ResolveMaximumItemsInLine(availableBreadth);
			var firstItemInLine = nextVisibleItem;

			//Find first item in line, since the item we are passed is the last
			if (direction == GeneratorDirection.Backward)
			{
				// We are recreating the last line of the group - it may be truncated (if the total items are not an even multiple
				// of the items-per-line).
				if (isNewGroup)
				{
					itemsInLine = XamlParent.GetItemsOnLastLine(firstItemInLine.Section, itemsInLine);
				}
				for (int i = 0; i < itemsInLine - 1; i++)
				{
					firstItemInLine = GetNextUnmaterializedItem(GeneratorDirection.Backward, firstItemInLine).Value;
					var isCorrectGroup = firstItemInLine.Section == nextVisibleItem.Section;
					if (!isCorrectGroup)
					{
						//TODO: fix bug that makes this happen (#47229)
					}
					Debug.Assert(isCorrectGroup, GetAssertMessage("First item should not be from a different group"));
				}
			}
			Uno.UI.IndexPath lastItemInLine = firstItemInLine;

			Uno.UI.IndexPath? currentItem = firstItemInLine;
			var availableWidth = ResolveAvailableWidth(availableBreadth);
			var availableHeight = ResolveAvailableHeight(availableBreadth);

			int usedBreadth = 0;
			for (int i = 0; i < itemsInLine; i++)
			{
				var view = recycler.GetViewForPosition(GetFlatItemIndex(currentItem.Value), state);

				if (!(view is SelectorItem))
				{
					throw new InvalidOperationException($"Expected {nameof(SelectorItem)} but received {view?.GetType().ToString() ?? "<null>"}");
				}

				//Add view before we measure it, this ensures that DP inheritances are correctly applied
				AddView(view, direction);

				var slotSize = new Size(availableWidth, availableHeight).PhysicalToLogicalPixels();
				var measuredSize = _layouter.MeasureChild(view, slotSize);
				var physicalMeasuredSize = measuredSize.LogicalToPhysicalPixels();
				var measuredWidth = (int)physicalMeasuredSize.Width;
				var measuredHeight = (int)physicalMeasuredSize.Height;

				if (_implicitItemWidth == null)
				{
					//Set these values to dimensions of first materialised item
					_implicitItemWidth = measuredWidth;
					_implicitItemHeight = measuredHeight;

					// When an item dimension is not fixed, we need to arrange based on the measured size,
					// otherwise the arrange will be passed a dimension that is too large and the first
					// few items will not be visible
					if (double.IsNaN(ItemWidth))
					{
						slotSize.Width = ViewHelper.PhysicalToLogicalPixels(_implicitItemWidth.Value);
					}
					if (double.IsNaN(ItemHeight))
					{
						slotSize.Height = ViewHelper.PhysicalToLogicalPixels(_implicitItemHeight.Value);
					}

					availableWidth = ResolveAvailableWidth(availableBreadth);
					availableHeight = ResolveAvailableHeight(availableBreadth);

					itemsInLine = ResolveMaximumItemsInLine(availableBreadth);
				}

				LayoutChild(view,
					GeneratorDirection.Forward,
					//We always lay out view 'top down' so that it is aligned correctly if its height is less than the line height
					direction == GeneratorDirection.Forward ? extentOffset : extentOffset - ResolveItemExtent().Value,
					breadthOffset + usedBreadth,
					slotSize
				);

				usedBreadth += ResolveItemBreadth().Value;
				lastItemInLine = currentItem.Value;

				currentItem = GetNextUnmaterializedItem(GeneratorDirection.Forward, currentItem);
				if (currentItem == null || currentItem.Value.Section != firstItemInLine.Section)
				{
					itemsInLine = i + 1;
					break;
				}
			}

			return new Line
			{
				NumberOfViews = itemsInLine,
				Extent = ResolveItemExtent().Value,
				Breadth = usedBreadth,
				FirstItem = firstItemInLine,
				LastItem = lastItemInLine
			};
		}

		protected override void ResetLayoutInfo()
		{
			base.ResetLayoutInfo();
			_implicitItemWidth = null;
			_implicitItemHeight = null;
		}

		/// <summary>
		/// Resolve the width available for a single item view, using, in decreasing order of priority, the <see cref="ItemWidth"/> if
		/// defined, the width of the first item in the grid, or the maximum available space if we are measuring the first item.
		/// </summary>
		private int ResolveAvailableWidth(int availableBreadth)
		{
			if (!double.IsNaN(ItemWidth))
			{
				return ViewHelper.LogicalToPhysicalPixels(ItemWidth);
			}

			if (_implicitItemWidth != null)
			{
				return _implicitItemWidth.Value;
			}

			if (ScrollOrientation == Orientation.Vertical)
			{
				return availableBreadth;
			}
			else
			{
				return int.MaxValue;
			}
		}

		/// <summary>
		/// Resolve the height available for a single item view, using, in decreasing order of priority, the <see cref="ItemHeight"/> if
		/// defined, the height of the first item in the grid, or the maximum available space if we are measuring the first item.
		/// </summary>

		private int ResolveAvailableHeight(int availableBreadth)
		{
			if (!double.IsNaN(ItemHeight))
			{
				return ViewHelper.LogicalToPhysicalPixels(ItemHeight);
			}

			if (_implicitItemHeight != null)
			{
				return _implicitItemHeight.Value;
			}

			if (ScrollOrientation == Orientation.Vertical)
			{
				return int.MaxValue;
			}
			else
			{
				return availableBreadth;
			}
		}

		/// <summary>
		/// Resolve the breadth of items in the grid, if it is known.
		/// </summary>
		private int? ResolveItemBreadth()
		{
			if (ScrollOrientation == Orientation.Vertical)
			{
				return ResolveItemWidth();
			}
			else
			{
				return ResolveItemHeight();
			}
		}

		/// <summary>
		/// Resolve the extent of items in the grid, if it is known.
		/// </summary>
		private int? ResolveItemExtent()
		{
			if (ScrollOrientation == Orientation.Vertical)
			{
				return ResolveItemHeight();
			}
			else
			{
				return ResolveItemWidth();
			}
		}

		/// <summary>
		/// Resolve the width of items in the grid, if it is known.
		/// </summary>
		private int? ResolveItemWidth()
		{
			if (!double.IsNaN(ItemWidth))
			{
				return ViewHelper.LogicalToPhysicalPixels(ItemWidth);
			}

			return _implicitItemWidth;
		}

		/// <summary>
		/// Resolve the height of items in the grid, if it is known.
		/// </summary>
		private int? ResolveItemHeight()
		{
			if (!double.IsNaN(ItemHeight))
			{
				return ViewHelper.LogicalToPhysicalPixels(ItemHeight);
			}

			return _implicitItemHeight;
		}

		/// <summary>
		/// Resolve the items per line, limited by dimensions and/or <see cref="MaximumRowsOrColumns"/>
		/// </summary>
		private int ResolveMaximumItemsInLine(int availableBreadth)
		{
			var itemBreadth = ResolveItemBreadth();
			var maximumItemsBySpace = availableBreadth / itemBreadth ?? 1;
			// Catch pathological case that item returns a measured breadth larger than its available space
			maximumItemsBySpace = Math.Max(maximumItemsBySpace, 1);
			var maximumItemsBySetting = (MaximumRowsOrColumns == -1 ? int.MaxValue : MaximumRowsOrColumns);
			return Math.Min(maximumItemsBySetting, maximumItemsBySpace);
		}

		protected override Size ApplyChildStretch(Size childSize, Size slotSize, ViewType viewType)
		{
			//Item views in a grid layout shouldn't be stretched
			if (viewType == ViewType.Item)
			{
				return childSize;
			}
			return base.ApplyChildStretch(childSize, slotSize, viewType);
		}

		private protected override Uno.UI.IndexPath? GetDynamicSeedIndex(Uno.UI.IndexPath? firstVisibleItem, int availableBreadth)
		{
			//Get the first preceding item that is at the end of a line
			var currentItem = firstVisibleItem;
			var itemsPerLine = ResolveMaximumItemsInLine(availableBreadth);
			while (currentItem != null)
			{
				currentItem = GetNextUnmaterializedItem(GeneratorDirection.Backward, currentItem);
				if (currentItem?.Section != firstVisibleItem?.Section)
				{
					return currentItem;
				}
				if ((currentItem?.Row + 1) % itemsPerLine == 0)
				{
					return currentItem;
				}
			}
			return null;
		}
	}
}
