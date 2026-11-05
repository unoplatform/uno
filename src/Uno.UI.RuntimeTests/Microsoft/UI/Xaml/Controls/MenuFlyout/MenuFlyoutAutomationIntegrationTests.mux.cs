// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference MenuFlyoutAutomationIntegrationTests.cpp, commit bac7a9c33

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Tests.Common;
using Microsoft.UI.Xaml.Tests.Enterprise;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.MUX.Helpers;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.MUX.Microsoft_UI_Xaml_Controls;

// Upstream queries the open MenuFlyout through a Win32 UIA client (IUIAutomation, AutomationClientManager).
// Uno has no UIA client in the runtime tests, so the UIA client node lookups and tree walks are done through
// the automation peers of the open popup, which is what the UIA provider exposes to a client.
// The windowed popup synchronization (TEMP_WaitForOpenWindowedPopup) is a UIA client concern and is dropped.

[TestClass]
[RequiresFullWindow] // The upstream tests expect window-absolute coordinates.
public class MenuFlyoutAutomationIntegrationTests
{
	private const string s_automationName = "TestMenuFlyoutName";
	private const string s_automationId = "TestMenuFlyoutId";

	private const double DefaultHorizontalOffset = -50;
	private const double DefaultVerticalOffset = 50;

	[TestCleanup]
	public async Task TestCleanup()
	{
		await RunOnUIThread(() =>
		{
			VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
			TestServices.WindowHelper.WindowContent = null;
		});
	}

	private async Task<Canvas> SetupRootPanelForSubMenuTest()
	{
		Canvas rootPanel = null;

		await RunOnUIThread(() =>
		{
			rootPanel = (Canvas)(XamlReader.Load(
				"<Canvas Background='RoyalBlue' " +
				" xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >" +
				"    <Button x:Name='button1' Content='Button' Width='100' Height='50' Canvas.Left='50' Canvas.Top='50' />" +
				"</Canvas>"));

			TestServices.WindowHelper.WindowContent = rootPanel;
		});
		await TestServices.WindowHelper.WaitForIdle();

		return rootPanel;
	}

	private async Task<MenuFlyout> CreateMenuFlyoutFromXaml()
	{
		MenuFlyout menuFlyout = null;

		await RunOnUIThread(() =>
		{
			menuFlyout = (MenuFlyout)(XamlReader.Load(
				"<MenuFlyout x:Name='menuFlyout' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >" +
				"    <MenuFlyoutItem>Menu item 1</MenuFlyoutItem>" +
				"    <MenuFlyoutItem>Menu item 2</MenuFlyoutItem>" +
				"    <MenuFlyoutSeparator/>" +
				"    <ToggleMenuFlyoutItem IsChecked='True'>Toggle item 3</ToggleMenuFlyoutItem>" +
				"    <MenuFlyoutSeparator/>" +
				"    <MenuFlyoutSubItem x:Name='subItem1' Text='Menu sub item 4'>" +
				"        <MenuFlyoutItem>Menu item 2.1</MenuFlyoutItem>" +
				"        <MenuFlyoutItem>Menu item 2.2</MenuFlyoutItem>" +
				"        <MenuFlyoutSeparator/>" +
				"        <ToggleMenuFlyoutItem IsChecked='True'>Toggle item 2.3</ToggleMenuFlyoutItem>" +
				"        <MenuFlyoutSeparator/>" +
				"        <MenuFlyoutSubItem  x:Name='subItem2' Text='Menu sub item 2.4'>" +
				"            <MenuFlyoutItem>Menu item 3.1</MenuFlyoutItem>" +
				"            <MenuFlyoutItem>Menu item 3.2</MenuFlyoutItem>" +
				"            <MenuFlyoutSeparator/>" +
				"            <ToggleMenuFlyoutItem IsChecked='True'>Toggle item 3.3</ToggleMenuFlyoutItem>" +
				"            <MenuFlyoutSeparator/>" +
				"        </MenuFlyoutSubItem>" +
				"    </MenuFlyoutSubItem>" +
				"    <MenuFlyoutSubItem x:Name='splitItem1' Text='Split menu item 5'>" +
				"        <MenuFlyoutItem>Split item 5.1</MenuFlyoutItem>" +
				"        <MenuFlyoutItem>Split item 5.2</MenuFlyoutItem>" +
				"        <ToggleMenuFlyoutItem IsChecked='True'>Toggle split item 5.3</ToggleMenuFlyoutItem>" +
				"    </MenuFlyoutSubItem>" +
				"    <MenuFlyoutItem>Menu item 6</MenuFlyoutItem>" +
				"</MenuFlyout>"));
		});

		return menuFlyout;
	}

