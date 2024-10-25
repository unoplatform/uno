// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.UI.Private.Controls;
using Windows.Foundation;
using MUXControlsTestApp.Samples.Model;
using Uno.UI.Samples.Controls;

namespace MUXControlsTestApp
{
	[Sample("SelectorBar")]
	public sealed partial class SelectorBarSummaryPage : TestPage
	{
		private List<string> _fullLogs = new List<string>();
		private SelectorBarItem _otherSelectorBarItem = new SelectorBarItem();

		public SelectorBarSummaryPage()
		{
			this.InitializeComponent();

			PopulateCmbSelectorBarSelectedItem();
			UpdateBackground();
			UpdateBorderBrush();
			UpdateBorderThickness();
			UpdateMargin();
			UpdatePadding();
			UpdateMaxWidth();
			UpdateMaxHeight();
			UpdateCornerRadius();
			UpdateTabNavigation();
			UpdateXYFocusKeyboardNavigation();
			UpdateIsEnabled();
			UpdateIsTabStop();
			UpdateSelectedItem();

			//if (chkLogItemContainerMessages.IsChecked == true)
			//{
			//	MUXControlsTestHooks.SetLoggingLevelForType("ItemContainer", isLoggingInfoLevel: true, isLoggingVerboseLevel: true);
			//}
			//if (chkLogItemsRepeaterMessages.IsChecked == true)
			//{
			//	MUXControlsTestHooks.SetLoggingLevelForType("ItemsRepeater", isLoggingInfoLevel: true, isLoggingVerboseLevel: true);
			//}
			//if (chkLogItemsViewMessages.IsChecked == true)
			//{
			//	MUXControlsTestHooks.SetLoggingLevelForType("ItemsView", isLoggingInfoLevel: true, isLoggingVerboseLevel: true);
			//}
			//if (chkLogSelectorBarMessages.IsChecked == true)
			//{
			//	MUXControlsTestHooks.SetLoggingLevelForType("SelectorBar", isLoggingInfoLevel: true, isLoggingVerboseLevel: true);
			//}

			//if (chkLogSelectorBarMessages.IsChecked == true ||
			//	chkLogItemsViewMessages.IsChecked == true ||
			//	chkLogItemsRepeaterMessages.IsChecked == true ||
			//	chkLogItemContainerMessages.IsChecked == true)
			//{
			//	MUXControlsTestHooks.LoggingMessage += MUXControlsTestHooks_LoggingMessage;
			//}
		}

		~SelectorBarSummaryPage()
		{
		}

		protected internal override void OnNavigatedFrom(NavigationEventArgs e)
		{
			//MUXControlsTestHooks.SetLoggingLevelForType("ItemContainer", isLoggingInfoLevel: false, isLoggingVerboseLevel: false);
			//MUXControlsTestHooks.SetLoggingLevelForType("ItemsRepeater", isLoggingInfoLevel: false, isLoggingVerboseLevel: false);
			//MUXControlsTestHooks.SetLoggingLevelForType("ItemsView", isLoggingInfoLevel: false, isLoggingVerboseLevel: false);
			//MUXControlsTestHooks.SetLoggingLevelForType("SelectorBar", isLoggingInfoLevel: false, isLoggingVerboseLevel: false);

			//MUXControlsTestHooks.LoggingMessage -= MUXControlsTestHooks_LoggingMessage;

			if (selectorBar != null)
			{
				ItemsView itemsView = SelectorBarTestHooks.GetItemsViewPart(selectorBar);

				if (itemsView != null)
				{
					ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(itemsView);

					if (itemsRepeater != null)
					{
						UnhookItemsRepeaterEvents(itemsRepeater);
					}

					UnhookItemsViewEvents(itemsView);
				}

				UnhookSelectorBarEvents();
			}

			base.OnNavigatedFrom(e);
		}

		private void SelectorBar_Loaded(object sender, RoutedEventArgs e)
		{
			AppendEventMessage($"SelectorBar.Loaded");
		}

		private void SelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
		{
			string selectedItem = (sender.SelectedItem == null) ? "<null>" : GetStringFromSelectorBarItem(sender.SelectedItem);

			AppendEventMessage($"SelectorBar.SelectionChanged SelectedItem={selectedItem}");
		}

		private void ItemsView_ItemInvoked(ItemsView sender, ItemsViewItemInvokedEventArgs args)
		{
			AppendEventMessage($"ItemsView.ItemInvoked InvokedItem={args.InvokedItem.ToString().Substring(0, 50)}");
		}

		private void ItemsView_Loaded(object sender, RoutedEventArgs e)
		{
			AppendEventMessage($"ItemsView.Loaded");
			if (chkLogItemsRepeaterEvents.IsChecked == true)
			{
				LogItemsRepeaterInfo();
			}

			ItemsView itemsView = sender as ItemsView;

			if (itemsView != null)
			{
				ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(itemsView);

				if (itemsRepeater != null)
				{
					HookItemsRepeaterEvents(itemsRepeater);
				}
			}
		}

		private void ItemsView_SelectionChanged(ItemsView sender, ItemsViewSelectionChangedEventArgs args)
		{
			string selectedItems = String.Empty;

			foreach (var selectedItem in sender.SelectedItems)
			{
				selectedItems += (selectedItem == null) ? "<null>, " : selectedItem.ToString().Substring(0, 9) + ", ";
			}

			AppendEventMessage($"ItemsView.SelectionChanged SelectedItems={selectedItems}");
		}

		private void ItemsView_GettingFocus(UIElement sender, Windows.UI.Xaml.Input.GettingFocusEventArgs args)
		{
			FrameworkElement oldFE = args.OldFocusedElement as FrameworkElement;
			string oldFEName = (oldFE == null) ? "null" : oldFE.Name;
			FrameworkElement newFE = args.NewFocusedElement as FrameworkElement;
			string newFEName = (newFE == null) ? "null" : newFE.Name;

			AppendEventMessage($"ItemsView.GettingFocus FocusState={args.FocusState}, Direction={args.Direction}, InputDevice={args.InputDevice}, oldFE={oldFEName}, newFE={newFEName}");
		}

		private void ItemsView_LostFocus(object sender, RoutedEventArgs e)
		{
			AppendEventMessage("ItemsView.LostFocus");
		}

		private void ItemsView_LosingFocus(UIElement sender, Windows.UI.Xaml.Input.LosingFocusEventArgs args)
		{
			FrameworkElement oldFE = args.OldFocusedElement as FrameworkElement;
			string oldFEName = (oldFE == null) ? "null" : oldFE.Name;
			FrameworkElement newFE = args.NewFocusedElement as FrameworkElement;
			string newFEName = (newFE == null) ? "null" : newFE.Name;

			AppendEventMessage($"ItemsView.LosingFocus FocusState={args.FocusState}, Direction={args.Direction}, InputDevice={args.InputDevice}, oldFE={oldFEName}, newFE={newFEName}");
		}

