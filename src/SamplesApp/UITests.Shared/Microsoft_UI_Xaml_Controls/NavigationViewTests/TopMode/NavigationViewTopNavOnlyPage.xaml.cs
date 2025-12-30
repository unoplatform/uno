// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel.Core;
using Windows.UI.ViewManagement;
using Windows.UI;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System.Text;
using System;

using NavigationViewPaneDisplayMode = Microsoft.UI.Xaml.Controls.NavigationViewPaneDisplayMode;
using NavigationViewSelectionChangedEventArgs = Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs;
using NavigationView = Microsoft.UI.Xaml.Controls.NavigationView;
using NavigationViewItem = Microsoft.UI.Xaml.Controls.NavigationViewItem;
using NavigationViewItemHeader = Microsoft.UI.Xaml.Controls.NavigationViewItemHeader;
using NavigationViewItemInvokedEventArgs = Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs;
using NavigationViewOverflowLabelMode = Microsoft.UI.Xaml.Controls.NavigationViewOverflowLabelMode;
using NavigationViewBackButtonVisible = Microsoft.UI.Xaml.Controls.NavigationViewBackButtonVisible;
using Uno.UI.Samples.Controls;
using MUXControlsTestApp.Utilities;

namespace MUXControlsTestApp
{
	[Sample("NavigationView", "MUX")]
	public sealed partial class NavigationViewTopNavOnlyPage : TestPage
	{
		int m_numberOfItemAdded = 0;
		private TextBlock contentOverlay = null;
		bool m_expectNullSelectedItemInNextInvoke = false;

		public NavigationViewTopNavOnlyPage()
		{
			this.InitializeComponent();

			//TODO: Add to uno
			//var testFrame = Window.Current.Content as TestFrame;
			//testFrame.ChangeBarVisibility(Visibility.Visible);

			NavView.ItemInvoked += NavView_ItemInvoked;
			NavView.SelectionChanged += NavView_SelectionChanged;

			var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;

			//remove the solid-colored backgrounds behind the caption controls and system back button
			var viewTitleBar = ApplicationView.GetForCurrentView().TitleBar;
			viewTitleBar.ButtonBackgroundColor = Colors.Transparent;
			viewTitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
			viewTitleBar.ButtonForegroundColor = (Color)Resources["SystemBaseHighColor"];

			Loaded += (s, e) => XamlRoot.Changed += (s, e) => UpdateAppTitle();
			coreTitleBar.LayoutMetricsChanged += (s, e) => UpdateAppTitle();
		}

		private void PaneDisplayModeCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var tag = Convert.ToString(((sender as ComboBox).SelectedItem as ComboBoxItem).Tag);
			var mode = Enum.Parse<NavigationViewPaneDisplayMode>(tag);
			NavView.PaneDisplayMode = mode;
		}

		private void FlipOrientation_Click(object sender, RoutedEventArgs e)
		{
			NavView.PaneDisplayMode = NavView.PaneDisplayMode == NavigationViewPaneDisplayMode.Top ? NavigationViewPaneDisplayMode.Auto : NavigationViewPaneDisplayMode.Top;
		}

		private void ResetResult_Click(object sender, RoutedEventArgs e)
		{
			ItemInvokedResult.Text = "";
			SelectionChangedResult.Text = "";
			SelectionChangeRecommendedTransition.Text = "";
			InvokeRecommendedTransition.Text = "";
		}

		private void ChangeDataSource_Click(object sender, RoutedEventArgs e)
		{
			NavView.MenuItems.Clear();
			NavView.MenuItems.Add(new NavigationViewItem { Content = "First Item" });
			NavView.MenuItems.Add(new NavigationViewItem { Content = "Second Item" });
			NavView.MenuItems.Add(new NavigationViewItem { Content = "Happy birthday Item" });
			NavView.MenuItems.Add(new NavigationViewItem { Content = "Happy new year Item" });
			NavView.MenuItems.Add(new NavigationViewItem { Content = "OK Item" });
			NavView.SelectedItem = NavView.MenuItems[3];
		}

