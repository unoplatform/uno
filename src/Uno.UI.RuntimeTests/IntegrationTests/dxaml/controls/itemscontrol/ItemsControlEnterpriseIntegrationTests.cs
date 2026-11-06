// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Tests.Common;
using Private.Infrastructure;
using Uno.UI.RuntimeTests;
using static Private.Infrastructure.TestServices;

namespace Microsoft.UI.Xaml.Tests.Enterprise;

[TestClass]
[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
public class ItemsControlEnterpriseIntegrationTests : BaseDxamlTestClass
{
	[Flags]
	private enum VerifyTabOrderFlags
	{
		Default = 0,
		UseVirtualizingStackPanelAsItemsPanel = 1,
		UseStackPanelAsItemsPanel = 2,
		UseHeaderAndFooter = 4,
		UseGroupedItems = 8
	}

	//
	// Class & Test Setup
	//
	[ClassInitialize]
	public static void ClassSetup()
	{
		CommonTestSetupHelper.CommonTestClassSetup();
	}

	[ClassCleanup]
	public static void ClassCleanup()
	{
	}

	[TestCleanup]
	public void TestCleanup()
	{
		WindowHelper.VerifyTestCleanup();
	}

	[TestMethod]
	public async Task VerifyTabOrderWithLocalTabNavigation()
	{
		string expectedFocusSequence = "[B][1][.1][2][.1][3][.1][A][.1][3][.1][2][.1][1][B]";

		await VerifyTabOrder(KeyboardNavigationMode.Local, VerifyTabOrderFlags.Default, expectedFocusSequence);
		await VerifyTabOrder(KeyboardNavigationMode.Local, VerifyTabOrderFlags.UseVirtualizingStackPanelAsItemsPanel, expectedFocusSequence);
		await VerifyTabOrder(KeyboardNavigationMode.Local, VerifyTabOrderFlags.UseStackPanelAsItemsPanel, expectedFocusSequence);
	}

	[TestMethod]
	public async Task VerifyTabOrderWithCycleTabNavigation()
	{
		string expectedFocusSequence = "[B][1][.1][2][.1][3][.1][1][.1][3][.1][2][.1][1][.1]";

		await VerifyTabOrder(KeyboardNavigationMode.Cycle, VerifyTabOrderFlags.Default, expectedFocusSequence);
		await VerifyTabOrder(KeyboardNavigationMode.Cycle, VerifyTabOrderFlags.UseVirtualizingStackPanelAsItemsPanel, expectedFocusSequence);
		await VerifyTabOrder(KeyboardNavigationMode.Cycle, VerifyTabOrderFlags.UseStackPanelAsItemsPanel, expectedFocusSequence);
	}

	[TestMethod]
	public async Task VerifyTabOrderWithOnceTabNavigation()
	{
		string expectedFocusSequence = "[B][1][A][B][A][.1][1]";

		await VerifyTabOrder(KeyboardNavigationMode.Once, VerifyTabOrderFlags.Default, expectedFocusSequence);
		await VerifyTabOrder(KeyboardNavigationMode.Once, VerifyTabOrderFlags.UseVirtualizingStackPanelAsItemsPanel, expectedFocusSequence);
		await VerifyTabOrder(KeyboardNavigationMode.Once, VerifyTabOrderFlags.UseStackPanelAsItemsPanel, expectedFocusSequence);
	}

	[TestMethod]
	public async Task VerifyTabOrderWithLocalTabNavigationHeaderAndFooter()
	{
		await VerifyTabOrder(KeyboardNavigationMode.Local, VerifyTabOrderFlags.UseHeaderAndFooter, "[B][H][1][.1][2][.1][3][.1][F][A][F][.1][3][.1][2][.1][1][H][B]");
		await VerifyTabOrder(KeyboardNavigationMode.Local, VerifyTabOrderFlags.UseHeaderAndFooter | VerifyTabOrderFlags.UseVirtualizingStackPanelAsItemsPanel, "[B][H][1][.1][2][.1][3][.1][A][.1][3][.1][2][.1][1][H][B]");
		await VerifyTabOrder(KeyboardNavigationMode.Local, VerifyTabOrderFlags.UseHeaderAndFooter | VerifyTabOrderFlags.UseStackPanelAsItemsPanel, "[B][H][1][.1][2][.1][3][.1][F][A][F][.1][3][.1][2][.1][1][H][B]");
	}

	[TestMethod]
	public async Task VerifyTabOrderWithCycleTabNavigationHeaderAndFooter()
	{
		await VerifyTabOrder(KeyboardNavigationMode.Cycle, VerifyTabOrderFlags.UseHeaderAndFooter, "[B][H][1][.1][2][.1][3][.1][F][H][F][.1][3][.1][2][.1][1][H][F]");
		await VerifyTabOrder(KeyboardNavigationMode.Cycle, VerifyTabOrderFlags.UseHeaderAndFooter | VerifyTabOrderFlags.UseVirtualizingStackPanelAsItemsPanel, "[B][H][1][.1][2][.1][3][.1][H][.1][3][.1][2][.1][1][H][.1]");
		await VerifyTabOrder(KeyboardNavigationMode.Cycle, VerifyTabOrderFlags.UseHeaderAndFooter | VerifyTabOrderFlags.UseStackPanelAsItemsPanel, "[B][H][1][.1][2][.1][3][.1][F][H][F][.1][3][.1][2][.1][1][H][F]");
	}

	[TestMethod]
	public async Task VerifyTabOrderWithOnceTabNavigationHeaderAndFooter()
	{
		await VerifyTabOrder(KeyboardNavigationMode.Once, VerifyTabOrderFlags.UseHeaderAndFooter, "[B][H][1][F][A][B][A][F][.1][1][H]");
		await VerifyTabOrder(KeyboardNavigationMode.Once, VerifyTabOrderFlags.UseHeaderAndFooter | VerifyTabOrderFlags.UseVirtualizingStackPanelAsItemsPanel, "[B][H][1][A][B][A][.1][1][H]");
		await VerifyTabOrder(KeyboardNavigationMode.Once, VerifyTabOrderFlags.UseHeaderAndFooter | VerifyTabOrderFlags.UseStackPanelAsItemsPanel, "[B][H][1][F][A][B][A][F][.1][1][H]");
	}

	[TestMethod]
	public async Task VerifyTabOrderWithVirtualizedItemsAndLocalTabNavigation()
	{
		string focusSequence = "";

		var rootLoadedEvent = new Event();
		var rootLoadedRegistration = CreateSafeEventRegistration<Page, RoutedEventHandler>("Loaded");
		var rootGotFocusRegistration = CreateSafeEventRegistration<Page, RoutedEventHandler>("GotFocus");

		Page rootPage = null;
		Button beforeButton = null;
		Button afterButton = null;

		await RunOnUIThread(() =>
		{
			rootPage = (Page)XamlReader.Load(ItemsControlForTabNavigationXaml);

			WindowHelper.WindowContent = rootPage;

			rootLoadedRegistration.Attach(rootPage, (s, e) =>
			{
				LOG_OUTPUT("Loaded raised.");
				rootLoadedEvent.Set();
			});

			rootGotFocusRegistration.Attach(rootPage, (s, args) =>
			{
				focusSequence += FocusSequenceEntry(args);
			});

			beforeButton = (Button)rootPage.FindName("BeforeButton");
			VERIFY_IS_NOT_NULL(beforeButton);

			afterButton = (Button)rootPage.FindName("AfterButton");
			VERIFY_IS_NOT_NULL(afterButton);

			ItemsControl mainVirtualizedItemsControl = (ItemsControl)rootPage.FindName("MainVirtualizedItemsControl");
			VERIFY_IS_NOT_NULL(mainVirtualizedItemsControl);

			mainVirtualizedItemsControl.Visibility = Visibility.Visible;
		});

		LOG_OUTPUT("Waiting for content to load.");
		await rootLoadedEvent.WaitForDefault();
		await WindowHelper.WaitForIdle();

		LOG_OUTPUT("Giving focus to AfterButton.");
		await FocusTestHelper.EnsureFocus(afterButton, FocusState.Keyboard);

		LOG_OUTPUT("Pressing shift-tab twice.");
		await KeyboardHelper.ShiftTab();
		await WindowHelper.WaitForIdle();

		await KeyboardHelper.ShiftTab();
		await WindowHelper.WaitForIdle();

		LOG_OUTPUT("Pressing tab twice.");
		await KeyboardHelper.Tab();
		await WindowHelper.WaitForIdle();

		await KeyboardHelper.Tab();
		await WindowHelper.WaitForIdle();

		LOG_OUTPUT("Giving focus to the BeforeButton.");
		await FocusTestHelper.EnsureFocus(beforeButton, FocusState.Keyboard);

		LOG_OUTPUT("Pressing tab twice.");
		await KeyboardHelper.Tab();
		await WindowHelper.WaitForIdle();

		await KeyboardHelper.Tab();
		await WindowHelper.WaitForIdle();

		LOG_OUTPUT("Pressing shift-tab 5 times.");
		for (int tabOccurrence = 0; tabOccurrence < 5; tabOccurrence++)
		{
			await KeyboardHelper.ShiftTab();
			await WindowHelper.WaitForIdle();
		}

		string expectedFocusSequence = "[B][A][.1][15][.1][A][B][1][.1][1][B][A][.1][15]";

		LOG_OUTPUT("Expected focus sequence: %s", expectedFocusSequence);
		LOG_OUTPUT("Actual focus sequence:   %s", focusSequence);
		VERIFY_ARE_EQUAL(focusSequence, expectedFocusSequence);
	}

	[TestMethod]
	public async Task VerifyTabOrderWithVirtualizedItemsAndCycleTabNavigation()
	{
		string focusSequence = "";

		var rootLoadedEvent = new Event();
		var rootLoadedRegistration = CreateSafeEventRegistration<Page, RoutedEventHandler>("Loaded");
		var rootGotFocusRegistration = CreateSafeEventRegistration<Page, RoutedEventHandler>("GotFocus");

		Page rootPage = null;
		Button beforeButton = null;
		Button afterButton = null;

		await RunOnUIThread(() =>
		{
			rootPage = (Page)XamlReader.Load(ItemsControlForTabNavigationXaml);

			WindowHelper.WindowContent = rootPage;

			rootLoadedRegistration.Attach(rootPage, (s, e) =>
			{
				LOG_OUTPUT("Loaded raised.");
				rootLoadedEvent.Set();
			});

			rootGotFocusRegistration.Attach(rootPage, (s, args) =>
			{
				focusSequence += FocusSequenceEntry(args);
			});

			beforeButton = (Button)rootPage.FindName("BeforeButton");
			VERIFY_IS_NOT_NULL(beforeButton);

			afterButton = (Button)rootPage.FindName("AfterButton");
			VERIFY_IS_NOT_NULL(afterButton);

			ItemsControl mainVirtualizedItemsControl = (ItemsControl)rootPage.FindName("MainVirtualizedItemsControl");
			VERIFY_IS_NOT_NULL(mainVirtualizedItemsControl);

			mainVirtualizedItemsControl.Visibility = Visibility.Visible;
			mainVirtualizedItemsControl.TabNavigation = KeyboardNavigationMode.Cycle;
		});

		LOG_OUTPUT("Waiting for content to load.");
		await rootLoadedEvent.WaitForDefault();
		await WindowHelper.WaitForIdle();

		LOG_OUTPUT("Giving focus to AfterButton.");
		await FocusTestHelper.EnsureFocus(afterButton, FocusState.Keyboard);

		LOG_OUTPUT("Pressing shift-tab twice.");
		await KeyboardHelper.ShiftTab();
		await WindowHelper.WaitForIdle();

		await KeyboardHelper.ShiftTab();
		await WindowHelper.WaitForIdle();

		LOG_OUTPUT("Pressing tab 5 times.");
		for (int tabOccurrence = 0; tabOccurrence < 5; tabOccurrence++)
		{
			await KeyboardHelper.Tab();
			await WindowHelper.WaitForIdle();
		}

		LOG_OUTPUT("Pressing shift-tab 5 times.");
		for (int tabOccurrence = 0; tabOccurrence < 5; tabOccurrence++)
		{
			await KeyboardHelper.ShiftTab();
			await WindowHelper.WaitForIdle();
		}

		string expectedFocusSequence = "[B][A][.1][15][.1][1][.1][2][.1][2][.1][1][.1][15]";

		LOG_OUTPUT("Expected focus sequence: %s", expectedFocusSequence);
		LOG_OUTPUT("Actual focus sequence:   %s", focusSequence);
		VERIFY_ARE_EQUAL(focusSequence, expectedFocusSequence);
	}

	[TestMethod]
	public async Task VerifyTabOrderWithGroupedItemsAndLocalTabNavigation()
	{
		await VerifyTabOrder(KeyboardNavigationMode.Local, VerifyTabOrderFlags.UseGroupedItems | VerifyTabOrderFlags.UseVirtualizingStackPanelAsItemsPanel, "[B][11][21][A][B][11][21][A][21][11][B][A][21][11][B]");
	}

	[TestMethod]
	public async Task VerifyTabOrderWithGroupedItemsAndCycleTabNavigation()
	{
		await VerifyTabOrder(KeyboardNavigationMode.Cycle, VerifyTabOrderFlags.UseGroupedItems | VerifyTabOrderFlags.UseVirtualizingStackPanelAsItemsPanel, "[B][11][21][11][21][11][21][11][21][11][21][11][21][11][21]");
	}

	[TestMethod]
	public async Task VerifyTabOrderWithGroupedItemsAndOnceTabNavigation()
	{
		string expectedFocusSequence = "[B][11][A][B][A][11][B]";

		await VerifyTabOrder(KeyboardNavigationMode.Once, VerifyTabOrderFlags.UseGroupedItems, expectedFocusSequence);
		await VerifyTabOrder(KeyboardNavigationMode.Once, VerifyTabOrderFlags.UseGroupedItems | VerifyTabOrderFlags.UseVirtualizingStackPanelAsItemsPanel, expectedFocusSequence);
	}

	[TestMethod]
	public async Task VerifyTabOrderWithGroupedItemsLocalTabNavigationHeaderAndFooter()
	{
		await VerifyTabOrder(KeyboardNavigationMode.Local, VerifyTabOrderFlags.UseHeaderAndFooter | VerifyTabOrderFlags.UseGroupedItems | VerifyTabOrderFlags.UseVirtualizingStackPanelAsItemsPanel, "[B][H][11][21][A][B][H][11][21][11][H][B][A][21][11][H][B]");
	}

	[TestMethod]
	public async Task VerifyTabOrderWithGroupedItemsCycleTabNavigationHeaderAndFooter()
	{
		await VerifyTabOrder(KeyboardNavigationMode.Cycle, VerifyTabOrderFlags.UseHeaderAndFooter | VerifyTabOrderFlags.UseGroupedItems | VerifyTabOrderFlags.UseVirtualizingStackPanelAsItemsPanel, "[B][H][11][21][H][11][21][H][11][H][21][11][H][21][11][H][21]");
	}

	[TestMethod]
	public async Task VerifyTabOrderWithGroupedItemsOnceTabNavigationHeaderAndFooter()
	{
		await VerifyTabOrder(KeyboardNavigationMode.Once, VerifyTabOrderFlags.UseHeaderAndFooter | VerifyTabOrderFlags.UseGroupedItems, "[B][H][11][F][A][B][A][F][11][H][B]");
		await VerifyTabOrder(KeyboardNavigationMode.Once, VerifyTabOrderFlags.UseHeaderAndFooter | VerifyTabOrderFlags.UseGroupedItems | VerifyTabOrderFlags.UseVirtualizingStackPanelAsItemsPanel, "[B][H][11][A][B][A][11][H][B]");
	}

	private async Task VerifyTabOrder(
		KeyboardNavigationMode tabNavigation,
		VerifyTabOrderFlags flags,
		string expectedFocusSequence)
	{
		string focusSequence = "";

		CollectionViewSource cvs = null;
		List<object> itemsSource = null;

		var rootLoadedEvent = new Event();
		var rootLoadedRegistration = CreateSafeEventRegistration<Page, RoutedEventHandler>("Loaded");
		var rootGotFocusRegistration = CreateSafeEventRegistration<Page, RoutedEventHandler>("GotFocus");

		Page rootPage = null;
		Button beforeButton = null;
		ItemsControl mainItemsControl = null;

		await RunOnUIThread(() =>
		{
			rootPage = (Page)XamlReader.Load((flags & VerifyTabOrderFlags.UseGroupedItems) != 0 ? GroupedItemsControlForTabNavigationXaml : ItemsControlForTabNavigationXaml);

			WindowHelper.WindowContent = rootPage;

			rootLoadedRegistration.Attach(rootPage, (s, e) =>
			{
				LOG_OUTPUT("Loaded raised.");
				rootLoadedEvent.Set();
			});

			rootGotFocusRegistration.Attach(rootPage, (s, args) =>
			{
				focusSequence += FocusSequenceEntry(args);
			});

			beforeButton = (Button)rootPage.FindName("BeforeButton");
			VERIFY_IS_NOT_NULL(beforeButton);

			if ((flags & VerifyTabOrderFlags.UseVirtualizingStackPanelAsItemsPanel) != 0)
			{
				mainItemsControl = (ItemsControl)rootPage.FindName("MainItemsControlWithVirtualizingStackPanel");
			}
			else if ((flags & VerifyTabOrderFlags.UseStackPanelAsItemsPanel) != 0)
			{
				mainItemsControl = (ItemsControl)rootPage.FindName("MainItemsControlWithStackPanel");
			}
			else
			{
				mainItemsControl = (ItemsControl)rootPage.FindName("MainItemsControl");
			}
			VERIFY_IS_NOT_NULL(mainItemsControl);

			mainItemsControl.Visibility = Visibility.Visible;
			mainItemsControl.TabNavigation = tabNavigation;

			if ((flags & VerifyTabOrderFlags.UseGroupedItems) != 0)
			{
				cvs = (CollectionViewSource)rootPage.FindName("cvs");
				VERIFY_IS_NOT_NULL(cvs);

				itemsSource = GetGroupedData();
				VERIFY_IS_NOT_NULL(itemsSource);

				cvs.Source = itemsSource;
				cvs.IsSourceGrouped = true;
			}
		});

		LOG_OUTPUT("Waiting for content to load.");
		await rootLoadedEvent.WaitForDefault();
		await WindowHelper.WaitForIdle();

		if ((flags & VerifyTabOrderFlags.UseHeaderAndFooter) != 0)
		{
			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("Accessing ItemsPresenter.");
				ItemsPresenter itemsPresenter = (ItemsPresenter)VisualTreeHelper.GetChild(mainItemsControl, 0);
				VERIFY_IS_NOT_NULL(itemsPresenter);

				LOG_OUTPUT("Setting header.");
				Button headerButton = new Button();
				headerButton.Content = "ItemsPresenter Header";
				headerButton.Tag = "H";
				itemsPresenter.Header = headerButton;

				// VirtualizingStackPanel does not support footer.
				if ((flags & VerifyTabOrderFlags.UseVirtualizingStackPanelAsItemsPanel) == 0)
				{
					LOG_OUTPUT("Setting footer.");
					Button footerButton = new Button();
					footerButton.Content = "ItemsPresenter Footer";
					footerButton.Tag = "F";
					itemsPresenter.Footer = footerButton;
				}
			});

			LOG_OUTPUT("Waiting for header/footer updates.");
			await WindowHelper.WaitForIdle();
		}

		LOG_OUTPUT("Giving focus to the initial button.");
		await FocusTestHelper.EnsureFocus(beforeButton, FocusState.Keyboard);

		// When ItemsControl.TabNavigation is Once, the Tab key is hit at least enough times to enter and exit the control - exactly one item should be visited.
		// When ItemsControl.TabNavigation is Local or Cycle, it is hit at least enough times to go through all three items and either exit or cycle.
		int tabCount = KeyboardNavigationMode.Once == tabNavigation ? 3 : 7;

		if ((flags & VerifyTabOrderFlags.UseHeaderAndFooter) != 0)
		{
			// All panels use a focusable header, so add 1 to the Tab count.
			tabCount++;

			if ((flags & VerifyTabOrderFlags.UseVirtualizingStackPanelAsItemsPanel) == 0)
			{
				// All panels except the VirtualizingStackPanel use a footer, add 1 to the Tab count for those.
				tabCount++;
			}
		}

		LOG_OUTPUT("Pressing tab %d times.", tabCount);
		for (int tabOccurrence = 0; tabOccurrence < tabCount; tabOccurrence++)
		{
			await KeyboardHelper.Tab();
			await WindowHelper.WaitForIdle();
		}

		LOG_OUTPUT("Pressing shift-tab %d times.", tabCount);
		for (int tabOccurrence = 0; tabOccurrence < tabCount; tabOccurrence++)
		{
			await KeyboardHelper.ShiftTab();
			await WindowHelper.WaitForIdle();
		}

		string logMessage = "Scenario: ItemsPanel=";

		if ((flags & VerifyTabOrderFlags.UseVirtualizingStackPanelAsItemsPanel) != 0)
		{
			logMessage += "VirtualizingStackPanel";
		}
		else if ((flags & VerifyTabOrderFlags.UseStackPanelAsItemsPanel) != 0)
		{
			logMessage += "StackPanel";
		}
		else
		{
			logMessage += "Modern Panel";
		}

		if ((flags & VerifyTabOrderFlags.UseHeaderAndFooter) != 0)
		{
			if ((flags & VerifyTabOrderFlags.UseVirtualizingStackPanelAsItemsPanel) != 0)
			{
				logMessage += ", with Header";
			}
			else
			{
				logMessage += ", with Header+Footer";
			}
		}

		if ((flags & VerifyTabOrderFlags.UseGroupedItems) != 0)
		{
			logMessage += ", with groups";
		}

		LOG_OUTPUT(logMessage);
		LOG_OUTPUT("Expected focus sequence: %s", expectedFocusSequence);
		LOG_OUTPUT("Actual focus sequence:   %s", focusSequence);
		VERIFY_ARE_EQUAL(focusSequence, expectedFocusSequence);
	}

	//
	// Private Methods
	//
	private static string FocusSequenceEntry(RoutedEventArgs args)
	{
		FrameworkElement originalSourceFE = args.OriginalSource as FrameworkElement;
		VERIFY_IS_NOT_NULL(originalSourceFE);

		object tag = originalSourceFE.Tag;

		LOG_OUTPUT("GotFocus raised. OriginalSource Tag: %s, OriginalSource Type: %s", tag?.ToString() ?? "null", originalSourceFE.GetType().ToString());

		return tag == null ? "[]" : "[" + tag + "]";
	}

	private static List<object> GetGroupedData()
	{
		var groupedData = new List<object>();

		GroupedHeader group = new GroupedHeader("1");
		group.Append("11");
		group.Append("12");
		groupedData.Add(group);

		group = new GroupedHeader("2");
		group.Append("21");
		group.Append("22");
		groupedData.Add(group);

		return groupedData;
	}

	private const string ItemsControlForTabNavigationXaml =
		"""
		<Page xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
		  <Grid>
		    <Grid.RowDefinitions>
		      <RowDefinition Height="Auto"/>
		      <RowDefinition Height="*"/>
		      <RowDefinition Height="Auto"/>
		    </Grid.RowDefinitions>

		    <Button x:Name="BeforeButton" Content="Before Button" Tag="B"/>

		    <StackPanel Grid.Row="1">
		      <ItemsControl x:Name="MainItemsControl" 
		        IsTabStop="False" Visibility="Collapsed">
		        <ItemsControl.ItemTemplate>
		          <DataTemplate>
		            <StackPanel>
		              <Button Content="{Binding}" Tag="{Binding}"/>
		              <ItemsControl IsTabStop="False" TabNavigation="Once">
		                <ItemsControl.ItemsPanel>
		                  <ItemsPanelTemplate>
		                    <StackPanel Orientation="Horizontal"/>
		                  </ItemsPanelTemplate>
		                </ItemsControl.ItemsPanel>
		                <ItemsControl.ItemTemplate>
		                  <DataTemplate>
		                    <Button Content="{Binding}" Tag="{Binding}"/>
		                  </DataTemplate>
		                </ItemsControl.ItemTemplate>
		                <x:String>.1</x:String>
		                <x:String>.2</x:String>
		                <x:String>.3</x:String>
		              </ItemsControl>
		            </StackPanel>
		          </DataTemplate>
		        </ItemsControl.ItemTemplate>
		        <x:String>1</x:String>
		        <x:String>2</x:String>
		        <x:String>3</x:String>
		      </ItemsControl>

		      <ItemsControl x:Name="MainVirtualizedItemsControl"
		        IsTabStop="False" Visibility="Collapsed" TabNavigation="Local" MaxHeight="250">
		        <ItemsControl.ItemsPanel>
		          <ItemsPanelTemplate>
		            <ItemsStackPanel CacheLength="1"/>
		          </ItemsPanelTemplate>
		        </ItemsControl.ItemsPanel>
		        <ItemsControl.ItemTemplate>
		          <DataTemplate>
		            <StackPanel>
		              <Button Content="{Binding}" Tag="{Binding}"/>
		              <ItemsControl IsTabStop="False" TabNavigation="Once">
		                <ItemsControl.ItemsPanel>
		                  <ItemsPanelTemplate>
		                    <ItemsWrapGrid
		                        HorizontalAlignment="Stretch"
		                        VerticalAlignment="Stretch"
		                        MaximumRowsOrColumns="1"
		                        Orientation="Vertical"/>
		                  </ItemsPanelTemplate>
		                </ItemsControl.ItemsPanel>
		                <ItemsControl.ItemTemplate>
		                  <DataTemplate>
		                    <Button Content="{Binding}" Tag="{Binding}"/>
		                  </DataTemplate>
		                </ItemsControl.ItemTemplate>
		                <x:String>.1</x:String>
		                <x:String>.2</x:String>
		                <x:String>.3</x:String>
		              </ItemsControl>
		            </StackPanel>
		          </DataTemplate>
		        </ItemsControl.ItemTemplate>
		        <x:String>1</x:String>
		        <x:String>2</x:String>
		        <x:String>3</x:String>
		        <x:String>4</x:String>
		        <x:String>5</x:String>
		        <x:String>6</x:String>
		        <x:String>7</x:String>
		        <x:String>8</x:String>
		        <x:String>9</x:String>
		        <x:String>10</x:String>
		        <x:String>11</x:String>
		        <x:String>12</x:String>
		        <x:String>13</x:String>
		        <x:String>14</x:String>
		        <x:String>15</x:String>
		      </ItemsControl>

		      <ItemsControl x:Name="MainItemsControlWithVirtualizingStackPanel" 
		        IsTabStop="False" Visibility="Collapsed">
		        <ItemsControl.ItemTemplate>
		          <DataTemplate>
		            <StackPanel>
		              <Button Content="{Binding}" Tag="{Binding}"/>
		              <ItemsControl IsTabStop="False" TabNavigation="Once">
		                <ItemsControl.ItemsPanel>
		                  <ItemsPanelTemplate>
		                    <StackPanel Orientation="Horizontal"/>
		                  </ItemsPanelTemplate>
		                </ItemsControl.ItemsPanel>
		                <ItemsControl.ItemTemplate>
		                  <DataTemplate>
		                    <Button Content="{Binding}" Tag="{Binding}"/>
		                  </DataTemplate>
		                </ItemsControl.ItemTemplate>
		                <x:String>.1</x:String>
		                <x:String>.2</x:String>
		                <x:String>.3</x:String>
		              </ItemsControl>
		            </StackPanel>
		          </DataTemplate>
		        </ItemsControl.ItemTemplate>
		        <ItemsControl.ItemsPanel>
		          <ItemsPanelTemplate>
		            <VirtualizingStackPanel/>
		          </ItemsPanelTemplate>
		        </ItemsControl.ItemsPanel>
		        <x:String>1</x:String>
		        <x:String>2</x:String>
		        <x:String>3</x:String>
		      </ItemsControl>

		      <ItemsControl x:Name="MainItemsControlWithStackPanel" 
		        IsTabStop="False" Visibility="Collapsed">
		        <ItemsControl.ItemTemplate>
		          <DataTemplate>
		            <StackPanel>
		              <Button Content="{Binding}" Tag="{Binding}"/>
		              <ItemsControl IsTabStop="False" TabNavigation="Once">
		                <ItemsControl.ItemsPanel>
		                  <ItemsPanelTemplate>
		                    <StackPanel Orientation="Horizontal"/>
		                  </ItemsPanelTemplate>
		                </ItemsControl.ItemsPanel>
		                <ItemsControl.ItemTemplate>
		                  <DataTemplate>
		                    <Button Content="{Binding}" Tag="{Binding}"/>
		                  </DataTemplate>
		                </ItemsControl.ItemTemplate>
		                <x:String>.1</x:String>
		                <x:String>.2</x:String>
		                <x:String>.3</x:String>
		              </ItemsControl>
		            </StackPanel>
		          </DataTemplate>
		        </ItemsControl.ItemTemplate>
		        <ItemsControl.ItemsPanel>
		          <ItemsPanelTemplate>
		            <StackPanel/>
		          </ItemsPanelTemplate>
		        </ItemsControl.ItemsPanel>
		        <x:String>1</x:String>
		        <x:String>2</x:String>
		        <x:String>3</x:String>
		      </ItemsControl>
		    </StackPanel>

		    <Button x:Name="AfterButton" Content="After Button" Tag="A" Grid.Row="2"/>
		  </Grid>
		</Page>
		""";

	private const string GroupedItemsControlForTabNavigationXaml =
		"""
		<Page xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
		  <Grid>
		    <Grid.RowDefinitions>
		      <RowDefinition Height="Auto"/>
		      <RowDefinition Height="*"/>
		      <RowDefinition Height="Auto"/>
		    </Grid.RowDefinitions>

		    <Grid.Resources>
		      <CollectionViewSource x:Name="cvs"/>
		    </Grid.Resources>

		    <Button x:Name="BeforeButton" Content="Before Button" Tag="B"/>

		    <StackPanel Grid.Row="1">
		      <ItemsControl x:Name="MainItemsControl"
		        IsTabStop="False" Visibility="Collapsed"
		        ItemsSource='{Binding Source={StaticResource cvs}}'>
		        <ItemsControl.ItemTemplate>
		          <DataTemplate>
		            <Button Content="{Binding}" Tag="{Binding}"/>
		          </DataTemplate>
		        </ItemsControl.ItemTemplate>
		        <ItemsControl.GroupStyle>
		          <GroupStyle>
		            <GroupStyle.HeaderTemplate>
		              <DataTemplate>
		                <TextBlock Text="{Binding}" Foreground="Red"/>
		              </DataTemplate>
		            </GroupStyle.HeaderTemplate>
		          </GroupStyle>
		        </ItemsControl.GroupStyle>
		      </ItemsControl>

		      <ItemsControl x:Name="MainItemsControlWithVirtualizingStackPanel"
		        IsTabStop="False" Visibility="Collapsed"
		        ItemsSource='{Binding Source={StaticResource cvs}}'>
		        <ItemsControl.ItemTemplate>
		          <DataTemplate>
		            <Button Content="{Binding}" Tag="{Binding}"/>
		          </DataTemplate>
		        </ItemsControl.ItemTemplate>
		        <ItemsControl.GroupStyle>
		          <GroupStyle>
		            <GroupStyle.HeaderTemplate>
		              <DataTemplate>
		                <TextBlock Text="{Binding}" Foreground="Red"/>
		              </DataTemplate>
		            </GroupStyle.HeaderTemplate>
		          </GroupStyle>
		        </ItemsControl.GroupStyle>
		        <ItemsControl.ItemsPanel>
		          <ItemsPanelTemplate>
		            <VirtualizingStackPanel/>
		          </ItemsPanelTemplate>
		        </ItemsControl.ItemsPanel>
		      </ItemsControl>
		    </StackPanel>

		    <Button x:Name="AfterButton" Content="After Button" Tag="A" Grid.Row="2"/>
		  </Grid>
		</Page>
		""";
}
