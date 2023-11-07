// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if HAS_UNO
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Input.Preview.Injection;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Common;
using Microsoft.UI.Xaml.Automation.Peers;
using MUXControlsTestApp;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using Uno.Extensions;
using Uno.UI.RuntimeTests;
using Uno.UI.RuntimeTests.Helpers;
using MenuBarItem = Microsoft.UI.Xaml.Controls.MenuBarItem;
using MenuBar = Microsoft.UI.Xaml.Controls.MenuBar;

// These are InteractionTests ported from WinUI but modified to run as RuntimeTests
// Uno specific: We don't have an implementation of VerifyElement that searches by AutomationProperties.Name
// so when possible, we search by UIElement.Name (which is called an "id" by WinUI tests).

namespace Microsoft.UI.Xaml.Tests.MUXControls.InteractionTests
{
	[TestClass]
	[RunsOnUIThread]
	public class MenuBarTests : MUXApiTestBase
	{
		[TestMethod]
#if !__SKIA__
		[Ignore("InputInjector is only supported on skia")]
#endif
		public async Task BasicMouseInteractionTest()
		{
			var menuBarPage = new MenuBarPage();
			TestServices.WindowHelper.WindowContent = menuBarPage;
			await TestServices.WindowHelper.WaitForIdle();

			var fileButton = FindElementById<FrameworkElement>("FileItem");
			var editButton = FindElementById<FrameworkElement>("EditItem");

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			// click and click
			// From bug 17343407: this test is sometimes unreliable, use retries and see if that helps.
			await InputHelperLeftClick(fileButton, mouse);
			await VerifyAreEqualWithRetry(20,
				() => true,
				() => FindElementById<UIElement>("NewItem") is { }); // The item should be in the tree

			await InputHelperLeftClick(fileButton, mouse);
			VerifyElementNotFound("NewItem");

			// Uno TODO: FlyoutBase.OverlayInputPassThroughElement is not implemented
			// // click and hover
			// await InputHelperLeftClick(fileButton, mouse);
			// VerifyElementNotFound("UndoItem");
			// await InputHelperMoveMouse(editButton, 0, 0, mouse);
			// await InputHelperMoveMouse(editButton, 1, 1, mouse);
			// VerifyElementFound("UndoItem");
		}

		[TestMethod]
		public async Task AutomationPeerTest()
		{
			var menuBarPage = new MenuBarPage();
			TestServices.WindowHelper.WindowContent = menuBarPage;
			await TestServices.WindowHelper.WaitForIdle();

			var fileButton = FindElementById<MenuBarItem>("FileItem");
			var fileButtonEC = fileButton.GetAutomationPeer() as MenuBarItemAutomationPeer;

			// Invoke
			fileButton.Invoke();
			await TestServices.WindowHelper.WaitForIdle();
			VerifyElementFound("NewItem");
			fileButton.Invoke();
			await TestServices.WindowHelper.WaitForIdle();
			VerifyElementNotFound("NewItem");

			// Expand collapse
			fileButtonEC.Expand();
			await TestServices.WindowHelper.WaitForIdle();
			VerifyElementFound("NewItem");
			fileButtonEC.Collapse();
			await TestServices.WindowHelper.WaitForIdle();
			VerifyElementNotFound("NewItem");

			// Verify GetNameCore() is working if AutomationProperties.Name isn't set
			// VerifyElement.Found("Format", FindBy.Name); // Uno specific : we don't have an implementation that looks through AutomationProperties.Name
		}

