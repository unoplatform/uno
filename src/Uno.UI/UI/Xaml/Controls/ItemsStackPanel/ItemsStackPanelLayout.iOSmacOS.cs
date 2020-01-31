using Uno.Extensions;
using Windows.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Uno.Disposables;

using Foundation;
using CoreGraphics;
#if __IOS__
using UIKit;
using LayoutInfo = System.Collections.Generic.Dictionary<Foundation.NSIndexPath, UIKit.UICollectionViewLayoutAttributes>;
using _LayoutAttributes = UIKit.UICollectionViewLayoutAttributes;
#else
using AppKit;
using LayoutInfo = System.Collections.Generic.Dictionary<Foundation.NSIndexPath, AppKit.NSCollectionViewLayoutAttributes>;
using _LayoutAttributes = AppKit.NSCollectionViewLayoutAttributes;
#endif

namespace Windows.UI.Xaml.Controls
{
	/// <summary>
	/// A native layout which implements <see cref="ItemsStackPanel"/> behaviour.
	/// </summary>
	internal partial class ItemsStackPanelLayout : VirtualizingPanelLayout
	{
		#region Properties
		internal override bool SupportsDynamicItemSizes => true;
		#endregion

		public ItemsStackPanelLayout() { }

		protected override nfloat LayoutItemsInGroup(int group, nfloat availableBreadth, ref CGRect frame, bool createLayoutInfo, Dictionary<NSIndexPath, CGSize?> oldItemSizes)
		{
#if __IOS__
			var itemsInGroup = CollectionView.NumberOfItemsInSection(group);
#else
			var itemsInGroup = CollectionView.GetNumberOfItems(group);
#endif

			_sectionEnd[group] = GetExtentEnd(frame);

			nfloat measuredBreadth = 0;
			for (var row = 0; row < itemsInGroup; ++row)
			{
				var indexPath = GetNSIndexPathFromRowSection(row, group);
				frame.Size = oldItemSizes?.UnoGetValueOrDefault(indexPath) ?? GetItemSizeForIndexPath(indexPath, availableBreadth);

				//Give the maximum breadth available, since for now we don't adjust the measured width of the list based on the databound item
				SetBreadth(ref frame, availableBreadth);

				if (createLayoutInfo)
				{
					CreateItemLayoutInfo(row, group, frame);
				}

				_sectionEnd[group] = GetExtentEnd(frame);

				IncrementExtent(ref frame);
				measuredBreadth = NMath.Max(measuredBreadth, GetBreadth(frame.Size));
			}
			return measuredBreadth;
		}

		private void SetExtentStart(ref CGRect frame, nfloat extentStart)
		{
			if (ScrollOrientation == Orientation.Vertical)
			{
				frame.Y = extentStart;
			}
			else
			{
				frame.X = extentStart;
			}
		}

		private protected override void UpdateLayoutAttributesForItem(_LayoutAttributes updatingItem, bool shouldRecurse)
		{
			while (updatingItem != null)
			{
				//Update extent of either subsequent item in group, subsequent group header, or footer
				var currentIndex = updatingItem.IndexPath;
				var nextIndexInGroup = GetNSIndexPathFromRowSection((int)(currentIndex.Item + 1), (int)currentIndex.Section);

				// Get next item in current group
				var elementToAdjust = LayoutAttributesForItem(nextIndexInGroup);

				if (elementToAdjust == null)
				{
					// No more items in current group, get group header of next group
					elementToAdjust = LayoutAttributesForSupplementaryView(
						NativeListViewBase.ListViewSectionHeaderElementKindNS,
						GetNSIndexPathFromRowSection(0, (int)currentIndex.Section + 1));

					//This is the last item in section, update information used by sticky headers.
					_sectionEnd[(int)currentIndex.Section] = GetExtentEnd(updatingItem.Frame);
				}

				if (elementToAdjust == null)
				{
					// No more groups in source, get footer
					elementToAdjust = LayoutAttributesForSupplementaryView(
						NativeListViewBase.ListViewFooterElementKindNS,
						GetNSIndexPathFromRowSection(0, 0));
				}

				if (elementToAdjust == null)
				{
					break;
				}

				if (elementToAdjust.RepresentedElementKind != NativeListViewBase.ListViewSectionHeaderElementKind)
				{
					//Update position of subsequent item based on position of this item, which may have changed
					var frame = elementToAdjust.Frame;
					SetExtentStart(ref frame, GetExtentEnd(updatingItem.Frame));
					elementToAdjust.Frame = frame;

					if (shouldRecurse && elementToAdjust.RepresentedElementKind == null)
					{
						updatingItem = elementToAdjust;
					}
					else
					{
						updatingItem = null;
					}
				}
				else
				{
					//Update group header
					var inlineFrame = GetInlineHeaderFrame((int)elementToAdjust.IndexPath.Section);
					var extentDifference = GetExtentEnd(updatingItem.Frame) - GetExtentStart(inlineFrame);
					if (extentDifference != 0)
					{
						UpdateLayoutAttributesForGroupHeader(elementToAdjust, extentDifference, true);
					}

					updatingItem = null;
				}
			}
		}
	}
}
