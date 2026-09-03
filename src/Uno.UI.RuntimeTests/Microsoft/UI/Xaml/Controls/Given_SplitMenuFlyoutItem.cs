// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if HAS_UNO
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS | RuntimeTestPlatforms.NativeWasm)]
public class Given_SplitMenuFlyoutItem
{
	[TestMethod]
	public void When_Created_Has_Default_Properties()
	{
		var item = new SplitMenuFlyoutItem();

		Assert.IsNotNull(item.Items);
		Assert.AreEqual(0, item.Items.Count);
		Assert.IsNull(item.SubMenuPresenterStyle);
		Assert.IsNull(item.SubMenuItemStyle);
	}

	[TestMethod]
	public void When_Items_Added()
	{
		var item = new SplitMenuFlyoutItem();
		var child1 = new MenuFlyoutItem { Text = "Child 1" };
		var child2 = new MenuFlyoutItem { Text = "Child 2" };

		item.Items.Add(child1);
		item.Items.Add(child2);

		Assert.AreEqual(2, item.Items.Count);
		Assert.AreSame(child1, item.Items[0]);
		Assert.AreSame(child2, item.Items[1]);
	}

	[TestMethod]
	public void When_Items_Cleared()
	{
		var item = new SplitMenuFlyoutItem();
		item.Items.Add(new MenuFlyoutItem { Text = "Child 1" });
		item.Items.Add(new MenuFlyoutItem { Text = "Child 2" });

		item.Items.Clear();

		Assert.AreEqual(0, item.Items.Count);
	}

	[TestMethod]
	public void When_ContentProperty_Set_In_Collection()
	{
		// The [ContentProperty] attribute should make Items the default content
		var item = new SplitMenuFlyoutItem { Text = "Parent" };
		var child = new MenuFlyoutItem { Text = "Child" };
		item.Items.Add(child);

		Assert.AreEqual(1, item.Items.Count);
	}

	[TestMethod]
	public async Task When_Template_Applied_Has_Buttons()
	{
		var flyout = new MenuFlyout();
		var splitItem = new SplitMenuFlyoutItem { Text = "Split Item" };
		splitItem.Items.Add(new MenuFlyoutItem { Text = "Sub Item 1" });
		flyout.Items.Add(splitItem);

		var button = new Button { Content = "Test", Flyout = flyout };
		TestServices.WindowHelper.WindowContent = button;
		await TestServices.WindowHelper.WaitForLoaded(button);
		await TestServices.WindowHelper.WaitForIdle();

		flyout.ShowAt(button);
		await TestServices.WindowHelper.WaitForIdle();

		// Template should be applied now
		Assert.IsNotNull(splitItem.PrimaryButton, "PrimaryButton template part should be found");

		flyout.Hide();
	}

	[TestMethod]
	public async Task When_Primary_Button_Clicked_Invokes_Click()
	{
		bool clicked = false;
		var splitItem = new SplitMenuFlyoutItem { Text = "Click Me" };
		splitItem.Click += (s, e) => clicked = true;
		splitItem.Items.Add(new MenuFlyoutItem { Text = "Sub Item" });

		var flyout = new MenuFlyout();
		flyout.Items.Add(splitItem);

		var button = new Button { Content = "Test", Flyout = flyout };
		TestServices.WindowHelper.WindowContent = button;
		await TestServices.WindowHelper.WaitForLoaded(button);
		await TestServices.WindowHelper.WaitForIdle();

		flyout.ShowAt(button);
		await TestServices.WindowHelper.WaitForIdle();

		// Invoke the primary action
		splitItem.Invoke();

		Assert.IsTrue(clicked, "Click event should have been raised");
	}

	[TestMethod]
	public void When_SubMenuPresenterStyle_Set()
	{
		var item = new SplitMenuFlyoutItem();
		var style = new Style(typeof(MenuFlyoutPresenter));

		item.SubMenuPresenterStyle = style;

		Assert.AreSame(style, item.SubMenuPresenterStyle);
	}