		private void ItemsView_GotFocus(object sender, RoutedEventArgs e)
		{
			AppendEventMessage("ItemsView.GotFocus");
		}

		private void ItemsRepeater_Loaded(object sender, RoutedEventArgs e)
		{
			if (chkLogItemsRepeaterEvents.IsChecked == true)
			{
				AppendEventMessage($"ItemsRepeater.Loaded");
				LogItemsRepeaterInfo();
			}
		}

		private void ItemsRepeater_ElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
		{
			if (chkLogItemsRepeaterEvents.IsChecked == true)
			{
				AppendEventMessage($"ItemsRepeater.ElementPrepared Index={args.Index}, Element={args.Element}");
			}
		}

		private void ItemsRepeater_ElementClearing(ItemsRepeater sender, ItemsRepeaterElementClearingEventArgs args)
		{
			if (chkLogItemsRepeaterEvents.IsChecked == true)
			{
				AppendEventMessage($"ItemsRepeater.ElementClearing Element={args.Element}");
			}
		}

		private void LogItemsRepeaterInfo()
		{
			if (selectorBar != null)
			{
				ItemsView itemsView = SelectorBarTestHooks.GetItemsViewPart(selectorBar);

				if (itemsView != null)
				{
					ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(itemsView);

					if (itemsRepeater != null)
					{
						AppendEventMessage($"ItemsRepeater Info: ItemsSource={itemsRepeater.ItemsSource}, ItemTemplate={itemsRepeater.ItemTemplate}, Layout={itemsRepeater.Layout}, ChildrenCount={VisualTreeHelper.GetChildrenCount(itemsRepeater)}");
					}
				}
			}
		}

		#region UI Controls Updates

		private void UpdateMargin()
		{
			if (selectorBar != null && txtMargin != null)
			{
				txtMargin.Text = selectorBar.Margin.ToString();
			}
		}

		private void UpdateMaxHeight()
		{
			if (selectorBar != null && txtMaxHeight != null)
			{
				txtMaxHeight.Text = selectorBar.MaxHeight.ToString();
			}
		}

		private void UpdateMaxWidth()
		{
			if (selectorBar != null && txtMaxWidth != null)
			{
				txtMaxWidth.Text = selectorBar.MaxWidth.ToString();
			}
		}

		private void UpdatePadding()
		{
			if (selectorBar != null && txtPadding != null)
			{
				txtPadding.Text = selectorBar.Padding.ToString();
			}
		}

		private void UpdateBackground()
		{
			if (selectorBar != null && cmbBackground != null)
			{
				SolidColorBrush bg = selectorBar.Background as SolidColorBrush;

				if (bg == null)
				{
					cmbBackground.SelectedIndex = 0;
				}
				else if (bg.Color == Colors.Transparent)
				{
					cmbBackground.SelectedIndex = 1;
				}
				else if (bg.Color == Colors.AliceBlue)
				{
					cmbBackground.SelectedIndex = 2;
				}
				else if (bg.Color == Colors.Aqua)
				{
					cmbBackground.SelectedIndex = 3;
				}
				else
				{
					cmbBackground.SelectedIndex = -1;
				}
			}
		}

		private void UpdateBorderBrush()
		{
			if (selectorBar != null && cmbBorderBrush != null)
			{
				SolidColorBrush bb = selectorBar.BorderBrush as SolidColorBrush;

				if (bb == null)
				{
					cmbBorderBrush.SelectedIndex = 0;
				}
				else if (bb.Color == Colors.Transparent)
				{
					cmbBorderBrush.SelectedIndex = 1;
				}
				else if (bb.Color == Colors.Blue)
				{
					cmbBorderBrush.SelectedIndex = 2;
				}
				else if (bb.Color == Colors.Green)
				{
					cmbBorderBrush.SelectedIndex = 3;
				}
				else
				{
					cmbBorderBrush.SelectedIndex = -1;
				}
			}
		}

		private void UpdateBorderThickness()
		{
			if (selectorBar != null && txtBorderThickness != null)
			{
				txtBorderThickness.Text = selectorBar.BorderThickness.ToString();
			}
		}

		private void UpdateCornerRadius()
		{
			if (selectorBar != null && txtCornerRadius != null)
			{
				txtCornerRadius.Text = selectorBar.CornerRadius.ToString();
			}
		}

		private void UpdateTabNavigation()
		{
			try
			{
				if (selectorBar != null && cmbTabNavigation != null)
				{
					cmbTabNavigation.SelectedIndex = (int)selectorBar.TabNavigation;
				}
			}
			catch (Exception ex)
			{
				txtExceptionReport.Text = ex.ToString();
				AppendEventMessage(ex.ToString());
			}
		}

		private void UpdateXYFocusKeyboardNavigation()
		{
			try
			{
				if (selectorBar != null && cmbXYFocusKeyboardNavigation != null)
				{
					cmbXYFocusKeyboardNavigation.SelectedIndex = (int)selectorBar.XYFocusKeyboardNavigation;
				}
			}
			catch (Exception ex)
			{
				txtExceptionReport.Text = ex.ToString();
				AppendEventMessage(ex.ToString());
			}
		}

		private void UpdateItemsCount()
		{
			// TODO
		}

		private void UpdateIsEnabled()
		{
			try
			{
				if (selectorBar != null && chkIsEnabled != null)
				{
					chkIsEnabled.IsChecked = selectorBar.IsEnabled;
				}
			}
			catch (Exception ex)
			{
				txtExceptionReport.Text = ex.ToString();
				AppendEventMessage(ex.ToString());
			}
		}

		private void UpdateIsTabStop()
		{
			try
			{
				if (selectorBar != null && chkIsTabStop != null)
				{
					chkIsTabStop.IsChecked = selectorBar.IsTabStop;
				}
			}
			catch (Exception ex)
			{
				txtExceptionReport.Text = ex.ToString();
				AppendEventMessage(ex.ToString());
			}
		}

		private void PopulateCmbSelectorBarSelectedItem()
		{
			if (selectorBar != null && cmbSelectorBarSelectedItem != null)
			{
				cmbSelectorBarSelectedItem.Items.Clear();

				cmbSelectorBarSelectedItem.Items.Add(new ComboBoxItem()
				{
					Content = "null"
				});

				if (selectorBar.Items != null)
				{
					foreach (SelectorBarItem selectorBarItem in selectorBar.Items)
					{
						cmbSelectorBarSelectedItem.Items.Add(new ComboBoxItem()
						{
							Content = GetStringFromSelectorBarItem(selectorBarItem),
							Tag = selectorBarItem
						});
					}
				}

				cmbSelectorBarSelectedItem.Items.Add(new ComboBoxItem()
				{
					Content = "Other",
					Tag = _otherSelectorBarItem
				});
			}
		}