		[TestMethod]
		public async Task KeyboardNavigationWithArrowKeysTest()
		{
			var menuBarPage = new MenuBarPage();
			TestServices.WindowHelper.WindowContent = menuBarPage;
			await TestServices.WindowHelper.WaitForIdle();

			var editButton = FindElementById<MenuBarItem>("EditItem");
			editButton.Invoke();
			await TestServices.WindowHelper.WaitForIdle();
			VerifyElementFound("UndoItem");

			TestServices.KeyboardHelper.Left();
			await TestServices.WindowHelper.WaitForIdle();
			VerifyElementFound("NewItem");

			TestServices.KeyboardHelper.Escape();
			await TestServices.WindowHelper.WaitForIdle();
			VerifyElementNotFound("NewItem");

			TestServices.KeyboardHelper.Left();
			await TestServices.WindowHelper.WaitForIdle();
			VerifyElementNotFound("WordWrapItem");

			TestServices.KeyboardHelper.Enter();
			await TestServices.WindowHelper.WaitForIdle();
			VerifyElementFound("WordWrapItem");

			TestServices.KeyboardHelper.Escape();
			await TestServices.WindowHelper.WaitForIdle();
			TestServices.KeyboardHelper.Right();
			await TestServices.WindowHelper.WaitForIdle();

			TestServices.KeyboardHelper.Right();
			await TestServices.WindowHelper.WaitForIdle();
			VerifyElementNotFound("UndoItem");

			TestServices.KeyboardHelper.Enter();
			await TestServices.WindowHelper.WaitForIdle();
			VerifyElementFound("UndoItem");

			TestServices.KeyboardHelper.Down();
			TestServices.KeyboardHelper.Down();
			TestServices.KeyboardHelper.Down();
			TestServices.KeyboardHelper.Down();
			TestServices.KeyboardHelper.Down();
			await TestServices.WindowHelper.WaitForIdle();
			VerifyElementNotFound("Item1");

			TestServices.KeyboardHelper.Right();
			VerifyElementFound("Item1");

			// Uno specific: make sure to close the popups at the end
			VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.XamlRoot).ForEach(p => p.IsOpen = false);
		}

		// [TestMethod]
		// public void KeyboardNavigationWithAccessKeysTest()
		// {
		// 	if (PlatformConfiguration.IsDevice(DeviceType.Phone))
		// 	{
		// 		Log.Comment("Skipping tests on phone, because menubar is not supported.");
		// 		return;
		// 	}
		// 	using (var setup = new TestSetupHelper("MenuBar Tests"))
		// 	{
		// 		KeyboardHelper.PressDownModifierKey(ModifierKey.Alt);
		// 		KeyboardHelper.ReleaseModifierKey(ModifierKey.Alt);
		// 		Wait.ForIdle();
		//
		// 		TextInput.SendText("A"); // this clicks File menu bar item
		// 		Wait.ForIdle();
		// 		TextInput.SendText("B"); // this clicks New flyout item
		// 		Wait.ForIdle();
		//
		// 		VerifyElement.Found("New Clicked", FindBy.Name);
		// 	}
		// }
		//
		// [TestMethod]
		// public void KeyboardAcceleratorsTest()
		// {
		// 	if (PlatformConfiguration.IsDevice(DeviceType.Phone))
		// 	{
		// 		Log.Comment("Skipping tests on phone, because menubar is not supported.");
		// 		return;
		// 	}
		// 	using (var setup = new TestSetupHelper("MenuBar Tests"))
		// 	{
		// 		VerifyElement.NotFound("Undo Clicked", FindBy.Name);
		//
		// 		KeyboardHelper.PressDownModifierKey(ModifierKey.Control);
		// 		Log.Comment("Send text z.");
		// 		TextInput.SendText("z");
		// 		KeyboardHelper.ReleaseModifierKey(ModifierKey.Control);
		//
		// 		Wait.ForIdle();
		//
		// 		VerifyElement.Found("Undo Clicked", FindBy.Name);
		// 	}
		// }

		[TestMethod]
		public async Task AddRemoveMenuBarItemTest()
		{
			var menuBarPage = new MenuBarPage();
			TestServices.WindowHelper.WindowContent = menuBarPage;
			await TestServices.WindowHelper.WaitForIdle();

			var addButton = FindElementById<Button>("AddMenuBarItemButton");
			var removeButton = FindElementById<Button>("RemoveMenuBarItemButton");

			Log.Comment("Verify that menu bar items can be added");
			addButton.AutomationPeerClick();
			VerifyElementFound("NewMenuBarItem");

			Log.Comment("Verify that menu bar items can be removed");
			removeButton.AutomationPeerClick();
			VerifyElementNotFound("NewMenuBarItem");

			Log.Comment("Verify that menu bar pre-existing items can be removed");
			VerifyElementFound("FormatItem");
			removeButton.AutomationPeerClick();
			VerifyElementNotFound("FormatItem");
		}

