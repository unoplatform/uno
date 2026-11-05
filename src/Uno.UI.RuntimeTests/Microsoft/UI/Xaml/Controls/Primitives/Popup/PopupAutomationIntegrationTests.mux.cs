// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference PopupAutomationIntegrationTests.cpp, commit bac7a9c33

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Tests.Common;
using Microsoft.UI.Xaml.Tests.Enterprise;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.MUX.Helpers;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.MUX.Microsoft_UI_Xaml_Controls.Primitives;

// Notes on the port:
// - Upstream drives a UIA client (IUIAutomation / IRawElementProviderFragment) against the Popup. Where an equivalent
//   exists, it is replaced by the AutomationPeer API (name, id, class name, patterns, Navigate). The UIA client's
//   "find the element by name and id" step becomes asserting the peer's name and id.
// - Not yet supported tests (blocked, kept under #if false): VerifyParentlessPopupAutomationTree_NoFrameAP,
//   VerifyParentlessPopupAutomationTree_FrameAP, VerifyOpenPopupPreservesMainTree (need a window-level UIA root that
//   parents the open popups), VerifyPopupButtonAutomationPropertyChangedEvent (needs mock UIA clients listening).
[TestClass]
public class PopupAutomationIntegrationTests
{
	private const string s_automationName = "TestPopupName";
	private const string s_automationId = "TestPopupId";
	private const string s_automationPopupButtonName = "TestPopupButtonName";
	private const string s_automationPopupButtonId = "TestPopupButtonId";

	private const string s_automationName2 = "TestPopupName2";
	private const string s_automationId2 = "TestPopupId2";
	private const string s_automationPopupButtonName2 = "TestPopupButtonName2";
	private const string s_automationPopupButtonId2 = "TestPopupButtonId2";

	[TestCleanup]
	public async Task TestCleanup()
	{
		await RunOnUIThread(() =>
		{
			if (TestServices.WindowHelper.XamlRoot is { } xamlRoot)
			{
				VisualTreeHelper.CloseAllPopups(xamlRoot);
			}
		});
	}

	// Upstream: FlyoutHelper::CreateDefaultFlyout
	private static async Task<Flyout> CreateDefaultFlyout(FlyoutPlacementMode placement)
	{
		Flyout flyout = null;

		await RunOnUIThread(() =>
		{
			var content = new StackPanel();
			content.Width = 300;

			var button = new Button();
			button.Margin = ThicknessHelper.FromUniformLength(12);
			button.FontSize = 25;
			button.FontWeight = FontWeights.Thin;
			button.HorizontalAlignment = HorizontalAlignment.Stretch;
			button.Content = "button";

			var textbox = new TextBox();
			textbox.Margin = ThicknessHelper.FromUniformLength(12);
			textbox.Height = 225;
			textbox.HorizontalAlignment = HorizontalAlignment.Stretch;
			button.FontSize = 18;
			button.FontWeight = FontWeights.Bold;
			textbox.Text = "textbox";

			var checkbox = new CheckBox();
			checkbox.Margin = ThicknessHelper.FromUniformLength(12);
			checkbox.FontSize = 25;
			checkbox.FontWeight = FontWeights.Thin;
			checkbox.HorizontalAlignment = HorizontalAlignment.Stretch;
			checkbox.Content = "checkbox";

			content.Children.Add(button);
			content.Children.Add(textbox);
			content.Children.Add(checkbox);

			var style = new Style(typeof(FlyoutPresenter));
			style.Setters.Add(new Setter(FrameworkElement.TagProperty, "presenter_style"));

			flyout = new Flyout();
			flyout.Placement = placement;
			flyout.Content = content;
			flyout.FlyoutPresenterStyle = style;
		});

		return flyout;
	}

	private static void CheckUIAElementName(AutomationPeer uiaElement, string expectedName)
	{
		Assert.IsNotNull(uiaElement);
		Assert.AreEqual(expectedName, uiaElement.GetName());
	}

	// Test Cases

	[TestMethod]
	[Ignore("TODO Uno: needs a window-level UIA root (GetUIAWindow) whose children are the open popups followed by the root content")]
	public async Task VerifyParentlessPopupAutomationTree_NoFrameAP()
	{
		await VerifyParentlessPopupAutomationTreeCommon(false /* hasFrameAP */);
	}

