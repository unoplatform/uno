using System;
using System.Collections;
using System.Linq;
using UIKit;
using Foundation;
using Uno.Extensions;
using Uno.UI.Views.Controls;
using Uno.UI.Controls;
using Uno.Extensions.Specialized;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls
{
	public partial class FlipViewSource : UICollectionViewSource
	{
		public FlipViewSource()
		{
		}

		private WeakReference<FlipView> _ownerReference;
		internal FlipView Owner
		{
			get { return _ownerReference?.GetTarget(); }
			set { _ownerReference = new WeakReference<FlipView>(value); }
		}

		public override UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
		{
			var index = indexPath.Row;

			var cell = (Uno.UI.Controls.Legacy.ListViewBaseSource.InternalContainer)collectionView.DequeueReusableCell(FlipView.FlipViewItemReuseIdentifier, indexPath);

			var selectorItem = cell.Content as SelectorItem;
			if (
				// Create the selectorItem if this is a new cell
				selectorItem == null ||
				// Even if this is a recycled cell, check if the item itself is a FlipViewItem (typically because it's defined in xaml)
				(Owner?.IsIndexItsOwnContainer(index) ?? false)
			)
			{
				selectorItem = Owner?.GetContainerForIndex(index) as SelectorItem;
				cell.Content = selectorItem;
			}

			Owner?.PrepareContainerForIndex(cell.Content, index);

			// If PrepareContainerForItem sets the DataContext explicitly (ie ItemsSource is populated), this will have no effect
			selectorItem.SetValue(SelectorItem.DataContextProperty, Owner?.DataContext, DependencyPropertyValuePrecedences.Inheritance);

			return cell;
		}

		public override nint GetItemsCount(UICollectionView collectionView, nint section)
		{
			return Owner?.NumberOfItems ?? 0;
		}

		public override nint NumberOfSections(UICollectionView collectionView)
		{
			return 1;
		}

		public override void DraggingEnded(UIScrollView scrollView, bool willDecelerate)
		{
			try
			{
				// This is a workaround for an apparent iOS bug where Dragging will inaccurately return true in certain cases (seemingly
				// after multiple gestures when Bounces = false).
				(scrollView as PagedCollectionView).InDraggingEnded = true;

				//This will very occasionally occur if scroll is released exactly on the boundary of a page, in which case DecelerationEnded won't get called
				if (!willDecelerate)
				{
					UpdateCurrentPage(scrollView);
				}
			}
			finally
			{
				(scrollView as PagedCollectionView).InDraggingEnded = false;
			}
		}

		public override void DecelerationEnded(UIScrollView scrollView)
		{
			UpdateCurrentPage(scrollView);
		}

		private void UpdateCurrentPage(UIScrollView scrollView)
		{
			int page;
			var orientation = (scrollView as PagedCollectionView)?.Orientation ?? UICollectionViewScrollDirection.Horizontal;
			if (orientation == UICollectionViewScrollDirection.Horizontal)
			{
				var pageWidth = scrollView.Frame.Width;
				//Clamp to prevent outOfRangeException if scrollView is 'stretched' too far
				var offset = Clamp(scrollView.ContentOffset.X, 0, scrollView.ContentSize.Width - pageWidth);
				page = (int)Math.Round(offset / pageWidth);
			}
			else
			{
				var pageHeight = scrollView.Frame.Height;
				var offset = Clamp(scrollView.ContentOffset.Y, 0, scrollView.ContentSize.Height - pageHeight);
				page = (int)Math.Round(offset / pageHeight);
			}
			if (Owner != null)
			{
				Owner.SelectedIndex = page;
			}
		}

		double Clamp(double valueToClamp, double min, double max)
		{
			return Math.Min(Math.Max(valueToClamp, min), max);
		}
	}
}