		[TestMethod]
		public async Task AddRemoveMenuFlyoutItemTest()
		{
			var menuBarPage = new MenuBarPage();
			TestServices.WindowHelper.WindowContent = menuBarPage;
			await TestServices.WindowHelper.WaitForIdle();

			var addButton = FindElementById<Button>("AddFlyoutItemButton");
			var removeButton = FindElementById<Button>("RemoveFlyoutItemButton");
			var fileButton = FindElementById<MenuBarItem>("FileItem");

			Log.Comment("Verify that menu flyout items can be added");
			addButton.AutomationPeerClick();
			await TestServices.WindowHelper.WaitForIdle();

			// open flyout
			fileButton.Invoke();
			await TestServices.WindowHelper.WaitForIdle();

			VerifyElementFound("NewFlyoutItem");

			// close flyout
			fileButton.Invoke();
			await TestServices.WindowHelper.WaitForIdle();

			Log.Comment("Verify that menu flyout items can be removed");
			removeButton.AutomationPeerClick();
			await TestServices.WindowHelper.WaitForIdle();

			// open flyout
			fileButton.Invoke();
			await TestServices.WindowHelper.WaitForIdle();

			VerifyElementNotFound("NewFlyoutItem");

			// close flyout
			fileButton.Invoke();
			await TestServices.WindowHelper.WaitForIdle();
		}

		[TestMethod]
		public async Task MenuBarHeightTest()
		{
			using var _ = StyleHelper.UseFluentStyles();

			var menuBarPage = new MenuBarPage();
			TestServices.WindowHelper.WindowContent = menuBarPage;
			await TestServices.WindowHelper.WaitForIdle();

			var menuBar = FindElementById<MenuBar>("SizedMenuBar");
			var menuBarItem = FindElementById<MenuBarItem>("SizedMenuBarItem");

			Log.Comment("Verify that the size of the MenuBar can be set.");

			Verify.AreEqual(menuBar.ActualSize.Y, 24);
			Verify.AreEqual(menuBarItem.ActualSize.Y, 16);
		}

