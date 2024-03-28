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
using Windows.UI.Xaml.Controls;
using ObjCRuntime;

namespace Uno.UI.Controls.Legacy
{
	public partial class GridViewWrapGridLayout : ListViewBaseLayout
	{
		private static readonly string GridViewItemKind = ListViewBase.ListViewItemElementKind;

		public GridViewWrapGridLayout()
		{
			Initialize();
		}

		void Initialize()
		{
			ScrollDirection = ListViewBaseScrollDirection.Vertical;
			ItemSize = new SizeF(200f, 112f);
			InteritemSpacing = 8f;
			LineSpacing = 8f;
			AreStickyGroupHeadersEnabled = true;
		}

		#region Properties
		private float _interitemSpacing;

		public float InteritemSpacing
		{
			get
			{
				return _interitemSpacing;
			}
			set
			{
				_interitemSpacing = value;
				InvalidateLayout();
			}
		}

		#endregion


		#region Members
		private readonly Dictionary<int, CGRect> _inlineHeaderFrames = new Dictionary<int, CGRect>();
		private readonly Dictionary<int, nfloat> _sectionEnd = new Dictionary<int, nfloat>();
		#endregion


		#region ItemMaxWidth DependencyProperty

		public double ItemMaxWidth
		{
			get { return (double)this.GetValue(ItemMaxWidthProperty); }
			set { this.SetValue(ItemMaxWidthProperty, value); }
		}

		// Using a DependencyProperty as the backing store for ItemMaxWidth.  This enables animation, styling, binding, etc...
		public static DependencyProperty ItemMaxWidthProperty { get; } =
			DependencyProperty.Register("ItemMaxWidth", typeof(double), typeof(GridViewWrapGridLayout), new FrameworkPropertyMetadata(0.0, (s, e) => ((GridViewWrapGridLayout)s)?.OnItemMaxWidthChanged(e)));

		private void OnItemMaxWidthChanged(DependencyPropertyChangedEventArgs e)
		{
			(CollectionView as ListViewBase)?.ReloadData();
		}

		#endregion


		#region MaximumRowsOrColumns DependencyProperty

		public int MaximumRowsOrColumns
		{
			get { return (int)this.GetValue(MaximumRowsOrColumnsProperty); }
			set { this.SetValue(MaximumRowsOrColumnsProperty, value); }
		}

		// Using a DependencyProperty as the backing store for MaximumRowsOrColumns.  This enables animation, styling, binding, etc...
		public static DependencyProperty MaximumRowsOrColumnsProperty { get; } =
			DependencyProperty.Register("MaximumRowsOrColumns", typeof(int), typeof(GridViewWrapGridLayout), new FrameworkPropertyMetadata(0, (s, e) => ((GridViewWrapGridLayout)s)?.OnMaximumRowsOrColumnsChanged(e)));

		private void OnMaximumRowsOrColumnsChanged(DependencyPropertyChangedEventArgs e)
		{
			(CollectionView as ListViewBase)?.ReloadData();
		}

		#endregion

		protected override CGSize PrepareLayoutInternal(bool createLayoutInfo = false, CGSize? availableSize = null)
		{
			var newLayoutInfo = new Dictionary<string, Dictionary<NSIndexPath, UICollectionViewLayoutAttributes>>(StringComparer.Ordinal);
			var headerLayoutInfo = new Dictionary<NSIndexPath, UICollectionViewLayoutAttributes>();
			var itemLayoutInfo = new Dictionary<NSIndexPath, UICollectionViewLayoutAttributes>();

			var contentSize = CGSize.Empty;

			if (CollectionView.NumberOfSections() != 0 && CollectionView.NumberOfItemsInSection(0) != 0)
			{
				_itemSize = GetItemSizeForIndexPath(GetNSIndexPathFromRowSection(0, 0));
			}


			bool hasSectionHeader = Source.GetTarget()?.Owner.GroupStyle?.HeaderTemplate != null;
			var sectionTitleHeight = GetSectionHeaderSize().Height;
			var sectionTitleWidth = GetSectionHeaderSize().Width;

			var availableWidth = availableSize?.Width ?? CollectionView.Bounds.Width;
			var availableHeight = availableSize?.Height ?? CollectionView.Bounds.Height;

			switch (ScrollDirection)
			{
				case ListViewBaseScrollDirection.Horizontal:
					contentSize = CreateHorizontalLayout(createLayoutInfo, headerLayoutInfo, itemLayoutInfo, hasSectionHeader, sectionTitleWidth, sectionTitleHeight, availableWidth, availableHeight);
					break;

				case ListViewBaseScrollDirection.Vertical:
					contentSize = CreateVerticalLayout(createLayoutInfo, headerLayoutInfo, itemLayoutInfo, hasSectionHeader, sectionTitleHeight, availableWidth);
					break;
			}

			if (createLayoutInfo)
			{
				newLayoutInfo[ListViewBase.ListViewSectionHeaderElementKind] = headerLayoutInfo;
				newLayoutInfo[GridViewItemKind] = itemLayoutInfo;
				_layoutInfos = newLayoutInfo;

				if (AreStickyGroupHeadersEnabled)
				{
					UpdateHeaderPositions();
				}
			}

			return contentSize;
		}

