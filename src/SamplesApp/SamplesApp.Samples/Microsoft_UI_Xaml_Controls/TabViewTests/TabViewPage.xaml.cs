// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Markup;
using Windows.UI;
using System.Windows.Input;
using Microsoft.UI.Xaml.Automation;

using TabView = Microsoft.UI.Xaml.Controls.TabView;
using TabViewItem = Microsoft.UI.Xaml.Controls.TabViewItem;
using TabViewTabCloseRequestedEventArgs = Microsoft.UI.Xaml.Controls.TabViewTabCloseRequestedEventArgs;
using TabViewTabDragStartingEventArgs = Microsoft.UI.Xaml.Controls.TabViewTabDragStartingEventArgs;
using TabViewTabDragCompletedEventArgs = Microsoft.UI.Xaml.Controls.TabViewTabDragCompletedEventArgs;
using SymbolIconSource = Microsoft.UI.Xaml.Controls.SymbolIconSource;
using System.Collections.ObjectModel;
using Windows.Devices.PointOfService;
using Windows.ApplicationModel.DataTransfer;
using MUXControlsTestApp.Utilities;
using System.Threading.Tasks;
using Uno.UI.Samples.Controls;
using UITests.Microsoft_UI_Xaml_Controls.TabViewTests;
using Microsoft.UI.Xaml.Shapes;
using System.Reflection;

namespace MUXControlsTestApp
{
	public partial class TabDataItem : DependencyObject
	{
		public String Header { get; set; }
		public SymbolIconSource IconSource { get; set; }
		public String Content { get; set; }
	}

	[Sample("TabView", "MUX")]
	public sealed partial class TabViewPage : TestPage
	{
		int _newTabNumber = 1;
		SymbolIconSource _iconSource;

		public TabViewPage()
		{
			this.InitializeComponent();

			_iconSource = new SymbolIconSource();
			_iconSource.Symbol = Symbol.Placeholder;

			Tabs.TabItemsChanged += Tabs_TabItemsChanged;

			ObservableCollection<TabDataItem> itemSource = new ObservableCollection<TabDataItem>();
			for (int i = 0; i < 5; i++)
			{
				var item = new TabDataItem();
				item.IconSource = _iconSource;
				item.Header = "Item " + i;
				item.Content = "This is tab " + i + ".";
				itemSource.Add(item);
			}
			DataBindingTabView.TabItemsSource = itemSource;

			backgroundColorCache = BackgroundGrid.Background;
			activeTabContentBackgroundBrushCache = FirstTabContent.Background;
			CacheFirstTabSelectedBackgroundPathFill();
		}

		private void Tabs_TabItemsChanged(TabView sender, Windows.Foundation.Collections.IVectorChangedEventArgs args)
		{
			TabsItemChangedEventArgsTextBlock.Text = args.CollectionChange.ToString();
			TabsItemChangedEventArgsIndexTextBlock.Text = args.Index.ToString();
		}

		private Brush backgroundColorCache;
		private Brush activeTabSelectedBackgroundPathBrushCache;
		private Brush activeTabContentBackgroundBrushCache;

		protected
#if HAS_UNO
			internal
#endif
			async override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs args)
		{
			NotCloseableTab.Visibility = Visibility.Collapsed;
			await Task.Delay(TimeSpan.FromMilliseconds(1));
			NotCloseableTab.Visibility = Visibility.Visible;
		}

		public void IsClosableCheckBox_CheckChanged(object sender, RoutedEventArgs e)
		{
			if (FirstTab != null)
			{
				FirstTab.IsClosable = (bool)IsClosableCheckBox.IsChecked;
			}
		}

		public void IsDisabledTabVisibleCheckBox_CheckChanged(object sender, RoutedEventArgs e)
		{
			if (Tabs != null && DisabledTab != null)
			{
				var isVisible = (bool)IsDisabledTabVisibleCheckBox.IsChecked;
				if (isVisible && !Tabs.TabItems.Contains(DisabledTab))
				{
					// Let's insert the DisabledTab just after NotCloseableTab
					var n = Tabs.TabItems.IndexOf(NotCloseableTab) + 1;
					Tabs.TabItems.Insert(n, DisabledTab);
				}
				else
				{
					Tabs.TabItems.Remove(DisabledTab);
				}
			}
		}

