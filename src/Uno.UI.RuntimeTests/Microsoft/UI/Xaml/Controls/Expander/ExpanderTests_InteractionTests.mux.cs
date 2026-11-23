// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference Expander/InteractionTests/ExpanderTests.cs

using System.Collections.Generic;
using System.Threading.Tasks;
using Common;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests;
using static Private.Infrastructure.TestServices;

namespace Microsoft.UI.Xaml.Tests.MUXControls.InteractionTests
{
	// Upstream drives the Expander test page through UI Automation. Here the relevant part of the page
	// is loaded directly and UIA lookups go through the automation peers. KeyboardHelper is a no-op
	// on WinAppSDK, so the tests skip NativeWinUI.
	// VerifyAxeScanPasses is not ported: Uno has no Axe infrastructure.
	[TestClass]
	[RunsOnUIThread]
	public class ExpanderInteractionTests
	{
		[TestCleanup]
		public void TestCleanup() => WindowHelper.WindowContent = null;

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task ExpandCollapseAutomationTests()
		{
			var root = await LoadTestPage();
			var expander = (Expander)root.FindName("ExpandedExpander");
			var expanderPeer = (ExpanderAutomationPeer)FrameworkElementAutomationPeer.CreatePeerForElement(expander);
			var header = GetHeader(expander);

			header.Focus(FocusState.Keyboard);
			await WindowHelper.WaitForIdle();

			Log.Comment("Collapse using keyboard space key.");
			await KeyboardHelper.Space();
			Verify.AreEqual(ExpandCollapseState.Collapsed, expanderPeer.ExpandCollapseState);

			Log.Comment("Expand using keyboard space key.");
			await KeyboardHelper.Space();
			Verify.AreEqual(ExpandCollapseState.Expanded, expanderPeer.ExpandCollapseState);

			Log.Comment("Collapse by clicking.");
			await Click(header);
			Verify.AreEqual(ExpandCollapseState.Collapsed, expanderPeer.ExpandCollapseState);

			Log.Comment("Expand by clicking.");
			await Click(header);
			Verify.AreEqual(ExpandCollapseState.Expanded, expanderPeer.ExpandCollapseState);

			var expandCollapse = (IExpandCollapseProvider)expanderPeer.GetPattern(PatternInterface.ExpandCollapse);

			Log.Comment("Collapse using UIA ExpandCollapse pattern");
			expandCollapse.Collapse();
			await WindowHelper.WaitForIdle();
			Verify.AreEqual(ExpandCollapseState.Collapsed, expanderPeer.ExpandCollapseState);

			Log.Comment("Expand using UIA ExpandCollapse pattern");
			expandCollapse.Expand();
			await WindowHelper.WaitForIdle();
			Verify.AreEqual(ExpandCollapseState.Expanded, expanderPeer.ExpandCollapseState);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task AutomationPeerTest()
		{
			var root = await LoadTestPage();

			// An control to take initial focus:
			var dummyButton = (Button)root.FindName("DummyButton");
			dummyButton.Focus(FocusState.Keyboard);
			await WindowHelper.WaitForIdle();

			var expander = (Expander)root.FindName("ExpanderWithToggleSwitch");
			var expanderPeer = (ExpanderAutomationPeer)FrameworkElementAutomationPeer.CreatePeerForElement(expander);
			Verify.IsTrue(expanderPeer.IsKeyboardFocusable(), "Expander should be Keyboard Focusable");
			Verify.IsFalse(expanderPeer.HasKeyboardFocus(), "Expander should initially be unfocused");

			// UIA SetFocus lands on the header toggle button, which carries the expander's keyboard focus.
			GetHeader(expander).Focus(FocusState.Keyboard);
			await WindowHelper.WaitForIdle();

			Verify.IsTrue(expanderPeer.HasKeyboardFocus(), "Expander should be focused");

			// Verify ExpandedExpander header content AutomationProperties.Name properties are set
			Verify.IsTrue(IsFound(expanderPeer, "This expander with ToggleSwitch is expanded by default."));
			Verify.IsTrue(IsFound(expanderPeer, "This is the second line of text."));
			Verify.IsTrue(IsFound(expanderPeer, "SettingsToggleSwitch"));

			// Verify ExpandedExpander content AutomationProperties.Name property is set
			Verify.IsTrue(IsFound(expanderPeer, "ExpanderWithToggleSwitch Content"));

			Log.Comment("Collapse using keyboard space key.");
			await KeyboardHelper.Space();
			Verify.AreEqual(ExpandCollapseState.Collapsed, expanderPeer.ExpandCollapseState);

			// Verify ExpandedExpander content AutomationProperties.Name property is not visible once collapsed
			// VerifyElement.NotFound retries while the collapse animation hides the content.
			await WindowHelper.WaitFor(() => !IsFound(expanderPeer, "ExpanderWithToggleSwitch Content"), timeoutMS: 3000, message: "Content should not be found once collapsed");
		}

		// The part of upstream TestUI/ExpanderPage.xaml the tests above use.
		private static async Task<StackPanel> LoadTestPage()
		{
			var root = (StackPanel)XamlReader.Load(
				"""
				<StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
							xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
							xmlns:controls="using:Microsoft.UI.Xaml.Controls"
							Orientation="Vertical" HorizontalAlignment="Left">
					<Button x:Name="DummyButton" Content="Dummy Button" AutomationProperties.Name="DummyButton" />

					<controls:Expander x:Name="ExpandedExpander" AutomationProperties.Name="ExpandedExpander" IsExpanded="True" Margin="12" HorizontalAlignment="Left">
						<controls:Expander.Header>
							<StackPanel Margin="0,14,0,16">
								<TextBlock Text="This expander is expanded by default." Margin="0,0,0,4"/>
								<TextBlock Text="This is the second line of text." />
							</StackPanel>
						</controls:Expander.Header>
						<Button AutomationProperties.AutomationId="ExpandedExpanderContent">Content</Button>
					</controls:Expander>

					<controls:Expander x:Name="ExpanderWithToggleSwitch" AutomationProperties.Name="ExpanderWithToggleSwitch" IsExpanded="True" Margin="12" HorizontalAlignment="Left">
						<controls:Expander.Header>
							<Grid>
								<Grid.ColumnDefinitions>
									<ColumnDefinition />
									<ColumnDefinition Width="80"/>
								</Grid.ColumnDefinitions>
								<StackPanel Margin="0,14,24,16">
									<TextBlock Text="This expander with ToggleSwitch is expanded by default." Margin="0,0,0,4"/>
									<TextBlock Text="This is the second line of text." />
								</StackPanel>
								<ToggleSwitch AutomationProperties.Name="SettingsToggleSwitch" Grid.Column="1"/>
							</Grid>
						</controls:Expander.Header>
						<Button AutomationProperties.AutomationId="ExpandedExpanderContent">ExpanderWithToggleSwitch Content</Button>
					</controls:Expander>
				</StackPanel>
				""");

			WindowHelper.WindowContent = root;
			await WindowHelper.WaitForLoaded(root);
			await WindowHelper.WaitForIdle();
			return root;
		}

		private static ToggleButton GetHeader(DependencyObject parent)
		{
			for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
			{
				var child = VisualTreeHelper.GetChild(parent, i);
				var header = child is ToggleButton { Name: "ExpanderHeader" } match ? match : GetHeader(child);
				if (header is not null)
				{
					return header;
				}
			}

			return null;
		}

		private static async Task Click(ToggleButton header)
		{
#if HAS_INPUT_INJECTOR
			InputHelper.LeftMouseClick(header);
#else
			((IToggleProvider)FrameworkElementAutomationPeer.CreatePeerForElement(header)).Toggle();
#endif
			await WindowHelper.WaitForIdle();
		}

		// Stands in for UIA's FindBy.Name: searches the automation tree under the peer.
		private static bool IsFound(AutomationPeer peer, string name)
		{
			var stack = new Stack<AutomationPeer>();
			stack.Push(peer);
			while (stack.Count > 0)
			{
				var current = stack.Pop();
				if (current.GetName() == name)
				{
					return true;
				}

				if (current.GetChildren() is { } children)
				{
					foreach (var child in children)
					{
						stack.Push(child);
					}
				}
			}

			return false;
		}
	}
}