		private CGSize CreateHorizontalLayout(
			bool createLayoutInfo,
			Dictionary<NSIndexPath, UICollectionViewLayoutAttributes> headerLayoutInfo,
			Dictionary<NSIndexPath, UICollectionViewLayoutAttributes> itemLayoutInfo,
			bool hasSectionHeader,
			nfloat sectionTitleWidth,
			nfloat sectionTitleHeight,
			nfloat availableWidth,
			nfloat availableHeight
		)
		{
			nfloat expectedWidth = 0;
			nfloat expectedHeight = 0;

			CGSize contentSize;
			var frame = new CGRect(Margin.Left, Margin.Top, ItemSize.Width, ItemSize.Height);
			var maxRight = availableWidth - Margin.Right;
			var maxBottom = availableHeight - Margin.Bottom;

			var numberOfSections = CollectionView.NumberOfSections();
			for (int section = 0; section < numberOfSections; ++section)
			{
				//Start layouting each section at top left and furthest point right
				frame.Y = (nfloat)Margin.Top;
				var sectionStartX = frame.X = NMath.Max((nfloat)Margin.Left, expectedWidth);

				// Add header item
				if (hasSectionHeader)
				{
					if (section > 0)
					{
						frame.Y += IntersectionSpacing;
					}

					if (createLayoutInfo)
					{
						var sectionHeaderFrame = new CGRect(frame.X, frame.Y, sectionTitleWidth, sectionTitleHeight);
						CreateSectionHeaderLayoutInfo(headerLayoutInfo, section, sectionHeaderFrame);
					}

					frame.Y += sectionTitleHeight + LineSpacing;
				}

				// Add items
				var numberOfItems = CollectionView.NumberOfItemsInSection(section);
				var numberOfRowsInColumn = 0;
				for (int row = 0; row < numberOfItems; ++row)
				{
					var indexPath = GetNSIndexPathFromRowSection(row, section);
					var itemSize = GetItemSizeForIndexPath(indexPath);

					if (createLayoutInfo)
					{
						var layoutAttributes = GetLayoutAttributesForIndexPath(row, section);
						layoutAttributes.Frame = frame;
						itemLayoutInfo[indexPath] = layoutAttributes;
					}

					expectedWidth = NMath.Max(expectedWidth, (nfloat)(frame.X + itemSize.Width - Margin.Left));
					expectedHeight = NMath.Max(expectedHeight, (nfloat)(frame.Y + itemSize.Height - Margin.Top));

					numberOfRowsInColumn++;

					// Since it's not the last element, we need to move to the next one.
					if (row < numberOfItems - 1)
					{
						//Try moving vertically
						frame.Y += itemSize.Height + LineSpacing;

						//If item exceeds the limit vertically, wrap it to the next column
						if (frame.Bottom > maxBottom ||
							(MaximumRowsOrColumns > 0 && numberOfRowsInColumn > MaximumRowsOrColumns - 1)
						)
						{
							frame.X += itemSize.Width + InteritemSpacing;
							frame.Y = (nfloat)Margin.Top;
							numberOfRowsInColumn = 0;
						}
					}
				}

				//Reset for next section
				frame.Y = (nfloat)Margin.Top;
				//Record section end for positioning sticky headers
				_sectionEnd[section] = frame.X + frame.Width;
			}

			contentSize = new CGSize(expectedWidth + Margin.Left + Margin.Right, expectedHeight + Margin.Top + Margin.Bottom);
			return contentSize;
		}