		private void UpdateSelectedItem()
		{
			if (selectorBar != null && cmbSelectorBarSelectedItem != null)
			{
				if (selectorBar.SelectedItem == null)
				{
					cmbSelectorBarSelectedItem.SelectedIndex = 0;
				}
				else
				{
					int selectedIndex = 1;

					if (selectorBar.Items != null)
					{
						foreach (SelectorBarItem selectorBarItem in selectorBar.Items)
						{
							if (selectorBarItem == selectorBar.SelectedItem)
							{
								cmbSelectorBarSelectedItem.SelectedIndex = selectedIndex;
								break;
							}

							selectedIndex++;
						}
					}

					cmbSelectorBarSelectedItem.SelectedIndex = selectedIndex;
				}
			}
		}

		#endregion

		#region Property Getters

		private void BtnGetBorderThickness_Click(object sender, RoutedEventArgs e)
		{
			UpdateBorderThickness();
		}

		private void BtnGetCornerRadius_Click(object sender, RoutedEventArgs e)
		{
			UpdateCornerRadius();
		}

		private void BtnGetMargin_Click(object sender, RoutedEventArgs e)
		{
			UpdateMargin();
		}

		private void BtnGetMaxHeight_Click(object sender, RoutedEventArgs e)
		{
			UpdateMaxHeight();
		}

		private void BtnGetMaxWidth_Click(object sender, RoutedEventArgs e)
		{
			UpdateMaxWidth();
		}

		private void BtnGetPadding_Click(object sender, RoutedEventArgs e)
		{
			UpdatePadding();
		}

		private void BtnGetSelectorBarSelectedItem_Click(object sender, RoutedEventArgs e)
		{
			UpdateSelectedItem();
		}

		#endregion

		#region Property Setters

		private void ChkIsEnabled_CheckedChanged(object sender, RoutedEventArgs e)
		{
			try
			{
				if (selectorBar != null && chkIsEnabled != null)
				{
					selectorBar.IsEnabled = (bool)chkIsEnabled.IsChecked;
				}
			}
			catch (Exception ex)
			{
				txtExceptionReport.Text = ex.ToString();
				AppendEventMessage(ex.ToString());
			}
		}

		private void ChkIsTabStop_CheckedChanged(object sender, RoutedEventArgs e)
		{
			try
			{
				if (selectorBar != null && chkIsTabStop != null)
				{
					selectorBar.IsTabStop = (bool)chkIsTabStop.IsChecked;
				}
			}
			catch (Exception ex)
			{
				txtExceptionReport.Text = ex.ToString();
				AppendEventMessage(ex.ToString());
			}
		}