		public void AddButtonClick(object sender, object e)
		{
			if (Tabs != null)
			{
				TabViewItem item = new TabViewItem();
				item.IconSource = _iconSource;
				item.Header = "New Tab " + _newTabNumber;
				item.Content = item.Header;

				Tabs.TabItems.Add(item);

				_newTabNumber++;
			}
		}

		public void RemoveTabButton_Click(object sender, RoutedEventArgs e)
		{
			if (Tabs != null && Tabs.TabItems.Count > 0)
			{
				Tabs.TabItems.RemoveAt(Tabs.TabItems.Count - 1);
			}
		}


		public void SelectItemButton_Click(object sender, RoutedEventArgs e)
		{
			if (Tabs != null)
			{
				Tabs.SelectedItem = Tabs.TabItems[1];
			}
		}

		public void SelectIndexButton_Click(object sender, RoutedEventArgs e)
		{
			if (Tabs != null)
			{
				Tabs.SelectedIndex = 2;
			}
		}

		public void ChangeShopTextButton_Click(object sender, RoutedEventArgs e)
		{
			SecondTab.Header = "Changed";
		}

		public void CustomTooltipButton_Click(object sender, RoutedEventArgs e)
		{
			ToolTipService.SetToolTip(SecondTab, "Custom");
		}

		public void GetTab0ToolTipButton_Click(object sender, RoutedEventArgs e)
		{
			GetToolTipStringForUIElement(FirstTab, Tab0ToolTipTextBlock);
		}

		public void GetTab1ToolTipButton_Click(object sender, RoutedEventArgs e)
		{
			GetToolTipStringForUIElement(SecondTab, Tab1ToolTipTextBlock);
		}

		public void GetToolTipStringForUIElement(UIElement item, TextBlock textBlock)
		{
			var tooltip = ToolTipService.GetToolTip(item);
			if (tooltip is ToolTip)
			{
				textBlock.Text = (tooltip as ToolTip).Content.ToString();
			}
			else
			{
				textBlock.Text = tooltip.ToString();
			}
		}

		public void GetFirstTabLocationButton_Click(object sender, RoutedEventArgs e)
		{
			FrameworkElement element = FirstTab as FrameworkElement;
			while (element != null)
			{
				if (element == Tabs)
				{
					FirstTabLocationTextBlock.Text = "FirstTabView";
					return;
				}
				if (element == SecondTabView)
				{
					FirstTabLocationTextBlock.Text = "SecondTabView";
					return;
				}

				element = VisualTreeHelper.GetParent(element) as FrameworkElement;
			}

			FirstTabLocationTextBlock.Text = "";
		}