		[TestMethod]
#if !__SKIA__
		[Ignore("InputInjector is only supported on skia")]
#endif
		public async Task HoveringBehaviorTest()
		{
			var menuBarPage = new MenuBarPage();
			TestServices.WindowHelper.WindowContent = menuBarPage;
			await TestServices.WindowHelper.WaitForIdle();

			var menuBar = FindElementById<MenuBar>("SizedMenuBar");
			var addButton = FindElementById<Button>("AddItemsToEmptyMenuBar");

			addButton.AutomationPeerClick();
			addButton.AutomationPeerClick();
			addButton.AutomationPeerClick();
			await TestServices.WindowHelper.WaitForIdle();

			var help0 = FindElementById<MenuBarItem>("Help0");
			var help1 = FindElementById<MenuBarItem>("Help1");

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			// This behavior seems to a bit unreliable, so repeat
			await InputHelperLeftClick(help0, mouse);
			await VerifyAreEqualWithRetry(20,
				() => true,
				() => FindElementById<MenuFlyoutItem>("Add0") != null); // The item should be in the tree

			// Uno TODO: FlyoutBase.OverlayInputPassThroughElement is not implemented
			// // Check if hovering over the next button actually will show the correct item
			// VerifyElementNotFound("Add1");
			// await InputHelperMoveMouse(help1, 0, 0, mouse);
			// await InputHelperMoveMouse(help1, 1, 1, mouse);
			// await InputHelperMoveMouse(help1, 5, 5, mouse);
			//
			// UIElement add1Element = FindElementById<UIElement>("Help0");;
			// Verify.IsNotNull(add1Element);

			// Uno specific: Remove this when FlyoutBase.OverlayInputPassThroughElement is implemented
			VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.XamlRoot).ForEach(p => p.IsOpen = false);
		}

		[TestMethod]
		public async Task TabTest()
		{
			var menuBarPage = new MenuBarPage();
			TestServices.WindowHelper.WindowContent = menuBarPage;
			await TestServices.WindowHelper.WaitForIdle();

			var firstButton = FindElementById<Button>("FirstButton");
			var fileButton = FindElementById<MenuBarItem>("FileItem");

			firstButton.Focus(FocusState.Programmatic);
			await TestServices.WindowHelper.WaitForIdle();

			Log.Comment("Verify that pressing tab from previous control goes to the File item");
			TestServices.KeyboardHelper.Tab();
			await TestServices.WindowHelper.WaitForIdle();

			Verify.AreEqual(FocusState.Keyboard, fileButton.FocusState);
		}

		[TestMethod]
		public async Task EmptyMenuBarItemNoPopupTest()
		{
			var menuBarPage = new MenuBarPage();
			TestServices.WindowHelper.WindowContent = menuBarPage;
			await TestServices.WindowHelper.WaitForIdle();

			FindElementById<MenuBarItem>("NoChildrenFlyoutMenuBarItem").Invoke();
			// VerifyElement.NotFound("Popup",FindBy.Name);
			Assert.AreEqual(0, VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.XamlRoot).Count);

			FindElementById<MenuBarItem>("OneChildrenFlyoutMenuBarItem").Invoke();
			// VerifyElement.Found("Popup", FindBy.Name);
			Assert.AreEqual(1, VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.XamlRoot).Count);

			// Click twice to close flyout
			FindElementById<Button>("RemoveItemsFromOneChildrenItem").AutomationPeerClick();
			FindElementById<Button>("RemoveItemsFromOneChildrenItem").AutomationPeerClick();

			FindElementById<MenuBarItem>("OneChildrenFlyoutMenuBarItem").Invoke();
			// VerifyElement.NotFound("Popup", FindBy.Name);
			Assert.AreEqual(0, VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.XamlRoot).Count);
		}

		private async Task InputHelperLeftClick(FrameworkElement result, Mouse mouse)
		{
			var position = result.GetAbsoluteBounds().GetCenter();
			mouse.Press(position);
			mouse.Release();
			await TestServices.WindowHelper.WaitForIdle();
		}

		private async Task InputHelperMoveMouse(FrameworkElement result, int offsetX, int offsetY, Mouse mouse)
		{
			var position = result.GetAbsoluteBounds().GetLocation();
			mouse.MoveTo(position + new Point(offsetX, offsetY));
			await TestServices.WindowHelper.WaitForIdle();
		}

		private void VerifyElementFound(string name) => Verify.IsNotNull(FindElementById<UIElement>(name));

		private void VerifyElementNotFound(string name) => Verify.IsNull(FindElementById<UIElement>(name));

		public static async Task VerifyAreEqualWithRetry(int maxRetries, Func<object> expectedFunc, Func<object> actualFunc, Func<Task> retryAction = null)
		{
			if (retryAction == null)
			{
				retryAction = async () =>
				{
					await Task.Delay(50);
				};
			}

			for (int retry = 0; retry <= maxRetries; retry++)
			{
				object expected = expectedFunc();
				object actual = actualFunc();
				if (Equals(expected, actual) || retry == maxRetries)
				{
					Log.Comment("Actual retry times: " + retry);
					Verify.AreEqual(expected, actual);
					return;
				}
				else
				{
					await retryAction();
				}
			}
		}

		private static T FindElementById<T>(string name) where T : UIElement => FindElementById<T>(TestServices.WindowHelper.XamlRoot.VisualTree.RootElement, name);

		private static T FindElementById<T>(DependencyObject parent, string name) where T : UIElement
		{
			var count = VisualTreeHelper.GetChildrenCount(parent);
			for (var i = 0; i < count; i++)
			{
				var child = VisualTreeHelper.GetChild(parent, i);
				if (child is FrameworkElement fe && fe.Name == name)
				{
					return fe as T;
				}
				else
				{
					var result = FindElementById<T>(child, name);
					if (result != null)
					{
						return result;
					}
				}
			}
			return null;
		}
	}
}
#endif
