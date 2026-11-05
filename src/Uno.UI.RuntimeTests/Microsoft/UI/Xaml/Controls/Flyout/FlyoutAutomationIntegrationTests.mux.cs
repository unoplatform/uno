// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference FlyoutAutomationIntegrationTests.cpp, commit bac7a9c33

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Tests.Common;
using Microsoft.UI.Xaml.Tests.Enterprise;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.MUX.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.MUX.Microsoft_UI_Xaml_Controls;

// Upstream queries the open flyout through a Win32 UIA client (IUIAutomation, AutomationClientManager).
// Uno has no UIA client in the runtime tests, so the UIA client node lookups are done through the
// automation peers of the open popup, which is what the UIA provider exposes to a client.
// Not ported: VerifyUIAFocusEntersFlyoutWithContentControl (needs UIA focus-change events).

[TestClass]
[RequiresFullWindow] // The upstream tests expect window-absolute coordinates.
public class FlyoutAutomationIntegrationTests
{
	private const string s_automationName = "TestFlyoutName";
	private const string s_automationId = "TestFlyoutId";
	private const string s_automationItemStatus = "TestFlyout";

	[TestCleanup]
	public async Task TestCleanup()
	{
		await RunOnUIThread(() =>
		{
#if HAS_UNO
			VisualTreeHelper.CloseAllPopups(TestServices.WindowHelper.XamlRoot);
#endif
			TestServices.WindowHelper.WindowContent = null;
		});
	}

	[TestMethod]
	[Description("Verify AutomationProperties for Flyout.")]
	public async Task VerifyAutomationProperties()
	{
		var flyoutWithName = await FlyoutHelper.CreateDefaultFlyout(FlyoutPlacementMode.Left);
		var flyoutWithContent = await FlyoutHelper.CreateDefaultFlyout(FlyoutPlacementMode.Left);
		var flyoutWithLongContent = await FlyoutHelper.CreateDefaultFlyout(FlyoutPlacementMode.Left);
		var flyoutWithWithContentWithCarriageReturn = await FlyoutHelper.CreateDefaultFlyout(FlyoutPlacementMode.Left);
		var flyoutWithNothing = await FlyoutHelper.CreateDefaultFlyout(FlyoutPlacementMode.Left);

		await RunOnUIThread(() =>
		{
			AutomationProperties.SetName(flyoutWithName, s_automationName);

			flyoutWithName.ShouldConstrainToRootBounds = false;

			var textBlock = new TextBlock();
			textBlock.Text = s_automationName;
			flyoutWithContent.Content = textBlock;
			flyoutWithContent.ShouldConstrainToRootBounds = false;

			textBlock = new TextBlock();
			textBlock.Text = "    1  2    3  4    5  6    7  8    9  10    11  12    13  14    15  16    17  18    19  20    21  22";
			flyoutWithLongContent.Content = textBlock;
			flyoutWithLongContent.ShouldConstrainToRootBounds = false;

			textBlock = new TextBlock();
			textBlock.Text = "1 2 3 4 5\r\n6 7 8 9 10";
			flyoutWithWithContentWithCarriageReturn.Content = textBlock;
			flyoutWithWithContentWithCarriageReturn.ShouldConstrainToRootBounds = false;

			flyoutWithNothing.Content = null;
			flyoutWithNothing.ShouldConstrainToRootBounds = false;
		});

		var target = await FlyoutHelper.CreateTarget(
			300 /*width*/, 300 /*height*/,
			new Thickness(100),
			HorizontalAlignment.Left,
			VerticalAlignment.Center);

		await RunOnUIThread(() =>
		{
			var rootPanel = new Grid();
			rootPanel.Children.Add(target);
			VERIFY_IS_NOT_NULL(rootPanel);

			TestServices.WindowHelper.WindowContent = rootPanel;
		});

		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Performing verification on the Flyout with AutomationProperties.Name set.");
		await VerifyFlyoutAutomationName(flyoutWithName, target, s_automationName);
		LOG_OUTPUT("Performing verification on the Flyout with content.");
		await VerifyFlyoutAutomationName(flyoutWithContent, target, s_automationName);
		LOG_OUTPUT("Performing verification on the Flyout with long content.");
		await VerifyFlyoutAutomationName(flyoutWithLongContent, target, "    1  2    3  4    5  6    7  8    9  10    11  12    13  14    15  16    17  18    19  20...");
		LOG_OUTPUT("Performing verification on the Flyout with content containing a carriage return..");
		await VerifyFlyoutAutomationName(flyoutWithWithContentWithCarriageReturn, target, "1 2 3 4 5...");

		LOG_OUTPUT("Performing verification on the Flyout with nothing set. We have nothing with which to retrieve the ContentDialog, so we expect the automation peer to be unretrievable.");

		await RunOnUIThread(() =>
		{
			Flyout.SetAttachedFlyout(target, flyoutWithNothing);
		});

		await FlyoutHelper.OpenFlyout(flyoutWithNothing, target, FlyoutOpenMethod.Programmatic_ShowAttachedFlyout);

		await RunOnUIThread(() =>
		{
			var spFlyout = FindPeerByName(GetOpenPopupPeer(target), s_automationName);

			LOG_OUTPUT("Verifying that we couldn't get the UIA client-side node for Flyout.");
			VERIFY_IS_NULL(spFlyout);
		});

		await FlyoutHelper.HideFlyout(flyoutWithNothing);
	}

