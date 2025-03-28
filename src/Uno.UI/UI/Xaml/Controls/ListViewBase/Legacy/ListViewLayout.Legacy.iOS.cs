using Uno.Extensions;
using Windows.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Uno.Disposables;
using Windows.UI.Xaml.Controls;
using ObjCRuntime;

using Foundation;
using UIKit;
using CoreGraphics;
using LayoutInfo = System.Collections.Generic.Dictionary<Foundation.NSIndexPath, UIKit.UICollectionViewLayoutAttributes>;

namespace Uno.UI.Controls.Legacy
{
	/// <summary>
	/// TODO Implement header for Horizontal ListView
	/// TODO Implement footer for Horizontal ListView
	/// </summary>
	public partial class ListViewLayout : ListViewBaseLayout
	{
		#region Properties
		public double ItemHeight
		{
			get { return (double)ItemSize.Height; }
			set
			{
				if (Math.Abs(ItemSize.Height - value) > double.Epsilon)
				{
					ItemSize = new CGSize(nfloat.NaN, value);
					InvalidateLayout();
				}
			}
		}
		#endregion

		public ListViewLayout()
		{
			Initialize();
		}

		private void Initialize()
		{
			ScrollDirection = ListViewBaseScrollDirection.Vertical;
			Margin = new Thickness(0f, 0f, 0f, 0f);
			ItemHeight = 112f;
			LineSpacing = 0f;
		}

		protected override CGSize PrepareLayoutInternal(bool createLayoutInfo = false, CGSize? availableSize = null)
		{
			ListViewBaseSource source;
			Source.TryGetTarget(out source);

			if (source == null)
			{
				return CGSize.Empty;
			}

			var newLayoutInfo = createLayoutInfo ? new Dictionary<string, LayoutInfo>(StringComparer.Ordinal) : null;

			var availableWidth = availableSize.SelectOrDefault(size => size.Value.Width, CollectionView.Bounds.Width);
			var availableHeight = availableSize.SelectOrDefault(size => size.Value.Height, CollectionView.Bounds.Height);

			var frame = (ScrollDirection == ListViewBaseScrollDirection.Vertical)
				? new CGRect(Margin.Left, Margin.Top, availableWidth - (Margin.Left + Margin.Right), 0)
				: new CGRect(Margin.Left, Margin.Top, 0, availableHeight - (Margin.Top + Margin.Bottom));

			//Layout header for whole ListView
			if (source.Owner.Header != null ||
				source.Owner.HeaderTemplate != null)
			{
				LayoutHeader(newLayoutInfo, ref frame);
			}

			var numberOfSections = CollectionView.NumberOfSections();
			for (var section = 0; section < numberOfSections; ++section)
			{
				//Layout section header
				if (source.Owner.GroupStyle?.HeaderTemplate != null)
				{
					LayoutSectionHeader(newLayoutInfo, section, ref frame);
				}

				LayoutItems(newLayoutInfo, section, ref frame);

			}

			//Layout footer for whole ListView
			if (source.Owner.Footer != null ||
				source.Owner.FooterTemplate != null)
			{
				LayoutFooter(newLayoutInfo, ref frame);
			}

			if (createLayoutInfo)
			{
				_layoutInfos = newLayoutInfo;
			}

			return (ScrollDirection == ListViewBaseScrollDirection.Vertical)
				? new CGSize(availableWidth, frame.Y + Margin.Bottom)
				: new CGSize(frame.X + Margin.Right, availableHeight);
		}

		/// <summary>
		/// Adjusts the size of the frame by calculating the required space of the items.
		/// </summary>
		private void LayoutItems(Dictionary<string, LayoutInfo> layoutInfos, int section, ref CGRect frame)
		{
			var createLayoutInfo = layoutInfos != null;
			var itemLayoutInfo = createLayoutInfo ?
				layoutInfos.FindOrCreate(ListViewBase.ListViewItemElementKind, () => new LayoutInfo())
				: default(LayoutInfo);

			var numberOfItems = CollectionView.NumberOfItemsInSection(section);
			if (ScrollDirection == ListViewBaseScrollDirection.Vertical)
			{
				for (var row = 0; row < numberOfItems; ++row)
				{
					var indexPath = GetNSIndexPathFromRowSection(row, section);
					frame.Height = GetItemSizeForIndexPath(indexPath).Height;

					if (createLayoutInfo)
					{
						var layoutAttributes = GetLayoutAttributesForIndexPath(row, section);
						layoutAttributes.Frame = frame;
						itemLayoutInfo[indexPath] = layoutAttributes;
					}

					frame.Y += frame.Height + LineSpacing;
				}
			}
			else
			{
				for (var row = 0; row < numberOfItems; ++row)
				{
					var indexPath = GetNSIndexPathFromRowSection(row, section);
					frame.Width = GetItemSizeForIndexPath(indexPath).Width;

					if (createLayoutInfo)
					{
						var layoutAttributes = GetLayoutAttributesForIndexPath(row, section);
						layoutAttributes.Frame = frame;
						itemLayoutInfo[indexPath] = layoutAttributes;
					}

					frame.X += frame.Width + LineSpacing;
				}
			}
		}

