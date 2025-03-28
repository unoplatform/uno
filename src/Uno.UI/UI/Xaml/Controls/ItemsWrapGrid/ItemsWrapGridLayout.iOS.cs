using System;
using System.Collections.Generic;
using System.Drawing;
using Windows.UI.Xaml;
using Uno.Extensions;
using Uno.Disposables;
using Foundation;
using UIKit;
using CoreGraphics;
using Uno.UI.Extensions;
using ObjCRuntime;

namespace Windows.UI.Xaml.Controls
{
	/// <summary>
	/// A native layout which implements <see cref="ItemsWrapGrid"/> behaviour.
	/// </summary>
	internal partial class ItemsWrapGridLayout : VirtualizingPanelLayout
	{
		private CGSize? _implicitItemSize;

		public ItemsWrapGridLayout() { }

		#region Properties
		internal override bool SupportsDynamicItemSizes => false;
		#endregion

		partial void OnMaximumRowsOrColumnsChangedPartialNative(int oldMaximumRowsOrColumns, int newMaximumRowsOrColumns)
		{
			InvalidateLayout();
		}

		protected override nfloat LayoutItemsInGroup(int group, nfloat availableBreadth, ref CGRect frame, bool createLayoutInfo, Dictionary<NSIndexPath, CGSize?> oldItemSizes)
		{
			var itemsInGroup = CollectionView.NumberOfItemsInSection(group);
			if (itemsInGroup == 0)
			{
				_sectionEnd[group] = GetExtentEnd(frame);
				return 0;
			}

			var itemSize = ResolveItemSize(group, availableBreadth);
			var itemBreadth = GetBreadth(itemSize);
			var itemsDisplayablePerLine = Math.Max((int)(availableBreadth / itemBreadth), 1);
			var itemsPerLine = MaximumRowsOrColumns > 0 ? Math.Min(itemsDisplayablePerLine, MaximumRowsOrColumns) : itemsDisplayablePerLine;
			var numberOfLines = (itemsInGroup + itemsPerLine - 1) / itemsPerLine; //Rounds up

			var groupBreadthStart = GetBreadthStart(frame);

			frame.Size = itemSize;

			int item = -1;
			for (int line = 0; line < numberOfLines; line++)
			{
				for (int column = 0; column < itemsPerLine; column++)
				{
					item++;
					if (item == itemsInGroup)
					{
						break;
					}
					if (createLayoutInfo)
					{
						CreateItemLayoutInfo(item, group, frame);
					}
					IncrementBreadth(ref frame);
				}

				_sectionEnd[group] = GetExtentEnd(frame);
				IncrementExtent(ref frame);
				SetBreadthStart(ref frame, groupBreadthStart);
			}

			return (nfloat)(itemBreadth * Math.Min(itemsPerLine, itemsInGroup));
		}

		private CGSize ResolveItemSize(int currentGroup, nfloat availableBreadth)
		{
			if ((double.IsNaN(ItemWidth) || double.IsNaN(ItemHeight)) && _implicitItemSize == null)
			{
				//TODO: this should measure the databound item, currently it only measures the empty template
				_implicitItemSize = GetItemSizeForIndexPath(GetNSIndexPathFromRowSection(0, currentGroup), availableBreadth);
			}

			return new CGSize(
				double.IsNaN(ItemWidth) ? _implicitItemSize.Value.Width : ItemWidth,
				double.IsNaN(ItemHeight) ? _implicitItemSize.Value.Height : ItemHeight
			);
		}
	}
}
