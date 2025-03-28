// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using System.Text;
using Common;
using Windows.UI.Xaml.Automation;

using NavigationViewPaneDisplayMode = Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationViewPaneDisplayMode;
//using MaterialHelperTestApi = Microsoft.UI.Private.Media.MaterialHelperTestApi;
using NavigationViewItem = Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationViewItem;
using NavigationViewItemSeparator = Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationViewItemSeparator;
using NavigationViewItemBase = Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationViewItemBase;
using NavigationViewSelectionFollowsFocus = Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationViewSelectionFollowsFocus;
using NavigationViewItemInvokedEventArgs = Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs;
using Uno.UI.Samples.Controls;
using MUXControlsTestApp.Utilities;

namespace MUXControlsTestApp
{
	//[Sample("NavigationView", "MUX")]
	public sealed partial class NavigationViewTopNavStorePage : TestPage
	{
		ObservableCollection<NavigationViewItemBase> m_menuItems;
		private TextBlock contentOverlay = null;
		private int currentSelectedItem = 0;
		private int suppressSelectionNextNumber = 0;

		public NavigationViewTopNavStorePage()
		{
			this.InitializeComponent();

			if (PlatformConfiguration.IsOsVersionGreaterThan(OSVersion.Redstone3))
			{
				App.Current.FocusVisualKind = FocusVisualKind.Reveal;
			}

			//MaterialHelperTestApi.IgnoreAreEffectsFast = true;
			//MaterialHelperTestApi.SimulateDisabledByPolicy = false;

			m_menuItems = new ObservableCollection<NavigationViewItemBase>();

			m_menuItems.Add(new NavigationViewItem() { Content = "Menu Item 1" });
			m_menuItems.Add(new NavigationViewItem() { Content = "Menu Item 2" });
			m_menuItems.Add(new NavigationViewItem() { Content = "Menu Item 3" });
			m_menuItems.Add(new NavigationViewItemSeparator());
			m_menuItems.Add(new NavigationViewItem() { Content = "Menu Item 4" });

			NavView.MenuItemsSource = m_menuItems;
			NavView.SelectedItem = m_menuItems[currentSelectedItem];

			NavViewIsTitleBarAutoPaddingEnabled.Text = NavView.IsTitleBarAutoPaddingEnabled.ToString();
		}

		protected
#if HAS_UNO
			internal
#endif
			override void OnNavigatedFrom(NavigationEventArgs e)
		{
			// Unset all override flags to avoid impacting subsequent tests
			//MaterialHelperTestApi.IgnoreAreEffectsFast = false;
			//MaterialHelperTestApi.SimulateDisabledByPolicy = false;
			base.OnNavigatedFrom(e);
		}

		private void AddButton_Click(object sender, RoutedEventArgs e)
		{
			m_menuItems.Add(new NavigationViewItem() { Content = "New Menu Item" });
		}

		private void AddButtonSuppressSelection_Click(object sender, RoutedEventArgs e)
		{
			var newItem = new NavigationViewItem() { Content = "New Menu Item S.S", SelectsOnInvoked = false };
			AutomationProperties.SetAutomationId(newItem, "sup-selection-nav-item-" + suppressSelectionNextNumber);
			m_menuItems.Add(newItem);
			suppressSelectionNextNumber++;
		}

		private void RemoveButton_Click(object sender, RoutedEventArgs e)
		{
			if (m_menuItems.Count > 0)
			{
				m_menuItems.RemoveAt(m_menuItems.Count - 1);
			}
		}

		private void ChangeToIEnumerableButton_Clicks(object sender, RoutedEventArgs e)
		{
			var newMenuItems = new LinkedList<string>();
			newMenuItems.AddLast("IIterator/Enumerable/LinkedList Item 1");
			newMenuItems.AddLast("IIterator/Enumerable/LinkedList Item 2");
			newMenuItems.AddLast("IIterator/Enumerable/LinkedList Item 3");

			NavView.MenuItemsSource = newMenuItems;
		}

		private void FlipIsTitleBarAutoPaddingEnabledButton_Click(object sender, RoutedEventArgs e)
		{
			NavView.IsTitleBarAutoPaddingEnabled = !NavView.IsTitleBarAutoPaddingEnabled;
			NavViewIsTitleBarAutoPaddingEnabled.Text = NavView.IsTitleBarAutoPaddingEnabled.ToString();
		}

		private void FlipOrientation_Click(object sender, RoutedEventArgs e)
		{
			NavView.PaneDisplayMode = NavView.PaneDisplayMode == NavigationViewPaneDisplayMode.Top ? NavigationViewPaneDisplayMode.Auto : NavigationViewPaneDisplayMode.Top;
		}

		private void AppBarButton_Click(object sender, RoutedEventArgs e)
		{
		}

		private void ChangeTopNavVisibility_Click(object sender, RoutedEventArgs e)
		{
			NavView.IsPaneVisible = !NavView.IsPaneVisible;
		}

		private void AddRemoveContentOverlay_Click(object sender, RoutedEventArgs e)
		{
			contentOverlay = contentOverlay == null ? new TextBlock() { Text = "CONTENT OVERLAY" } : null;
			NavView.ContentOverlay = contentOverlay;
		}