		// Click and SelectionChange would have an container. This function verify that Content == container.Content
		// If content is null, Container.tag is returned.
		private string GetAndVerifyTheContainer(object content, NavigationViewItem container)
		{
			if (content == null)
			{
				if (container.Content != null)
				{
					return "container's content should be null but it is " + container.Content.ToString();
				}

				return container.Tag != null ? container.Tag.ToString() : "container should have tag property since content is null";
			}

			var contentString = content.ToString();
			var containerString = container.Content.ToString();

			if (contentString.Equals(containerString))
			{
				return contentString;
			}
			else
			{
				return string.Format("container's content:{0} doesn't match with content:{1}", containerString, contentString);
			}
		}

		private string RecommendedNavigationTransitionInfoToString(NavigationTransitionInfo info)
		{
			if ((info as EntranceNavigationTransitionInfo) != null)
			{
				return "Default";
			}
			var slideNav = (info as SlideNavigationTransitionInfo);
			if (slideNav != null)
			{
				try
				{
					return slideNav.Effect.ToString();
				}
				catch (InvalidCastException)
				{
					return "You are running on old RS5 machine which doesn't support ISlideNavigationTransitionInfo2 yet";
				}
			}
			else
			{
				return "Error";
			}
		}

		private void BackButtonVisibilityCheckbox_Checked(object sender, RoutedEventArgs e)
		{
			NavView.IsBackButtonVisible = NavigationViewBackButtonVisible.Visible;
		}

		private void BackButtonVisibilityCheckbox_Unchecked(object sender, RoutedEventArgs e)
		{
			NavView.IsBackButtonVisible = NavigationViewBackButtonVisible.Collapsed;
		}