	private async Task ShowMenuFlyout(MenuFlyout menuFlyout, UIElement relativeTo, double horizontalOffset, double verticalOffset)
	{
		var openedEvent = new Event();
		var openedRegistration = CreateSafeEventRegistration<MenuFlyout, EventHandler<object>>(nameof(MenuFlyout.Opened));

		await RunOnUIThread(() =>
		{
			openedRegistration.Attach(menuFlyout, (s, e) =>
			{
				openedEvent.Set();
			});

			menuFlyout.ShowAt(relativeTo, new Point(horizontalOffset, verticalOffset));
		});

		await openedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[Description("Verify AutomationProperties for MenuFlyout.")]
	public async Task VerifyAutomationProperties()
	{
		Button button1 = null;

		Canvas rootPanel = await SetupRootPanelForSubMenuTest();
		MenuFlyout menuFlyout = await CreateMenuFlyoutFromXaml();

		await RunOnUIThread(() =>
		{
			AutomationProperties.SetName(menuFlyout, s_automationName);
			AutomationProperties.SetAutomationId(menuFlyout, s_automationId);
		});

		await RunOnUIThread(() =>
		{
			button1 = (Button)(rootPanel.FindName("button1"));
		});

		await ShowMenuFlyout(menuFlyout, button1, DefaultHorizontalOffset, DefaultVerticalOffset);

		await RunOnUIThread(() =>
		{
			var spUIAutomationElement = FindPeerByName(GetOpenPopupPeer(button1), s_automationName);

			LOG_OUTPUT("Verifying that UIA client-side node for MenuFlyoutPresenter exists.");
			VERIFY_IS_NOT_NULL(spUIAutomationElement);

			LOG_OUTPUT("Verifying the UIA name property from client-side node for MenuFlyoutPresenter.");
			LOG_OUTPUT("NamePropertyId = '%s', Expected = '%s'.", spUIAutomationElement.GetName(), s_automationName);
			VERIFY_ARE_EQUAL(s_automationName, spUIAutomationElement.GetName());

			LOG_OUTPUT("Verifying the UIA automation id property from client-side node for MenuFlyoutPresenter.");
			VERIFY_ARE_EQUAL(s_automationId, spUIAutomationElement.GetAutomationId());

			LOG_OUTPUT("Verifying the UIA class name property from client-side node for MenuFlyoutPresenter.");
			LOG_OUTPUT("ClassNamePropertyId = '%s', Expected = '%s'.", spUIAutomationElement.GetClassName(), "MenuFlyout");
			VERIFY_ARE_EQUAL("MenuFlyout", spUIAutomationElement.GetClassName());
		});

		await FlyoutHelper.HideFlyout(menuFlyout);
	}

	// Loads simple markup with MenuFlyout and verifies AutomationProperties are correct.
	[TestMethod]
	[Description("Verify Automation of MenuFlyout Sub Item.")]
	public async Task VerifyFlyoutMenuSubItemAutomation()
	{
		Button button1 = null;
		IList<MenuFlyoutItemBase> items = null;

		Canvas rootPanel = await SetupRootPanelForSubMenuTest();
		MenuFlyout menuFlyout = await CreateMenuFlyoutFromXaml();

		AutomationPeer spUIAutomationElement = null;
		IExpandCollapseProvider spExpandCollapsePattern = null;

		await RunOnUIThread(() =>
		{
			button1 = (Button)(rootPanel.FindName("button1"));
			items = menuFlyout.Items;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await ShowMenuFlyout(menuFlyout, button1, DefaultHorizontalOffset, DefaultVerticalOffset);

		await RunOnUIThread(() =>
		{
			spUIAutomationElement = FindPeerByName(GetOpenPopupPeer(button1), "Menu sub item 4");
			VERIFY_IS_NOT_NULL(spUIAutomationElement);
			VERIFY_ARE_EQUAL("subItem1", spUIAutomationElement.GetAutomationId());
			VERIFY_ARE_EQUAL(AutomationControlType.MenuItem, spUIAutomationElement.GetAutomationControlType());

			spExpandCollapsePattern = spUIAutomationElement.GetPattern(PatternInterface.ExpandCollapse) as IExpandCollapseProvider;
			VERIFY_IS_NOT_NULL(spExpandCollapsePattern);

			ExpandCollapseState retVal = ExpandCollapseState.Expanded;
			retVal = spExpandCollapsePattern.ExpandCollapseState;
			VERIFY_ARE_EQUAL(retVal, ExpandCollapseState.Collapsed);

			LOG_OUTPUT("Verifying PositionInSet is 4.");
			VERIFY_ARE_EQUAL(spUIAutomationElement.GetPositionInSet(), 4);

			LOG_OUTPUT("Verifying SizeOfSet is 6.");
			VERIFY_ARE_EQUAL(spUIAutomationElement.GetSizeOfSet(), 6);

			spExpandCollapsePattern.Expand();
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			ExpandCollapseState retVal = ExpandCollapseState.Collapsed;
			retVal = spExpandCollapsePattern.ExpandCollapseState;
			VERIFY_ARE_EQUAL(retVal, ExpandCollapseState.Expanded);

			spExpandCollapsePattern.Collapse();
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			ExpandCollapseState retVal = ExpandCollapseState.Expanded;
			retVal = spExpandCollapsePattern.ExpandCollapseState;
			VERIFY_ARE_EQUAL(retVal, ExpandCollapseState.Collapsed);
		});

		await FlyoutHelper.HideFlyout(menuFlyout);
	}

	// Loads simple markup with MenuFlyout and verifies AutomationProperties are correct for Split Menu Items.
	[TestMethod]
	[Description("Verify Automation of MenuFlyout Split Item.")]
	public async Task VerifyFlyoutMenuSplitItemAutomation()
	{
		Button button1 = null;
		IList<MenuFlyoutItemBase> items = null;

		Canvas rootPanel = await SetupRootPanelForSubMenuTest();
		MenuFlyout menuFlyout = await CreateMenuFlyoutFromXaml();

		AutomationPeer spUIAutomationElement = null;
		IExpandCollapseProvider spExpandCollapsePattern = null;

		await RunOnUIThread(() =>
		{
			button1 = (Button)(rootPanel.FindName("button1"));
			items = menuFlyout.Items;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await ShowMenuFlyout(menuFlyout, button1, DefaultHorizontalOffset, DefaultVerticalOffset);

		await RunOnUIThread(() =>
		{
			spUIAutomationElement = FindPeerByName(GetOpenPopupPeer(button1), "Split menu item 5");
			VERIFY_IS_NOT_NULL(spUIAutomationElement);
			VERIFY_ARE_EQUAL("splitItem1", spUIAutomationElement.GetAutomationId());
			VERIFY_ARE_EQUAL(AutomationControlType.MenuItem, spUIAutomationElement.GetAutomationControlType());

			spExpandCollapsePattern = spUIAutomationElement.GetPattern(PatternInterface.ExpandCollapse) as IExpandCollapseProvider;
			VERIFY_IS_NOT_NULL(spExpandCollapsePattern);

			ExpandCollapseState retVal = ExpandCollapseState.Expanded;
			retVal = spExpandCollapsePattern.ExpandCollapseState;
			VERIFY_ARE_EQUAL(retVal, ExpandCollapseState.Collapsed);

			LOG_OUTPUT("Verifying PositionInSet is 5.");
			VERIFY_ARE_EQUAL(spUIAutomationElement.GetPositionInSet(), 5);

			LOG_OUTPUT("Verifying SizeOfSet is 6.");
			VERIFY_ARE_EQUAL(spUIAutomationElement.GetSizeOfSet(), 6);

			// Verify that the split item supports Invoke pattern
			// var spInvokePattern = spUIAutomationElement.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
			// VERIFY_IS_NOT_NULL(spInvokePattern);

			spExpandCollapsePattern.Expand();
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			ExpandCollapseState retVal = ExpandCollapseState.Collapsed;
			retVal = spExpandCollapsePattern.ExpandCollapseState;
			VERIFY_ARE_EQUAL(retVal, ExpandCollapseState.Expanded);

			spExpandCollapsePattern.Collapse();
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			ExpandCollapseState retVal = ExpandCollapseState.Expanded;
			retVal = spExpandCollapsePattern.ExpandCollapseState;
			VERIFY_ARE_EQUAL(retVal, ExpandCollapseState.Collapsed);
		});

		await FlyoutHelper.HideFlyout(menuFlyout);
	}

	[TestMethod]
	[Description("Verify overriding automation name for split menu item overrides the automation name for primary and secondary buttons.")]
	public async Task VerifyOverridingAutomationNameForSplitMenuItem()
	{
		Button button1 = null;
		Canvas rootPanel = await SetupRootPanelForSubMenuTest();

		MenuFlyout menuFlyout = null;
		SplitMenuFlyoutItem splitItem = null;

		await RunOnUIThread(() =>
		{
			menuFlyout = (MenuFlyout)(XamlReader.Load(
				"<MenuFlyout x:Name='menuFlyout' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >" +
				"    <MenuFlyoutItem>Menu item 1</MenuFlyoutItem>" +
				"    <SplitMenuFlyoutItem x:Name='splitItem1' Text='Split Item' AutomationProperties.Name='Custom Split Item'>" +
				"        <MenuFlyoutItem>Split child 1</MenuFlyoutItem>" +
				"        <MenuFlyoutItem>Split child 2</MenuFlyoutItem>" +
				"    </SplitMenuFlyoutItem>" +
				"</MenuFlyout>"));

			button1 = (Button)(rootPanel.FindName("button1"));
			splitItem = (SplitMenuFlyoutItem)(menuFlyout.Items[1]);
		});
		await TestServices.WindowHelper.WaitForIdle();

		await ShowMenuFlyout(menuFlyout, button1, DefaultHorizontalOffset, DefaultVerticalOffset);

		await RunOnUIThread(() =>
		{
			var spSplitItemElement = FindPeerByName(GetOpenPopupPeer(button1), "Custom Split Item");
			VERIFY_IS_NOT_NULL(spSplitItemElement);
			VERIFY_ARE_EQUAL("splitItem1", spSplitItemElement.GetAutomationId());
			VERIFY_ARE_EQUAL(AutomationControlType.MenuItem, spSplitItemElement.GetAutomationControlType());

			// Verify split item itself has the custom automation name
			string splitItemName = spSplitItemElement.GetName();
			LOG_OUTPUT("Actual: '%s', Expected: 'Custom Split Item'", splitItemName);
			VERIFY_ARE_EQUAL("Custom Split Item", splitItemName);

			// Get secondary button (first child of split menu item)
			// Since SplitMenuFlyoutItem's EventSource is the primary button, primary button is not included in
			// the children of SplitMenuFlyoutItem. So first child is secondary button.
			var spSecondaryButton = GetFirstChild(spSplitItemElement);
			VERIFY_IS_NOT_NULL(spSecondaryButton);

			// Verify secondary button automation name is overridden to "Custom Split Name, More options"
			string secondaryName = spSecondaryButton.GetName();
			LOG_OUTPUT("Secondary button name: '%s', Expected: 'More options for Custom Split Item'", secondaryName);
			VERIFY_ARE_EQUAL("More options for Custom Split Item", secondaryName);
		});

		await FlyoutHelper.HideFlyout(menuFlyout);
	}

	[TestMethod]
	[Description("Verify that SplitMenuFlyoutItem automation tree has secondary button as next sibling with correct position and size of set.")]
	public async Task VerifySplitMenuItemAutomationTreeStructure()
	{
		Button button1 = null;
		Canvas rootPanel = await SetupRootPanelForSubMenuTest();

		MenuFlyout menuFlyout = null;

		await RunOnUIThread(() =>
		{
			menuFlyout = (MenuFlyout)(XamlReader.Load(
				"<MenuFlyout x:Name='menuFlyout' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >" +
				"    <MenuFlyoutItem>Menu item 1</MenuFlyoutItem>" +
				"    <SplitMenuFlyoutItem x:Name='splitItem1' Text='Split Item 2'>" +
				"        <MenuFlyoutItem>Split child 1</MenuFlyoutItem>" +
				"        <MenuFlyoutItem>Split child 2</MenuFlyoutItem>" +
				"    </SplitMenuFlyoutItem>" +
				"    <MenuFlyoutItem>Menu item 3</MenuFlyoutItem>" +
				"</MenuFlyout>"));

			button1 = (Button)(rootPanel.FindName("button1"));
		});
		await TestServices.WindowHelper.WaitForIdle();

		await ShowMenuFlyout(menuFlyout, button1, DefaultHorizontalOffset, DefaultVerticalOffset);

		await RunOnUIThread(() =>
		{
			var spSplitItemElement = FindPeerByName(GetOpenPopupPeer(button1), "Split Item 2");
			VERIFY_IS_NOT_NULL(spSplitItemElement);
			VERIFY_ARE_EQUAL("splitItem1", spSplitItemElement.GetAutomationId());
			VERIFY_ARE_EQUAL(AutomationControlType.MenuItem, spSplitItemElement.GetAutomationControlType());

			// Verify split item PositionInSet and SizeOfSet
			int splitItemPositionInSet = spSplitItemElement.GetPositionInSet();
			LOG_OUTPUT("Split item PositionInSet: %d, Expected: 2", splitItemPositionInSet);
			VERIFY_ARE_EQUAL(2, splitItemPositionInSet);

			int splitItemSizeOfSet = spSplitItemElement.GetSizeOfSet();
			LOG_OUTPUT("Split item SizeOfSet: %d, Expected: 3", splitItemSizeOfSet);
			VERIFY_ARE_EQUAL(3, splitItemSizeOfSet);

			// Get the first child of split item (should be secondary button, as primary button is filtered out)
			var spSecondaryButton = GetFirstChild(spSplitItemElement);
			VERIFY_IS_NOT_NULL(spSecondaryButton);

			// Get next sibling after split item (should be Menu item 3)
			var spNextMenuItem = GetNextSibling(spSplitItemElement);
			VERIFY_IS_NOT_NULL(spNextMenuItem);

			// Verify next menu item name
			string nextItemName = spNextMenuItem.GetName();
			LOG_OUTPUT("Next menu item name: '%s', Expected: 'Menu item 3'", nextItemName);
			VERIFY_ARE_EQUAL("Menu item 3", nextItemName);

			// Verify next menu item PositionInSet and SizeOfSet
			int nextItemPositionInSet = spNextMenuItem.GetPositionInSet();
			LOG_OUTPUT("Next menu item PositionInSet: %d, Expected: 3", nextItemPositionInSet);
			VERIFY_ARE_EQUAL(3, nextItemPositionInSet);

			int nextItemSizeOfSet = spNextMenuItem.GetSizeOfSet();
			LOG_OUTPUT("Next menu item SizeOfSet: %d, Expected: 3", nextItemSizeOfSet);
			VERIFY_ARE_EQUAL(3, nextItemSizeOfSet);
		});

		await FlyoutHelper.HideFlyout(menuFlyout);
	}

	[TestMethod]
	[Description("Verify that MenuFlyout items are read when visible, and not when they are collapsed.")]
	public async Task VerifyItemIsNotReadWhenCollapsed()
	{
		Button button1 = null;
		Canvas rootPanel = await SetupRootPanelForSubMenuTest();

		MenuFlyout menuFlyout = null;
		MenuFlyoutItem hiddenItem = null;
		MenuFlyoutSubItem hiddenSubItem = null;
		SplitMenuFlyoutItem hiddenSplitItem = null;

		await RunOnUIThread(() =>
		{
			menuFlyout = (MenuFlyout)(XamlReader.Load(
				"<MenuFlyout x:Name='menuFlyout' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >" +
				"    <MenuFlyoutItem>0</MenuFlyoutItem>" +
				"    <MenuFlyoutItem>1</MenuFlyoutItem>" +
				"    <MenuFlyoutItem x:Name='hiddenItem'>2</MenuFlyoutItem>" +
				"    <MenuFlyoutItem>3</MenuFlyoutItem>" +
				"    <MenuFlyoutSubItem x:Name='hiddenSubItem' Text='4'>" +
				"        <MenuFlyoutItem>4.1</MenuFlyoutItem>" +
				"    </MenuFlyoutSubItem>" +
				"    <MenuFlyoutSubItem Text='5'>" +
				"        <MenuFlyoutItem>5.1</MenuFlyoutItem>" +
				"    </MenuFlyoutSubItem>" +
				"    <SplitMenuFlyoutItem x:Name='hiddenSplitItem' Text='6'>" +
				"        <MenuFlyoutItem>6.1</MenuFlyoutItem>" +
				"    </SplitMenuFlyoutItem>" +
				"    <SplitMenuFlyoutItem Text='7'>" +
				"        <MenuFlyoutItem>7.1</MenuFlyoutItem>" +
				"    </SplitMenuFlyoutItem>" +
				"</MenuFlyout>"));
		});

		await RunOnUIThread(() =>
		{
			AutomationProperties.SetName(menuFlyout, s_automationName);
			AutomationProperties.SetAutomationId(menuFlyout, s_automationId);

			button1 = (Button)(rootPanel.FindName("button1"));
			hiddenItem = (MenuFlyoutItem)(menuFlyout.Items[2]);
			hiddenSubItem = (MenuFlyoutSubItem)(menuFlyout.Items[4]);
			hiddenSplitItem = (SplitMenuFlyoutItem)(menuFlyout.Items[6]);
		});

		await ShowMenuFlyout(menuFlyout, button1, DefaultHorizontalOffset, DefaultVerticalOffset);

		await RunOnUIThread(() =>
		{
			var spUIAutomationElement = FindPeerByName(GetOpenPopupPeer(button1), s_automationName);
			VERIFY_IS_NOT_NULL(spUIAutomationElement);
			VERIFY_ARE_EQUAL(s_automationId, spUIAutomationElement.GetAutomationId());

			// Verify that all items are returned
			var children = spUIAutomationElement.GetChildren();
			VERIFY_IS_NOT_NULL(children);
			for (int i = 0; i < 8; i++)
			{
				VERIFY_IS_TRUE(i < children.Count);
				var spCurrentElement = children[i];
				VERIFY_IS_NOT_NULL(spCurrentElement);

				string expectedUIAName = i.ToString();
				string actualUIAName = spCurrentElement.GetName();

				VERIFY_ARE_EQUAL(expectedUIAName, actualUIAName);
			}
		});

		// Set the visibility of hitddenItem, hiddenSubItem and hiddenSplitItem to collapsed
		await RunOnUIThread(() =>
		{
			hiddenItem.Visibility = Visibility.Collapsed;
			hiddenSubItem.Visibility = Visibility.Collapsed;
			hiddenSplitItem.Visibility = Visibility.Collapsed;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			var spUIAutomationElement = FindPeerByName(GetOpenPopupPeer(button1), s_automationName);
			VERIFY_IS_NOT_NULL(spUIAutomationElement);

			// Verify that items 0, 1, 3, 5 and 7 are returned
			var children = spUIAutomationElement.GetChildren();
			VERIFY_IS_NOT_NULL(children);
			int childIndex = 0;
			for (int i = 0; i < 8; i++)
			{
				if (i > 0 && i % 2 == 0)
				{
					continue;
				}

				VERIFY_IS_TRUE(childIndex < children.Count);
				var spCurrentElement = children[childIndex++];
				VERIFY_IS_NOT_NULL(spCurrentElement);

				string actualUIAName = spCurrentElement.GetName();

				// Convert the int i to string expectedItemString if it is item 0 or 1
				string expectedUIAName = i.ToString();
				VERIFY_ARE_EQUAL(expectedUIAName, actualUIAName);
			}
		});

		await FlyoutHelper.HideFlyout(menuFlyout);
	}

	[TestMethod]
	[Description("Validates that MenuFlyoutSeparators are excluded when determing position in set and size of set.")]
	public async Task DoesSizeOfSetAndPositionInSetExcludeSeparatorsAndCollapsedItems()
	{
		Button button = null;
		MenuFlyout menuFlyout = null;

		await RunOnUIThread(() =>
		{
			menuFlyout = (MenuFlyout)(XamlReader.Load(
				"""
				<MenuFlyout xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
					<MenuFlyoutItem Text="Collapsed" Visibility="Collapsed"/>
					<MenuFlyoutItem Text="Item 1"/>
					<MenuFlyoutSeparator/>
					<ToggleMenuFlyoutItem Text="Item 2"/>
					<MenuFlyoutSubItem Text="Item 3"/>
					<MenuFlyoutSeparator/>
					<MenuFlyoutSubItem Text="Collapsed Sub Item" Visibility="Collapsed">
						<MenuFlyoutItem Text="Sub Item 1"/>
					</MenuFlyoutSubItem>
					<MenuFlyoutSubItem Text="Item 4">
						<MenuFlyoutItem Text="Sub Item 2"/>
					</MenuFlyoutSubItem>
					<SplitMenuFlyoutItem Text="Collapsed Split Item" Visibility="Collapsed">
						<MenuFlyoutItem Text="Split Item 1"/>
					</SplitMenuFlyoutItem>
					<SplitMenuFlyoutItem Text="Item 5">
						<MenuFlyoutItem Text="Split Item 2"/>
					</SplitMenuFlyoutItem>
				</MenuFlyout>
				"""));

			AutomationProperties.SetName(menuFlyout, s_automationName);
			AutomationProperties.SetAutomationId(menuFlyout, s_automationId);

			button = new Button();
			button.Content = "Button!";
			button.Flyout = menuFlyout;

			var root = new Grid();
			root.Children.Add(button);

			TestServices.WindowHelper.WindowContent = root;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await ShowMenuFlyout(menuFlyout, button, 0, 0);

		await RunOnUIThread(() =>
		{
			var automationElement = FindPeerByName(GetOpenPopupPeer(button), s_automationName);
			VERIFY_IS_NOT_NULL(automationElement);
			VERIFY_ARE_EQUAL(s_automationId, automationElement.GetAutomationId());

			var children = automationElement.GetChildren();
			VERIFY_IS_NOT_NULL(children);
			VERIFY_IS_TRUE(children.Count > 0, "Failed to get first child.");

			const int expectedSizeOfSet = 5;
			int expectedPositionInSet = 1;
			int childIndex = 0;

			while (expectedPositionInSet <= expectedSizeOfSet)
			{
				VERIFY_IS_TRUE(childIndex < children.Count, "Failed to get next sibling");
				var currentElement = children[childIndex++];
				VERIFY_IS_NOT_NULL(currentElement);

				VERIFY_ARE_EQUAL(expectedSizeOfSet, currentElement.GetSizeOfSet());
				VERIFY_ARE_EQUAL(expectedPositionInSet, currentElement.GetPositionInSet());

				++expectedPositionInSet;
			}
		});

		await FlyoutHelper.HideFlyout(menuFlyout);
	}

	[TestMethod]
	[Description("Verify ScrollItemPattern on MenuFlyoutItem.")]
	public async Task VerifyScrollItemPattern()
	{
		Button button1 = null;

		Canvas rootPanel = await SetupRootPanelForSubMenuTest();
		MenuFlyout menuFlyout = await CreateMenuFlyoutFromXaml();

		await RunOnUIThread(() =>
		{
			button1 = (Button)(rootPanel.FindName("button1"));
		});
		await TestServices.WindowHelper.WaitForIdle();

		await ShowMenuFlyout(menuFlyout, button1, DefaultHorizontalOffset, DefaultVerticalOffset);

		await RunOnUIThread(() =>
		{
			var spUIAutomationElement = FindPeerByName(GetOpenPopupPeer(button1), "Menu item 1");
			VERIFY_IS_NOT_NULL(spUIAutomationElement);
			VERIFY_ARE_EQUAL(AutomationControlType.MenuItem, spUIAutomationElement.GetAutomationControlType());

			var spScrollItemPattern = spUIAutomationElement.GetPattern(PatternInterface.ScrollItem) as IScrollItemProvider;
			VERIFY_IS_NOT_NULL(spScrollItemPattern);
		});

		await FlyoutHelper.HideFlyout(menuFlyout);
	}

	[TestMethod]
	[Description("Verify that the app author can set the PositionInSet and SizeOfSet AutomationProperties to override the default values.")]
	public async Task VerifySizeAndPositionInSetCanBeOverridden()
	{
		// Verify that the app author can set the PositionInSet and SizeOfSet AutomationProperties to override the
		// default values.

		Button button = null;
		MenuFlyout menuFlyout = null;

		await RunOnUIThread(() =>
		{
			menuFlyout = (MenuFlyout)(XamlReader.Load(
				"""
				<MenuFlyout xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
					<MenuFlyoutItem Text="Item 1" AutomationProperties.PositionInSet="10" AutomationProperties.SizeOfSet="40" />
					<ToggleMenuFlyoutItem Text="Item 2" AutomationProperties.PositionInSet="20" AutomationProperties.SizeOfSet="40" />
					<MenuFlyoutSubItem Text="Item 3" AutomationProperties.PositionInSet="30" AutomationProperties.SizeOfSet="40" />
					<SplitMenuFlyoutItem Text="Item 4" AutomationProperties.PositionInSet="40" AutomationProperties.SizeOfSet="40" />
				</MenuFlyout>
				"""));

			AutomationProperties.SetName(menuFlyout, s_automationName);
			AutomationProperties.SetAutomationId(menuFlyout, s_automationId);

			button = new Button();
			button.Content = "Button!";
			button.Flyout = menuFlyout;

			var root = new Grid();
			root.Children.Add(button);

			TestServices.WindowHelper.WindowContent = root;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await ShowMenuFlyout(menuFlyout, button, 0, 0);

		await RunOnUIThread(() =>
		{
			var automationElement = FindPeerByName(GetOpenPopupPeer(button), s_automationName);
			VERIFY_IS_NOT_NULL(automationElement);
			VERIFY_ARE_EQUAL(s_automationId, automationElement.GetAutomationId());

			var children = automationElement.GetChildren();
			VERIFY_IS_NOT_NULL(children);
			VERIFY_IS_TRUE(children.Count > 0, "Failed to get first child.");

			// The items are set as 10/40, 20/40, 30/40 and 40/40, so we jump up in tens:
			const int expectedSizeOfSet = 40;
			int expectedPositionInSet = 10;
			int childIndex = 0;
			while (expectedPositionInSet <= expectedSizeOfSet)
			{
				VERIFY_IS_TRUE(childIndex < children.Count, "Failed to get next sibling");
				var currentElement = children[childIndex++];
				VERIFY_IS_NOT_NULL(currentElement);

				VERIFY_ARE_EQUAL(expectedSizeOfSet, currentElement.GetSizeOfSet());
				VERIFY_ARE_EQUAL(expectedPositionInSet, currentElement.GetPositionInSet());

				expectedPositionInSet += 10;
			}
		});

		await FlyoutHelper.HideFlyout(menuFlyout);
	}

	// The peer of the one open popup, standing in for the UIA element of the popup window.
	private static AutomationPeer GetOpenPopupPeer(FrameworkElement target)
	{
		var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(target.XamlRoot);
		if (popups.Count != 1)
		{
			throw new InvalidOperationException("Expected exactly one open Popup.");
		}

		return FrameworkElementAutomationPeer.CreatePeerForElement(popups[0]);
	}

	// Stands in for the UIA client search by name under the open popup.
	private static AutomationPeer FindPeerByName(AutomationPeer root, string name)
	{
		if (root is null)
		{
			return null;
		}

		if (root.GetName() == name)
		{
			return root;
		}

		var children = root.GetChildren();
		if (children is not null)
		{
			foreach (var child in children)
			{
				var found = FindPeerByName(child, name);
				if (found is not null)
				{
					return found;
				}
			}
		}

		return null;
	}

	// Stands in for IUIAutomationTreeWalker::GetFirstChildElement.
	private static AutomationPeer GetFirstChild(AutomationPeer peer)
	{
		var children = peer.GetChildren();
		return children is { Count: > 0 } ? children[0] : null;
	}

	// Stands in for IUIAutomationTreeWalker::GetNextSiblingElement.
	private static AutomationPeer GetNextSibling(AutomationPeer peer)
	{
		var siblings = peer.GetParent()?.GetChildren();
		if (siblings is null)
		{
			return null;
		}

		var index = siblings.IndexOf(peer);
		return index >= 0 && index + 1 < siblings.Count ? siblings[index + 1] : null;
	}
}
