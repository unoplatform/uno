// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference: dxaml\test\managed\enterprise\Moco\ListViewBaseTests.cs, commit bac7a9c33ff

#pragma warning disable CS0168 // Disable TestCleanupWrapper warnings
#pragma warning disable 168 // for cleanup imported member

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Common;
using MUXControlsTestApp.Utilities;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Tests.Common;
using Private.Infrastructure;
using Uno.UI.RuntimeTests;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using ListView = Microsoft.UI.Xaml.Controls.ListView;
using UIExecutor = MUXControlsTestApp.Utilities.RunOnUIThread;

namespace Microsoft.UI.Xaml.Tests.Enterprise;

public partial class ListViewBaseTests
{
	[TestMethod]
	public async Task PageDownWithNoCache()
	{
		ListView list = null;
		IList<string> data = Enumerable.Range(0, 100).Select(x => "Item " + x).ToList();

		UIExecutor.Execute(() =>
		{
			list = new ListView();
			list.Height = 300;
			list.ItemsSource = data;
			list.Loaded += (s, e) =>
			{
				(list.ItemsPanelRoot as ItemsStackPanel).CacheLength = 0.75;
			};

			TestServices.WindowHelper.WindowContent = list;

			Verify.AreEqual(-1, list.SelectedIndex);
		});

		await TestServices.WindowHelper.WaitForIdle();
		UIExecutor.Execute(() =>
		{
			list.Focus(FocusState.Keyboard);
		});

		await TestServices.WindowHelper.WaitForIdle();
		for (int i = 0; i < 3; i++)
		{
			await TestServices.KeyboardHelper.PressKeySequence("$d$_pagedown#$u$_pagedown");
		}

		await TestServices.WindowHelper.WaitForIdle();
		UIExecutor.Execute(() =>
		{
			// verify that the selection moved and we did not crash.
			Verify.IsGreaterThan(list.SelectedIndex, 17);
		});
	}