	private async Task VerifyFlyoutAutomationName(Flyout flyout, FrameworkElement target, string expectedName)
	{
		await RunOnUIThread(() =>
		{
			Flyout.SetAttachedFlyout(target, flyout);
		});

		await FlyoutHelper.OpenFlyout(flyout, target, FlyoutOpenMethod.Programmatic_ShowAttachedFlyout);

		await RunOnUIThread(() =>
		{
			var spFlyout = FindPeerByName(GetOpenPopupPeer(target), expectedName);

			LOG_OUTPUT("Verifying that the UIA client-side node for Flyout exists.");
			VERIFY_IS_NOT_NULL(spFlyout);

			LOG_OUTPUT("Verifying that this client-side node is the right ControlType.");
			VERIFY_ARE_EQUAL(AutomationControlType.Pane, spFlyout.GetAutomationControlType());

			LOG_OUTPUT("Verifying the UIA Name property from client-side node for ContentDialog.");
			VERIFY_ARE_EQUAL(expectedName, spFlyout.GetName());

			LOG_OUTPUT("Verifying the UIA class name property from client-side node for FlyoutPresenter.");
			VERIFY_ARE_EQUAL("Flyout", spFlyout.GetClassName());
		});

		await FlyoutHelper.HideFlyout(flyout);
	}

