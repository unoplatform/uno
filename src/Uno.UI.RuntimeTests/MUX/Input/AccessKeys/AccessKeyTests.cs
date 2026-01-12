#if HAS_UNO
using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using Windows.System;

namespace Uno.UI.RuntimeTests.MUX.Input.AccessKeys;

[TestClass]
[RunsOnUIThread]
public partial class AccessKeyTests : MUXApiTestBase
{
	[TestMethod]
	[DataRow(VirtualKey.A)]
	[DataRow(VirtualKey.B)]
	[DataRow(VirtualKey.Number1)]
	public async Task When_AccessKey_Invoked_Via_Alt_And_Key(VirtualKey key)
	{
		var invokedButton = string.Empty;

		var panel = new StackPanel();
		var buttonA = new Button { Content = "Button A", AccessKey = "A" };
		var buttonB = new Button { Content = "Button B", AccessKey = "B" };
		var button1 = new Button { Content = "Button 1", AccessKey = "1" };

		buttonA.Click += (s, e) => invokedButton = "A";
		buttonB.Click += (s, e) => invokedButton = "B";
		button1.Click += (s, e) => invokedButton = "1";

		panel.Children.Add(buttonA);
		panel.Children.Add(buttonB);
		panel.Children.Add(button1);

		TestServices.WindowHelper.WindowContent = panel;
		await TestServices.WindowHelper.WaitForLoaded(panel);
		await TestServices.WindowHelper.WaitForIdle();

		// Focus the first button
		buttonA.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		// Press Alt to enter access key mode
		await TestServices.KeyboardHelper.PressKeySequence("$d$_alt#$u$_alt");
		await TestServices.WindowHelper.WaitForIdle();

		// Verify we're in access key mode
		Assert.IsTrue(AccessKeyManager.IsDisplayModeEnabled, "Access key mode should be active after Alt press");

		// Press the access key character
		var keyChar = key switch
		{
			VirtualKey.A => "a",
			VirtualKey.B => "b",
			VirtualKey.Number1 => "1",
			_ => throw new ArgumentException()
		};
		await TestServices.KeyboardHelper.PressKeySequence($"$d$_{keyChar}#$u$_{keyChar}");
		await TestServices.WindowHelper.WaitForIdle();

		// Verify the correct button was invoked
		var expectedButton = key switch
		{
			VirtualKey.A => "A",
			VirtualKey.B => "B",
			VirtualKey.Number1 => "1",
			_ => throw new ArgumentException()
		};
		Assert.AreEqual(expectedButton, invokedButton, $"Button {expectedButton} should have been invoked");
	}

	[TestMethod]
	public async Task When_AccessKey_Mode_Entered_Events_Raised()
	{
		var displayRequestedCount = 0;
		var displayDismissedCount = 0;

		var button = new Button { Content = "Button A", AccessKey = "A" };
		button.AccessKeyDisplayRequested += (s, e) => displayRequestedCount++;
		button.AccessKeyDisplayDismissed += (s, e) => displayDismissedCount++;

		TestServices.WindowHelper.WindowContent = button;
		await TestServices.WindowHelper.WaitForLoaded(button);
		await TestServices.WindowHelper.WaitForIdle();

		button.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		// Press Alt to enter access key mode
		await TestServices.KeyboardHelper.PressKeySequence("$d$_alt#$u$_alt");
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(AccessKeyManager.IsDisplayModeEnabled, "Access key mode should be active");
		Assert.AreEqual(1, displayRequestedCount, "AccessKeyDisplayRequested should be raised once");

		// Press Escape to exit access key mode
		await TestServices.KeyboardHelper.Escape();
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsFalse(AccessKeyManager.IsDisplayModeEnabled, "Access key mode should be inactive after Escape");
		Assert.AreEqual(1, displayDismissedCount, "AccessKeyDisplayDismissed should be raised once");
	}