	[TestMethod]
	[RequiresFullWindow]
	public async Task FocusResetAfterSwitchingItemsSource()
	{
		ListView list = null;
		FrameworkElement itemContainerToTap = null;
		int indexOfItemToTap = 2;
		UnoAutoResetEvent listLoaded = new(false);

		UIExecutor.Execute(() =>
		{
			list = LoadXamlFileIntoWindow(@"resources\managed\controls\ListViewBase\ListView.xaml");
			list.ItemsSource = Enumerable.Range(0, 10).Select(x => "Item " + x).ToList(); ;
			list.Loaded += (s, e) =>
			{
				listLoaded.Set();
			};
		});

		Verify.IsTrue(await listLoaded.WaitOne(TimeSpan.FromSeconds(10)), "Received loaded event from list");
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			itemContainerToTap = (FrameworkElement)list.ContainerFromIndex(indexOfItemToTap);
		});

		TestServices.InputHelper.Tap(itemContainerToTap);
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			Verify.AreEqual(indexOfItemToTap, list.SelectedIndex);
			list.ItemsSource = Enumerable.Range(0, 10).Select(x => "Item " + x).ToList();
		});

		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			var focusedElement = FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot) as ListViewItem;
			Verify.IsNotNull(focusedElement);
			int index = list.IndexFromContainer(focusedElement);

			Verify.AreEqual(0, index);
		});
	}

	[TestMethod]
	public async Task GroupPaddingShouldReduceMeasureSizeForHeadersAndItems_ItemsStackPanel()
	{
		await GroupPaddingShouldReduceMeasureSizeForHeadersAndItems(
			@"<ItemsPanelTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
				<ItemsStackPanel GroupPadding='10,20,30,40' Orientation='Vertical'/>
			 </ItemsPanelTemplate>");
	}

	[TestMethod]
	public async Task GroupPaddingShouldReduceMeasureSizeForHeadersAndItems_ItemsWrapGrid()
	{
		await GroupPaddingShouldReduceMeasureSizeForHeadersAndItems(
			@"<ItemsPanelTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
				<ItemsWrapGrid GroupPadding='10,20,30,40' Orientation='Horizontal'/>
			 </ItemsPanelTemplate>",
			true /*isItemsWrapGrid*/);
	}

	[TestMethod]
	public async Task ValidateListViewHeaderArrangBounds()
	{
		await ValidateHeaderArrangBounds(true /*useListView*/);
	}

	[TestMethod]
	public async Task ValidateGridViewHeaderArrangeBounds()
	{
		await ValidateHeaderArrangBounds(false /*useListView*/);
	}

	public async Task ValidateHeaderArrangBounds(bool useListView)
	{
		Microsoft.UI.Xaml.Controls.ListViewBase list = null;
		UnoAutoResetEvent listLoaded = new(false);

		UIExecutor.Execute(() =>
		{
			if (useListView)
			{
				list = new ListView();
			}
			else
			{
				list = new GridView();
			}

			list.ShowsScrollingPlaceholders = false;
			list.Height = 500;
			list.Width = 400;

			list.Loaded += (s, e) =>
			{
				listLoaded.Set();
			};

			ObservableCollection<ObservableCollection<int>> data = new();
			ObservableCollection<int> group = new();
			group.Add(0);
			data.Add(group);

			CollectionViewSource cvs = new() { IsSourceGrouped = true };
			cvs.Source = data;
			list.ItemsSource = cvs.View;

			list.GroupStyle.Add((GroupStyle)Microsoft.UI.Xaml.Markup.XamlReader.Load(
			   " <GroupStyle xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>        " +
			   "     <GroupStyle.HeaderTemplate>                                                       " +
			   "         <DataTemplate>                                                                " +
			   "             <Grid Background='Red' >                                                  " +
			   "                 <TextBlock Text='{Binding}' Foreground='White' FontSize='30'/>    " +
			   "             </Grid>                                                                   " +
			   "         </DataTemplate>                                                               " +
			   "     </GroupStyle.HeaderTemplate>                                                      " +
			   " </GroupStyle>                                                                         "));

			list.ItemTemplate = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(
			   " <DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'> " +
			   "     <TextBlock Text='{Binding}' />  " +
			   " </DataTemplate>");

			TestServices.WindowHelper.WindowContent = list;
		});

		Verify.IsTrue(await listLoaded.WaitOne(TimeSpan.FromSeconds(10)), "Received loaded event from list");
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			if (useListView)
			{
				var isp = list.ItemsPanelRoot as ItemsStackPanel;
				Verify.IsNotNull(isp);
				isp.GroupPadding = new Thickness(10, 20, 30, 40);
				isp.GroupHeaderPlacement = GroupHeaderPlacement.Top;
			}
			else
			{
				var iwg = list.ItemsPanelRoot as ItemsWrapGrid;
				Verify.IsNotNull(iwg);
				iwg.GroupPadding = new Thickness(10, 20, 30, 40);
			}

			list.InvalidateMeasure();
		});

		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			var container = list.ContainerFromIndex(0);
			var header = list.GroupHeaderContainerFromItemContainer(container) as FrameworkElement;
			// 400 - (10+30)//group padding
			Verify.AreEqual(360.0, header.ActualWidth);
		});
	}

	private async Task GroupPaddingShouldReduceMeasureSizeForHeadersAndItems(string panelTemplate, bool isItemsWrapGrid = false)
	{
		ListView list = null;
		var data = GetGroupedData(numGroups: 2, itemsPerGroup: 10);

		UIExecutor.Execute(() =>
		{
			list = new ListView();
			list.ShowsScrollingPlaceholders = false;
			list.Height = 300;
			list.Width = 300;

			// Expected Width for available size in measure is 260 pixels
			// windowConstraint - (grouppadding.Left + groupPadding.Right)
			// 300 - (10 + 30) = 260
			list.ChoosingGroupHeaderContainer += (s, args) =>
			{
				args.GroupHeaderContainer = new MyListViewHeaderItem() { Padding = new Thickness(0), ExpectedWidth = 260 };
			};

			list.ChoosingItemContainer += (s, args) =>
			{
				args.ItemContainer = new MyListViewItem() { Padding = new Thickness(0), ExpectedWidth = 260, IsParentItemsWrapGrid = isItemsWrapGrid };
			};

			CollectionViewSource cvs = new() { IsSourceGrouped = true };
			cvs.Source = data;
			list.ItemsSource = cvs.View;

			list.GroupStyle.Add((GroupStyle)Microsoft.UI.Xaml.Markup.XamlReader.Load(
			   @"<GroupStyle xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
					<GroupStyle.HeaderTemplate>
						<DataTemplate>
							<TextBlock Text='{Binding GroupTitle}'/>
						</DataTemplate>
					</GroupStyle.HeaderTemplate>
				</GroupStyle>"));

			list.ItemsPanel = (ItemsPanelTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(panelTemplate);

			TestServices.WindowHelper.WindowContent = list;
		});

		await TestServices.WindowHelper.WaitForIdle();

		// verification is done in Measure Override of MyListViewItem and MyListViewHeaderItem
		// we just ensure below that items were shown on screen

		UIExecutor.Execute(() =>
		{
			int lastVisibleIndex = 0;
			if (list.ItemsPanelRoot is ItemsStackPanel)
			{
				lastVisibleIndex = (list.ItemsPanelRoot as ItemsStackPanel).LastCacheIndex;
			}
			else
			{
				lastVisibleIndex = (list.ItemsPanelRoot as ItemsWrapGrid).LastCacheIndex;
			}

			Verify.AreNotEqual(lastVisibleIndex, 0);
		});
	}

	[TestMethod]
	public async Task SettingProgrammaticFocusDoesNotGetRedirectedToSelectedItem()
	{
		ListView listView = null;
		Button button = null;

		const int selectedIndex = 1;
		const int expectedFocusedIndex = 2;
		ListViewItem expectedFocusedItem = null;

		UIExecutor.Execute(() =>
		{
			listView = new ListView();
			for (uint i = 0; i < 5; ++i)
			{
				listView.Items.Add(string.Format("Item {0}", i + 1));
			}
			listView.SingleSelectionFollowsFocus = true;
			listView.SelectedIndex = selectedIndex;

			button = new Button() { Content = "button" };

			StackPanel root = new();
			root.Children.Add(button);
			root.Children.Add(listView);

			TestServices.WindowHelper.WindowContent = root;
		});
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			button.Focus(FocusState.Keyboard);
		});
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			expectedFocusedItem = listView.ContainerFromIndex(expectedFocusedIndex) as ListViewItem;
			expectedFocusedItem.Focus(FocusState.Keyboard);
		});
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			var focusedElement = FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot);
			Verify.IsTrue(focusedElement.Equals(expectedFocusedItem));
		});
	}

	[TestMethod]
	public async Task ValidateNonGroupingFocusOrderWithTabNavigationLocal()
	{
		ListView listView = null;
		Button button = null;

		int numItems = 5;

		UIExecutor.Execute(() =>
		{
			listView = new ListView() { TabNavigation = KeyboardNavigationMode.Local };
			for (int i = 0; i < numItems; ++i)
			{
				// Disable all the even numbered items.
				listView.Items.Add(new ListViewItem() { Content = string.Format("Item {0}", i + 1) });
			}

			button = new Button() { Content = "button" };

			StackPanel root = new();
			root.Children.Add(button);
			root.Children.Add(listView);

			TestServices.WindowHelper.WindowContent = root;
		});
		await TestServices.WindowHelper.WaitForIdle();

		// Start with focus on the button.
		UIExecutor.Execute(() =>
		{
			button.Focus(FocusState.Keyboard);
		});
		await TestServices.WindowHelper.WaitForIdle();

		// Validate forward tab navigation.
		for (int i = 0; i < numItems; ++i)
		{
			await TestServices.KeyboardHelper.Tab();
			await TestServices.WindowHelper.WaitForIdle();

			UIExecutor.Execute(() =>
			{
				var expectedFocusedItem = listView.Items[i] as ListViewItem;
				var focusedElement = FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot);
				Verify.AreEqual(expectedFocusedItem, focusedElement);
			});
		}

		// Tab once more to get focus back on the button.
		await TestServices.KeyboardHelper.Tab();
		await TestServices.WindowHelper.WaitForIdle();

		// Validate backward tab navigation.
		for (int i = numItems - 1; i >= 0; --i)
		{
			await TestServices.KeyboardHelper.ShiftTab();
			await TestServices.WindowHelper.WaitForIdle();

			UIExecutor.Execute(() =>
			{
				var expectedFocusedItem = listView.Items[i] as ListViewItem;
				var focusedElement = FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot);
				Verify.AreEqual(expectedFocusedItem, focusedElement);
			});
		}
	}

	[TestMethod]
	public async Task DoesTabNavigationLocalSkipOverDisabledItems()
	{
		ListView listView = null;
		Button button = null;

		UIExecutor.Execute(() =>
		{
			listView = new ListView() { TabNavigation = KeyboardNavigationMode.Local };
			for (uint i = 0; i < 5; ++i)
			{
				// Disable all the even numbered items.
				listView.Items.Add(new ListViewItem() { Content = string.Format("Item {0}", i + 1), IsEnabled = (i % 2 != 0) });
			}

			button = new Button() { Content = "button" };

			StackPanel root = new();
			root.Children.Add(button);
			root.Children.Add(listView);

			TestServices.WindowHelper.WindowContent = root;
		});
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			button.Focus(FocusState.Keyboard);
		});
		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.KeyboardHelper.Tab();
		await TestServices.WindowHelper.WaitForIdle();

		// We should have skipped over the 1st item, because it is disabled, and
		// focused the second one.
		UIExecutor.Execute(() =>
		{
			var expectedFocusedItem = listView.Items[1] as ListViewItem;
			var focusedElement = FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot);
			Verify.AreEqual(expectedFocusedItem, focusedElement);
		});

		await TestServices.KeyboardHelper.Tab();
		await TestServices.WindowHelper.WaitForIdle();

		// We should have skipped over the 3rd item, because it is disabled, and
		// focused the fourth one.
		UIExecutor.Execute(() =>
		{
			var expectedFocusedItem = listView.Items[3] as ListViewItem;
			var focusedElement = FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot);
			Verify.AreEqual(expectedFocusedItem, focusedElement);
		});
	}

	[TestMethod]
	public async Task DoesBottomsUpListTabNavigationInitiallyFocusLastItem()
	{
		ListView listView = null;
		Button button = null;

		UIExecutor.Execute(() =>
		{
			listView = new ListView();
			for (uint i = 0; i < 5; ++i)
			{
				// Disable all the even numbered items.
				listView.Items.Add(new ListViewItem() { Content = string.Format("Item {0}", i + 1) });
			}

			button = new Button() { Content = "button" };

			StackPanel root = new();
			root.Children.Add(button);
			root.Children.Add(listView);

			TestServices.WindowHelper.WindowContent = root;
		});
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			var itemsStackPanel = listView.ItemsPanelRoot as ItemsStackPanel;
			itemsStackPanel.ItemsUpdatingScrollMode = ItemsUpdatingScrollMode.KeepLastItemInView;

			button.Focus(FocusState.Keyboard);
		});
		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.KeyboardHelper.Tab();
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			var expectedFocusedItem = listView.Items[listView.Items.Count - 1] as ListViewItem;
			var focusedElement = FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot);
			Verify.AreEqual(expectedFocusedItem, focusedElement);
		});
	}

	[TestMethod]
	[RequiresFullWindow]
	public async Task DoesRestoreFocusToPreviouslyFocusedItemWhenTabbingBackIn()
	{
		ListView listView = null;
		Button button = null;

		const int expectedFocusIndex = 3;

		UIExecutor.Execute(() =>
		{
			listView = new ListView();
			for (uint i = 0; i < 5; ++i)
			{
				// Disable all the even numbered items.
				listView.Items.Add(new ListViewItem() { Content = string.Format("Item {0}", i + 1) });
			}

			button = new Button() { Content = "button" };

			StackPanel root = new();
			root.Children.Add(button);
			root.Children.Add(listView);

			TestServices.WindowHelper.WindowContent = root;
		});
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			var expectedFocusedItem = listView.Items[expectedFocusIndex] as ListViewItem;
			expectedFocusedItem.Focus(FocusState.Keyboard);
		});
		await TestServices.WindowHelper.WaitForIdle();

		// Tab once to get out of the list and put focus on the button.
		await TestServices.KeyboardHelper.Tab();
		await TestServices.WindowHelper.WaitForIdle();

		// Tab again to put focus back into the list.
		await TestServices.KeyboardHelper.Tab();
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			var expectedFocusedItem = listView.Items[expectedFocusIndex] as ListViewItem;
			var focusedElement = FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot);
			Verify.AreEqual(expectedFocusedItem, focusedElement);
		});
	}

	[TestMethod]
	public async Task DoesNotHandleKeyboardArrowKeysWithFocusOnHeaderOrFooter()
	{
		ListView listView = null;
		Button header = null;
		Button footer = null;

		UnoAutoResetEvent keyDownEvent = new(false);

		UIExecutor.Execute(() =>
		{
			listView = new ListView();
			listView.Items.Add("Item");
			header = new Button() { Content = "Header" };
			footer = new Button() { Content = "Footer" };

			listView.Header = header;
			listView.Footer = footer;

			Grid root = new();
			root.Children.Add(listView);

			root.KeyDown += (s, e) => { keyDownEvent.Set(); };

			TestServices.WindowHelper.WindowContent = root;
		});
		await TestServices.WindowHelper.WaitForIdle();

		Func<Button, Task> runScenario = async (Button target) =>
		{
			UIExecutor.Execute(() =>
			{
				target.Focus(FocusState.Keyboard);
			});
			await TestServices.WindowHelper.WaitForIdle();

			await TestServices.KeyboardHelper.Left();
			await TestServices.WindowHelper.WaitForIdle();
			await keyDownEvent.WaitOne();
			UIExecutor.Execute(() => { Verify.AreEqual(target, FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot)); });

			await TestServices.KeyboardHelper.Right();
			await TestServices.WindowHelper.WaitForIdle();
			await keyDownEvent.WaitOne();
			UIExecutor.Execute(() => { Verify.AreEqual(target, FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot)); });

			await TestServices.KeyboardHelper.Up();
			await TestServices.WindowHelper.WaitForIdle();
			await keyDownEvent.WaitOne();
			UIExecutor.Execute(() => { Verify.AreEqual(target, FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot)); });

			await TestServices.KeyboardHelper.Down();
			await TestServices.WindowHelper.WaitForIdle();
			await keyDownEvent.WaitOne();
			UIExecutor.Execute(() => { Verify.AreEqual(target, FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot)); });
		};

		Log.Comment("Validate arrow keys are not handled for header object.");
		await runScenario(header);

		Log.Comment("Validate arrow keys are not handled for footer object.");
		await runScenario(footer);
	}

	[TestMethod]
	public async Task HeaderOrFooterDoesNotTrap2DNavigation()
	{
		ListView listView = null;
		Button beforeButton = null;
		Button afterButton = null;

		UnoAutoResetEvent keyDownEvent = new(false);

		UIExecutor.Execute(() =>
		{
			listView = new ListView();
			listView.Items.Add("Item");
			listView.Header = new Button() { Content = "Header" };
			listView.Footer = new Button() { Content = "Footer" };

			beforeButton = new Button() { Content = "Before" };
			afterButton = new Button() { Content = "After" };

			StackPanel root = new() { XYFocusKeyboardNavigation = XYFocusKeyboardNavigationMode.Enabled };
			root.Children.Add(beforeButton);
			root.Children.Add(listView);
			root.Children.Add(afterButton);

			root.KeyDown += (s, e) => { keyDownEvent.Set(); };

			TestServices.WindowHelper.WindowContent = root;
		});
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			beforeButton.Focus(FocusState.Keyboard);
		});
		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.KeyboardHelper.Down();
		await TestServices.WindowHelper.WaitForIdle();
		UIExecutor.Execute(() => { Verify.AreEqual(listView.Header, FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot)); });

		await TestServices.KeyboardHelper.Down();
		await TestServices.WindowHelper.WaitForIdle();
		UIExecutor.Execute(() => { Verify.AreEqual(listView.ContainerFromIndex(0), FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot)); });

		await TestServices.KeyboardHelper.Down();
		await TestServices.WindowHelper.WaitForIdle();
		UIExecutor.Execute(() => { Verify.AreEqual(listView.Footer, FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot)); });

		await TestServices.KeyboardHelper.Down();
		await TestServices.WindowHelper.WaitForIdle();
		UIExecutor.Execute(() => { Verify.AreEqual(afterButton, FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot)); });
	}

	[TestMethod]
	[RequiresFullWindow]
	[TestProperty("Description", "If focus is set to a ListViewItem programmatically (or via 2D navigation) and the item is then selected, it should not require 2 tabs to move focus out of the ListView.")]
	public async Task DoesNotRequireTwoTabsToMoveFocusWhenItemIsFocusedProgrammaticallyAndThenSelected()
	{
		ListView listView = null;
		Button button = null;

		UIExecutor.Execute(() =>
		{
			listView = new ListView();
			listView.Items.Add("Item");

			button = new Button() { Content = "Button" };

			StackPanel root = new();
			root.Children.Add(button);
			root.Children.Add(listView);

			TestServices.WindowHelper.WindowContent = root;
		});
		await TestServices.WindowHelper.WaitForIdle();

		Log.Comment("Programmatically focus the item and then select it.");
		UIExecutor.Execute(() =>
		{
			var item = (ListViewItem)listView.ContainerFromIndex(0);
			item.Focus(FocusState.Keyboard);

			listView.SelectedIndex = 0;
		});
		await TestServices.WindowHelper.WaitForIdle();

		Log.Comment("Press TAB -> focus should move out of the ListView.");
		await TestServices.KeyboardHelper.Tab();
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			Verify.AreEqual(button, FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot));
		});
	}

	[TestMethod]
	public async Task ValidateKeyNavWithVariableSizedItems()
	{
		GridView gridView = null;
		UIExecutor.Execute(() =>
		{
			// Gridview with three items spanning multiple rows. Even when items span multiple rows, key navigation
			// should go through items only once. We do not wrap at the edges since the items might be non-contiguous.
			// | 0 | 1 |
			// |   2   |
			gridView = (GridView)XamlReader.Load(@"<GridView Width='200' Height='300' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
														<GridView.ItemsPanel>
															<ItemsPanelTemplate>
																<VariableSizedWrapGrid ItemHeight='100' ItemWidth='100' MaximumRowsOrColumns='6' Orientation='Horizontal' />
															</ItemsPanelTemplate>
														</GridView.ItemsPanel>
														<GridViewItem VariableSizedWrapGrid.ColumnSpan='1' VariableSizedWrapGrid.RowSpan='2'>0</GridViewItem>
														<GridViewItem VariableSizedWrapGrid.ColumnSpan='1' VariableSizedWrapGrid.RowSpan='2'>1</GridViewItem>
														<GridViewItem VariableSizedWrapGrid.ColumnSpan='1' VariableSizedWrapGrid.RowSpan='1'>2</GridViewItem>
													</GridView>");
			TestServices.WindowHelper.WindowContent = gridView;
		});

		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			gridView.Focus(FocusState.Keyboard);
		});

		// layout is as follows. VSWG does not wrap on key navigation
		// | 0 | 1 |
		// |   2   |
		await ValidateFocusedGridViewItemContent("0");

		await TestServices.KeyboardHelper.Right();
		await ValidateFocusedGridViewItemContent("1");

		await TestServices.KeyboardHelper.Right();
		await ValidateFocusedGridViewItemContent("1");

		await TestServices.KeyboardHelper.Left();
		await ValidateFocusedGridViewItemContent("0");

		await TestServices.KeyboardHelper.Left();
		await ValidateFocusedGridViewItemContent("0");

		await TestServices.KeyboardHelper.Down();
		await ValidateFocusedGridViewItemContent("2");

		await TestServices.KeyboardHelper.Down();
		await ValidateFocusedGridViewItemContent("2");

		await TestServices.KeyboardHelper.Up();
		await ValidateFocusedGridViewItemContent("0");

		await TestServices.KeyboardHelper.Up();
		await ValidateFocusedGridViewItemContent("0");
	}

	[TestMethod]
	public async Task ValidateHomeEndKeyNavWithDisabledItems()
	{
		GridView gridView = null;

		UIExecutor.Execute(() =>
		{
			gridView = (GridView)XamlReader.Load(@"<GridView  xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
													  <GridViewItem IsEnabled='False'>0</GridViewItem>
													  <GridViewItem IsEnabled='False'>1</GridViewItem>
													  <GridViewItem>2</GridViewItem>
													  <GridViewItem>3</GridViewItem>
													  <GridViewItem>4</GridViewItem>
													  <GridViewItem>5</GridViewItem>
													  <GridViewItem IsEnabled='False'>6</GridViewItem>
													  <GridViewItem IsEnabled='False'>7</GridViewItem>
												  </GridView>");
			TestServices.WindowHelper.WindowContent = gridView;
		});

		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			gridView.Focus(FocusState.Keyboard);
		});

		await TestServices.WindowHelper.WaitForIdle();
		Verify.AreEqual(2, GetFocusedItemIndex(gridView));

		// End should put focus on the last eligible item which is 5
		await ListViewBaseTestsInput.End();
		await TestServices.WindowHelper.WaitForIdle();
		Verify.AreEqual(5, GetFocusedItemIndex(gridView));

		// Home should put focus on first eligible item which is 2
		await ListViewBaseTestsInput.Home();
		await TestServices.WindowHelper.WaitForIdle();
		Verify.AreEqual(2, GetFocusedItemIndex(gridView));

		// Disable all but one of the items in the girdview. Focus should
		// stay on the same item.
		UIExecutor.Execute(() =>
		{
			((GridViewItem)gridView.ContainerFromIndex(2)).IsEnabled = false;
			((GridViewItem)gridView.ContainerFromIndex(3)).Visibility = Visibility.Collapsed;
			((GridViewItem)gridView.ContainerFromIndex(4)).IsTabStop = false;

			// set focus on the only focusible item
			((GridViewItem)gridView.ContainerFromIndex(5)).Focus(FocusState.Keyboard);
		});

		// End should put focus on the last eligible item which is 5
		await ListViewBaseTestsInput.End();
		await TestServices.WindowHelper.WaitForIdle();
		Verify.AreEqual(5, GetFocusedItemIndex(gridView));

		// Home should put focus on first eligible item which is also 5
		await ListViewBaseTestsInput.Home();
		await TestServices.WindowHelper.WaitForIdle();
		Verify.AreEqual(5, GetFocusedItemIndex(gridView));
	}

	[TestMethod]
	[Description("Home/End key press when focus is on a group header caused us to mess up key navigation")]
	public async Task ValidateListViewHomeEndAndArrowKeyNav()
	{
		ListView listView = null;
		const int numGroups = 30;
		const int itemsPerGroup = 2;
		UIExecutor.Execute((Action)(() =>
		{
			listView = new ListView();
			listView.ShowsScrollingPlaceholders = false;
			listView.Height = 500;
			CollectionViewSource cvs = new() { IsSourceGrouped = true };
			cvs.Source = GetGroupedData(numGroups, itemsPerGroup);

			listView.GroupStyle.Add((GroupStyle)Microsoft.UI.Xaml.Markup.XamlReader.Load(
			   " <GroupStyle xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>        " +
			   "     <GroupStyle.HeaderTemplate>                                                       " +
			   "         <DataTemplate>                                                                " +
			   "             <Grid Background='Red' >                                                  " +
			   "                 <TextBlock Text='{Binding}' Foreground='White' FontSize='30'/>    " +
			   "             </Grid>                                                                   " +
			   "         </DataTemplate>                                                               " +
			   "     </GroupStyle.HeaderTemplate>                                                      " +
			   " </GroupStyle>                                                                         "));
			listView.ItemsSource = cvs.View;
			TestServices.WindowHelper.WindowContent = listView;
		}));

		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			var thirdContainer = listView.ContainerFromIndex(2);
			var secondHeader = (ListViewHeaderItem)listView.GroupHeaderContainerFromItemContainer(thirdContainer);
			secondHeader.Focus(FocusState.Keyboard);
		});

		const int totalNumberOfItems = numGroups * itemsPerGroup;
		await TestServices.WindowHelper.WaitForIdle();
		await ListViewBaseTestsInput.End();
		Verify.AreEqual(totalNumberOfItems - 1, GetFocusedItemIndex(listView));

		await TestServices.KeyboardHelper.Up();
		Verify.AreEqual(totalNumberOfItems - 2, GetFocusedItemIndex(listView));

		await TestServices.KeyboardHelper.Up();
		UIExecutor.Execute(() =>
		{
			var focusedElement = (ListViewHeaderItem)FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot);
			Verify.IsNotNull(focusedElement);
		});

		await TestServices.WindowHelper.WaitForIdle();
		await ListViewBaseTestsInput.Home();
		Verify.AreEqual(0, GetFocusedItemIndex(listView));

		await TestServices.KeyboardHelper.Down();
		Verify.AreEqual(1, GetFocusedItemIndex(listView));
	}

	[TestMethod]
	[RequiresFullWindow]
	[TestProperty("Description", "When a ListView is the root focusable control in the tree, validate that focus does not get trapped within the header sub-tree with multiple focusable controls.")]
	public async Task CanTabOutOfHeaderWithMultipleFocusableChildren()
	{
		ListView listView = null;
		Button button1 = null;

		UIExecutor.Execute(() =>
		{
			listView = (ListView)XamlReader.Load(@"<ListView
													   xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
													   xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
													   <ListView.Header>
														   <StackPanel>
															   <Button x:Name='button1' Content='Button 1'/>
															   <Button Content='Button 2'/>
														   </StackPanel>
													   </ListView.Header>
													   <ListViewItem Content='Item 1'/>
												   </ListView>");

			TestServices.WindowHelper.WindowContent = listView;
		});
		await TestServices.WindowHelper.WaitForIdle();

		// TODO Uno: FindName walks the visual tree, and the header and footer only join it once loaded.
		UIExecutor.Execute(() => button1 = (Button)listView.FindName("button1"));

		UIExecutor.Execute(() =>
		{
			listView.Focus(FocusState.Keyboard);
		});
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			if (!Object.ReferenceEquals(FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot), button1))
			{
				throw new Exception("First button in the header should have focus.");
			}
		});

		Log.Comment("Press TAB twice, which should move focus past the second header button and onto the first item.");
		await TestServices.KeyboardHelper.Tab();
		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.KeyboardHelper.Tab();
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			var firstItemContainer = listView.ContainerFromIndex(0);
			Verify.AreEqual(firstItemContainer, FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot));
		});
	}

	[TestMethod]
	[RequiresFullWindow]
	[TestProperty("Description", "When a ListView is the root focusable control in the tree, validate that focus does not get trapped within the footer sub-tree with multiple focusable controls.")]
	public async Task CanShiftTabOutOfFooterWithMultipleFocusableChildren()
	{
		ListView listView = null;
		Button button2 = null;

		UIExecutor.Execute(() =>
		{
			listView = (ListView)XamlReader.Load(@"<ListView
													   xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
													   xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
													   <ListView.Footer>
														   <StackPanel>
															   <Button Content='Button 1'/>
															   <Button x:Name='button2' Content='Button 2'/>
														   </StackPanel>
													   </ListView.Footer>
													   <ListViewItem Content='Item 1'/>
												   </ListView>");

			TestServices.WindowHelper.WindowContent = listView;
		});
		await TestServices.WindowHelper.WaitForIdle();

		// TODO Uno: FindName walks the visual tree, and the header and footer only join it once loaded.
		UIExecutor.Execute(() => button2 = (Button)listView.FindName("button2"));

		UIExecutor.Execute(() =>
		{
			button2.Focus(FocusState.Keyboard);
		});
		await TestServices.WindowHelper.WaitForIdle();

		Log.Comment("Press SHIFT-TAB twice, which should move focus past the first footer button and onto the first item.");
		await TestServices.KeyboardHelper.ShiftTab();
		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.KeyboardHelper.ShiftTab();
		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			var firstItemContainer = listView.ContainerFromIndex(0);
			Verify.AreEqual(firstItemContainer, FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot));
		});
	}

	[TestMethod]
	[RequiresFullWindow]
	[TestProperty("Description", "If the selected item's container is not focusable, validate that focus is sent to children instead.")]
	public async Task DoesFocusChildrenOfSelectedItemIfContainerIsNotFocusable()
	{
		StackPanel root = null;
		ListView listView = null;
		Button beforeButton = null;
		Button afterButton = null;
		Button firstFocusableChild = null;
		Button lastFocusableChild = null;

		UIExecutor.Execute(() =>
		{
			firstFocusableChild = new Button() { Content = "First Focusable" };
			lastFocusableChild = new Button() { Content = "Last Focusable" };

			StackPanel itemContentRoot = new();
			itemContentRoot.Children.Add(firstFocusableChild);
			itemContentRoot.Children.Add(lastFocusableChild);

			listView = new ListView();
			listView.Items.Add(new ListViewItem() { IsTabStop = false, Content = itemContentRoot });
			listView.SelectedIndex = 0;

			beforeButton = new Button() { Content = "Before" };
			afterButton = new Button() { Content = "After" };

			root = new StackPanel()
			{
				XYFocusKeyboardNavigation = XYFocusKeyboardNavigationMode.Enabled,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};

			root.Children.Add(beforeButton);
			root.Children.Add(listView);
			root.Children.Add(afterButton);

			TestServices.WindowHelper.WindowContent = root;
		});
		await TestServices.WindowHelper.WaitForIdle();

		Func<Func<Task>, Func<Task>, UIElement, UIElement, Task> runScenario = async (Func<Task> fowardNav, Func<Task> backwardNav, UIElement forwardExpectedFocused, UIElement backwardExpectedFocused) =>
		{
			Log.Comment("Start focus on the control before the ListView.");
			UIExecutor.Execute(() =>
			{
				beforeButton.Focus(FocusState.Keyboard);
			});
			await TestServices.WindowHelper.WaitForIdle();

			Log.Comment("Navigate Focus Forwards -> focus should move into the selected item's container and focus the forwardExpectedFocused element.");
			await fowardNav();
			await TestServices.WindowHelper.WaitForIdle();

			UIExecutor.Execute(() =>
			{
				Verify.AreEqual(forwardExpectedFocused, FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot));
			});

			Log.Comment("Reset focus to the control after the ListView.");
			UIExecutor.Execute(() =>
			{
				afterButton.Focus(FocusState.Keyboard);
			});
			await TestServices.WindowHelper.WaitForIdle();

			Log.Comment("Navigate Focus Backwards -> focus should move into the selected item's container and focus the backwardExpectedFocused element.");
			await backwardNav();
			await TestServices.WindowHelper.WaitForIdle();

			UIExecutor.Execute(() =>
			{
				Verify.AreEqual(backwardExpectedFocused, FocusManager.GetFocusedElement(TestServices.WindowHelper.WindowContent.XamlRoot));
			});
		};

		Log.Comment("Run Scenario with Tab/Shift-Tab.");
		await runScenario(async () => { await TestServices.KeyboardHelper.Tab(); }, async () => { await TestServices.KeyboardHelper.ShiftTab(); }, firstFocusableChild, lastFocusableChild);

		Log.Comment("Run Scenario with Down/Up.");
		await runScenario(async () => { await TestServices.KeyboardHelper.Down(); }, async () => { await TestServices.KeyboardHelper.Up(); }, firstFocusableChild, firstFocusableChild);

		// The following scenarios use horizontal navigation, so change the orientation of the panel.
		UIExecutor.Execute(() => { root.Orientation = Orientation.Horizontal; });
		await TestServices.WindowHelper.WaitForIdle();

		Log.Comment("Run Scenario with Right/Left.");
		await runScenario(async () => { await TestServices.KeyboardHelper.Right(); }, async () => { await TestServices.KeyboardHelper.Left(); }, firstFocusableChild, firstFocusableChild);

		// The next scenario requires us to be in RTL.
		UIExecutor.Execute(() => { root.FlowDirection = FlowDirection.RightToLeft; });
		await TestServices.WindowHelper.WaitForIdle();

		Log.Comment("Run Scenario with Left/Right (RTL-case).");
		await runScenario(async () => { await TestServices.KeyboardHelper.Left(); }, async () => { await TestServices.KeyboardHelper.Right(); }, firstFocusableChild, firstFocusableChild);
	}


	[TestMethod]
	public async Task CanRemoveGroupFromListViewWithEmptyGroupAfter()
	{
		ListView listView = null;

		ObservableCollection<Group> data = null;

		UIExecutor.Execute(() =>
		{
			listView = new ListView();

			listView.GroupStyle.Add((GroupStyle)Microsoft.UI.Xaml.Markup.XamlReader.Load(
				" <GroupStyle xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>        " +
				"     <GroupStyle.HeaderTemplate>                                                       " +
				"         <DataTemplate>                                                                " +
				"             <Grid Background='Red' >                                                  " +
				"                 <TextBlock Text='{Binding Name}' Foreground='White' FontSize='30'/>   " +
				"             </Grid>                                                                   " +
				"         </DataTemplate>                                                               " +
				"     </GroupStyle.HeaderTemplate>                                                      " +
				" </GroupStyle>                                                                         "));

			Grid root = new();
			root.Children.Add(listView);

			TestServices.WindowHelper.WindowContent = root;
		});

		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			data = new ObservableCollection<Group>(new Group[]
			{
				new Group("Non-empty Group #1", new string[] { "Item #1" }),
				new Group("Non-empty Group #2", new string[] { "Item #2", "Item #3" }),
				new Group("Empty group")
			});

			CollectionViewSource cvs = new();
			cvs.Source = data;
			cvs.IsSourceGrouped = true;
			listView.ItemsSource = cvs.View;
		});

		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			data.RemoveAt(1);
		});
	}

	[TestMethod]
	public async Task CanRemoveFromGroupInListViewWithEmptyGroupBefore()
	{
		ListView listView = null;

		ObservableCollection<Group> data = null;

		UIExecutor.Execute(() =>
		{
			listView = new ListView();

			listView.GroupStyle.Add((GroupStyle)Microsoft.UI.Xaml.Markup.XamlReader.Load(
				" <GroupStyle xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>        " +
				"     <GroupStyle.HeaderTemplate>                                                       " +
				"         <DataTemplate>                                                                " +
				"             <Grid Background='Red' >                                                  " +
				"                 <TextBlock Text='{Binding Name}' Foreground='White' FontSize='30'/>   " +
				"             </Grid>                                                                   " +
				"         </DataTemplate>                                                               " +
				"     </GroupStyle.HeaderTemplate>                                                      " +
				" </GroupStyle>                                                                         "));

			Grid root = new();
			root.Children.Add(listView);

			data = new ObservableCollection<Group>(new Group[]
			{
				new Group("Empty Group", new string[] {}),
				new Group("Non-empty Group #1", new string[] { "Item #1" })
			});

			CollectionViewSource cvs = new();
			cvs.Source = data;
			cvs.IsSourceGrouped = true;
			listView.ItemsSource = cvs.View;

			TestServices.WindowHelper.WindowContent = root;
		});

		await TestServices.WindowHelper.WaitForIdle();

		UIExecutor.Execute(() =>
		{
			data[1].Insert(0, "New Item");
			data[1].RemoveAt(1);

			// We used to crash during the next layout pass
			listView.UpdateLayout();

			// Validate the layout
			Verify.AreEqual("New Item", ((ListViewItem)listView.ContainerFromIndex(0)).Content);
			//Verify.AreEqual("Non-empty Group #1", ((ListViewHeaderItem)listView.GroupHeaderContainerFromItemContainer(listView.ContainerFromIndex(0))).Content);
			Verify.AreEqual("Non-empty Group #1", ((Group)((ListViewHeaderItem)listView.GroupHeaderContainerFromItemContainer(listView.ContainerFromIndex(0))).Content).Name);
		});

		await TestServices.WindowHelper.WaitForIdle();
	}
}
