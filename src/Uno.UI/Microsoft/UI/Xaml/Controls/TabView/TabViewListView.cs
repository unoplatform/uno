// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference: TabViewListView.cpp, commit 309c88f

using Uno.UI.Helpers.WinUI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls.Primitives
{
	/// <summary>
	/// Represents the ListView corresponding to the TabStrip within the TabView.
	/// </summary>
	public partial class TabViewListView : ListView
	{
		/// <summary>
		/// Initializes a new instance of the TabViewListView class.
		/// </summary>
		public TabViewListView()
		{
			// TODO: Uno specific - avoid stretching tabs vertically.
			// Can be removed when #1133 is fixed.
#if __ANDROID__ || __IOS__
			ShouldApplyChildStretch = false;
#endif

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
				internalTabView.OnItemsChanged(item, this);
			}
		}

		protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
		{
			var itemContainer = (TabViewItem)element;
			var tvi = itemContainer;

			// Due to virtualization, a TabViewItem might be recycled to display a different tab data item.
			// In that case, there is no need to set the TabWidthMode of the TabViewItem or its parent TabView
			// as they are already set correctly here.
			//
			// We know we are currently looking at a TabViewItem being recycled if its parent TabView has already been set.
			if (tvi.GetParentTabView() == null)
			{
				var tabView = SharedHelpers.GetAncestorOfType<TabView>(VisualTreeHelper.GetParent(this));
				if (tabView != null)
				{
					tvi.OnTabViewWidthModeChanged(tabView.TabWidthMode);
					tvi.SetParentTabView(tabView);
				}
			}

			base.PrepareContainerForItemOverride(element, item);
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