	[TestMethod]
	public void When_SubMenuItemStyle_Set()
	{
		var item = new SplitMenuFlyoutItem();
		var style = new Style(typeof(MenuFlyoutItem));

		item.SubMenuItemStyle = style;

		Assert.AreSame(style, item.SubMenuItemStyle);
	}

	[TestMethod]
	public void When_AutomationPeer_Created()
	{
		var item = new SplitMenuFlyoutItem { Text = "Test" };
		var peer = FrameworkElementAutomationPeer.CreatePeerForElement(item);

		Assert.IsNotNull(peer);
		Assert.AreEqual(AutomationControlType.MenuItem, peer.GetAutomationControlType());
		Assert.AreEqual("SplitMenuFlyoutItem", peer.GetClassName());
	}

	[TestMethod]
	public void When_AutomationPeer_Supports_Invoke_And_ExpandCollapse()
	{
		var item = new SplitMenuFlyoutItem { Text = "Test" };
		var peer = FrameworkElementAutomationPeer.CreatePeerForElement(item);

		var invokeProvider = peer.GetPattern(PatternInterface.Invoke);
		var expandCollapseProvider = peer.GetPattern(PatternInterface.ExpandCollapse);

		Assert.IsNotNull(invokeProvider, "Should support IInvokeProvider");
		Assert.IsNotNull(expandCollapseProvider, "Should support IExpandCollapseProvider");
		Assert.IsInstanceOfType(invokeProvider, typeof(IInvokeProvider));
		Assert.IsInstanceOfType(expandCollapseProvider, typeof(IExpandCollapseProvider));
	}

	[TestMethod]
	public void When_IsOpen_Default_False()
	{
		var item = new SplitMenuFlyoutItem { Text = "Test" };
		Assert.IsFalse(item.IsOpen);
	}

	[TestMethod]
	public void When_Implements_ISubMenuOwner()
	{
		var item = new SplitMenuFlyoutItem();
		Assert.IsInstanceOfType(item, typeof(ISubMenuOwner));

		var owner = (ISubMenuOwner)item;
		Assert.IsFalse(owner.IsSubMenuOpen);
		Assert.IsTrue(owner.IsSubMenuPositionedAbsolutely);
	}

	[TestMethod]
	public void When_Inherits_From_MenuFlyoutItem()
	{
		var item = new SplitMenuFlyoutItem();
		Assert.IsInstanceOfType(item, typeof(MenuFlyoutItem));
		Assert.IsInstanceOfType(item, typeof(MenuFlyoutItemBase));
	}

	[TestMethod]
	public void When_Text_Set()
	{
		var item = new SplitMenuFlyoutItem { Text = "Hello" };
		Assert.AreEqual("Hello", item.Text);
	}

	[TestMethod]
	public async Task When_Added_To_MenuFlyout()
	{
		var flyout = new MenuFlyout();
		var splitItem = new SplitMenuFlyoutItem { Text = "Split" };
		splitItem.Items.Add(new MenuFlyoutItem { Text = "Option 1" });
		splitItem.Items.Add(new MenuFlyoutItem { Text = "Option 2" });

		flyout.Items.Add(splitItem);
		flyout.Items.Add(new MenuFlyoutItem { Text = "Regular Item" });

		Assert.AreEqual(2, flyout.Items.Count);
		Assert.IsInstanceOfType(flyout.Items[0], typeof(SplitMenuFlyoutItem));
	}

	[TestMethod]
	public void When_Separator_Added_To_Items()
	{
		var item = new SplitMenuFlyoutItem { Text = "Split" };
		item.Items.Add(new MenuFlyoutItem { Text = "Item 1" });
		item.Items.Add(new MenuFlyoutSeparator());
		item.Items.Add(new MenuFlyoutItem { Text = "Item 2" });

		Assert.AreEqual(3, item.Items.Count);
		Assert.IsInstanceOfType(item.Items[1], typeof(MenuFlyoutSeparator));
	}
}
#endif
