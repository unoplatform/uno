using Uno.Extensions;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Uno.Disposables;

#if NET6_0_OR_GREATER
using ObjCRuntime;
#endif

#if XAMARIN_IOS_UNIFIED
using Foundation;
using UIKit;
using CoreGraphics;
using LayoutInfo = System.Collections.Generic.Dictionary<Foundation.NSIndexPath, UIKit.UICollectionViewLayoutAttributes>;
#elif XAMARIN_IOS
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.CoreGraphics;
using CGRect = System.Drawing.RectangleF;
using nfloat = System.Single;
using nint = System.Int32;
using CGPoint = System.Drawing.PointF;
using CGSize = System.Drawing.SizeF;
#endif

namespace Microsoft.UI.Xaml.Controls
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

			var numberOfItems = CollectionView.NumberOfItemsInSection(group);

			_sectionEnd[group] = GetExtentEnd(frame);

			nfloat measuredBreadth = 0;
			for (var row = 0; row < numberOfItems; ++row)
			{
				var indexPath = GetNSIndexPathFromRowSection(row, group);
				frame.Size = oldItemSizes?.UnoGetValueOrDefault(indexPath) ?? GetItemSizeForIndexPath(indexPath, availableBreadth);

				if (ShouldApplyChildStretch)
				{
					//Give the maximum breadth available, since for now we don't adjust the measured width of the list based on the databound item
					SetBreadth(ref frame, availableBreadth);
				}

				if (createLayoutInfo)
				{
					CreateItemLayoutInfo(row, group, frame);
				}

				_sectionEnd[group] = GetExtentEnd(frame);

				IncrementExtent(ref frame);
				measuredBreadth = NMath.Max(measuredBreadth, GetBreadthEnd(frame));
			}
			return measuredBreadth;
		}

		private protected override void UpdateLayoutAttributesForItem(UICollectionViewLayoutAttributes updatingItem, bool shouldRecurse)
		{
			while (updatingItem != null)
			{
				//Update extent of either subsequent item in group, subsequent group header, or footer
				var currentIndex = updatingItem.IndexPath;
				var nextIndexInGroup = GetNSIndexPathFromRowSection(currentIndex.Row + 1, currentIndex.Section);

				// Get next item in current group
				var elementToAdjust = LayoutAttributesForItem(nextIndexInGroup);

				if (elementToAdjust == null)
				{
					// No more items in current group, get group header of next group
					elementToAdjust = LayoutAttributesForSupplementaryView(
						NativeListViewBase.ListViewSectionHeaderElementKindNS,
						GetNSIndexPathFromRowSection(0, currentIndex.Section + 1));

					//This is the last item in section, update information used by sticky headers.
					_sectionEnd[currentIndex.Section] = GetExtentEnd(updatingItem.Frame);
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
					var inlineFrame = GetInlineHeaderFrame(elementToAdjust.IndexPath.Section);
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
