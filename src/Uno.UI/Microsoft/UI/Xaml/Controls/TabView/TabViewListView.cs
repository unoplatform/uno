// MUX Reference: TabViewListView.cpp, commit 46f9da3

using System.Collections.Specialized;
using Uno.Extensions;
using Uno.UI.Helpers.WinUI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls.Primitives
{
	public partial class TabViewListView : ListView
	{
		public TabViewListView()
		{
			// TODO: Uno specific - avoid stretching tabs vertically.
			// Can be removed when #1133 is fixed.
			ShouldApplyChildStretch = false;

			this.DefaultStyleKey = typeof(TabViewListView);

			ContainerContentChanging += OnContainerContentChanging;
		}

		protected override DependencyObject GetContainerForItemOverride() => new TabViewItem();

		protected override bool IsItemItsOwnContainerOverride(object args)
		{
			var isItemItsOwnContainer = false;
			var item = args as TabViewItem;
			if (item != null)
			{
				isItemItsOwnContainer = true;
			}
			return isItemItsOwnContainer;
		}

		protected override void OnItemsChanged(object item)
		{
			base.OnItemsChanged(item);

			var tabView = SharedHelpers.GetAncestorOfType<TabView>(VisualTreeHelper.GetParent(this));
			if (tabView != null)
			{
				var internalTabView = tabView;
				internalTabView.OnItemsChanged(item);
			}
		}

		private void OnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
		{
			var tabView = SharedHelpers.GetAncestorOfType<TabView>(VisualTreeHelper.GetParent(this));
			if (tabView != null)
			{
				var internalTabView = tabView;
				internalTabView.UpdateTabContent();
			}
		}


		// TODO Uno specific: ensure the items are updated for ItemsSource change as Items are not yet in sync with ItemsSource properly

		internal override void OnItemsSourceSingleCollectionChanged(object sender, NotifyCollectionChangedEventArgs args, int section)
		{
			base.OnItemsSourceSingleCollectionChanged(sender, args, section);

			var tabView = SharedHelpers.GetAncestorOfType<TabView>(VisualTreeHelper.GetParent(this));
			if (tabView != null)
			{
				var internalTabView = tabView;
				var vectorChangedArgs = args.ToVectorChangedEventArgs();
				internalTabView.OnItemsChanged(vectorChangedArgs);
			}
		}
	}
}
