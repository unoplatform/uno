// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX reference NavigationViewPage.xaml, commit 426e54f

#pragma warning disable 105 // Disabled until the tree is migrate to WinUI

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Automation;
using Windows.ApplicationModel.Core;

using NavigationViewDisplayMode = Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode;
using NavigationView = Microsoft.UI.Xaml.Controls.NavigationView;
using NavigationViewSelectionChangedEventArgs = Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs;
using NavigationViewItemInvokedEventArgs = Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs;
using NavigationViewItem = Microsoft.UI.Xaml.Controls.NavigationViewItem;
using NavigationViewDisplayModeChangedEventArgs = Microsoft.UI.Xaml.Controls.NavigationViewDisplayModeChangedEventArgs;
using NavigationViewPaneClosingEventArgs = Microsoft.UI.Xaml.Controls.NavigationViewPaneClosingEventArgs;
using NavigationViewBackButtonVisible = Microsoft.UI.Xaml.Controls.NavigationViewBackButtonVisible;
using NavigationViewBackRequestedEventArgs = Microsoft.UI.Xaml.Controls.NavigationViewBackRequestedEventArgs;
using NavigationViewPaneDisplayMode = Microsoft.UI.Xaml.Controls.NavigationViewPaneDisplayMode;
//using MaterialHelperTestApi = Microsoft.UI.Private.Media.MaterialHelperTestApi;
using NavigationViewSelectionFollowsFocus = Microsoft.UI.Xaml.Controls.NavigationViewSelectionFollowsFocus;
using Microsoft.UI.Xaml.Controls;
using MUXControlsTestApp.Utilities;
using System.Collections.Generic;
using Uno.UI.Samples.Controls;

namespace MUXControlsTestApp
{
	[Sample("NavigationView", "MUX", IgnoreInSnapshotTests = true)]
	public sealed partial class NavigationViewPage : TestPage
	{
		private int m_newItemIndex = 0;
		private int m_newFooterItemIndex = 0;
		private int m_closingEventsFired = 0;
		private int m_closedEventsFired = 0;
		private bool m_useFocusVisualKindReveal = false;
		// FocusVisualKind impacts other testing, recover to setting in unload page
		private FocusVisualKind m_focusVisualKind;

		private int _selectionChangedEventsFired = 0;
		public int m_selectionChangedEventsFired
		{
			get
			{
				return _selectionChangedEventsFired;
			}
			set
			{
				_selectionChangedEventsFired = value;
				NumberOfSelectionChangedEventsRaisedTextBlock.Text = _selectionChangedEventsFired.ToString();
			}
		}

		private int _itemInvokedEventsFired = 0;
		public int m_itemInvokedEventsFired
		{
			get
			{
				return _itemInvokedEventsFired;
			}
			set
			{
				_itemInvokedEventsFired = value;
				NumberOfItemInvokedEventsRaisedTextBlock.Text = _itemInvokedEventsFired.ToString();
			}
		}