	[TestMethod]
	[Ignore("TODO Uno: needs a window-level UIA root (GetUIAWindow) whose children are the open popups followed by the root content")]
	public async Task VerifyParentlessPopupAutomationTree_FrameAP()
	{
		await VerifyParentlessPopupAutomationTreeCommon(true /* hasFrameAP */);
	}

	private async Task VerifyParentlessPopupAutomationTreeCommon(bool hasFrameAP)
	{
#if false // TODO Uno: needs a window-level UIA root (GetUIAWindow) whose children are the open popups followed by the root content
		var wh = TestServices.WindowHelper;

		Popup popup = null;
		Popup popup2 = null;

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("> Setting up tree");

			var button = new Rectangle();
			AutomationProperties.SetName(button, s_automationPopupButtonName);

			popup = new Popup();
			popup.Child = button;
			AutomationProperties.SetName(popup, s_automationName);

			var button2 = new Rectangle();
			AutomationProperties.SetName(button2, s_automationPopupButtonName2);

			popup2 = new Popup();
			popup2.Child = button2;
			AutomationProperties.SetName(popup2, s_automationName2);

			var page = new Page();
			AutomationProperties.SetName(page, "RootPageName");

			var frame = new Frame();
			frame.Content = page;
			if (hasFrameAP)
			{
				AutomationProperties.SetName(frame, "RootFrameName");
			}

			wh.WindowContent = frame;

			LOG_OUTPUT("> Opening popups");
			popup.XamlRoot = frame.XamlRoot;
			popup2.XamlRoot = frame.XamlRoot;
			popup.IsOpen = true;
			popup2.IsOpen = true;
		});
		await wh.WaitForIdle();

		await RunOnUIThread(() =>
		{
			AutomationPeer window = GetUIAWindow();

			LOG_OUTPUT("  > Window's first child = Popup1");
			var uiaPopup1 = (AutomationPeer)window.Navigate(AutomationNavigationDirection.FirstChild);
			CheckUIAElementName(uiaPopup1, s_automationName);

			LOG_OUTPUT("  > Popup1's parent = window");
			var uiaParent = (AutomationPeer)uiaPopup1.Navigate(AutomationNavigationDirection.Parent);
			Assert.AreEqual(window, uiaParent);

			LOG_OUTPUT("  > Popup1's next sibling = Popup2");
			var uiaPopup2 = (AutomationPeer)uiaPopup1.Navigate(AutomationNavigationDirection.NextSibling);
			CheckUIAElementName(uiaPopup2, s_automationName2);

			LOG_OUTPUT("  > Popup2's parent = window");
			uiaParent = (AutomationPeer)uiaPopup2.Navigate(AutomationNavigationDirection.Parent);
			Assert.AreEqual(window, uiaParent);

			AutomationPeer uiaPage;
			if (hasFrameAP)
			{
				LOG_OUTPUT("  > Popup2's next sibling = Frame");
				var uiaFrame = (AutomationPeer)uiaPopup2.Navigate(AutomationNavigationDirection.NextSibling);
				CheckUIAElementName(uiaFrame, "RootFrameName");

				LOG_OUTPUT("  > Frame's parent = window");
				uiaParent = (AutomationPeer)uiaFrame.Navigate(AutomationNavigationDirection.Parent);
				Assert.AreEqual(window, uiaParent);

				LOG_OUTPUT("  > Frame's next sibling = null");
				Assert.IsNull(uiaFrame.Navigate(AutomationNavigationDirection.NextSibling));

				LOG_OUTPUT("  > Frame's first child = Page");
				uiaPage = (AutomationPeer)uiaFrame.Navigate(AutomationNavigationDirection.FirstChild);
				CheckUIAElementName(uiaPage, "RootPageName");

				LOG_OUTPUT("  > Page's parent = Frame");
				uiaParent = (AutomationPeer)uiaPage.Navigate(AutomationNavigationDirection.Parent);
				Assert.AreEqual(uiaFrame, uiaParent);

				LOG_OUTPUT("  > Page's next sibling = null");
				Assert.IsNull(uiaPage.Navigate(AutomationNavigationDirection.NextSibling));
			}
			else
			{
				LOG_OUTPUT("  > Popup2's next sibling = Page");
				uiaPage = (AutomationPeer)uiaPopup2.Navigate(AutomationNavigationDirection.NextSibling);
				CheckUIAElementName(uiaPage, "RootPageName");

				LOG_OUTPUT("  > Page's parent = window");
				uiaParent = (AutomationPeer)uiaPage.Navigate(AutomationNavigationDirection.Parent);
				Assert.AreEqual(window, uiaParent);

				LOG_OUTPUT("  > Page's next sibling = null");
				Assert.IsNull(uiaPage.Navigate(AutomationNavigationDirection.NextSibling));
			}

			LOG_OUTPUT("  > Popup1's first child = Button1");
			var uiaButton1 = (AutomationPeer)uiaPopup1.Navigate(AutomationNavigationDirection.FirstChild);
			CheckUIAElementName(uiaButton1, s_automationPopupButtonName);

			LOG_OUTPUT("  > Button1's parent = Popup1");
			uiaPopup1 = (AutomationPeer)uiaButton1.Navigate(AutomationNavigationDirection.Parent);
			CheckUIAElementName(uiaPopup1, s_automationName);

			LOG_OUTPUT("  > Popup2's first child = Button2");
			var uiaButton2 = (AutomationPeer)uiaPopup2.Navigate(AutomationNavigationDirection.FirstChild);
			CheckUIAElementName(uiaButton2, s_automationPopupButtonName2);

			LOG_OUTPUT("  > Button2's parent = Popup2");
			uiaPopup2 = (AutomationPeer)uiaButton2.Navigate(AutomationNavigationDirection.Parent);
			CheckUIAElementName(uiaPopup2, s_automationName2);
		});
		await wh.WaitForIdle();
