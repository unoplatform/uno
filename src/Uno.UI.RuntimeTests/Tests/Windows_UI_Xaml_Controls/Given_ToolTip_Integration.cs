// MUX Reference dxaml\test\native\external\controls\tooltip\ToolTipIntegrationTests.cpp, tag 5f9e85113
// Faithful port of Microsoft.UI.Xaml.Tests.Controls.ToolTip.ToolTipIntegrationTests.

using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_ToolTip_Integration
{
	// Helpers ported from ToolTipIntegrationTests.cpp lines 426-535:
	// SetupToolTipTest (line 426), CreateToolTip (line 518), CreateLongToolTip (line 537).
	// Each helper wraps the C++ RunOnUIThread + WaitForIdle pattern with the Uno
	// equivalents (TestServices.WindowHelper.WindowContent + WaitForIdle).

	private static async Task<Button> SetupToolTipTest()
	{
		var rootPanel = new Grid();
		var border = new Border
		{
			Width = 400,
			Height = 250,
		};

		var button = new Button
		{
			Content = "Button.ToolTip",
			HorizontalAlignment = HorizontalAlignment.Center,
		};

		border.Child = button;
		rootPanel.Children.Add(border);

		TestServices.WindowHelper.WindowContent = rootPanel;
		await TestServices.WindowHelper.WaitForLoaded(rootPanel);
		await TestServices.WindowHelper.WaitForIdle();

		return button;
	}

	private static ToolTip CreateToolTip()
	{
		var textBlock = new TextBlock { Text = "look...  its a tooltip" };
		return new ToolTip { Content = textBlock };
	}

	private static ToolTip CreateLongToolTip()
	{
		var textBlock = new TextBlock { Text = "look...  its a tooltip! look...  its a tooltip!" };
		return new ToolTip { Content = textBlock };
	}

	// MUX Reference: ToolTipIntegrationTests.cpp CanInstantiate (line 49).
	// "Validates that we can successfully create a ToolTip."
	[TestMethod]
	public void CanInstantiate()
	{
		var toolTip = new ToolTip();
		Assert.IsNotNull(toolTip);
	}

	// MUX Reference: ToolTipIntegrationTests.cpp CanEnterAndLeaveLiveTree (line 54).
	// "Validates that we can successfully add/remove a ToolTip from the live tree."
	[TestMethod]
	public async Task CanEnterAndLeaveLiveTree()
	{
		var toolTip = new ToolTip { Content = "tooltip text" };

		TestServices.WindowHelper.WindowContent = toolTip;
		await TestServices.WindowHelper.WaitForLoaded(toolTip);
		Assert.IsTrue(toolTip.IsLoaded);

		TestServices.WindowHelper.WindowContent = null;
		await TestServices.WindowHelper.WaitForIdle();
		Assert.IsFalse(toolTip.IsLoaded);
	}

	// MUX Reference: ToolTipIntegrationTests.cpp CanSetToolTipBeforeAndAfterManagedPeerCreation (line 59).
	// "Validates that no crash occurs when attempting to set a tool tip both before and after this
	//  element's managed peer is created."
	// Regression: WINTH:870783
	[TestMethod]
	public async Task CanSetToolTipBeforeAndAfterManagedPeerCreation()
	{
		var grid = (Grid)XamlReader.Load(
			"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' ToolTipService.ToolTip='Tool tip!' />");

		TestServices.WindowHelper.WindowContent = grid;
		await TestServices.WindowHelper.WaitForIdle();

		var s = "New tool tip!";
		ToolTipService.SetToolTip(grid, s);
		var setString = ToolTipService.GetToolTip(grid) as string;

		Assert.IsNotNull(setString);
		Assert.AreEqual(s, setString);
	}

	// MUX Reference: ToolTipIntegrationTests.cpp CanSetToolTipOnNonStatefulObjects (line 1306-area in cpp).
	// "Verify we can set ToolTips for objects that normally don't have any peer state."
	// Regression: WINTH:2839403
	[TestMethod]
	public void CanSetToolTipOnNonStatefulObjects()
	{
		// The C++ test sets a tooltip on a fresh DependencyObject without putting it in the tree
		// to verify no crash. Uno's attached-property machinery accepts any DependencyObject.
		var dummy = new ContentControl();
		ToolTipService.SetToolTip(dummy, "Tool tip on non-stateful object");
		var retrieved = ToolTipService.GetToolTip(dummy) as string;
		Assert.AreEqual("Tool tip on non-stateful object", retrieved);
	}

	// MUX Reference: ToolTipIntegrationTests.cpp CanEnableAndDisableOpenedToolTip (line 1330).
	// "Verify that enabling and disabling of an opened ToolTip makes it appear and disappear
	//  respectively."
	[TestMethod]
	public async Task CanEnableAndDisableOpenedToolTip()
	{
		var button = await SetupToolTipTest();
		var toolTip = CreateToolTip();
		ToolTipService.SetToolTip(button, toolTip);
		await TestServices.WindowHelper.WaitForIdle();

		toolTip.IsOpen = true;
		await TestServices.WindowHelper.WaitForIdle();

		// ToolTip is enabled by default.
		Assert.IsTrue(toolTip.IsOpen);
		Assert.IsTrue(toolTip.IsEnabled);
		var popups = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopupsForXamlRoot(
			TestServices.WindowHelper.WindowContent.XamlRoot);
		Assert.AreEqual(1, popups.Count);
		var toolTipParentPopup = popups[0];
		Assert.AreEqual(1, toolTipParentPopup.Opacity);

		toolTip.IsEnabled = false;
		await TestServices.WindowHelper.WaitForIdle();

		// Disabling the ToolTip causes the ToolTip to disappear.
		Assert.IsTrue(toolTip.IsOpen);
		Assert.IsFalse(toolTip.IsEnabled);
		popups = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopupsForXamlRoot(
			TestServices.WindowHelper.WindowContent.XamlRoot);
		Assert.AreEqual(1, popups.Count);
		toolTipParentPopup = popups[0];
		Assert.AreEqual(0, toolTipParentPopup.Opacity);

		// TODO: Bug ID 5254453 - Figure out why we can't enable an opened ToolTip.
		// (C++ block disabled with /* */; preserved here as a reminder.)

		// Cleanup
		toolTip.IsOpen = false;
		await TestServices.WindowHelper.WaitForIdle();
	}

	// MUX Reference: ToolTipIntegrationTests.cpp CanOpenAndCloseDisabledToolTip (line 1398).
	// "Verify that opening and closing of a disabled ToolTip does not make it appear."
	[TestMethod]
	public async Task CanOpenAndCloseDisabledToolTip()
	{
		var button = await SetupToolTipTest();
		var toolTip = CreateToolTip();
		ToolTipService.SetToolTip(button, toolTip);
		toolTip.IsEnabled = false;
		await TestServices.WindowHelper.WaitForIdle();

		// ToolTip is closed by default.
		Assert.IsFalse(toolTip.IsOpen);
		Assert.IsFalse(toolTip.IsEnabled);
		var popups = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopupsForXamlRoot(
			TestServices.WindowHelper.WindowContent.XamlRoot);
		Assert.AreEqual(0, popups.Count);

		toolTip.IsOpen = true;
		await TestServices.WindowHelper.WaitForIdle();

		// Setting IsOpen to true on a disabled ToolTip does not make it appear.
		Assert.IsTrue(toolTip.IsOpen);
		Assert.IsFalse(toolTip.IsEnabled);
		popups = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopupsForXamlRoot(
			TestServices.WindowHelper.WindowContent.XamlRoot);
		Assert.AreEqual(1, popups.Count);
		var toolTipParentPopup = popups[0];
		Assert.AreEqual(0, toolTipParentPopup.Opacity);

		toolTip.IsOpen = false;
		await TestServices.WindowHelper.WaitForIdle();

		// Setting IsOpen to false, closes the parent Popup.
		Assert.IsFalse(toolTip.IsOpen);
		Assert.IsFalse(toolTip.IsEnabled);
		popups = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopupsForXamlRoot(
			TestServices.WindowHelper.WindowContent.XamlRoot);
		Assert.AreEqual(0, popups.Count);
	}

	// MUX Reference: ToolTipIntegrationTests.cpp CloseToolTipBeforeItIsFullyOpen (line 1298).
	// Regression: MSFT:3417091 "Excel: Font and Fill color ToolTip sometimes gets stuck in Open state".
	// Prior to the bug fix, it was possible to get ToolTip into a state where its Popup remained
	// Open even when the ToolTip was in the Closed state. This could happen by setting the IsOpen
	// property back to false before it had time to fully complete opening.
	[TestMethod]
	public async Task CloseToolTipBeforeItIsFullyOpen()
	{
		var button = await SetupToolTipTest();
		var toolTip = CreateToolTip();
		ToolTipService.SetToolTip(button, toolTip);
		await TestServices.WindowHelper.WaitForIdle();

		toolTip.IsOpen = true;
		toolTip.IsOpen = false;
		await TestServices.WindowHelper.WaitForIdle();

		// We expect there to be no open popups on the screen.
		var currentlyOpenPopups = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopupsForXamlRoot(
			TestServices.WindowHelper.WindowContent.XamlRoot);
		Assert.AreEqual(0, currentlyOpenPopups.Count);
	}
}