	[TestMethod]
	public async Task When_AccessKey_Invoked_Event_Raised()
	{
		var invokedEventRaised = false;

		var button = new Button { Content = "Button A", AccessKey = "A" };
		button.AccessKeyInvoked += (s, e) => invokedEventRaised = true;

		TestServices.WindowHelper.WindowContent = button;
		await TestServices.WindowHelper.WaitForLoaded(button);
		await TestServices.WindowHelper.WaitForIdle();

		button.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		// Press Alt+A to invoke
		await TestServices.KeyboardHelper.PressKeySequence("$d$_alt#$u$_alt");
		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.KeyboardHelper.PressKeySequence("$d$_a#$u$_a");
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(invokedEventRaised, "AccessKeyInvoked event should be raised");
	}

	[TestMethod]
	public async Task When_AccessKey_Mode_Exited_On_Escape()
	{
		var button = new Button { Content = "Button A", AccessKey = "A" };

		TestServices.WindowHelper.WindowContent = button;
		await TestServices.WindowHelper.WaitForLoaded(button);
		await TestServices.WindowHelper.WaitForIdle();

		button.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		// Enter access key mode
		await TestServices.KeyboardHelper.PressKeySequence("$d$_alt#$u$_alt");
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(AccessKeyManager.IsDisplayModeEnabled, "Access key mode should be active");

		// Exit with Escape
		await TestServices.KeyboardHelper.Escape();
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsFalse(AccessKeyManager.IsDisplayModeEnabled, "Access key mode should be inactive after Escape");
	}

	[TestMethod]
	public async Task When_No_AccessKeys_Alt_Does_Not_Enter_Mode()
	{
		// Button without access key
		var button = new Button { Content = "Button without AccessKey" };

		TestServices.WindowHelper.WindowContent = button;
		await TestServices.WindowHelper.WaitForLoaded(button);
		await TestServices.WindowHelper.WaitForIdle();

		button.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		// Press Alt - should not enter access key mode since no elements have access keys
		await TestServices.KeyboardHelper.PressKeySequence("$d$_alt#$u$_alt");
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsFalse(AccessKeyManager.IsDisplayModeEnabled, "Access key mode should not activate when no elements have access keys");
	}

	[TestMethod]
	public async Task When_Disabled_Button_AccessKey_Not_Invoked()
	{
		var invokedButton = string.Empty;

		var panel = new StackPanel();
		var buttonA = new Button { Content = "Button A", AccessKey = "A", IsEnabled = false };
		var buttonB = new Button { Content = "Button B", AccessKey = "B" };

		buttonA.Click += (s, e) => invokedButton = "A";
		buttonB.Click += (s, e) => invokedButton = "B";

		panel.Children.Add(buttonA);
		panel.Children.Add(buttonB);

		TestServices.WindowHelper.WindowContent = panel;
		await TestServices.WindowHelper.WaitForLoaded(panel);
		await TestServices.WindowHelper.WaitForIdle();

		buttonB.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		// Enter access key mode
		await TestServices.KeyboardHelper.PressKeySequence("$d$_alt#$u$_alt");
		await TestServices.WindowHelper.WaitForIdle();

		// Try to invoke disabled button A
		await TestServices.KeyboardHelper.PressKeySequence("$d$_a#$u$_a");
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreNotEqual("A", invokedButton, "Disabled button should not be invoked");
	}

	[TestMethod]
	public async Task When_Hidden_Button_AccessKey_Not_Invoked()
	{
		var invokedButton = string.Empty;

		var panel = new StackPanel();
		var buttonA = new Button { Content = "Button A", AccessKey = "A", Visibility = Visibility.Collapsed };
		var buttonB = new Button { Content = "Button B", AccessKey = "B" };

		buttonA.Click += (s, e) => invokedButton = "A";
		buttonB.Click += (s, e) => invokedButton = "B";

		panel.Children.Add(buttonA);
		panel.Children.Add(buttonB);

		TestServices.WindowHelper.WindowContent = panel;
		await TestServices.WindowHelper.WaitForLoaded(panel);
		await TestServices.WindowHelper.WaitForIdle();

		buttonB.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		// Enter access key mode
		await TestServices.KeyboardHelper.PressKeySequence("$d$_alt#$u$_alt");
		await TestServices.WindowHelper.WaitForIdle();

		// Try to invoke hidden button A
		await TestServices.KeyboardHelper.PressKeySequence("$d$_a#$u$_a");
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreNotEqual("A", invokedButton, "Hidden button should not be invoked");
	}

