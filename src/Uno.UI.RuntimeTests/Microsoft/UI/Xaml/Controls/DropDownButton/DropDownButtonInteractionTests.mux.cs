// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference InteractionTests/DropDownButtonTests.cs

using System.Threading.Tasks;
using Common;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.MUX.Microsoft_UI_Xaml_Controls;

// Upstream drives the DropDownButton test page through UI Automation. Here the page's button and
// flyout counters are built in code, and the peer's IExpandCollapseProvider stands in for the UIA pattern.
[TestClass]
[RunsOnUIThread]
public class DropDownButtonInteractionTests
{
	private int _flyoutOpenedCount;
	private int _flyoutClosedCount;

	[TestMethod]
	public async Task AccessibilityTest()
	{
		var testFlyout = CreateFlyout("Hello");
		var dropDownButton = new DropDownButton { Content = "Test Button", Flyout = testFlyout };
		await UITestHelper.Load(dropDownButton);
		var expandCollapse = (IExpandCollapseProvider)FrameworkElementAutomationPeer.CreatePeerForElement(dropDownButton);

		try
		{
			// Verify that DropDownButton implements IExpandCollapseProvider
			Verify.AreEqual(0, _flyoutOpenedCount);
			// IExpandCollapseProvider.Expand
			await ExpandAndWait(expandCollapse, expectedOpenedCount: 1);
			Verify.IsTrue(testFlyout.IsOpen);
			Verify.AreEqual(ExpandCollapseState.Expanded, expandCollapse.ExpandCollapseState);

			Verify.AreEqual(0, _flyoutClosedCount);
			// IExpandCollapseProvider.Collapse
			await CollapseAndWait(expandCollapse, expectedClosedCount: 1);
			Verify.AreEqual(ExpandCollapseState.Collapsed, expandCollapse.ExpandCollapseState);

			// Change to a different flyout
			dropDownButton.Flyout = CreateFlyout("New Flyout");
			await WindowHelper.WaitForIdle();

			Verify.AreEqual(1, _flyoutOpenedCount);
			// IExpandCollapseProvider.Expand
			await ExpandAndWait(expandCollapse, expectedOpenedCount: 2);
			Verify.AreEqual(ExpandCollapseState.Expanded, expandCollapse.ExpandCollapseState);

			Verify.AreEqual(1, _flyoutClosedCount);
			// IExpandCollapseProvider.Collapse
			await CollapseAndWait(expandCollapse, expectedClosedCount: 2);
			Verify.AreEqual(ExpandCollapseState.Collapsed, expandCollapse.ExpandCollapseState);
		}
		finally
		{
			dropDownButton.Flyout?.Hide();
			await WindowHelper.WaitForIdle();
		}
	}

	private Flyout CreateFlyout(string text)
	{
		var flyout = new Flyout { Placement = FlyoutPlacementMode.Bottom, Content = new TextBlock { Text = text } };
		flyout.Opened += (_, _) => _flyoutOpenedCount++;
		flyout.Closed += (_, _) => _flyoutClosedCount++;
		return flyout;
	}

	private async Task ExpandAndWait(IExpandCollapseProvider expandCollapse, int expectedOpenedCount)
	{
		expandCollapse.Expand();
		await WindowHelper.WaitFor(() => _flyoutOpenedCount == expectedOpenedCount);
		await WindowHelper.WaitForIdle();
	}

	private async Task CollapseAndWait(IExpandCollapseProvider expandCollapse, int expectedClosedCount)
	{
		expandCollapse.Collapse();
		await WindowHelper.WaitFor(() => _flyoutClosedCount == expectedClosedCount);
		await WindowHelper.WaitForIdle();
	}
}