		private void TabWidthComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (Tabs != null)
			{
				switch (TabWidthComboBox.SelectedIndex)
				{
					case 0: Tabs.TabWidthMode = Microsoft.UI.Xaml.Controls.TabViewWidthMode.Equal; break;
					case 1: Tabs.TabWidthMode = Microsoft.UI.Xaml.Controls.TabViewWidthMode.SizeToContent; break;
					case 2: Tabs.TabWidthMode = Microsoft.UI.Xaml.Controls.TabViewWidthMode.Compact; break;
				}
			}
		}

		private void CloseButtonOverlayModeCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (Tabs != null)
			{
				switch (CloseButtonOverlayModeCombobox.SelectedIndex)
				{
					case 0: Tabs.CloseButtonOverlayMode = Microsoft.UI.Xaml.Controls.TabViewCloseButtonOverlayMode.Auto; break;
					case 1: Tabs.CloseButtonOverlayMode = Microsoft.UI.Xaml.Controls.TabViewCloseButtonOverlayMode.OnPointerOver; break;
					case 2: Tabs.CloseButtonOverlayMode = Microsoft.UI.Xaml.Controls.TabViewCloseButtonOverlayMode.Always; break;
				}
			}
		}

		private void TabViewSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			SelectedIndexTextBlock.Text = Tabs.SelectedIndex.ToString();
		}

		private void TabViewTabCloseRequested(object sender, Microsoft.UI.Xaml.Controls.TabViewTabCloseRequestedEventArgs e)
		{
			if ((bool)HandleTabCloseRequestedCheckBox.IsChecked)
			{
				Tabs.TabItems.Remove(e.Tab);
			}
		}

		private void FirstTab_CloseRequested(object sender, Microsoft.UI.Xaml.Controls.TabViewTabCloseRequestedEventArgs e)
		{
			if ((bool)HandleTabItemCloseRequestedCheckBox.IsChecked)
			{
				Tabs.TabItems.Remove(e.Tab);
			}
		}

		private void TabViewTabDroppedOutside(object sender, Microsoft.UI.Xaml.Controls.TabViewTabDroppedOutsideEventArgs e)
		{
			TabViewItem tab = e.Tab;
			if (tab != null)
			{
				TabDroppedOutsideTextBlock.Text = tab.Header.ToString();
			}
		}

		// Drag/drop stuff

		private const string DataIdentifier = "MyTabItem";
		private const string DataTabView = "MyTabView";

		private TabViewItem FindTabViewItemFromContent(TabView tabView, object content)
		{
			var numItems = tabView.TabItems.Count;
			for (int i = 0; i < numItems; i++)
			{
				var tabItem = tabView.ContainerFromIndex(i) as TabViewItem;
				if (tabItem.Content == content)
				{
					return tabItem;
				}
			}
			return null;
		}

		private void OnTabDragStarting(object sender, TabViewTabDragStartingEventArgs e)
		{
			// Set the drag data to the tab
			e.Data.Properties.Add(DataIdentifier, e.Tab);
			e.Data.Properties.Add(DataTabView, sender as TabView);

			// And indicate that we can move it 
			e.Data.RequestedOperation = DataPackageOperation.Move;
		}

		private void OnTabStripDragOver(object sender, Microsoft.UI.Xaml.DragEventArgs e)
		{
			if (e.DataView.Properties.ContainsKey(DataIdentifier))
			{
				e.AcceptedOperation = DataPackageOperation.Move;
			}
		}

		private void OnTabStripDrop(object sender, Microsoft.UI.Xaml.DragEventArgs e)
		{
			// This event is called when we're dragging between different TabViews
			// It is responsible for handling the drop of the item into the second TabView

			object obj = null;
			object objOriginTabView = null;
			if (e.DataView.Properties.TryGetValue(DataIdentifier, out obj) &&
				e.DataView.Properties.TryGetValue(DataTabView, out objOriginTabView))
			{
				// TODO - BUG: obj should never be null, but occassionally is. Why?
				if (obj == null || objOriginTabView == null)
				{
					return;
				}

				var originTabView = objOriginTabView as TabView;
				var destinationTabView = sender as TabView;
				var destinationItems = destinationTabView.TabItems;
				var tabViewItem = obj as TabViewItem;

				if (destinationItems != null)
				{
					// First we need to get the position in the List to drop to
					var index = -1;

					// Determine which items in the list our pointer is inbetween.
					for (int i = 0; i < destinationTabView.TabItems.Count; i++)
					{
						var item = destinationTabView.ContainerFromIndex(i) as TabViewItem;

						if (e.GetPosition(item).X - item.ActualWidth < 0)
						{
							index = i;
							break;
						}
					}

					// Remove item from the old TabView
					originTabView.TabItems.Remove(tabViewItem);

					if (index < 0)
					{
						// We didn't find a transition point, so we're at the end of the list
						destinationItems.Add(tabViewItem);
					}
					else if (index < destinationTabView.TabItems.Count)
					{
						// Otherwise, insert at the provided index.
						destinationItems.Insert(index, tabViewItem);
					}

					// Select the newly dragged tab
					destinationTabView.SelectedItem = tabViewItem;
				}
			}
		}

		public void SetTabViewWidth_Click(object sender, RoutedEventArgs e)
		{
			// This is the smallest width that fits our content without any scrolling.
			Tabs.Width = 752;
		}

		public void GetScrollButtonsVisible_Click(object sender, RoutedEventArgs e)
		{
			var scrollDecrease = VisualTreeUtils.FindVisualChildByName(Tabs, "ScrollDecreaseButtonContainer") as FrameworkElement;
			var scrollIncrease = VisualTreeUtils.FindVisualChildByName(Tabs, "ScrollIncreaseButtonContainer") as FrameworkElement;
			if (scrollDecrease.Visibility == Visibility.Visible && scrollIncrease.Visibility == Visibility.Visible)
			{
				ScrollButtonsVisible.Text = "True";
			}
			else if (scrollIncrease.Visibility == Visibility.Collapsed && scrollDecrease.Visibility == Visibility.Collapsed)
			{
				ScrollButtonsVisible.Text = "False";
			}
			else
			{
				ScrollButtonsVisible.Text = "Unexpected";
			}
		}

		public void TabViewScrollToTheLeftButton_Click(object sender, RoutedEventArgs e)
		{
			var scrollViewer = VisualTreeUtils.FindVisualChildByName(Tabs, "ScrollViewer") as ScrollViewer;
			scrollViewer.ChangeView(0, null, null, true);
		}

		public void TabViewScrollToTheMiddleButton_Click(object sender, RoutedEventArgs e)
		{
			var scrollViewer = VisualTreeUtils.FindVisualChildByName(Tabs, "ScrollViewer") as ScrollViewer;
			scrollViewer.ChangeView(scrollViewer.ScrollableWidth / 2.0f, null, null, true);
		}

		public void TabViewScrollToTheRightButton_Click(object sender, RoutedEventArgs e)
		{
			var scrollViewer = VisualTreeUtils.FindVisualChildByName(Tabs, "ScrollViewer") as ScrollViewer;
			scrollViewer.ChangeView(double.MaxValue, null, null, true);
		}

		public void GetScrollDecreaseButtonEnabled_Click(object sender, RoutedEventArgs e)
		{
			var scrollDecreaseButton = VisualTreeUtils.FindVisualChildByName(Tabs, "ScrollDecreaseButton") as RepeatButton;
			ScrollDecreaseButtonEnabled.Text = scrollDecreaseButton.IsEnabled ? "True" : "False";
		}

		public void GetScrollIncreaseButtonEnabled_Click(object sender, RoutedEventArgs e)
		{
			var scrollIncreaseButton = VisualTreeUtils.FindVisualChildByName(Tabs, "ScrollIncreaseButton") as RepeatButton;
			ScrollIncreaseButtonEnabled.Text = scrollIncreaseButton.IsEnabled ? "True" : "False";
		}

		private void TabViewSizingPageButton_Click(object sender, RoutedEventArgs e)
		{
			this.Frame.Navigate(typeof(TabViewSizingPage));
		}

		private void TabViewTabClosingBehaviorButton_Click(object sender, RoutedEventArgs e)
		{
			this.Frame.Navigate(typeof(TabViewTabClosingBehaviorPage));
		}

		private void TabViewTabItemsSourcePageButton_Click(object sender, RoutedEventArgs e)
		{
			this.Frame.Navigate(typeof(TabViewTabItemsSourcePage));
		}

		private void ShortLongTextButton_Click(object sender, RoutedEventArgs e)
		{
			FirstTab.Header = "s";
			LongHeaderTab.Header = "long long long long long long long long";
		}

		private void HomeTabOverlapCheck_Click(object sender, RoutedEventArgs e)
		{
			var redBrush = new SolidColorBrush();
			redBrush.Color = Colors.Red;
			BackgroundGrid.Background = redBrush;

			var tabBrush = new SolidColorBrush();
			tabBrush.Color = Colors.Blue;
			SetFirstTabSelectedBackgroundPathFill(tabBrush);

			var contentBrush = new SolidColorBrush();
			contentBrush.Color = Colors.Green;
			FirstTabContent.Background = contentBrush;
		}

		private void SetActiveTabTransparent_Click(object sender, RoutedEventArgs e)
		{
			var tabBrush = new SolidColorBrush();
			tabBrush.Color = Colors.Transparent;
			SetFirstTabSelectedBackgroundPathFill(tabBrush);
		}

		private void SetActiveContentTransparent_Click(object sender, RoutedEventArgs e)
		{
			var contentBrush = new SolidColorBrush();
			contentBrush.Color = Colors.Transparent;
			FirstTabContent.Background = contentBrush;
		}

		private void ClearOverlapCheck_Click(object sender, RoutedEventArgs e)
		{
			BackgroundGrid.Background = backgroundColorCache;

			if (activeTabSelectedBackgroundPathBrushCache != null)
			{
				FrameworkElement selectedBackgroundPath = FindFrameworkElementWithName("SelectedBackgroundPath", FirstTab);
				if (selectedBackgroundPath != null)
				{
					(selectedBackgroundPath as Path).Fill = activeTabSelectedBackgroundPathBrushCache;
				}
			}

			if (activeTabContentBackgroundBrushCache != null)
			{
				FirstTabContent.Background = activeTabContentBackgroundBrushCache;
			}
		}

		private void CacheFirstTabSelectedBackgroundPathFill()
		{
			FrameworkElement selectedBackgroundPath = FindFrameworkElementWithName("SelectedBackgroundPath", FirstTab);
			if (selectedBackgroundPath != null)
			{
				activeTabSelectedBackgroundPathBrushCache = (selectedBackgroundPath as Path).Fill;
			}
		}

		private void SetFirstTabSelectedBackgroundPathFill(Brush newBrush)
		{
			FrameworkElement selectedBackgroundPath = FindFrameworkElementWithName("SelectedBackgroundPath", FirstTab);
			if (selectedBackgroundPath != null)
			{
				(selectedBackgroundPath as Path).Fill = newBrush;
			}
		}

		private FrameworkElement FindFrameworkElementWithName(string name, DependencyObject startNode)
		{
			int count = VisualTreeHelper.GetChildrenCount(startNode);
			for (int i = 0; i < count; i++)
			{
				DependencyObject current = VisualTreeHelper.GetChild(startNode, i);
				if ((current.GetType()).Equals(typeof(FrameworkElement)) || (current.GetType().GetTypeInfo().IsSubclassOf(typeof(FrameworkElement))))
				{
					FrameworkElement fe = (FrameworkElement)current;
					if (fe.Name == name)
					{
						return fe;
					}
				}
				var result = FindFrameworkElementWithName(name, current);
				if (result != null)
				{
					return result;
				}
			}
			return null;
		}

		private void SetColorsButton_Click(object sender, RoutedEventArgs e)
		{
			var foregroundBrush = new SolidColorBrush();
			foregroundBrush.Color = Colors.Blue;
			SecondTab.Foreground = foregroundBrush;
			var backgroundBrush = new SolidColorBrush();
			backgroundBrush.Color = Colors.Purple;
			SecondTab.Background = backgroundBrush;
		}

		private void ClearColorsButton_Click(object sender, RoutedEventArgs e)
		{
			SecondTab.ClearValue(ForegroundProperty);
			SecondTab.ClearValue(BackgroundProperty);
		}

		private void GetScrollDecreaseButtonToolTipButton_Click(object sender, RoutedEventArgs e)
		{
			if (VisualTreeUtils.FindVisualChildByName(Tabs, "ScrollDecreaseButton") is RepeatButton scrollDecreaseButton)
			{
				GetToolTipStringForUIElement(scrollDecreaseButton, ScrollDecreaseButtonToolTipTextBlock);
			}
		}

		private void GetScrollIncreaseButtonToolTipButton_Click(object sender, RoutedEventArgs e)
		{
			if (VisualTreeUtils.FindVisualChildByName(Tabs, "ScrollIncreaseButton") is RepeatButton scrollIncreaseButton)
			{
				GetToolTipStringForUIElement(scrollIncreaseButton, ScrollIncreaseButtonToolTipTextBlock);
			}
		}
	}
}