		public NavigationViewPage()
		{
			this.InitializeComponent();

			this.Loaded += NavigationViewPage_Loaded;
			this.Unloaded += NavigationViewPage_Unloaded;

			//MaterialHelperTestApi.IgnoreAreEffectsFast = true;
			//MaterialHelperTestApi.SimulateDisabledByPolicy = false;

			IntegerItem.Content = 7; // Testing that boxed ints don't cause crashes in tooltips
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

		private void NavigationViewPage_Loaded(object sender, RoutedEventArgs e)
		{
			m_focusVisualKind = Application.Current.FocusVisualKind;

			Grid rootGrid = VisualTreeHelper.GetChild(NavView, 0) as Grid;
			if (rootGrid != null)
			{
				Grid paneContentGrid = rootGrid.FindName("PaneContentGrid") as Grid;
				if (paneContentGrid != null)
				{
					Button toggleButton = paneContentGrid.FindName("TogglePaneButton") as Button;
					if (toggleButton != null)
					{
						toggleButton.RegisterPropertyChangedCallback(Button.BackgroundProperty, new DependencyPropertyChangedCallback(ToggleBackgroundChanged));
						UpdateToggleBackgroundColor(toggleButton);
					}
				}
			}

			SettingsItemVisibilityCheckbox.IsChecked = NavView.IsSettingsVisible;
			PaneToggleButtonVisiblityCheckbox.IsChecked = NavView.IsPaneToggleButtonVisible;
			HeaderVisiblityCheckbox.IsChecked = NavView.AlwaysShowHeader;

			NavView.SelectedItem = HomeItem;

			FindAndGiveAutomationNameToVisualChild("NavigationViewBackButton");
		}

		private void NavigationViewPage_Unloaded(object sender, RoutedEventArgs e)
		{
			Application.Current.FocusVisualKind = m_focusVisualKind;
		}

		private void ToggleBackgroundChanged(DependencyObject o, DependencyProperty p)
		{
			UpdateToggleBackgroundColor(o as Button);
		}

		private void UpdateToggleBackgroundColor(Button toggleButton)
		{
			if (toggleButton.Background != null && toggleButton.Background is SolidColorBrush)
			{
				ToggleButtonBackgroundColor.Text = (toggleButton.Background as SolidColorBrush).Color.ToString();
			}
			else
			{
				ToggleButtonBackgroundColor.Text = String.Empty;
			}
		}

		private void OpenPaneButton_Click(object sender, RoutedEventArgs e)
		{
			NavView.IsPaneOpen = true;
		}

		private void ChangeContent_Click(object sender, RoutedEventArgs e)
		{
			NavView.Content = new TextBox();
		}

		private void CompactModeCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var tag = Convert.ToDouble(((sender as ComboBox).SelectedItem as ComboBoxItem).Tag);
			NavView.CompactModeThresholdWidth = tag;
		}

		private void ExpandedModeCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var tag = Convert.ToDouble(((sender as ComboBox).SelectedItem as ComboBoxItem).Tag);
			NavView.ExpandedModeThresholdWidth = tag;
		}

		private void TestPage_Loaded(object sender, RoutedEventArgs e)
		{
		}