		private void ChangeContentOverlayVisibility_Click(object sender, RoutedEventArgs e)
		{
			if (contentOverlay != null)
			{
				contentOverlay.Visibility = contentOverlay.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
			}
		}

		private void MoveContentUnderTopnav_Click(object sender, RoutedEventArgs e)
		{
			var topNavArea = FindVisualChildByName(NavView, "TopNavArea") as FrameworkElement;
			var topNavGrid = FindVisualChildByName(NavView, "TopNavGrid") as FrameworkElement;
			var topNavHeight = topNavGrid.Height;
			CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = false;

			if (ContentScrollViewer.ActualHeight == 200)
			{
				Grid.SetRow(topNavArea, 0);
				ContentScrollViewer.Height = double.NaN;
				ContentStackPanel.Padding = new Thickness(0);
				ContentScrollViewer.Margin = new Thickness(0);
				ContentScrollViewer.Padding = new Thickness(0);
			}
			else
			{
				Grid.SetRow(topNavArea, 1);
				ContentScrollViewer.Height = 200;
				ContentStackPanel.Padding = new Thickness(0, topNavHeight, 0, 0);
				ContentScrollViewer.Margin = new Thickness(0, topNavHeight, 0, 0);
				ContentScrollViewer.Padding = new Thickness(0, -topNavHeight, 0, 0);
			}
		}

		private void MoveContentUnderTopnavTitleBar_Click(object sender, RoutedEventArgs e)
		{
			var topNavArea = FindVisualChildByName(NavView, "TopNavArea") as FrameworkElement;
			var topNavGrid = FindVisualChildByName(NavView, "TopNavGrid") as FrameworkElement;
			var topNavHeight = topNavGrid.Height + 33;

			//TODO: Add to uno
			//var testFrame = Window.Current.Content as TestFrame;

			if (CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar)
			{
				//AppTitleBar
				//testFrame.ChangeBarVisibility(Visibility.Visible);
				CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = false;

				// Reset values
				Grid.SetRow(topNavArea, 0);
				ContentScrollViewer.Height = double.NaN;
				ContentStackPanel.Padding = new Thickness(0);
				ContentScrollViewer.Margin = new Thickness(0);
				ContentScrollViewer.Padding = new Thickness(0);
			}
			else
			{
				//AppTitleBar
				//testFrame.ChangeBarVisibility(Visibility.Collapsed);
				CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;

				Grid.SetRow(topNavArea, 1);
				ContentScrollViewer.Height = 200;
				ContentStackPanel.Padding = new Thickness(0, topNavHeight, 0, 0);
				ContentScrollViewer.Margin = new Thickness(0, topNavHeight, 0, 0);
				ContentScrollViewer.Padding = new Thickness(0, -topNavHeight, 0, 0);
			}
		}

		private void ChangeSelectionInCode_Click(object sender, RoutedEventArgs e)
		{
			currentSelectedItem++;
			if (currentSelectedItem == m_menuItems.Count)
			{
				currentSelectedItem = 0;
			}

			NavView.SelectedItem = m_menuItems[currentSelectedItem];
		}

		private void GetTopPaddingHeightButton_Click(object sender, RoutedEventArgs e)
		{
			Grid rootGrid = VisualTreeHelper.GetChild(NavView, 0) as Grid;
			if (rootGrid != null)
			{
				Grid paneContentGrid = rootGrid.FindName("TopNavTopPadding") as Grid;
				TopPaddingRenderedValue.Text = paneContentGrid.ActualHeight.ToString();
			}

			if (CoreApplication.GetCurrentView().TitleBar.IsVisible)
			{
				TitleBarIsVisible.Text = "True";
			}
			else
			{
				TitleBarIsVisible.Text = "False";
			}
		}

		private void EnableSelectionFollowsFocus_Click(object sender, RoutedEventArgs e)
		{
			if (NavView.SelectionFollowsFocus == NavigationViewSelectionFollowsFocus.Disabled)
			{
				NavView.SelectionFollowsFocus = NavigationViewSelectionFollowsFocus.Enabled;
			}
			else
			{
				NavView.SelectionFollowsFocus = NavigationViewSelectionFollowsFocus.Disabled;
			}
			EnableSelectionFollowsFocusButton.Content = NavView.SelectionFollowsFocus == NavigationViewSelectionFollowsFocus.Enabled ? "Disable Selection Follows Focus" : "Enable Selection Follows Focus";
		}

		private void GetActiveVisualState_Click(object sender, RoutedEventArgs e)
		{
			ActiveVisualStates.Text = Utilities.VisualStateHelper.GetCurrentVisualStateNameString(NavView);
		}

		private void GetNavItemActiveNavItemVisualState_Click(object sender, RoutedEventArgs e)
		{
			NavItemActiveVisualStates.Text = Utilities.VisualStateHelper.GetCurrentVisualStateNameString(NavView.SelectedItem as FrameworkElement);
		}

		private void ClearItemInvokedTextButton_Click(object sender, RoutedEventArgs e)
		{
			ItemInvokedText.Text = string.Empty;
		}

		private void OnNavItemInvoked(object sender, NavigationViewItemInvokedEventArgs e)
		{
			ItemInvokedText.Text = e.InvokedItem as string;
		}
	}
}
