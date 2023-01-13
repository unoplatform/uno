using System;
using UIKit;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Foundation;
using CoreGraphics;
using Uno.UI.Views.Controls;
using Uno.Extensions;
using Uno.UI.Extensions;
using System.Collections.Specialized;
using Uno.Extensions.Specialized;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls
{
	public partial class FlipView : Selector
	{
		private CGRect _oldBounds;

		private PagedCollectionView CollectionView { get { return InternalItemsPanelRoot as PagedCollectionView; } }

		internal const string FlipViewItemReuseIdentifier = "FlipViewItemReuseIdentifier";

		public override void LayoutSubviews()
		{
			base.LayoutSubviews();

			if (CollectionView == null)
			{
				return;
			}

			CollectionView.Frame = Bounds;
			CollectionView.ItemSize = Bounds.Size;

			//Set offset to current page if bounds change, eg if the device is rotated
			if (Bounds.AreSizesDifferent(_oldBounds)
				//Only snap if SelectedIndex is set
				&& SelectedIndex >= 0)
			{
				CollectionView?.SnapToPage(SelectedIndex, animated: UseTouchAnimationsForAllNavigation);
			}
			_oldBounds = Bounds;
		}

		private void OnSelectedIndexChangedPartial(int oldValue, int newValue, bool animateChange)
		{
			if (CollectionView == null
				// Don't snap if we are unsetting SelectedIndex
				|| newValue == -1)
			{
				return;
			}
			// If user is not currently swiping, jump to the selected page, otherwise apply scroll offset corresponding to the difference
			if (!CollectionView.Dragging)
			{
				CollectionView.SnapToPage(newValue, animated: animateChange);
			}
			else
			{
				CollectionView.ShiftOffset(newValue - oldValue, animated: animateChange);
			}
		}

		private protected override void UpdateItems(NotifyCollectionChangedEventArgs args)
		{
			if (InternalItemsPanelRoot == null)
			{
				//We probably haven't called OnApplyTemplate() yet
				base.UpdateItems(args);
				return;
			}

			if (CollectionView == null)
			{
				throw new InvalidOperationException($"Native implementation not found. Make sure FlipView has a style which contains an ItemsPanel of type {nameof(PagedCollectionView)}.");
			}

			if (CollectionView.Source == null)
			{
				var source = new FlipViewSource()
				{
					Owner = this
				};
				CollectionView.Source = source;
			}

			base.UpdateItems(args);

			CollectionView.ReloadData();
		}

		internal override void OnItemsSourceSingleCollectionChanged(object sender, NotifyCollectionChangedEventArgs c, int section)
		{
			base.OnItemsSourceSingleCollectionChanged(sender, c, section);

			if (SelectedIndex == -1 && HasItems)
			{
				//When we add items to the FlipView, SelectedIndex should be set to the first item
				SelectedIndex = 0;
			}
		}

		protected override UICollectionViewCell GetCellForItem(object item)
		{
			return IndexPathForItem(item).SelectOrDefault(ip => CollectionView?.CellForItem(ip));
		}

		private NSIndexPath IndexPathForItem(object item)
		{
			var index = IndexFromItem(item);
			if (index < 0)
			{
				return null;
			}
			return NSIndexPath.FromItemSection(index, 0);
		}

		internal protected override void OnDataContextChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnDataContextChanged(e);

			GetItems()?.ForEach(item =>
			{
				// The iOS collection view has several native views in its hierarchy and 'normal' DataContext inheritance doesn't work,
				// so we manually propagate the DataContext to items which need it (views defined inline)
				if (item is IDependencyObjectStoreProvider provider)
				{
					provider.Store.SetValue(provider.Store.DataContextProperty, DataContext, DependencyPropertyValuePrecedences.Local);
				}
			});
		}
	}
}

