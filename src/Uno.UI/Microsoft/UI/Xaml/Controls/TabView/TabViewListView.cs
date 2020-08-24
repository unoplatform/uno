using Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Microsoft.UI.Xaml.Controls.TabView
{
	public class TabViewListView : ListView
    {
		public TabViewListView()
		{
			this.DefaultStyleKey = typeof(TabViewListView);

			
		}

		protected override DependencyObject GetContainerForItemOverride() => new TabViewItem();

		protected override bool IsItemItsOwnContainerOverride(object args)
		{
			var isItemItsOwnContainer = false;
			var item = args as TabViewItem;
			if (item != null)
			{
				isItemItsOwnContainer = true; //TODO:MZ:Fix this
			}
			return isItemItsOwnContainer;
		}
	}
}