		private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs e)
		{
			var container = (e.SelectedItemContainer as NavigationViewItem);

			if (container != null && !e.IsSettingsSelected && !container.IsSelected)
			{
				SelectionChangedResult.Text = "Error, NavigationViewItem.IsSelected should be true before raise SelectionChanged event";
			}
			else
			{
				if (e.SelectedItem is NavigationViewItem item)
				{
					SelectionChangedResult.Text = GetAndVerifyTheContainer(item.Content, container);
				}
				else
				{
					SelectionChangedResult.Text = "Null";
				}
			}

			SelectionChangeRecommendedTransition.Text = RecommendedNavigationTransitionInfoToString(e.RecommendedNavigationTransitionInfo);
		}

		private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs e)
		{
			var container = (e.InvokedItemContainer as NavigationViewItem);
			ItemInvokedResult.Text = GetAndVerifyTheContainer(e.InvokedItem, container);
			InvokeRecommendedTransition.Text = RecommendedNavigationTransitionInfoToString(e.RecommendedNavigationTransitionInfo);

			if (m_expectNullSelectedItemInNextInvoke)
			{
				NavView.SelectedItem = null;
				m_expectNullSelectedItemInNextInvoke = false;
			}
		}

		private void ChangeGamesContent_Click(object sender, RoutedEventArgs e)
		{
			GamesItem.Content = "Games AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
		}

		private void NavInvalidateMeasure_Click(object sender, RoutedEventArgs e)
		{
			NavView.InvalidateMeasure();
		}

		private void ClearNavItemContent_Click(object sender, RoutedEventArgs e)
		{
			for (int i = NavView.MenuItems.Count - 1; i >= 0; i--)
			{
				var item = NavView.MenuItems[i];
				var container = item as NavigationViewItem;
				if (container != null)
				{
					container.Content = null;
				}

				var navHeader = item as NavigationViewItemHeader;
				if (navHeader != null)
				{
					navHeader.Content = null;
				}
			}
		}

		private void AddLongNavItem_Click(object sender, RoutedEventArgs e)
		{
			var content = "Bill".PadRight(100, '.') + "Gates";
			NavView.MenuItems.Add(new NavigationViewItem() { Content = content });
		}

		private void AddTenItems_Click(object sender, RoutedEventArgs e)
		{
			var menuItems = NavView.MenuItems;
			for (int i = 0; i < 10; i++)
			{
				m_numberOfItemAdded++;
				var item = new NavigationViewItem() { Content = "Added Item " + m_numberOfItemAdded };
				menuItems.Add(item);
			}
		}
		private void ExpectNullSelectedItemInItemInvoke_Click(object sender, RoutedEventArgs e)
		{
			m_expectNullSelectedItemInNextInvoke = true;
		}

		private void FlipExtendViewIntoTitleBar_Click(object sender, RoutedEventArgs e)
		{
			var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
			coreTitleBar.ExtendViewIntoTitleBar = !coreTitleBar.ExtendViewIntoTitleBar;
		}

		private void UpdateAppTitle()
		{
			var full = (ApplicationView.GetForCurrentView().IsFullScreenMode);
			var left = 12 + (full ? 0 : CoreApplication.GetCurrentView().TitleBar.SystemOverlayLeftInset);
			AppTitle.Margin = new Thickness(left, 8, 0, 0);
		}

		private void TestFrameCheckbox_Checked(object sender, RoutedEventArgs e)
		{
			//TODO: Add to uno
			//var testFrame = Window.Current.Content as TestFrame;
			//testFrame.ChangeBarVisibility(Visibility.Visible);
		}

		private void TestFrameCheckbox_Unchecked(object sender, RoutedEventArgs e)
		{
			//TODO: Add to uno
			//var testFrame = Window.Current.Content as TestFrame;
			//testFrame.ChangeBarVisibility(Visibility.Collapsed);
		}
		private void ChangeOverflowLabelVisibility_Checked(object sender, RoutedEventArgs e)
		{
			NavView.OverflowLabelMode = NavigationViewOverflowLabelMode.MoreLabel;
		}

		private void ChangeOverflowLabelVisibility_Unchecked(object sender, RoutedEventArgs e)
		{
			NavView.OverflowLabelMode = NavigationViewOverflowLabelMode.NoLabel;
		}
		private void GetActiveVisualState_Click(object sender, RoutedEventArgs e)
		{
			TestResult.Text = Utilities.VisualStateHelper.GetCurrentVisualStateNameString(NavView);
		}

		private void AddRemoveContentOverlay_Click(object sender, RoutedEventArgs e)
		{
			contentOverlay = (contentOverlay == null ? new TextBlock() { Text = "CONTENT OVERLAY" } : null);
			NavView.ContentOverlay = contentOverlay;
		}

		private void ChangeTopNavVisibility_Click(object sender, RoutedEventArgs e)
		{
			NavView.IsPaneVisible = !NavView.IsPaneVisible;
		}

		private void SetInvalidSelectedItem_Click(object sender, RoutedEventArgs e)
		{
			NavView.SelectedItem = new CheckBox();
		}

		private void ShoulderNavigationEnabledSetter_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			string selectedItem = (e.AddedItems[0] as ComboBoxItem).Content as string;

			switch (selectedItem)
			{
				case "Always":
					NavView.ShoulderNavigationEnabled = Microsoft.UI.Xaml.Controls.NavigationViewShoulderNavigationEnabled.Always;
					break;
				case "WhenSelectionFollowsFocus":
					NavView.ShoulderNavigationEnabled = Microsoft.UI.Xaml.Controls.NavigationViewShoulderNavigationEnabled.WhenSelectionFollowsFocus;
					break;
				case "Never":
					NavView.ShoulderNavigationEnabled = Microsoft.UI.Xaml.Controls.NavigationViewShoulderNavigationEnabled.Never;
					break;
			}
		}
		private void SelectionFollowsFocusSetter_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			string selectedItem = (e.AddedItems[0] as ComboBoxItem).Content as string;

			if (selectedItem == "Enabled")
			{
				NavView.SelectionFollowsFocus = Microsoft.UI.Xaml.Controls.NavigationViewSelectionFollowsFocus.Enabled;
			}
			else
			{
				NavView.SelectionFollowsFocus = Microsoft.UI.Xaml.Controls.NavigationViewSelectionFollowsFocus.Disabled;

			}
		}
	}
}
