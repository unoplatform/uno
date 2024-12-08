using System;
using UIKit;
using CoreGraphics;
using Foundation;
using System.Collections.Generic;
using Uno.Extensions;

namespace Windows.UI.Xaml.Controls
{
	/// <summary>
	/// Natively-derived class that implements FlipView behavior
	/// </summary>
	public partial class PagedCollectionView : UICollectionView
	{
		public PagedCollectionView() : base(new CGRect(0, 0, 50, 50), GetLayout())
		{
			PagingEnabled = true;
			Bounces = true;

			BackgroundColor = UIColor.Clear;

			_layout = (UICollectionViewFlowLayout)this.CollectionViewLayout;

			RegisterClassForCell(typeof(Uno.UI.Controls.Legacy.ListViewBaseSource.InternalContainer), FlipView.FlipViewItemReuseIdentifier);

			if (ScrollViewer.UseContentInsetAdjustmentBehavior)
			{
				ContentInsetAdjustmentBehavior = UIScrollViewContentInsetAdjustmentBehavior.Never;
			}
		}


		UICollectionViewFlowLayout _layout;

		public override void LayoutSubviews()
		{
			base.LayoutSubviews();

			//These values can be overridden when using translucent navbar
			ScrollIndicatorInsets = new UIEdgeInsets();
			ContentInset = new UIEdgeInsets();
		}

		public UICollectionViewScrollDirection Orientation
		{
			get { return _layout.ScrollDirection; }
			set { _layout.ScrollDirection = value; }
		}

		public CGSize ItemSize
		{
			get { return _layout.ItemSize; }
			set { _layout.ItemSize = value; }
		}
		public override CGSize ContentSize
		{
			get
			{
				return base.ContentSize;
			}

			set
			{
				var hasChanged = base.ContentSize != value;
				base.ContentSize = value;
				//This ensures that the offset is snapped correctly in the edge case that the drag is released just at the moment that the content size changes. 
				//This can occur for specialized item sources which modify the collection while the FlipView is being dragged
				if (hasChanged && !Dragging && Source is FlipViewSource)
				{
					var selectedIndex = (Source as FlipViewSource).Owner.SelectedIndex;
					//Don't snap if SelectedIndex is unset
					if (selectedIndex >= 0)
					{
						SnapToPage(selectedIndex, animated: true);
					}
				}
			}
		}

		internal bool InDraggingEnded { get; set; }

		public override bool Dragging => base.Dragging && !InDraggingEnded;

		private static UICollectionViewFlowLayout GetLayout()
		{
			return new UICollectionViewFlowLayout()
			{
				ScrollDirection = UICollectionViewScrollDirection.Horizontal,
				MinimumLineSpacing = 0,
				MinimumInteritemSpacing = 0,
				SectionInset = new UIEdgeInsets(),
			};
		}

		/// <summary>
		/// Snap scroll offset exactly to nominated page.
		/// </summary>
		/// <param name="pageNumber">The page to snap to</param>
		/// <param name="animated">If we want to animate the Offset change</param>
		public void SnapToPage(int pageNumber, bool animated = false)
		{
			CGPoint newOffset;
			if (Orientation == UICollectionViewScrollDirection.Horizontal)
			{
				newOffset = new CGPoint(Frame.Width * pageNumber, ContentOffset.Y);
			}
			else //Vertical
			{
				newOffset = new CGPoint(ContentOffset.X, Frame.Height * pageNumber);
			}

			if (newOffset != ContentOffset)
			{
				this.SetContentOffset(newOffset, animated);
			}
			else
			{
				//This is needed in case an animated SnapToPage is already pending,
				// but should be overwritten by this (more recent) SnapToPage call.
				this.SetContentOffset(newOffset, false);
			}
		}

		/// <summary>
		/// Shift the scroll offset by a distance corresponding to the nominated number of pages, without snapping to a page boundary.
		/// </summary>
		internal void ShiftOffset(int numberOfPages, bool animated = false)
		{
			var newOffset = ContentOffset;
			if (Orientation == UICollectionViewScrollDirection.Horizontal)
			{
				newOffset.X += Frame.Width * numberOfPages;
			}
			else
			{
				newOffset.Y += Frame.Height * numberOfPages;
			}

			if (newOffset != ContentOffset)
			{
				this.SetContentOffset(newOffset, animated);
			}
		}
	}
}