#else
		await Task.CompletedTask;
#endif
	}

	[TestMethod]
	[Ignore("TODO Uno: needs a window-level UIA root (GetUIAWindow) whose children are the open popups followed by the root content")]
	public async Task VerifyOpenPopupPreservesMainTree()
	{
#if false // TODO Uno: needs a window-level UIA root (GetUIAWindow) whose children are the open popups followed by the root content
		var wh = TestServices.WindowHelper;

		Popup popup = null;

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("> Setting up tree");

			var button = new Rectangle();
			AutomationProperties.SetName(button, s_automationPopupButtonName);

			popup = new Popup();
			popup.Child = button;
			AutomationProperties.SetName(popup, s_automationName);

			var button2 = new Rectangle();
			AutomationProperties.SetName(button2, s_automationPopupButtonName2);

			var buttonOnPage = new Button();
			AutomationProperties.SetName(buttonOnPage, "ButtonOnPageName");

			var page = new Page();
			AutomationProperties.SetName(page, "RootPageName");
			page.Content = buttonOnPage;

			var frame = new Frame();
			frame.Content = page;

			wh.WindowContent = frame;

			LOG_OUTPUT("> Opening popup");
			popup.XamlRoot = frame.XamlRoot;
			popup.IsOpen = true;
		});
		await wh.WaitForIdle();

		await RunOnUIThread(() =>
		{
			AutomationPeer window = GetUIAWindow();

			LOG_OUTPUT("  > Window's first child = Popup1");
			var uiaPopup1 = (AutomationPeer)window.Navigate(AutomationNavigationDirection.FirstChild);
			CheckUIAElementName(uiaPopup1, s_automationName);

			LOG_OUTPUT("  > Popup1's next sibling = Page");
			var uiaPage = (AutomationPeer)uiaPopup1.Navigate(AutomationNavigationDirection.NextSibling);
			CheckUIAElementName(uiaPage, "RootPageName");

			LOG_OUTPUT("  > Page's first child = ButtonOnPage");
			var uiaButtonOnPage = (AutomationPeer)uiaPage.Navigate(AutomationNavigationDirection.FirstChild);
			CheckUIAElementName(uiaButtonOnPage, "ButtonOnPageName");
		});
		await wh.WaitForIdle();
#else
		await Task.CompletedTask;
