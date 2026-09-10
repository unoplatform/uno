#nullable enable

using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

[TestClass]
public class Given_AccessibilityFocus
{
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_MoveFocus_Next_Then_Focus_Moves_Sequentially()
	{
		var first = new Button { Content = "First" };
		var second = new Button { Content = "Second" };
		var third = new Button { Content = "Third" };
		var panel = new StackPanel { Children = { first, second, third } };
		await UITestHelper.Load(panel);

		Assert.IsTrue(first.Focus(FocusState.Programmatic));
		var moved = FocusManager.TryMoveFocus(
			FocusNavigationDirection.Next,
			new FindNextElementOptions { SearchRoot = TestServices.WindowHelper.XamlRoot.Content });

		Assert.IsTrue(moved);
		Assert.AreSame(second, FocusManager.GetFocusedElement(TestServices.WindowHelper.XamlRoot));
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Dialog_Content_Is_Shown_Then_It_Can_Receive_Focus()
	{
		var opener = new Button { Content = "Open Dialog" };
		var dialogContent = new Button
		{
			Content = "Dialog Button",
			Visibility = Visibility.Collapsed,
		};
		var panel = new StackPanel { Children = { opener, dialogContent } };
		await UITestHelper.Load(panel);

		Assert.IsTrue(opener.Focus(FocusState.Programmatic));
		dialogContent.Visibility = Visibility.Visible;
		Assert.IsTrue(dialogContent.Focus(FocusState.Programmatic));
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreSame(dialogContent, FocusManager.GetFocusedElement(TestServices.WindowHelper.XamlRoot));
		Assert.IsTrue(FrameworkElementAutomationPeer.CreatePeerForElement(dialogContent).IsKeyboardFocusable());
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Focused_Element_Is_Disabled_Then_Peer_State_Is_Updated()
	{
		var button = new Button { Content = "Will Disable" };
		await UITestHelper.Load(button);
		Assert.IsTrue(button.Focus(FocusState.Programmatic));

		button.IsEnabled = false;
		await TestServices.WindowHelper.WaitForIdle();

		var peer = FrameworkElementAutomationPeer.CreatePeerForElement(button);
		Assert.IsNotNull(peer);
		Assert.IsFalse(peer.IsEnabled());
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Element_Is_Not_Focusable_Then_Peer_Reports_Not_Focusable()
	{
		var textBlock = new TextBlock { Text = "Not focusable" };
		await UITestHelper.Load(textBlock);

		var peer = FrameworkElementAutomationPeer.CreatePeerForElement(textBlock);

		Assert.IsNotNull(peer);
		Assert.IsFalse(peer.IsKeyboardFocusable());
	}
}
