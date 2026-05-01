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
}