		private void BtnSetCornerRadius_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (selectorBar != null && txtCornerRadius != null)
				{
					selectorBar.CornerRadius = GetCornerRadiusFromString(txtCornerRadius.Text);
				}
			}
			catch (Exception ex)
			{
				txtExceptionReport.Text = ex.ToString();
				AppendEventMessage(ex.ToString());
			}
		}

		private void BtnSetMargin_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (selectorBar != null && txtMargin != null)
				{
					selectorBar.Margin = GetThicknessFromString(txtMargin.Text);
				}
			}
			catch (Exception ex)
			{
				txtExceptionReport.Text = ex.ToString();
				AppendEventMessage(ex.ToString());
			}
		}

		private void BtnSetMaxHeight_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (selectorBar != null && txtMaxHeight != null)
				{
					selectorBar.MaxHeight = Convert.ToDouble(txtMaxHeight.Text);
				}
			}
			catch (Exception ex)
			{
				txtExceptionReport.Text = ex.ToString();
				AppendEventMessage(ex.ToString());
			}
		}

		private void BtnSetMaxWidth_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (selectorBar != null && txtMaxWidth != null)
				{
					selectorBar.MaxWidth = Convert.ToDouble(txtMaxWidth.Text);
				}
			}
			catch (Exception ex)
			{
				txtExceptionReport.Text = ex.ToString();
				AppendEventMessage(ex.ToString());
			}
		}

		private void BtnSetPadding_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (selectorBar != null && txtPadding != null)
				{
					selectorBar.Padding = GetThicknessFromString(txtPadding.Text);
				}
			}
			catch (Exception ex)
			{
				txtExceptionReport.Text = ex.ToString();
				AppendEventMessage(ex.ToString());
			}
		}

		private void BtnSetSelectorBarSelectedItem_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (selectorBar != null && cmbSelectorBarSelectedItem != null)
				{
					selectorBar.SelectedItem = (cmbSelectorBarSelectedItem.SelectedItem as ComboBoxItem).Tag as SelectorBarItem;
				}
			}
			catch (Exception ex)
			{
				txtExceptionReport.Text = ex.ToString();
				AppendEventMessage(ex.ToString());
			}
		}

		private void BtnSetBorderThickness_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (selectorBar != null)
				{
					selectorBar.BorderThickness = GetThicknessFromString(txtBorderThickness.Text);
				}
			}
			catch (Exception ex)
			{
				txtExceptionReport.Text = ex.ToString();
				AppendEventMessage(ex.ToString());
			}
		}

		private void CmbBackground_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			try
			{
				if (selectorBar != null)
				{
					switch (cmbBackground.SelectedIndex)
					{
						case 0:
							selectorBar.Background = null;
							break;
						case 1:
							selectorBar.Background = new SolidColorBrush(Colors.Transparent);
							break;
						case 2:
							selectorBar.Background = new SolidColorBrush(Colors.AliceBlue);
							break;
						case 3:
							selectorBar.Background = new SolidColorBrush(Colors.Aqua);
							break;
					}
				}
			}
			catch (Exception ex)
			{
				txtExceptionReport.Text = ex.ToString();
				AppendEventMessage(ex.ToString());
			}
		}

		private void CmbBorderBrush_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			try
			{
				if (selectorBar != null)
				{
					switch (cmbBorderBrush.SelectedIndex)
					{
						case 0:
							selectorBar.BorderBrush = null;
							break;
						case 1:
							selectorBar.BorderBrush = new SolidColorBrush(Colors.Transparent);
							break;
						case 2:
							selectorBar.BorderBrush = new SolidColorBrush(Colors.Blue);
							break;
						case 3:
							selectorBar.BorderBrush = new SolidColorBrush(Colors.Green);
							break;
					}
				}
			}
			catch (Exception ex)
			{
				txtExceptionReport.Text = ex.ToString();
				AppendEventMessage(ex.ToString());
			}
		}

		private void CmbTabNavigation_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			try
			{
				if (selectorBar != null && cmbTabNavigation != null && selectorBar.TabNavigation != (KeyboardNavigationMode)cmbTabNavigation.SelectedIndex)
				{
					selectorBar.TabNavigation = (KeyboardNavigationMode)cmbTabNavigation.SelectedIndex;
				}
			}
			catch (Exception ex)
			{
				txtExceptionReport.Text = ex.ToString();
				AppendEventMessage(ex.ToString());
			}
		}

		private void CmbXYFocusKeyboardNavigation_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			try
			{
				if (selectorBar != null && cmbXYFocusKeyboardNavigation != null && selectorBar.XYFocusKeyboardNavigation != (XYFocusKeyboardNavigationMode)cmbXYFocusKeyboardNavigation.SelectedIndex)
				{
					selectorBar.XYFocusKeyboardNavigation = (XYFocusKeyboardNavigationMode)cmbXYFocusKeyboardNavigation.SelectedIndex;
				}
			}
			catch (Exception ex)
			{
				txtExceptionReport.Text = ex.ToString();
				AppendEventMessage(ex.ToString());
			}
		}

		#endregion

		#region Items methods

		private void BtnGetSelectorBarItemsCount_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (selectorBar != null && txtSelectorBarItemsCount != null)
				{
					if (selectorBar.Items == null)
					{
						txtSelectorBarItemsCount.Text = "null";
					}
					else
					{
						txtSelectorBarItemsCount.Text = selectorBar.Items.Count.ToString();
					}
				}
			}
			catch (Exception ex)
			{
				txtExceptionReport.Text = ex.ToString();
				AppendEventMessage(ex.ToString());
			}
		}

		private void BtnGetSelectorBarItemIsEnabled_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (selectorBar != null && txtSelectorBarItemIndex != null && cmbSelectorBarItemIsEnabled != null)
				{
					cmbSelectorBarItemIsEnabled.SelectedIndex = selectorBar.Items[int.Parse(txtSelectorBarItemIndex.Text)].IsEnabled ? 1 : 0;
				}
			}
			catch (Exception ex)
			{
				txtExceptionReport.Text = ex.ToString();
				AppendEventMessage(ex.ToString());
			}
		}

		private void BtnSetSelectorBarItemIsEnabled_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (selectorBar != null && txtSelectorBarItemIndex != null && cmbSelectorBarItemIsEnabled != null)
				{
					selectorBar.Items[int.Parse(txtSelectorBarItemIndex.Text)].IsEnabled = cmbSelectorBarItemIsEnabled.SelectedIndex == 1;
				}
			}
			catch (Exception ex)
			{
				txtExceptionReport.Text = ex.ToString();
				AppendEventMessage(ex.ToString());
			}
		}

		private void BtnGetSelectorBarItemIsSelected_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (selectorBar != null && txtSelectorBarItemIndex != null && cmbSelectorBarItemIsSelected != null)
				{
					cmbSelectorBarItemIsSelected.SelectedIndex = selectorBar.Items[int.Parse(txtSelectorBarItemIndex.Text)].IsSelected ? 1 : 0;
				}
			}
			catch (Exception ex)
			{
				txtExceptionReport.Text = ex.ToString();
				AppendEventMessage(ex.ToString());
			}
		}

		private void BtnSetSelectorBarItemIsSelected_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (selectorBar != null && txtSelectorBarItemIndex != null && cmbSelectorBarItemIsSelected != null)
				{
					selectorBar.Items[int.Parse(txtSelectorBarItemIndex.Text)].IsSelected = cmbSelectorBarItemIsSelected.SelectedIndex == 1;
				}
			}
			catch (Exception ex)
			{
				txtExceptionReport.Text = ex.ToString();
				AppendEventMessage(ex.ToString());
			}
		}

		private void BtnGetSelectorBarItemVisibility_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (cmbSelectorBarItemVisibility != null)
				{
					if (selectorBar == null || txtSelectorBarItemIndex == null)
					{
						cmbSelectorBarItemVisibility.SelectedIndex = -1;
					}
					else
					{
						cmbSelectorBarItemVisibility.SelectedIndex = (int)selectorBar.Items[int.Parse(txtSelectorBarItemIndex.Text)].Visibility;
					}
				}
			}
			catch (Exception ex)
			{
				txtExceptionReport.Text = ex.ToString();
				AppendEventMessage(ex.ToString());
			}
		}

		private void BtnSetSelectorBarItemVisibility_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (selectorBar != null && txtSelectorBarItemIndex != null && cmbSelectorBarItemVisibility != null && cmbSelectorBarItemVisibility.SelectedIndex != -1)
				{
					selectorBar.Items[int.Parse(txtSelectorBarItemIndex.Text)].Visibility = (Visibility)cmbSelectorBarItemVisibility.SelectedIndex;
				}
			}
			catch (Exception ex)
			{
				txtExceptionReport.Text = ex.ToString();
				AppendEventMessage(ex.ToString());
			}
		}

		private void BtnGetSelectorBarItemForeground_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (selectorBar != null && txtSelectorBarItemIndex != null && cmbSelectorBarItemForeground != null)
				{
					SolidColorBrush fg = selectorBar.Items[int.Parse(txtSelectorBarItemIndex.Text)].Foreground as SolidColorBrush;

					if (fg == null)
					{
						cmbSelectorBarItemForeground.SelectedIndex = 0;
					}
					else if (fg.Color == Colors.Black)
					{
						cmbSelectorBarItemForeground.SelectedIndex = 1;
					}
					else if (fg.Color == Colors.Red)
					{
						cmbSelectorBarItemForeground.SelectedIndex = 2;
					}
					else if (fg.Color == Colors.Green)
					{
						cmbSelectorBarItemForeground.SelectedIndex = 3;
					}
					else
					{
						cmbSelectorBarItemForeground.SelectedIndex = -1;
					}
				}
			}
			catch (Exception ex)
			{
				txtExceptionReport.Text = ex.ToString();
				AppendEventMessage(ex.ToString());
			}
		}

		private void BtnSetSelectorBarItemForeground_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (selectorBar != null && txtSelectorBarItemIndex != null && cmbSelectorBarItemForeground != null)
				{
					selectorBar.Items[int.Parse(txtSelectorBarItemIndex.Text)].Foreground = GetForeground();
				}
			}
			catch (Exception ex)
			{
				txtExceptionReport.Text = ex.ToString();
				AppendEventMessage(ex.ToString());
			}
		}

		private void BtnGetSelectorBarItemMargin_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (selectorBar != null && txtSelectorBarItemIndex != null && txtSelectorBarItemMargin != null)
				{
					txtSelectorBarItemMargin.Text = selectorBar.Items[int.Parse(txtSelectorBarItemIndex.Text)].Margin.ToString();
				}
			}
			catch (Exception ex)
			{
				txtExceptionReport.Text = ex.ToString();
				AppendEventMessage(ex.ToString());
			}
		}

		private void BtnSetSelectorBarItemMargin_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (selectorBar != null && txtSelectorBarItemIndex != null && txtSelectorBarItemMargin != null)
				{
					selectorBar.Items[int.Parse(txtSelectorBarItemIndex.Text)].Margin = GetThicknessFromString(txtSelectorBarItemMargin.Text);
				}
			}
			catch (Exception ex)
			{
				txtExceptionReport.Text = ex.ToString();
				AppendEventMessage(ex.ToString());
			}
		}

		private void BtnGetSelectorBarItemPadding_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (selectorBar != null && txtSelectorBarItemIndex != null && txtSelectorBarItemPadding != null)
				{
					txtSelectorBarItemPadding.Text = selectorBar.Items[int.Parse(txtSelectorBarItemIndex.Text)].Padding.ToString();
				}
			}
			catch (Exception ex)
			{
				txtExceptionReport.Text = ex.ToString();
				AppendEventMessage(ex.ToString());
			}
		}

		private void BtnSetSelectorBarItemPadding_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (selectorBar != null && txtSelectorBarItemIndex != null && txtSelectorBarItemPadding != null)
				{
					selectorBar.Items[int.Parse(txtSelectorBarItemIndex.Text)].Padding = GetThicknessFromString(txtSelectorBarItemPadding.Text);
				}
			}
			catch (Exception ex)
			{
				txtExceptionReport.Text = ex.ToString();
				AppendEventMessage(ex.ToString());
			}
		}

		private void BtnGetSelectorBarItemText_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (selectorBar != null && txtSelectorBarItemIndex != null && txtSelectorBarItemText != null)
				{
					txtSelectorBarItemText.Text = selectorBar.Items[int.Parse(txtSelectorBarItemIndex.Text)].Text;
				}
			}
			catch (Exception ex)
			{
				txtExceptionReport.Text = ex.ToString();
				AppendEventMessage(ex.ToString());
			}
		}

		private void BtnSetSelectorBarItemText_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (selectorBar != null && txtSelectorBarItemIndex != null && txtSelectorBarItemText != null)
				{
					selectorBar.Items[int.Parse(txtSelectorBarItemIndex.Text)].Text = txtSelectorBarItemText.Text;
					PopulateCmbSelectorBarSelectedItem();
				}
			}
			catch (Exception ex)
			{
				txtExceptionReport.Text = ex.ToString();
				AppendEventMessage(ex.ToString());
			}
		}

		private void BtnGetSelectorBarItemIcon_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (selectorBar != null && txtSelectorBarItemIndex != null && cmbSelectorBarItemIcon != null)
				{
					cmbSelectorBarItemIcon.SelectedIndex = -1;

					SymbolIcon symbolIcon = selectorBar.Items[int.Parse(txtSelectorBarItemIndex.Text)].Icon as SymbolIcon;

					if (symbolIcon == null)
					{
						cmbSelectorBarItemIcon.SelectedIndex = 0;
						return;
					}

					cmbSelectorBarItemIcon.SelectedIndex = GetIntFromSymbol(symbolIcon.Symbol);
				}
			}
			catch (Exception ex)
			{
				txtExceptionReport.Text = ex.ToString();
				AppendEventMessage(ex.ToString());
			}
		}

		private void BtnSetSelectorBarItemIcon_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (selectorBar != null && txtSelectorBarItemIndex != null && cmbSelectorBarItemIcon != null)
				{
					SelectorBarItem selectorBarItem = selectorBar.Items[int.Parse(txtSelectorBarItemIndex.Text)];

					if (cmbSelectorBarItemIcon.SelectedIndex < 1)
					{
						selectorBarItem.Icon = null;
						return;
					}

					SymbolIcon symbolIcon = selectorBarItem.Icon as SymbolIcon;

					if (symbolIcon == null)
					{
						symbolIcon = new SymbolIcon();
					}

					symbolIcon.Symbol = GetSymbolFromInt(cmbSelectorBarItemIcon.SelectedIndex);
					selectorBarItem.Icon = symbolIcon;
					PopulateCmbSelectorBarSelectedItem();
				}
			}
			catch (Exception ex)
			{
				txtExceptionReport.Text = ex.ToString();
				AppendEventMessage(ex.ToString());
			}
		}

		private void BtnGetSelectorBarItemChild_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (selectorBar != null && txtSelectorBarItemIndex != null && cmbSelectorBarItemChild != null)
				{
					cmbSelectorBarItemChild.SelectedIndex = -1;

					SelectorBarItem selectorBarItem = selectorBar.Items[int.Parse(txtSelectorBarItemIndex.Text)];

					if (selectorBarItem.Child == null)
					{
						cmbSelectorBarItemChild.SelectedIndex = 0;
					}
					else if (selectorBarItem.Child is TextBlock)
					{
						cmbSelectorBarItemChild.SelectedIndex = 1;
					}
					else if (selectorBarItem.Child is TextBox)
					{
						cmbSelectorBarItemChild.SelectedIndex = 2;
					}
				}
			}
			catch (Exception ex)
			{
				txtExceptionReport.Text = ex.ToString();
				AppendEventMessage(ex.ToString());
			}
		}

		private void BtnSetSelectorBarItemChild_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (selectorBar != null && txtSelectorBarItemIndex != null && cmbSelectorBarItemChild != null)
				{
					SelectorBarItem selectorBarItem = selectorBar.Items[int.Parse(txtSelectorBarItemIndex.Text)];

					switch (cmbSelectorBarItemChild.SelectedIndex)
					{
						case -1:
						case 0:
							selectorBarItem.Child = null;
							break;
						case 1:
							selectorBarItem.Child = GetTextBlockChild();
							break;
						case 2:
							selectorBarItem.Child = GetTextBoxChild();
							break;
					}
				}
			}
			catch (Exception ex)
			{
				txtExceptionReport.Text = ex.ToString();
				AppendEventMessage(ex.ToString());
			}
		}

		private void BtnAddSelectorBarItem_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (selectorBar != null)
				{
					selectorBar.Items.Add(GetSelectorBarItem());
					PopulateCmbSelectorBarSelectedItem();
				}
			}
			catch (Exception ex)
			{
				txtExceptionReport.Text = ex.ToString();
				AppendEventMessage(ex.ToString());
			}
		}

		private void BtnRemoveAllSelectorBarItems_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (selectorBar != null)
				{
					selectorBar.Items.Clear();
					PopulateCmbSelectorBarSelectedItem();
				}
			}
			catch (Exception ex)
			{
				txtExceptionReport.Text = ex.ToString();
				AppendEventMessage(ex.ToString());
			}
		}

		private void BtnReplaceSelectorBarItem_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (selectorBar != null && txtSelectorBarItemIndex != null)
				{
					selectorBar.Items[int.Parse(txtSelectorBarItemIndex.Text)] = GetSelectorBarItem();
					PopulateCmbSelectorBarSelectedItem();
				}
			}
			catch (Exception ex)
			{
				txtExceptionReport.Text = ex.ToString();
				AppendEventMessage(ex.ToString());
			}
		}

		private void BtnInsertSelectorBarItem_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (selectorBar != null && txtSelectorBarItemIndex != null)
				{
					selectorBar.Items.Insert(int.Parse(txtSelectorBarItemIndex.Text), GetSelectorBarItem());
					PopulateCmbSelectorBarSelectedItem();
				}
			}
			catch (Exception ex)
			{
				txtExceptionReport.Text = ex.ToString();
				AppendEventMessage(ex.ToString());
			}
		}

		private void BtnRemoveSelectorBarItem_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (selectorBar != null && txtSelectorBarItemIndex != null)
				{
					selectorBar.Items.RemoveAt(int.Parse(txtSelectorBarItemIndex.Text));
					PopulateCmbSelectorBarSelectedItem();
				}
			}
			catch (Exception ex)
			{
				txtExceptionReport.Text = ex.ToString();
				AppendEventMessage(ex.ToString());
			}
		}

		#endregion

		private void ChkSelectorBarProperties_Checked(object sender, RoutedEventArgs e)
		{
			if (svSelectorBarProperties != null)
				svSelectorBarProperties.Visibility = Visibility.Visible;
		}

		private void ChkSelectorBarProperties_Unchecked(object sender, RoutedEventArgs e)
		{
			if (svSelectorBarProperties != null)
				svSelectorBarProperties.Visibility = Visibility.Collapsed;
		}

		private void ChkSelectorBarItems_Checked(object sender, RoutedEventArgs e)
		{
			if (svSelectorBarItems != null)
				svSelectorBarItems.Visibility = Visibility.Visible;
		}

		private void ChkSelectorBarItems_Unchecked(object sender, RoutedEventArgs e)
		{
			if (svSelectorBarItems != null)
				svSelectorBarItems.Visibility = Visibility.Collapsed;
		}

		private void ChkLogs_Checked(object sender, RoutedEventArgs e)
		{
			if (grdLogs != null)
				grdLogs.Visibility = Visibility.Visible;
		}

		private void ChkLogs_Unchecked(object sender, RoutedEventArgs e)
		{
			if (grdLogs != null)
				grdLogs.Visibility = Visibility.Collapsed;
		}

		private void BtnGetFullLog_Click(object sender, RoutedEventArgs e)
		{
			GetFullLog();
		}

		private void BtnClearFullLog_Click(object sender, RoutedEventArgs e)
		{
			ClearFullLog();
		}

		private void BtnClearLogs_Click(object sender, RoutedEventArgs e)
		{
			lstLogs.Items.Clear();
		}

		private void BtnCopyEvents_Click(object sender, RoutedEventArgs e)
		{
			string logs = string.Empty;

			foreach (object log in lstLogs.Items)
			{
				logs += log.ToString() + "\n";
			}

			var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
			dataPackage.SetText(logs);
			Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
		}

		private void ChkLogItemsRepeaterEvents_Checked(object sender, RoutedEventArgs e)
		{
			if (selectorBar != null)
			{
				ItemsView itemsView = SelectorBarTestHooks.GetItemsViewPart(selectorBar);

				if (itemsView != null)
				{
					ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(itemsView);

					if (itemsRepeater != null)
					{
						HookItemsRepeaterEvents(itemsRepeater);
					}
				}
			}
		}

		private void ChkLogItemsRepeaterEvents_Unchecked(object sender, RoutedEventArgs e)
		{
			if (selectorBar != null)
			{
				ItemsView itemsView = SelectorBarTestHooks.GetItemsViewPart(selectorBar);

				if (itemsView != null)
				{
					ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(itemsView);

					if (itemsRepeater != null)
					{
						UnhookItemsRepeaterEvents(itemsRepeater);
					}
				}
			}
		}

		private void ChkLogItemsViewEvents_Checked(object sender, RoutedEventArgs e)
		{
			if (selectorBar != null)
			{
				ItemsView itemsView = SelectorBarTestHooks.GetItemsViewPart(selectorBar);

				if (itemsView != null)
				{
					HookItemsViewEvents(itemsView);
				}
			}
		}

		private void ChkLogItemsViewEvents_Unchecked(object sender, RoutedEventArgs e)
		{
			if (selectorBar != null)
			{
				ItemsView itemsView = SelectorBarTestHooks.GetItemsViewPart(selectorBar);

				if (itemsView != null)
				{
					UnhookItemsViewEvents(itemsView);
				}
			}
		}

		private void ChkLogSelectorBarEvents_Checked(object sender, RoutedEventArgs e)
		{
			HookSelectorBarEvents();
		}

		private void ChkLogSelectorBarEvents_Unchecked(object sender, RoutedEventArgs e)
		{
			UnhookSelectorBarEvents();
		}

		private void ChkLogItemContainerMessages_Checked(object sender, RoutedEventArgs e)
		{
			//MUXControlsTestHooks.SetLoggingLevelForType("ItemContainer", isLoggingInfoLevel: true, isLoggingVerboseLevel: true);
			//if (chkLogSelectorBarMessages != null && chkLogSelectorBarMessages.IsChecked == false &&
			//	chkLogItemsViewMessages != null && chkLogItemsViewMessages.IsChecked == false &&
			//	chkLogItemsRepeaterMessages != null && chkLogItemsRepeaterMessages.IsChecked == false)
			//	MUXControlsTestHooks.LoggingMessage += MUXControlsTestHooks_LoggingMessage;
		}

		private void ChkLogItemContainerMessages_Unchecked(object sender, RoutedEventArgs e)
		{
			//MUXControlsTestHooks.SetLoggingLevelForType("ItemContainer", isLoggingInfoLevel: false, isLoggingVerboseLevel: false);
			//if (chkLogSelectorBarMessages.IsChecked == false &&
			//	chkLogItemsViewMessages.IsChecked == false &&
			//	chkLogItemsRepeaterMessages.IsChecked == false)
			//	MUXControlsTestHooks.LoggingMessage -= MUXControlsTestHooks_LoggingMessage;
		}

		private void ChkLogItemsRepeaterMessages_Checked(object sender, RoutedEventArgs e)
		{
			//MUXControlsTestHooks.SetLoggingLevelForType("ItemsRepeater", isLoggingInfoLevel: true, isLoggingVerboseLevel: true);
			//if (chkLogSelectorBarMessages != null && chkLogSelectorBarMessages.IsChecked == false &&
			//	chkLogItemsViewMessages != null && chkLogItemsViewMessages.IsChecked == false &&
			//	chkLogItemContainerMessages != null && chkLogItemContainerMessages.IsChecked == false)
			//	MUXControlsTestHooks.LoggingMessage += MUXControlsTestHooks_LoggingMessage;
		}

		private void ChkLogItemsRepeaterMessages_Unchecked(object sender, RoutedEventArgs e)
		{
			//MUXControlsTestHooks.SetLoggingLevelForType("ItemsRepeater", isLoggingInfoLevel: false, isLoggingVerboseLevel: false);
			//if (chkLogSelectorBarMessages.IsChecked == false &&
			//	chkLogItemsViewMessages.IsChecked == false &&
			//	chkLogItemContainerMessages.IsChecked == false)
			//	MUXControlsTestHooks.LoggingMessage -= MUXControlsTestHooks_LoggingMessage;
		}

		private void ChkLogItemsViewMessages_Checked(object sender, RoutedEventArgs e)
		{
			//MUXControlsTestHooks.SetLoggingLevelForType("ItemsView", isLoggingInfoLevel: true, isLoggingVerboseLevel: true);
			//if (chkLogSelectorBarMessages != null && chkLogSelectorBarMessages.IsChecked == false &&
			//	chkLogItemsRepeaterMessages != null && chkLogItemsRepeaterMessages.IsChecked == false &&
			//	chkLogItemContainerMessages != null && chkLogItemContainerMessages.IsChecked == false)
			//	MUXControlsTestHooks.LoggingMessage += MUXControlsTestHooks_LoggingMessage;
		}

		private void ChkLogItemsViewMessages_Unchecked(object sender, RoutedEventArgs e)
		{
			//MUXControlsTestHooks.SetLoggingLevelForType("ItemsView", isLoggingInfoLevel: false, isLoggingVerboseLevel: false);
			//if (chkLogSelectorBarMessages.IsChecked == false &&
			//	chkLogItemsRepeaterMessages.IsChecked == false &&
			//	chkLogItemContainerMessages.IsChecked == false)
			//	MUXControlsTestHooks.LoggingMessage -= MUXControlsTestHooks_LoggingMessage;
		}

		private void ChkLogSelectorBarMessages_Checked(object sender, RoutedEventArgs e)
		{
			//MUXControlsTestHooks.SetLoggingLevelForType("SelectorBar", isLoggingInfoLevel: true, isLoggingVerboseLevel: true);
			//if (chkLogItemsViewMessages != null && chkLogItemsViewMessages.IsChecked == false &&
			//	chkLogItemsRepeaterMessages != null && chkLogItemsRepeaterMessages.IsChecked == false &&
			//	chkLogItemContainerMessages != null && chkLogItemContainerMessages.IsChecked == false)
			//	MUXControlsTestHooks.LoggingMessage += MUXControlsTestHooks_LoggingMessage;
		}

		private void ChkLogSelectorBarMessages_Unchecked(object sender, RoutedEventArgs e)
		{
			//MUXControlsTestHooks.SetLoggingLevelForType("SelectorBar", isLoggingInfoLevel: false, isLoggingVerboseLevel: false);
			//if (chkLogItemsViewMessages.IsChecked == false &&
			//	chkLogItemsRepeaterMessages.IsChecked == false &&
			//	chkLogItemContainerMessages.IsChecked == false)
			//	MUXControlsTestHooks.LoggingMessage -= MUXControlsTestHooks_LoggingMessage;
		}

		private void HookItemsRepeaterEvents(ItemsRepeater itemsRepeater)
		{
			if (itemsRepeater != null)
			{
				itemsRepeater.Loaded += ItemsRepeater_Loaded;
				itemsRepeater.ElementPrepared += ItemsRepeater_ElementPrepared;
				itemsRepeater.ElementClearing += ItemsRepeater_ElementClearing;
			}
		}

		private void UnhookItemsRepeaterEvents(ItemsRepeater itemsRepeater)
		{
			if (itemsRepeater != null)
			{
				itemsRepeater.Loaded -= ItemsRepeater_Loaded;
				itemsRepeater.ElementPrepared -= ItemsRepeater_ElementPrepared;
				itemsRepeater.ElementClearing -= ItemsRepeater_ElementClearing;
			}
		}

		private void HookItemsViewEvents(ItemsView itemsView)
		{
			if (itemsView != null)
			{
				itemsView.ItemInvoked += ItemsView_ItemInvoked;
				itemsView.GettingFocus += ItemsView_GettingFocus;
				itemsView.GotFocus += ItemsView_GotFocus;
				itemsView.LosingFocus += ItemsView_LosingFocus;
				itemsView.LostFocus += ItemsView_LostFocus;
				itemsView.Loaded += ItemsView_Loaded;
				itemsView.SelectionChanged += ItemsView_SelectionChanged;
			}
		}

		private void UnhookItemsViewEvents(ItemsView itemsView)
		{
			if (itemsView != null)
			{
				itemsView.ItemInvoked -= ItemsView_ItemInvoked;
				itemsView.GettingFocus -= ItemsView_GettingFocus;
				itemsView.GotFocus -= ItemsView_GotFocus;
				itemsView.LosingFocus -= ItemsView_LosingFocus;
				itemsView.LostFocus -= ItemsView_LostFocus;
				itemsView.Loaded -= ItemsView_Loaded;
				itemsView.SelectionChanged -= ItemsView_SelectionChanged;
			}
		}

		private void HookSelectorBarEvents()
		{
			if (selectorBar != null)
			{
				selectorBar.Loaded += SelectorBar_Loaded;
				selectorBar.SelectionChanged += SelectorBar_SelectionChanged;
			}
		}

		private void UnhookSelectorBarEvents()
		{
			if (selectorBar != null)
			{
				selectorBar.Loaded -= SelectorBar_Loaded;
				selectorBar.SelectionChanged -= SelectorBar_SelectionChanged;
			}
		}

		//private void MUXControlsTestHooks_LoggingMessage(object sender, MUXControlsTestHooksLoggingMessageEventArgs args)
		//{
		//	// Cut off the terminating new line.
		//	string msg = args.Message.Substring(0, args.Message.Length - 1);
		//	string eventMessage;
		//	string senderName = string.Empty;

		//	try
		//	{
		//		FrameworkElement fe = sender as FrameworkElement;

		//		if (fe != null)
		//		{
		//			senderName = "s:" + fe.Name + ", ";
		//		}
		//	}
		//	catch
		//	{
		//	}

		//	if (args.IsVerboseLevel)
		//	{
		//		eventMessage = "Verbose: " + senderName + "m:" + msg;
		//	}
		//	else
		//	{
		//		eventMessage = "Info: " + senderName + "m:" + msg;
		//	}

		//	AppendEventMessage(eventMessage);
		//}

		private void AppendEventMessage(string eventMessage)
		{
			while (eventMessage.Length > 0)
			{
				string msgHead = eventMessage;

				if (eventMessage.Length > 110)
				{
					int commaIndex = eventMessage.IndexOf(',', 110);
					if (commaIndex != -1)
					{
						msgHead = eventMessage.Substring(0, commaIndex);
						eventMessage = eventMessage.Substring(commaIndex + 1);
					}
					else
					{
						eventMessage = string.Empty;
					}
				}
				else
				{
					eventMessage = string.Empty;
				}

				lstLogs.Items.Add(msgHead);
				_fullLogs.Add(msgHead);

				if ((bool)chkOutputDebugString.IsChecked)
				{
					System.Diagnostics.Debug.WriteLine(msgHead);
				}
			}
		}

		private void GetFullLog()
		{
			this.txtStatus.Text = "GetFullLog. Populating cmbFullLog...";
			chkLogCleared.IsChecked = false;
			foreach (string log in _fullLogs)
			{
				this.cmbFullLog.Items.Add(log);
			}
			chkLogUpdated.IsChecked = true;
			this.txtStatus.Text = "GetFullLog. Done.";
		}

		private void ClearFullLog()
		{
			this.txtStatus.Text = "ClearFullLog. Clearing cmbFullLog & fullLogs...";
			chkLogUpdated.IsChecked = false;
			_fullLogs.Clear();
			this.cmbFullLog.Items.Clear();
			chkLogCleared.IsChecked = true;
			this.txtStatus.Text = "ClearFullLog. Done.";
		}

		private void BtnClearExceptionReport_Click(object sender, RoutedEventArgs e)
		{
			txtExceptionReport.Text = string.Empty;
		}

		private Thickness GetThicknessFromString(string thickness)
		{
			string[] lengths = thickness.Split(',');
			if (lengths.Length < 4)
				return new Thickness(
					Convert.ToDouble(lengths[0]));
			else
				return new Thickness(
					Convert.ToDouble(lengths[0]), Convert.ToDouble(lengths[1]), Convert.ToDouble(lengths[2]), Convert.ToDouble(lengths[3]));
		}

		private CornerRadius GetCornerRadiusFromString(string cornerRadius)
		{
			string[] lengths = cornerRadius.Split(',');
			if (lengths.Length < 4)
				return new CornerRadius(
					Convert.ToDouble(lengths[0]));
			else
				return new CornerRadius(
					Convert.ToDouble(lengths[0]), Convert.ToDouble(lengths[1]), Convert.ToDouble(lengths[2]), Convert.ToDouble(lengths[3]));
		}

		private IconElement GetIconElementFromString(string icon)
		{
			switch (icon)
			{
				case "Cut":
					return new SymbolIcon(Symbol.Cut);
				case "Copy":
					return new SymbolIcon(Symbol.Copy);
				case "Paste":
					return new SymbolIcon(Symbol.Paste);
				case "Bold":
					return new SymbolIcon(Symbol.Bold);
				case "Italic":
					return new SymbolIcon(Symbol.Italic);
				case "Underline":
					return new SymbolIcon(Symbol.Underline);
			}

			return null;
		}

		private string GetStringFromIconElement(IconElement iconElement)
		{
			SymbolIcon symbolIcon = iconElement as SymbolIcon;

			if (symbolIcon == null)
			{
				return "null";
			}

			return symbolIcon.Symbol.ToString();
		}

		private string GetStringFromChild(SelectorBarItem selectorBarItem)
		{
			if (selectorBarItem == null || selectorBarItem.Child == null)
			{
				return "null";
			}

			if (selectorBarItem.Child is TextBlock)
			{
				return (selectorBarItem.Child as TextBlock).Text;
			}
			else
			{
				return (selectorBarItem.Child as TextBox).Text;
			}
		}

		private string GetStringFromSelectorBarItem(SelectorBarItem selectorBarItem)
		{
			return "Icon:" + GetStringFromIconElement(selectorBarItem.Icon) + ", Text:" + selectorBarItem.Text + ", Child:" + GetStringFromChild(selectorBarItem);
		}

		private Symbol GetSymbolFromInt(int symbolIndex)
		{
			switch (symbolIndex)
			{
				case 1:
					return Symbol.Cut;
				case 2:
					return Symbol.Copy;
				case 3:
					return Symbol.Paste;
				case 4:
					return Symbol.Bold;
				case 5:
					return Symbol.Italic;
				default:
					return Symbol.Underline;
			}
		}

		private int GetIntFromSymbol(Symbol symbol)
		{
			switch (symbol)
			{
				case Symbol.Cut:
					return 1;
				case Symbol.Copy:
					return 2;
				case Symbol.Paste:
					return 3;
				case Symbol.Bold:
					return 4;
				case Symbol.Italic:
					return 5;
				case Symbol.Underline:
					return 6;
				default:
					return -1;
			}
		}

		private SelectorBarItem GetSelectorBarItem()
		{
			SelectorBarItem selectorBarItem = new SelectorBarItem();

			if (txtSelectorBarItemText != null)
			{
				selectorBarItem.Text = txtSelectorBarItemText.Text;
			}

			if (cmbSelectorBarItemIcon != null && cmbSelectorBarItemIcon.SelectedIndex > 0)
			{
				selectorBarItem.Icon = new SymbolIcon(GetSymbolFromInt(cmbSelectorBarItemIcon.SelectedIndex));
			}

			if (cmbSelectorBarItemChild != null && cmbSelectorBarItemChild.SelectedIndex > 0)
			{
				selectorBarItem.Child = cmbSelectorBarItemChild.SelectedIndex == 1 ? GetTextBlockChild() : GetTextBoxChild();
			}

			return selectorBarItem;
		}

		private TextBlock GetTextBlockChild()
		{
			TextBlock textBlock = new TextBlock()
			{
				Text = "TextBlock",
				VerticalAlignment = VerticalAlignment.Center
			};

			return textBlock;
		}

		private TextBox GetTextBoxChild()
		{
			TextBox textBox = new TextBox()
			{
				Text = "TextBox",
				VerticalAlignment = VerticalAlignment.Center
			};

			return textBox;
		}

		private Brush GetForeground()
		{
			switch (cmbSelectorBarItemForeground.SelectedIndex)
			{
				case -1:
				case 0:
					return null;
				case 1:
					return new SolidColorBrush(Colors.Black);
				case 2:
					return new SolidColorBrush(Colors.Red);
				default:
					return new SolidColorBrush(Colors.Green);
			}
		}
	}
}