		private CGSize CreateVerticalLayout(
			bool createLayoutInfo,
			Dictionary<NSIndexPath, UICollectionViewLayoutAttributes> headerLayoutInfo,
			Dictionary<NSIndexPath, UICollectionViewLayoutAttributes> itemLayoutInfo,
			bool hasSectionHeader,
			nfloat sectionTitleHeight,
			nfloat availableWidth
		)
		{
			CGSize contentSize;

			var frame = new CGRect(Margin.Left, Margin.Top, ItemSize.Width, ItemSize.Height);
			var maxRight = (availableWidth - Margin.Right);
			var numberOfColumnsInLine = 0;

			var expectedWidth = ItemSize.Width;
			var expectedHeight = ItemSize.Height;
			var itemWidth = ItemSize.Width;

			if (ItemMaxWidth != 0)
			{
				var adjustedAvailableWidth = availableWidth - (Margin.Left + Margin.Right);

				// This computation is present to ensure that the itemWidth will always fill the available
				// width, by adjusting the column count and the item width, up to ItemMaxWidth.
				var columnCount = Math.Round(adjustedAvailableWidth / ItemMaxWidth);
				var newWidth = (adjustedAvailableWidth - (InteritemSpacing * (columnCount - 1))) / columnCount;

				itemWidth = NMath.Floor((float)newWidth);

				frame = new CGRect(Margin.Left, Margin.Top, itemWidth, ItemSize.Height);
			}

			var numberOfSections = CollectionView.NumberOfSections();
			nfloat maxHeightInRow = 0;
			for (int section = 0; section < numberOfSections; ++section)
			{
				// Add header item
				if (hasSectionHeader)
				{
					if (section > 0)
					{
						frame.Y += maxHeightInRow + LineSpacing;
						frame.Y += IntersectionSpacing;
					}

					if (createLayoutInfo)
					{
						var sectionHeaderFrame = new CGRect(frame.X, frame.Y, maxRight - Margin.Left, sectionTitleHeight);
						CreateSectionHeaderLayoutInfo(headerLayoutInfo, section, sectionHeaderFrame);
					}

					frame.Y += sectionTitleHeight + LineSpacing;
				}

				var numberOfItems = CollectionView.NumberOfItemsInSection(section);
				maxHeightInRow = 0;
				for (int row = 0; row < numberOfItems; ++row)
				{
					var indexPath = GetNSIndexPathFromRowSection(row, section);
					var itemSize = GetItemSizeForIndexPath(indexPath);

					if (ItemMaxWidth != 0)
					{
						itemSize.Width = itemWidth;
					}

					frame.Height = itemSize.Height;
					maxHeightInRow = NMath.Max(maxHeightInRow, itemSize.Height);

					if (createLayoutInfo)
					{
						var layoutAttributes = GetLayoutAttributesForIndexPath(row, section);
						layoutAttributes.Frame = frame;
						itemLayoutInfo[indexPath] = layoutAttributes;
					}

					expectedHeight = NMath.Max(expectedHeight, (nfloat)(frame.Y + maxHeightInRow - Margin.Top));
					expectedWidth = NMath.Max(expectedWidth, (nfloat)(frame.X + itemSize.Width - Margin.Left));

					numberOfColumnsInLine++;

					// Since it's not the last element, we need to move to the next one.
					if (row < numberOfItems - 1)
					{
						// Try moving horizontally
						frame.X += itemSize.Width + InteritemSpacing;

						// If the item exceeds the limit horizontally, wrap it to the next line
						if (frame.Right > maxRight || (MaximumRowsOrColumns > 0 && numberOfColumnsInLine > MaximumRowsOrColumns - 1))
						{
							frame.X = (nfloat)Margin.Left;
							frame.Y += maxHeightInRow + LineSpacing;

							numberOfColumnsInLine = 0;
							maxHeightInRow = 0;
						}
					}

				}

				//Reset for next section
				frame.X = (nfloat)Margin.Left;
				numberOfColumnsInLine = 0;
				//Record minimum height for positioning sticky headers
				_sectionEnd[section] = frame.Y + frame.Height;
			}

			contentSize = new CGSize(expectedWidth + Margin.Left + Margin.Right, expectedHeight + Margin.Top + Margin.Bottom);
			return contentSize;
		}

		protected override void UpdateHeaderPositions()
		{
			// Get coordinate index to modify
			var axisIndex = ScrollDirection == ListViewBaseScrollDirection.Horizontal ?
				0 :
				1;
			var offset = CollectionView.ContentOffset.GetXOrY(axisIndex);
			Dictionary<NSIndexPath, UIKit.UICollectionViewLayoutAttributes> headerAttributes;
			if (_layoutInfos.TryGetValue(ListViewBase.ListViewSectionHeaderElementKind, out headerAttributes))
			{
				foreach (var kvp in headerAttributes)
				{
					var layoutAttributes = kvp.Value;

					var section = kvp.Key.Section;

					//1. Start with frame if header were inline
					var frame = _inlineHeaderFrames[section];

					//2. If frame would be out of bounds, bring it just in bounds
					nfloat frameOffset = frame.GetXOrY(axisIndex);
					if (frameOffset < offset)
					{
						frameOffset = offset;
					}

					//3. If frame base would be below base of lowest element in section, bring it just above lowest element in section
					var sectionMin = _sectionEnd[section] - frame.GetWidthOrHeight(axisIndex);
					if (frameOffset > sectionMin)
					{
						frameOffset = sectionMin;
					}

					layoutAttributes.Frame = frame.SetXOrY(axisIndex, frameOffset);
					//Ensure headers appear above elements
					layoutAttributes.ZIndex = 1;
				}

			}
		}

		private void CreateSectionHeaderLayoutInfo(Dictionary<NSIndexPath, UICollectionViewLayoutAttributes> headerLayoutInfo, int section, CGRect sectionHeaderFrame)
		{
			var indexPath = GetNSIndexPathFromRowSection(0, section);
			var layoutAttributes = UICollectionViewLayoutAttributes
				.CreateForSupplementaryView<UICollectionViewLayoutAttributes>(ListViewBase.ListViewSectionHeaderElementKindNS, indexPath);
			layoutAttributes.Frame = sectionHeaderFrame;
			headerLayoutInfo[indexPath] = layoutAttributes;
			_inlineHeaderFrames[section] = sectionHeaderFrame;
		}
	}

}