	[TestMethod]
	public async Task When_AreKeyTipsEnabled_False_Mode_Not_Entered()
	{
		var originalValue = AccessKeyManager.AreKeyTipsEnabled;
		try
		{
			AccessKeyManager.AreKeyTipsEnabled = false;

			var button = new Button { Content = "Button A", AccessKey = "A" };

			TestServices.WindowHelper.WindowContent = button;
			await TestServices.WindowHelper.WaitForLoaded(button);
			await TestServices.WindowHelper.WaitForIdle();

			button.Focus(FocusState.Programmatic);
			await TestServices.WindowHelper.WaitForIdle();

			// Press Alt - should not enter access key mode when AreKeyTipsEnabled is false
			await TestServices.KeyboardHelper.PressKeySequence("$d$_alt#$u$_alt");
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsFalse(AccessKeyManager.IsDisplayModeEnabled, "Access key mode should not activate when AreKeyTipsEnabled is false");
		}
		finally
		{
			AccessKeyManager.AreKeyTipsEnabled = originalValue;
		}
	}

	[TestMethod]
	public async Task When_ExitDisplayMode_Called_Mode_Exits()
	{
		var button = new Button { Content = "Button A", AccessKey = "A" };

		TestServices.WindowHelper.WindowContent = button;
		await TestServices.WindowHelper.WaitForLoaded(button);
		await TestServices.WindowHelper.WaitForIdle();

		button.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		// Enter access key mode
		await TestServices.KeyboardHelper.PressKeySequence("$d$_alt#$u$_alt");
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(AccessKeyManager.IsDisplayModeEnabled, "Access key mode should be active");

		// Call ExitDisplayMode
		AccessKeyManager.ExitDisplayMode();
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsFalse(AccessKeyManager.IsDisplayModeEnabled, "Access key mode should be inactive after ExitDisplayMode");
	}

	[TestMethod]
	public async Task When_Multiple_Buttons_Same_AccessKey_First_Invoked()
	{
		var invokedButton = string.Empty;

		var panel = new StackPanel();
		var buttonA1 = new Button { Content = "Button A1", AccessKey = "A" };
		var buttonA2 = new Button { Content = "Button A2", AccessKey = "A" };

		buttonA1.Click += (s, e) => invokedButton = "A1";
		buttonA2.Click += (s, e) => invokedButton = "A2";

		panel.Children.Add(buttonA1);
		panel.Children.Add(buttonA2);

		TestServices.WindowHelper.WindowContent = panel;
		await TestServices.WindowHelper.WaitForLoaded(panel);
		await TestServices.WindowHelper.WaitForIdle();

		buttonA1.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		// Enter access key mode and press A
		await TestServices.KeyboardHelper.PressKeySequence("$d$_alt#$u$_alt");
		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.KeyboardHelper.PressKeySequence("$d$_a#$u$_a");
		await TestServices.WindowHelper.WaitForIdle();

		// First button with matching access key should be invoked
		Assert.AreEqual("A1", invokedButton, "First button with access key A should be invoked");
	}

	[TestMethod]
	public async Task When_CaseInsensitive_AccessKey_Matches()
	{
		var invoked = false;

		var button = new Button { Content = "Button A", AccessKey = "A" };
		button.Click += (s, e) => invoked = true;

		TestServices.WindowHelper.WindowContent = button;
		await TestServices.WindowHelper.WaitForLoaded(button);
		await TestServices.WindowHelper.WaitForIdle();

		button.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		// Enter access key mode and press lowercase 'a'
		await TestServices.KeyboardHelper.PressKeySequence("$d$_alt#$u$_alt");
		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.KeyboardHelper.PressKeySequence("$d$_a#$u$_a");
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(invoked, "Access key should match case-insensitively");
	}
}
#endif