	[TestMethod]
	[Description("Verify that a flyout with a ContentControl as its content still captures UIA focus.")]
	[Ignore("TODO Uno: needs a UIA client with focus-change events (AutomationFocusChangeHandler)")]
	public async Task VerifyUIAFocusEntersFlyoutWithContentControl()
	{
#if false // TODO Uno: needs a UIA client with focus-change events (AutomationClientManager / AutomationFocusChangeHandler)
		Button flyoutButton = null;

		var loadedEvent = new Event();
		var openedEvent = new Event();
		var closedEvent = new Event();

		var loadedRegistration = CreateSafeEventRegistration<Button, RoutedEventHandler>(nameof(Button.Loaded));
		var openedRegistration = CreateSafeEventRegistration<FlyoutBase, EventHandler<object>>(nameof(FlyoutBase.Opened));
		var closedRegistration = CreateSafeEventRegistration<FlyoutBase, EventHandler<object>>(nameof(FlyoutBase.Closed));

		var focusChangeEvent = new Event();
		AutomationFocusChangeHandler automationFocusChangeHandler;

		UIAElementInfo uiaInfo;
		uiaInfo.m_Name = "FlyoutButtonName";

		await RunOnUIThread(() =>
		{
			flyoutButton = (Button)(XamlReader.Load(
				"""
				<Button xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' AutomationProperties.Name='FlyoutButton' Content='Button'>
					<Button.Flyout>
						<Flyout ShouldConstrainToRootBounds='True'>
							<ContentControl>
								<TextBlock Text='Hello World'/>
							</ContentControl>
						</Flyout>
					</Button.Flyout>
				</Button>
				"""));

			loadedRegistration.Attach(flyoutButton, (s, e) => { loadedEvent.Set(); });
			openedRegistration.Attach(flyoutButton.Flyout, (s, e) => { openedEvent.Set(); });
			closedRegistration.Attach(flyoutButton.Flyout, (s, e) => { closedEvent.Set(); });
			TestServices.WindowHelper.WindowContent = flyoutButton;
		});

		await loadedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		UIAutomationHelper.RunOnCorrectThreadForUIA(() =>
		{
			var automationClientManager = AutomationClientManager.CreateAutomationClientManagerFromInfo(uiaInfo);
			automationFocusChangeHandler = new AutomationFocusChangeHandler(automationClientManager, focusChangeEvent, TreeScope_Subtree);
			automationFocusChangeHandler.Init();
		});

		await ControlHelper.EnsureFocused(flyoutButton);

		UIAutomationHelper.RunOnCorrectThreadForUIA(() =>
		{
			automationFocusChangeHandler.AttachEventHandler();
		});

		LOG_OUTPUT("Open the flyout with the keyboard.");
		await CommonInputHelper.Accept(InputDevice.Keyboard);

		await openedEvent.WaitForDefault();
		openedEvent.Reset();
		await TestServices.WindowHelper.WaitForIdle();

		UIAutomationHelper.RunOnCorrectThreadForUIA(() =>
		{
			automationFocusChangeHandler.Confirm();

			string focusedElementClassName = automationFocusChangeHandler.GetLastFocusedElement().CurrentClassName;

			LOG_OUTPUT("The currently focused element should be the popup now.");
			VERIFY_ARE_EQUAL("Popup", focusedElementClassName);
		});

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("Close the flyout.");
			flyoutButton.Flyout.Hide();
		});

		await closedEvent.WaitForDefault();
		closedEvent.Reset();
		await TestServices.WindowHelper.WaitForIdle();

		UIAutomationHelper.RunOnCorrectThreadForUIA(() =>
		{
			LOG_OUTPUT("Focus should have moved back to the button.");
			automationFocusChangeHandler.Confirm();

			string focusedElementClassName = automationFocusChangeHandler.GetLastFocusedElement().CurrentClassName;

			VERIFY_ARE_EQUAL("Button", focusedElementClassName);
		});

		LOG_OUTPUT("Open the flyout with the keyboard again.");
		await CommonInputHelper.Accept(InputDevice.Keyboard);

		await openedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		UIAutomationHelper.RunOnCorrectThreadForUIA(() =>
		{
			automationFocusChangeHandler.Confirm();

			string focusedElementClassName = automationFocusChangeHandler.GetLastFocusedElement().CurrentClassName;

			LOG_OUTPUT("The currently focused element should be the popup again.");
			VERIFY_ARE_EQUAL("Popup", focusedElementClassName);
		});

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("Close the flyout.");
			flyoutButton.Flyout.Hide();
		});

		await closedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		UIAutomationHelper.RunOnCorrectThreadForUIA(() =>
		{
			automationFocusChangeHandler.RemoveEventHandler();
		});