#endif
	}

	[TestMethod]
	public async Task VerifyAutomationProperties()
	{
		Popup popup = null;

		var loadedEvent = new Event();
		var loadedRegistration = CreateSafeEventRegistration<StackPanel, RoutedEventHandler>(nameof(StackPanel.Loaded));

		await RunOnUIThread(() =>
		{
			popup = new Popup();
			popup.Child = new Button();

			AutomationProperties.SetName(popup, s_automationName);
			AutomationProperties.SetAutomationId(popup, s_automationId);

			var rootPanel = new StackPanel();
			loadedRegistration.Attach(rootPanel, (s, e) => { loadedEvent.Set(); });

			TestServices.WindowHelper.WindowContent = rootPanel;
		});

		await loadedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			popup.XamlRoot = TestServices.WindowHelper.WindowContent.XamlRoot;
		});

		await PopupHelper.OpenPopup(popup);

		await RunOnUIThread(() =>
		{
			// Uno: the UIA client-side node for the Popup is the Popup's AutomationPeer.
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(popup);

			LOG_OUTPUT("Verifying that UIA client-side node for Popup exists.");
			VERIFY_IS_NOT_NULL(peer);
			Assert.AreEqual(s_automationId, peer.GetAutomationId());

			LOG_OUTPUT("Verifying the UIA name property from client-side node for Popup.");
			var name = peer.GetName();
			LOG_OUTPUT($"Name = '{name}', Expected = '{s_automationName}'.");
			VERIFY_IS_TRUE(string.Equals(s_automationName, name, StringComparison.Ordinal));

			LOG_OUTPUT("Verifying the UIA class name property from client-side node for Popup.");
			var className = peer.GetClassName();
			LOG_OUTPUT($"ClassName = '{className}', Expected = 'Popup'.");
			VERIFY_IS_TRUE(string.Equals("Popup", className, StringComparison.Ordinal));
		});
	}

	[TestMethod]
	public async Task VerifyBarePopupDoesNotImplementWindowPattern()
	{
		Popup popup = null;

		var loadedEvent = new Event();
		var loadedRegistration = CreateSafeEventRegistration<StackPanel, RoutedEventHandler>(nameof(StackPanel.Loaded));

		await RunOnUIThread(() =>
		{
			popup = new Popup();
			popup.Child = new Button();

			AutomationProperties.SetName(popup, s_automationName);
			AutomationProperties.SetAutomationId(popup, s_automationId);

			var rootPanel = new StackPanel();
			loadedRegistration.Attach(rootPanel, (s, e) => { loadedEvent.Set(); });

			TestServices.WindowHelper.WindowContent = rootPanel;
		});

		await loadedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			popup.XamlRoot = TestServices.WindowHelper.WindowContent.XamlRoot;
		});

		await PopupHelper.OpenPopup(popup);

		await RunOnUIThread(() =>
		{
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(popup);
			Assert.AreEqual(s_automationId, peer.GetAutomationId());
			Assert.AreEqual(s_automationName, peer.GetName());

			LOG_OUTPUT("Verifying that a loose Popup does not report that it supports the Window pattern.");
			var windowPattern = peer.GetPattern(PatternInterface.Window);
			VERIFY_IS_NULL(windowPattern);
		});
	}

	[TestMethod]
	public async Task VerifyFlyoutPopupImplementsWindowPattern()
	{
		var flyout = await CreateDefaultFlyout(FlyoutPlacementMode.Top);

		var flyoutTarget = await FlyoutHelper.CreateTarget(
			300 /*width*/, 300 /*height*/,
			ThicknessHelper.FromUniformLength(100),
			HorizontalAlignment.Center,
			VerticalAlignment.Center);

		var targetLoadedEvent = new Event();
		var targetLoadedRegistration = CreateSafeEventRegistration<Border, RoutedEventHandler>(nameof(Border.Loaded));

		await RunOnUIThread(() =>
		{
			targetLoadedRegistration.Attach(flyoutTarget, (s, e) => { targetLoadedEvent.Set(); });

			TestServices.WindowHelper.WindowContent = flyoutTarget;
		});

		await targetLoadedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await FlyoutHelper.OpenFlyout(flyout, flyoutTarget, FlyoutOpenMethod.Programmatic_ShowAt);

		await RunOnUIThread(() =>
		{
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(flyoutTarget.XamlRoot);
			var flyoutPopup = popups[0];

			AutomationProperties.SetName(flyoutPopup, s_automationName);
			AutomationProperties.SetAutomationId(flyoutPopup, s_automationId);
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(flyoutTarget.XamlRoot);
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(popups[0]);
			Assert.AreEqual(s_automationId, peer.GetAutomationId());
			Assert.AreEqual(s_automationName, peer.GetName());

			LOG_OUTPUT("Verifying that the Popup for a Flyout reports that it supports the Window pattern.");
			var windowPattern = peer.GetPattern(PatternInterface.Window);
			VERIFY_IS_NOT_NULL(windowPattern);
		});

		await FlyoutHelper.HideFlyout(flyout);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11 | RuntimeTestPlatforms.SkiaMacOS)] // Upstream: IncludeOnlyOn Desktop
	public async Task VerifyMenuFlyoutPopupImplementsWindowPattern()
	{
		MenuFlyout menuFlyout = null;
		MenuFlyoutSubItem menuFlyoutSubItem = null;

		var flyoutTarget = await FlyoutHelper.CreateTarget(
			300 /*width*/, 300 /*height*/,
			ThicknessHelper.FromUniformLength(100),
			HorizontalAlignment.Center,
			VerticalAlignment.Center);

		var targetLoadedEvent = new Event();
		var flyoutPresenterLostFocusEvent = new Event();
		var targetLoadedRegistration = CreateSafeEventRegistration<Border, RoutedEventHandler>(nameof(Border.Loaded));
		var flyoutPresenterRegistration = CreateSafeEventRegistration<MenuFlyoutPresenter, RoutedEventHandler>(nameof(MenuFlyoutPresenter.LostFocus));

		await RunOnUIThread(() =>
		{
			menuFlyout = (MenuFlyout)XamlReader.Load(
				"<MenuFlyout x:Name='menuFlyout' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' >" +
				"    <MenuFlyoutSubItem x:Name='subItem1' Text='Menu sub item'>" +
				"        <MenuFlyoutItem>Menu item</MenuFlyoutItem>" +
				"    </MenuFlyoutSubItem>" +
				"</MenuFlyout>");

			menuFlyoutSubItem = menuFlyout.Items[0] as MenuFlyoutSubItem;

			targetLoadedRegistration.Attach(flyoutTarget, (s, e) => { targetLoadedEvent.Set(); });

			TestServices.WindowHelper.WindowContent = flyoutTarget;
		});

		await targetLoadedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await FlyoutHelper.OpenFlyout(menuFlyout, flyoutTarget, FlyoutOpenMethod.Programmatic_ShowAt);

		if (PopupHelper.AreWindowedPopupsEnabled())
		{
			// Upstream: temporary fix for UiaEndpoint synchronization (TEMP_WaitForOpenWindowedPopup).
			await PopupHelper.WaitForOpenPopup(flyoutTarget);
		}

		await RunOnUIThread(() =>
		{
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(flyoutTarget.XamlRoot);

			var popup = popups[0];
			var menuFlyoutPresenter = popup.Child as MenuFlyoutPresenter;

			flyoutPresenterRegistration.Attach(menuFlyoutPresenter, (s, e) => { flyoutPresenterLostFocusEvent.Set(); });
		});

		TestServices.InputHelper.Tap(menuFlyoutSubItem);

		await flyoutPresenterLostFocusEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		const string menuFlyoutPopupAutomationName = "MenuFlyoutPopupName";
		const string menuFlyoutPopupAutomationId = "MenuFlyoutPopupId";
		const string menuFlyoutSubItemPopupAutomationName = "SubItemPopupName";
		const string menuFlyoutSubItemPopupAutomationId = "SubItemPopupId";

		await RunOnUIThread(() =>
		{
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(flyoutTarget.XamlRoot);
			var flyoutPopup = popups[0];
			var flyoutSubItemPopup = popups[1];

			AutomationProperties.SetName(flyoutPopup, menuFlyoutPopupAutomationName);
			AutomationProperties.SetAutomationId(flyoutPopup, menuFlyoutPopupAutomationId);
			AutomationProperties.SetName(flyoutSubItemPopup, menuFlyoutSubItemPopupAutomationName);
			AutomationProperties.SetAutomationId(flyoutSubItemPopup, menuFlyoutSubItemPopupAutomationId);
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(flyoutTarget.XamlRoot);
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(popups[0]);
			Assert.AreEqual(menuFlyoutPopupAutomationId, peer.GetAutomationId());
			Assert.AreEqual(menuFlyoutPopupAutomationName, peer.GetName());

			LOG_OUTPUT("Verifying that the Popup for a MenuFlyout reports that it supports the Window pattern.");
			var windowPattern = peer.GetPattern(PatternInterface.Window);
			VERIFY_IS_NOT_NULL(windowPattern);
		});

		await RunOnUIThread(() =>
		{
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(flyoutTarget.XamlRoot);
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(popups[1]);
			Assert.AreEqual(menuFlyoutSubItemPopupAutomationId, peer.GetAutomationId());
			Assert.AreEqual(menuFlyoutSubItemPopupAutomationName, peer.GetName());

			LOG_OUTPUT("Verifying that the Popup for a MenuFlyoutSubItem reports that it supports the Window pattern.");
			var windowPattern = peer.GetPattern(PatternInterface.Window);
			VERIFY_IS_NOT_NULL(windowPattern);
		});

		await FlyoutHelper.HideFlyout(menuFlyout);
	}

	[TestMethod]
	public async Task VerifyContentDialogPopupImplementsWindowPattern()
	{
		ContentDialog contentDialog = null;

		var openedEvent = new Event();
		var loadedEvent = new Event();
		var closedEvent = new Event();
		var unloadedEvent = new Event();
		var openedRegistration = CreateSafeEventRegistration<ContentDialog, TypedEventHandler<ContentDialog, ContentDialogOpenedEventArgs>>(nameof(ContentDialog.Opened));
		var loadedRegistration = CreateSafeEventRegistration<ContentDialog, RoutedEventHandler>(nameof(ContentDialog.Loaded));
		var closedRegistration = CreateSafeEventRegistration<ContentDialog, TypedEventHandler<ContentDialog, ContentDialogClosedEventArgs>>(nameof(ContentDialog.Closed));
		var unloadedRegistration = CreateSafeEventRegistration<ContentDialog, RoutedEventHandler>(nameof(ContentDialog.Unloaded));

		await RunOnUIThread(() =>
		{
			contentDialog = new ContentDialog();
			contentDialog.Title = "ContentDialog Title";
			contentDialog.PrimaryButtonText = "OK";
			contentDialog.SecondaryButtonText = "Cancel";

			loadedRegistration.Attach(contentDialog, (s, e) => { loadedEvent.Set(); });
			openedRegistration.Attach(contentDialog, (s, e) => { openedEvent.Set(); });
			closedRegistration.Attach(contentDialog, (s, e) => { closedEvent.Set(); });
			unloadedRegistration.Attach(contentDialog, (s, e) => { unloadedEvent.Set(); });

			TestServices.WindowHelper.WindowContent = new StackPanel();
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			var xamlRoot = TestServices.WindowHelper.WindowContent.XamlRoot;

			if (xamlRoot != null)
			{
				contentDialog.XamlRoot = xamlRoot;
			}
		});

		await RunOnUIThread(() =>
		{
			_ = contentDialog.ShowAsync();
		});

		await openedEvent.WaitForDefault();
		await loadedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(contentDialog.XamlRoot);
			var contentDialogPopup = popups[0];

			AutomationProperties.SetName(contentDialogPopup, s_automationName);
			AutomationProperties.SetAutomationId(contentDialogPopup, s_automationId);
		});

		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(contentDialog.XamlRoot);
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(popups[0]);
			Assert.AreEqual(s_automationId, peer.GetAutomationId());
			Assert.AreEqual(s_automationName, peer.GetName());

			LOG_OUTPUT("Verifying that the Popup for a ContentDialog reports that it supports the Window pattern.");
			var windowPattern = peer.GetPattern(PatternInterface.Window);
			VERIFY_IS_NOT_NULL(windowPattern);
		});

		await RunOnUIThread(() =>
		{
			contentDialog.Hide();
		});

		await closedEvent.WaitForDefault();
		await unloadedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[Ignore("TODO Uno: needs mock UIA clients listening (TestServices.Utilities.SetMockUIAClientsListening / ClearMockUIAClientsListening) and an AutomationPropertyChangedHandler to observe the UIA Name property changed event raised for a Popup Button")]
	public async Task VerifyPopupButtonAutomationPropertyChangedEvent()
	{
#if false // TODO Uno: needs mock UIA clients listening and an AutomationPropertyChangedHandler
		// Upstream cleanup: ClearMockUIAClientsListening(); ResetWindowContentAndWaitForIdle();
		Button button1 = null;
		TextBlock textblock1 = null;
		TextBlockAutomationPeer textblock1AP = null;

		await RunOnUIThread(() =>
		{
			var root = (Grid)XamlReader.Load(
				@"<Grid xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
						  Background=""{ThemeResource SystemControlBackgroundAltHighBrush}"" >
						<Popup x:Name=""popupName"">
							<Button x:Name=""popupButtonName"" AutomationProperties.Name=""{Binding Tag, RelativeSource={RelativeSource Self}}"" Click=""ButtonName_Click"" Tag=""Test Popup ButtonName Tag"">
								<StackPanel>
									<TextBlock x:Name=""TextBlock1"" Text=""{Binding Tag, ElementName=popupButtonName}"" />
								</StackPanel>
							</Button>
						</Popup>
						<Button x:Name=""buttonName2"" AutomationProperties.Name=""{Binding Tag, RelativeSource={RelativeSource Self}}"" Click=""ButtonName2_Click"" Tag=""Test Button2"">
							<StackPanel>
								<TextBlock Text=""{Binding Tag, ElementName=buttonName2}"" />
							</StackPanel>
						</Button>
					</Grid>");

			// Create AP for TextBlock to work around AutomationClientManager (Xaml test component) does not ensure AutomationPeer for objects being tested in some cases
			textblock1 = (TextBlock)root.FindName("TextBlock1");
			textblock1AP = (TextBlockAutomationPeer)FrameworkElementAutomationPeer.CreatePeerForElement(textblock1);

			button1 = (Button)root.FindName("popupButtonName");
			TestServices.WindowHelper.WindowContent = root;

			// We need to open the popup after WindowContent is set.  If we set IsOpen="true" in the Popup's Xaml, the Popup is opened
			// during parse time, but the island doesn't exist yet so Xaml doesn't know what PopupRoot to use.
			((Popup)root.FindName("popupName")).IsOpen = true;

			TestServices.Utilities.SetMockUIAClientsListening();
		});

		await TestServices.WindowHelper.WaitForIdle();

		var spClickEvent = new Event();
		var clickRegistration = CreateSafeEventRegistration<Button, RoutedEventHandler>(nameof(Button.Click));

		await RunOnUIThread(() =>
		{
			AutomationProperties.SetName(button1, s_automationPopupButtonName);
			AutomationProperties.SetAutomationId(button1, s_automationPopupButtonId);

			clickRegistration.Attach(button1, (sender, e) =>
			{
				LOG_OUTPUT("Popup Button Click event fired!");
				button1.Tag = button1.Tag + " Clicked!";
				spClickEvent.Set();
			});
		});
		await TestServices.WindowHelper.WaitForIdle();

		// Upstream: attach an AutomationPropertyChangedHandler (UIA_NamePropertyId, TreeScope_Subtree) on the UIA element
		// found by name s_automationPopupButtonName and id s_automationPopupButtonId.

		await RunOnUIThread(() =>
		{
			var button1AP = (ButtonAutomationPeer)FrameworkElementAutomationPeer.CreatePeerForElement(button1);
			LOG_OUTPUT("Click the popup button using UIA and wait for the button event!");
			button1AP.Invoke();
		});
		await spClickEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			var button1AP = (ButtonAutomationPeer)FrameworkElementAutomationPeer.CreatePeerForElement(button1);
			LOG_OUTPUT("Click the popup button using UIA and wait for Automation PropertyChanged event!");
			button1AP.Invoke();
		});
		await TestServices.WindowHelper.WaitForIdle();

		// Upstream: spAutomationPropertyChangedEventHandler->ConfirmAndUnregister();
#else
		await Task.CompletedTask;
#endif
	}
}