		private void PaneDisplayModeCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var tag = Convert.ToString(((sender as ComboBox).SelectedItem as ComboBoxItem).Tag);
			var mode = Enum.Parse<NavigationViewPaneDisplayMode>(tag);
			NavView.PaneDisplayMode = mode;
		}

		private void SelectedItemCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var tag = Convert.ToString(((sender as ComboBox).SelectedItem as ComboBoxItem).Tag);
			if (tag == "Home")
			{
				NavView.SelectedItem = HomeItem;
			}
			else if (tag == "Apps")
			{
				AppsItem.IsSelected = true;
			}
			else if (tag == "Games")
			{
				NavView.SelectedItem = GamesItem;
			}
			else if (tag == "Music")
			{
				NavView.SelectedItem = MusicItem;
			}
			else if (tag == "Movies")
			{
				NavView.SelectedItem = MoviesItem;
			}
			else if (tag == "TV")
			{
				NavView.SelectedItem = TVItem;
			}
			else if (tag == "Settings")
			{
				NavView.SelectedItem = NavView.SettingsItem;
			}
		}

		private void SelectedItemIsEnabledCheckbox_Checked(object sender, RoutedEventArgs e)
		{
			if (NavView.SelectedItem != null)
			{
				(NavView.SelectedItem as Control).IsEnabled = true;
			}
		}

		private void SelectedItemIsEnabledCheckbox_Unchecked(object sender, RoutedEventArgs e)
		{
			if (NavView.SelectedItem != null)
			{
				(NavView.SelectedItem as Control).IsEnabled = false;
			}
		}

		private void SettingsItemVisibilityCheckbox_Checked(object sender, RoutedEventArgs e)
		{
			NavView.IsSettingsVisible = true;
		}

		private void SettingsItemVisibilityCheckbox_Unchecked(object sender, RoutedEventArgs e)
		{
			NavView.IsSettingsVisible = false;
		}

		private void PaneToggleButtonVisiblityCheckbox_Checked(object sender, RoutedEventArgs e)
		{
			NavView.IsPaneToggleButtonVisible = true;
		}

		private void PaneToggleButtonVisiblityCheckbox_Unchecked(object sender, RoutedEventArgs e)
		{
			NavView.IsPaneToggleButtonVisible = false;
		}

		private void ToggleButtonStyleCheckbox_Checked(object sender, RoutedEventArgs e)
		{
			NavView.PaneToggleButtonStyle = this.Resources["AlternateToggleButtonStyle"] as Style;
		}

		private void ToggleButtonStyleCheckbox_Unchecked(object sender, RoutedEventArgs e)
		{
			NavView.PaneToggleButtonStyle = Application.Current.Resources["PaneToggleButtonStyle"] as Style;
		}

		private void CompactPaneLength_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var tag = Convert.ToDouble(((sender as ComboBox).SelectedItem as ComboBoxItem).Tag);
			NavView.CompactPaneLength = tag;
		}

		private void OpenPaneLength_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var tag = Convert.ToDouble(((sender as ComboBox).SelectedItem as ComboBoxItem).Tag);
			NavView.OpenPaneLength = tag;
		}

		private void NavView_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
		{
			switch (args.DisplayMode)
			{
				case NavigationViewDisplayMode.Minimal:
					DisplayModeTextBox.Text = "Minimal";
					DisplayModeTextBox.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DodgerBlue);
					break;
				case NavigationViewDisplayMode.Compact:
					DisplayModeTextBox.Text = "Compact";
					DisplayModeTextBox.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DarkMagenta);
					break;
				case NavigationViewDisplayMode.Expanded:
					DisplayModeTextBox.Text = "Expanded";
					DisplayModeTextBox.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DarkRed);
					break;
			}
		}

		private void HeaderVisiblityCheckbox_Checked(object sender, RoutedEventArgs e)
		{
			NavView.AlwaysShowHeader = true;
		}

		private void HeaderVisiblityCheckbox_Unchecked(object sender, RoutedEventArgs e)
		{
			NavView.AlwaysShowHeader = false;
		}

		private void FindAndGiveAutomationNameToVisualChild(string childName)
		{
			DependencyObject obj = FindVisualChildByName(this.NavView, childName);

			if (obj != null)
			{
				AutomationProperties.SetName(obj, childName);
			}
		}

		private void WidthCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var tag = Convert.ToDouble(((sender as ComboBox).SelectedItem as ComboBoxItem).Tag);
			NavView.Width = tag;
		}

		private void AddItemButton_Click(object sender, RoutedEventArgs e)
		{
			var menuItem = new NavigationViewItem();
			menuItem.Content = "New Menu Item " + m_newItemIndex.ToString();
			menuItem.Icon = new SymbolIcon(Symbol.AllApps);
			NavView.MenuItems.Add(menuItem);
			m_newItemIndex++;
		}

		private void RemoveItemButton_Click(object sender, RoutedEventArgs e)
		{
			if (NavView.MenuItems.Count > 0)
			{
				NavView.MenuItems.RemoveAt(NavView.MenuItems.Count - 1);
			}
		}

		private void AddFooterItemButton_Click(object sender, RoutedEventArgs e)
		{
			var menuItem = new NavigationViewItem();
			menuItem.Content = "New Footer Menu Item " + m_newFooterItemIndex.ToString();
			menuItem.Icon = new SymbolIcon(Symbol.AllApps);
			NavView.FooterMenuItems.Add(menuItem);
			m_newFooterItemIndex++;
		}

		private void RemoveFooterItemButton_Click(object sender, RoutedEventArgs e)
		{
			if (NavView.FooterMenuItems.Count > 0)
			{
				NavView.FooterMenuItems.RemoveAt(NavView.FooterMenuItems.Count - 1);
			}
		}

		private void ClearMenuButton_Click(object sender, RoutedEventArgs e)
		{
			NavView.MenuItems.Clear();
		}

		private void HeightCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var tag = Convert.ToString(((sender as ComboBox).SelectedItem as ComboBoxItem).Tag);
			if (tag == "Default")
			{
				MainGridRow.Height = new GridLength(1, GridUnitType.Star);
			}
			else if (tag == "300")
			{
				MainGridRow.Height = new GridLength(300, GridUnitType.Pixel);
			}
		}

		private void ResetEventCounters_Click(object sender, RoutedEventArgs e)
		{
			m_itemInvokedEventsFired = 0;
			m_selectionChangedEventsFired = 0;
		}

		private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
		{
			m_itemInvokedEventsFired++;

			// Reset argument type indicatiors
			ItemInvokedItemType.Text = "null";
			ItemInvokedItemContainerType.Text = "null";

			if (args.InvokedItem != null)
			{
				ItemInvokedItemType.Text = args.InvokedItem.GetType().ToString();
			}

			if (args.InvokedItemContainer != null)
			{
				ItemInvokedItemContainerType.Text = args.InvokedItemContainer.GetType().ToString();
			}

			var curSelected = sender.SelectedItem as NavigationViewItem;
			var text = args.InvokedItem as string;
			if (curSelected != null && (string)curSelected.Content == text)
			{
				if (InvokedItemState.Text == "ItemWasSelectedInItemInvoked")
				{
					InvokedItemState.Text = "ItemWasInvokedSecomdTimeWithCorrectSelection";
				}
				else
				{
					InvokedItemState.Text = "ItemWasSelectedInItemInvoked";
				}
			}
			else
			{
				InvokedItemState.Text = "Error:ItemUnselectedInItemInvoked";
			}
		}

		private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
		{
			SelectionChangedRaised.Text = "True";
			// Reset argument type indicatiors
			SelectionChangedItemType.Text = "null";
			SelectionChangedItemContainerType.Text = "null";

			m_selectionChangedEventsFired++;
			if (args.SelectedItem != null)
			{
				SelectionChangedItemType.Text = args.SelectedItem.GetType().ToString();

				var itemdata = args.SelectedItem as NavigationViewItem;
				if (itemdata != null)
				{
					if (itemdata.Content != null)
					{
						NavView.Header = itemdata.Content + " as header";
					}
					else if (args.IsSettingsSelected) // to handle settings without content case in top nav
					{
						NavView.Header = "Settings as header";
					}
				}
			}

			if (args.SelectedItemContainer != null)
			{
				SelectionChangedItemContainerType.Text = args.SelectedItemContainer.GetType().ToString();
			}

			if (NavView.SelectedItem == null)
			{
				SelectedItemIsEnabledCheckbox.IsChecked = null;
			}
			else
			{
				SelectedItemIsEnabledCheckbox.IsChecked = (NavView.SelectedItem as Control).IsEnabled;
			}
		}

		private void ClearSelectionChangeBlock(object sender, RoutedEventArgs e)
		{
			SelectionChangedRaised.Text = "False";
		}

		private void MoviesEnabledCheckbox_Checked(object sender, RoutedEventArgs e)
		{
			MoviesItem.IsEnabled = true;
			TVItem.IsEnabled = true;
		}

		private void MoviesEnabledCheckbox_Unchecked(object sender, RoutedEventArgs e)
		{
			MoviesItem.IsEnabled = false;
			TVItem.IsEnabled = false;
		}

		private void IsTitleBarAutoPaddingEnabledCheckbox_Checked(object sender, RoutedEventArgs e)
		{
			NavView.IsTitleBarAutoPaddingEnabled = true;
		}

		private void IsTitleBarAutoPaddingEnabledCheckbox_Unchecked(object sender, RoutedEventArgs e)
		{
			NavView.IsTitleBarAutoPaddingEnabled = false;
		}

		private void TitleBarCheckbox_Checked(object sender, RoutedEventArgs e)
		{
			CoreApplicationViewTitleBar titleBar = CoreApplication.GetCurrentView().TitleBar;
			titleBar.ExtendViewIntoTitleBar = false;
		}

		private void TitleBarCheckbox_Unchecked(object sender, RoutedEventArgs e)
		{
			CoreApplicationViewTitleBar titleBar = CoreApplication.GetCurrentView().TitleBar;
			titleBar.ExtendViewIntoTitleBar = true;
		}

		private void TestFrameCheckbox_Checked(object sender, RoutedEventArgs e)
		{
			//TODO: Add to uno
			//var testFrame = Window.Current.Content as TestFrame;
			//         testFrame.ChangeBarVisibility(Visibility.Visible);
		}

		private void TestFrameCheckbox_Unchecked(object sender, RoutedEventArgs e)
		{
			//TODO: Add to uno
			//var testFrame = Window.Current.Content as TestFrame;
			//testFrame.ChangeBarVisibility(Visibility.Collapsed);
		}

		private void AutoSuggestCheckbox_Checked(object sender, RoutedEventArgs e)
		{
			// recreate AutoSuggestBox
			var autoSuggestBox = new AutoSuggestBox();
			AutomationProperties.SetName(autoSuggestBox, "PaneAutoSuggestBox");
			NavView.AutoSuggestBox = autoSuggestBox;
		}

		private void AutoSuggestCheckbox_Unchecked(object sender, RoutedEventArgs e)
		{
			NavView.AutoSuggestBox = null;
		}

		private void ScrambleButton_Click(object sender, RoutedEventArgs e)
		{
			foreach (var entry in NavView.MenuItems)
			{
				NavigationViewItem item = entry as NavigationViewItem;

				if (item != null)
				{
					char[] newTitle = item.Content.ToString().ToCharArray();
					Array.Reverse(newTitle);
					item.Content = new string(newTitle);
				}
			}
		}

		private void CopyGamesLabelButton_Click(object sender, RoutedEventArgs e)
		{
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot);
			if (popups != null && popups.Count > 0)
			{
				var popup = popups[0];
				ToolTip toolTip = popup.Child as ToolTip;
				ToolTipStatusTextBlock.Text = toolTip.Content.ToString();
			}
			else
			{
				ToolTipStatusTextBlock.Text = "There are no popups";
			}
		}

		private void ClosingEventCountResetButton_Click(object sender, RoutedEventArgs e)
		{
			LastIngEventText.Text = "";
			LastEdEventText.Text = "";
			m_closingEventsFired = 0;
			m_closedEventsFired = 0;
			ClosingEventCountTextBlock.Text = m_closingEventsFired + "-" + m_closedEventsFired;
		}

		private void NavView_PaneClosing(NavigationView sender, NavigationViewPaneClosingEventArgs args)
		{
			if (CancelClosingEvents != null && (bool)CancelClosingEvents.IsChecked)
			{
				args.Cancel = true;
			}

			LastIngEventText.Text = "PaneClosing event fired";

			m_closingEventsFired++;
			ClosingEventCountTextBlock.Text = m_closingEventsFired + "-" + m_closedEventsFired;

		}

		private void NavView_PaneClosed(NavigationView sender, object args)
		{
			LastEdEventText.Text = "PaneClosed event fired";

			m_closedEventsFired++;
			ClosingEventCountTextBlock.Text = m_closingEventsFired + "-" + m_closedEventsFired;
			PaneOpenedOrClosedEvent.Text = "Closed";
		}

		private void NavView_PaneOpening(NavigationView sender, object args)
		{
			LastIngEventText.Text = "PaneOpening event fired";
		}

		private void NavView_PaneOpened(NavigationView sender, object args)
		{
			LastEdEventText.Text = "PaneOpened event fired";
			PaneOpenedOrClosedEvent.Text = "Opened";
		}

		private void ChangePaneTitle_Click(object sender, RoutedEventArgs e)
		{
			NavView.PaneTitle = string.IsNullOrEmpty(NavView.PaneTitle) ? "NavView Test" : string.Empty;
		}

		private void CopyVolumeToolTipButton_Click(object sender, RoutedEventArgs e)
		{
			var contentGrid = FindVisualChildByName(VolumeItem, "ContentGrid") as Grid;
			ToolTip tooltip = ToolTipService.GetToolTip(contentGrid) as ToolTip;
			if (tooltip.Content == null)
			{
				ToolTipStatusTextBlock.Text = "The volume navigation view item tooltip content is null";
			}
			else
			{
				ToolTipStatusTextBlock.Text = "The volume navigation view item tooltip content is definitely not null";
			}
		}

		private void SetHeaderButton_Click(object sender, RoutedEventArgs args)
		{
			NavView.Header = new TextBlock() { Text = "Bananas" };
		}

		private void BackButtonVisibilityCheckbox_Checked(object sender, RoutedEventArgs e)
		{
			NavView.IsBackButtonVisible = NavigationViewBackButtonVisible.Visible;
		}

		private void BackButtonVisibilityCheckbox_Unchecked(object sender, RoutedEventArgs e)
		{
			NavView.IsBackButtonVisible = NavigationViewBackButtonVisible.Collapsed;
		}

		private void BackButtonEnabledCheckbox_Checked(object sender, RoutedEventArgs e)
		{
			NavView.IsBackEnabled = true;
		}

		private void BackButtonEnabledCheckbox_Unchecked(object sender, RoutedEventArgs e)
		{
			NavView.IsBackEnabled = false;
		}

		private void SelectionFollowFocus_Checked(object sender, RoutedEventArgs e)
		{
			NavView.SelectionFollowsFocus = NavigationViewSelectionFollowsFocus.Enabled;
		}

		private void SelectionFollowFocus_Unchecked(object sender, RoutedEventArgs e)
		{
			NavView.SelectionFollowsFocus = NavigationViewSelectionFollowsFocus.Disabled;
		}

		private void NavView_BackRequested(NavigationView nv, NavigationViewBackRequestedEventArgs args)
		{
			BackRequestedStateTextBlock.Text = "Back was requested";
		}

		private void BackRequestedStateResetButton_Click(object sender, RoutedEventArgs args)
		{
			BackRequestedStateTextBlock.Text = "Test reset";
		}

		private void FlipOrientation_Click(object sender, RoutedEventArgs e)
		{
			NavView.PaneDisplayMode = NavView.PaneDisplayMode == NavigationViewPaneDisplayMode.Top ? NavigationViewPaneDisplayMode.Auto : NavigationViewPaneDisplayMode.Top;
			FindAndGiveAutomationNameToVisualChild("SettingsItem");
		}

		private void ClearSelectedItem_Click(object sender, RoutedEventArgs e)
		{
			NavView.SelectedItem = null;
		}

		private void GetHomeItemRevealVisualState_Click(object sender, RoutedEventArgs e)
		{
			var found = Utilities.VisualStateHelper.ContainsVisualState(HomeItem, "OnLeftNavigationReveal");
			OnLeftNavigationRevealVisualState.Text = found ? "True" : "False";
		}

		private void ChangeFocusVisualKind_Click(object sender, RoutedEventArgs e)
		{
			m_useFocusVisualKindReveal = !m_useFocusVisualKindReveal;
			if (m_useFocusVisualKindReveal)
			{
				Application.Current.FocusVisualKind = FocusVisualKind.Reveal;
			}
			else
			{
				Application.Current.FocusVisualKind = m_focusVisualKind;
			}

			// Force HomeItem to update visualstate
			if (HomeItem.Content.Equals("Home"))
			{
				HomeItem.Content = "Home1";
			}
			else
			{
				HomeItem.Content = "Home";
			}
		}

		private void GetHeaderContentMargin_Click(object sender, RoutedEventArgs e)
		{
			string margin = "Can't find HeaderContent";
			Grid rootGrid = VisualTreeHelper.GetChild(NavView, 0) as Grid;
			if (rootGrid != null)
			{
				var control = rootGrid.FindName("HeaderContent") as FrameworkElement;
				if (control != null)
				{
					margin = control.Margin.ToString();
				}
			}
			HeaderContentMarginResult.Text = margin;
		}

		private void GetNavViewActiveVisualStates_Click(object sender, RoutedEventArgs e)
		{
			var visualstates = Utilities.VisualStateHelper.GetCurrentVisualStateName(NavView);
			NavViewActiveVisualStatesResult.Text = string.Join(",", visualstates);
		}

		private void ChangePaneHeader_Click(object sender, RoutedEventArgs e)
		{
			if (NavView.PaneHeader == null)
			{
				TextBlock text = new TextBlock();
				text.Text = "Modified Pane Header";
				NavView.PaneHeader = text;
			}
			else if (NavView.PaneHeader is TextBlock)
			{
				TextBox text = new TextBox();
				text.Text = "Large Pane Header";
				text.IsReadOnly = true;
				text.FontSize = 26;
				NavView.PaneHeader = text;
			}
			else
			{
				NavView.PaneHeader = null;
			}

			FindAndGiveAutomationNameToVisualChild("PaneHeaderOnTopPane");
			FindAndGiveAutomationNameToVisualChild("PaneHeaderContentBorder");
		}

		private void ReadSettingsSelected_Click(object sender, RoutedEventArgs e)
		{
			DependencyObject settings = FindVisualChildByName(NavView, "SettingsItem");

			SettingsSelectedState.Text = (settings as NavigationViewItem).IsSelected.ToString();
		}

		private async void ContentDialogNavViewButton_Click(object sender, RoutedEventArgs e)
		{
			DialogWithNavView dialog = new DialogWithNavView();
			await dialog.ShowAsync();
		}

		private void GetTopLevelNavViewItemContentPresenterMarginButton_Click(object sender, RoutedEventArgs e)
		{
			if (FindVisualChildByName(HasChildItem, "ContentPresenter") is ContentPresenter presenter)
			{
				NavViewItemContentPresenterMarginTextBlock.Text = presenter.Margin.ToString();
			}
		}

		private void GetChildNavViewItemContentPresenterMarginButton_Click(object sender, RoutedEventArgs e)
		{
			if (FindVisualChildByName(ChildItem, "ContentPresenter") is ContentPresenter presenter)
			{
				NavViewItemContentPresenterMarginTextBlock.Text = presenter.Margin.ToString();
			}
		}

		private void BringSettingsIntoViewButton_Click(object sender, RoutedEventArgs e)
		{
			if (NavView.SettingsItem is NavigationViewItem settingsItem)
			{
				settingsItem.StartBringIntoView();
			}
		}

		private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
		{
			var suggestions = new List<string>();
			suggestions.Add(sender.Text);
			suggestions.Add(sender.Text + "1");
			PaneAutoSuggestBox.ItemsSource = suggestions;
		}

		private void PaneAutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
		{
			SuggestionChosenCheckbox.IsChecked = true;
		}

		private void ScrollItemIntoViewButton_Click(object sender, RoutedEventArgs e)
		{
			var tag = Convert.ToString((ScrollItemIntoViewComboBox.SelectedItem as ComboBoxItem).Tag);
			switch (tag)
			{
				case "Home":
					HomeItem.StartBringIntoView();
					break;
				case "HasChildItem":
					HasChildItem.StartBringIntoView();
					break;
			}
		}
	}
}