		/// <summary>
		/// Adjusts the size of the frame by calculating the required space of the header.
		/// </summary>
		private void LayoutHeader(Dictionary<string, LayoutInfo> layoutInfos, ref CGRect frame)
		{
			if (ScrollDirection == ListViewBaseScrollDirection.Horizontal)
			{
				throw new NotImplementedException("The header for an horizontal listview is not implemented.");
			}

			var createLayoutInfo = layoutInfos != null;

			var headerSize = GetHeaderSize();

			frame.Height = headerSize.Height;

			if (createLayoutInfo)
			{
				var headerLayoutInfo = new LayoutInfo();
				//"It is up to you to decide how to use the indexPath parameter to identify a given supplementary view. Typically, you use the elementKind parameter to identify the type
				//of the supplementary view and the indexPath information to distinguish between different instances of that view."
				//https://developer.apple.com/library/ios/documentation/UIKit/Reference/UICollectionViewLayoutAttributes_class/#//apple_ref/occ/clm/UICollectionViewLayoutAttributes/layoutAttributesForSupplementaryViewOfKind:withIndexPath:
				var indexPath = NSIndexPath.FromRowSection(0, 0);

				var layoutAttributes = UICollectionViewLayoutAttributes.CreateForSupplementaryView<UICollectionViewLayoutAttributes>(ListViewBase.ListViewHeaderElementKindNS, indexPath);
				layoutAttributes.Frame = new CGRect(Margin.Left, frame.Y, frame.Width, headerSize.Height);
				headerLayoutInfo[indexPath] = layoutAttributes;
				layoutInfos[ListViewBase.ListViewHeaderElementKind] = headerLayoutInfo;
			}

			frame.Y += frame.Height + LineSpacing;
		}

		/// <summary>
		/// Adjusts the size of the frame by calculating the required space of the footer.
		/// </summary>
		private void LayoutFooter(Dictionary<string, LayoutInfo> layoutInfos, ref CGRect frame)
		{
			if (ScrollDirection == ListViewBaseScrollDirection.Horizontal)
			{
				throw new NotImplementedException("The footer for an horizontal listview is not implemented.");
			}

			var createLayoutInfo = layoutInfos != null;

			var footerSize = GetFooterSize();

			frame.Height = footerSize.Height;

			if (createLayoutInfo)
			{
				var footerLayoutInfo = new LayoutInfo();
				var indexPath = NSIndexPath.FromRowSection(0, 0);

				var layoutAttributes = UICollectionViewLayoutAttributes.CreateForSupplementaryView<UICollectionViewLayoutAttributes>(ListViewBase.ListViewFooterElementKindNS, indexPath);
				layoutAttributes.Frame = new CGRect(Margin.Left, frame.Y, frame.Width, footerSize.Height);
				footerLayoutInfo[indexPath] = layoutAttributes;
				layoutInfos[ListViewBase.ListViewFooterElementKind] = footerLayoutInfo;
			}

			frame.Y += frame.Height + LineSpacing;
		}

		private void LayoutSectionHeader(Dictionary<string, LayoutInfo> layoutInfos, int section, ref CGRect frame)
		{
			if (ScrollDirection == ListViewBaseScrollDirection.Horizontal)
			{
				throw new NotImplementedException("The section header for a horizontal listview is not implemented.");
			}

			var createLayoutInfo = layoutInfos != null;

			if (section > 0)
			{
				frame.Y += IntersectionSpacing;
			}

			var sectionHeaderSize = GetSectionHeaderSize();

			frame.Height = sectionHeaderSize.Height;

			if (createLayoutInfo)
			{
				var sectionHeaderLayoutInfo = layoutInfos.FindOrCreate(ListViewBase.ListViewSectionHeaderElementKind, () => new LayoutInfo());

				//indexPath acts to uniquely identify this instance of ListViewSectionHeaderElementKind (one per section)
				var indexPath = NSIndexPath.FromRowSection(0, section);
				var layoutAttributes = UICollectionViewLayoutAttributes.
					CreateForSupplementaryView<UICollectionViewLayoutAttributes>(ListViewBase.ListViewSectionHeaderElementKindNS, indexPath);
				layoutAttributes.Frame = new CGRect(Margin.Left, frame.Y, frame.Width, sectionHeaderSize.Height);
				sectionHeaderLayoutInfo[indexPath] = layoutAttributes;
			}

			frame.Y += frame.Height + LineSpacing;
		}
	}
}
