// MUX Reference: TabViewListView.cpp, commit 46f9da3

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
	}
}