#endif
		await Task.CompletedTask;
	}

	[TestMethod]
	[Description("Validates the default IsDialog property from flyout dialog's popup is set to true")]
	public async Task DoesFlyoutSetIsDialogPropertyToTrueByDefault()
	{
		var flyout = await FlyoutHelper.CreateDefaultFlyout(FlyoutPlacementMode.Left);

		var target = await FlyoutHelper.CreateTarget(
			300 /*width*/, 300 /*height*/,
			new Thickness(100),
			HorizontalAlignment.Left,
			VerticalAlignment.Center);

		await RunOnUIThread(() =>
		{
			var rootPanel = new Grid();
			rootPanel.Children.Add(target);
			VERIFY_IS_NOT_NULL(rootPanel);
			flyout.ShouldConstrainToRootBounds = false;
			TestServices.WindowHelper.WindowContent = rootPanel;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await FlyoutHelper.OpenFlyout(flyout, target, FlyoutOpenMethod.Programmatic_ShowAt);

		await RunOnUIThread(() =>
		{
			var element = GetOpenPopupPeer(target);
			VERIFY_IS_NOT_NULL(element);
			VERIFY_ARE_EQUAL("Popup", element.GetName());

			VERIFY_IS_TRUE(element.IsDialog());
		});

		await FlyoutHelper.HideFlyout(flyout);
	}

	[TestMethod]
	[Description("Verify that a flyout with an Automation ID and name propagate them to the presenter.")]
	public async Task VerifyAutomationNameAndIdPropagateToPresenter()
	{
		string flyoutName = "FlyoutName";
		string flyoutId = "FlyoutId";

		var flyout = await FlyoutHelper.CreateDefaultFlyout(FlyoutPlacementMode.Left);

		var target = await FlyoutHelper.CreateTarget(
			300 /*width*/, 300 /*height*/,
			new Thickness(100),
			HorizontalAlignment.Left,
			VerticalAlignment.Center);

		await RunOnUIThread(() =>
		{
			AutomationProperties.SetName(flyout, flyoutName);
			AutomationProperties.SetAutomationId(flyout, flyoutId);

			var rootPanel = new Grid();
			rootPanel.Children.Add(target);
			VERIFY_IS_NOT_NULL(rootPanel);
			flyout.ShouldConstrainToRootBounds = false;
			TestServices.WindowHelper.WindowContent = rootPanel;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await FlyoutHelper.OpenFlyout(flyout, target, FlyoutOpenMethod.Programmatic_ShowAt);

		await RunOnUIThread(() =>
		{
			var element = FindPeerByName(GetOpenPopupPeer(target), flyoutName);
			VERIFY_IS_NOT_NULL(element);

			VERIFY_ARE_EQUAL(flyoutId, element.GetAutomationId());
		});

		await FlyoutHelper.HideFlyout(flyout);
	}

	[TestMethod]
	[Description("Verify that a flyout with an Automation ID and name does not propagate them to the presenter if they already have values.")]
	public async Task VerifyAutomationNameAndIdDoNotOverwritePresenterValues()
	{
		string flyoutName = "FlyoutName";
		string flyoutId = "FlyoutId";
		string flyoutPresenterName = "FlyoutPresenterName";
		string flyoutPresenterId = "FlyoutPresenterId";
		string styleXaml = "<Style" +
				"    xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'" +
				"    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'" +
				"    TargetType='FlyoutPresenter'" +
				"    BasedOn='{StaticResource DefaultFlyoutPresenterStyle}'>" +
				"    <Setter Property='AutomationProperties.AutomationId' Value='" + flyoutPresenterId + "' />" +
				"    <Setter Property='AutomationProperties.Name' Value='" + flyoutPresenterName + "' />" +
				"</Style>";

		var flyout = await FlyoutHelper.CreateDefaultFlyout(FlyoutPlacementMode.Left);

		await RunOnUIThread(() =>
		{
			var style = (Style)(XamlReader.Load(styleXaml));
			flyout.FlyoutPresenterStyle = style;
		});

		var target = await FlyoutHelper.CreateTarget(
			300 /*width*/, 300 /*height*/,
			new Thickness(100),
			HorizontalAlignment.Left,
			VerticalAlignment.Center);

		await RunOnUIThread(() =>
		{
			AutomationProperties.SetName(flyout, flyoutName);
			AutomationProperties.SetAutomationId(flyout, flyoutId);

			var rootPanel = new Grid();
			rootPanel.Children.Add(target);
			VERIFY_IS_NOT_NULL(rootPanel);
			flyout.ShouldConstrainToRootBounds = false;
			TestServices.WindowHelper.WindowContent = rootPanel;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await FlyoutHelper.OpenFlyout(flyout, target, FlyoutOpenMethod.Programmatic_ShowAt);

		await RunOnUIThread(() =>
		{
			var element = FindPeerByName(GetOpenPopupPeer(target), flyoutPresenterName);
			VERIFY_IS_NOT_NULL(element);

			VERIFY_ARE_EQUAL(flyoutPresenterId, element.GetAutomationId());
		});

		await FlyoutHelper.HideFlyout(flyout);
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
}
