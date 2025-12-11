using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Private.Infrastructure;
using Windows.System;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Input;

[TestClass]
[RunsOnUIThread]
public class Given_KeyRoutedEventArgs
{
	[TestMethod]
#if !__WASM__ && !__SKIA__
	[Ignore("This test is specific to WASM and Skia platforms where BrowserVirtualKeyHelper is used")]
#endif
	public async Task When_Control_Key_Pressed()
	{
		var button = new Button { Content = "Test Button" };
		VirtualKey? receivedKey = null;
		var keyDownFired = false;

		button.KeyDown += (s, e) =>
		{
			receivedKey = e.Key;
			keyDownFired = true;
		};

		await UITestHelper.Load(button);
		button.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		// Simulate Control key press using TestServices.KeyboardHelper
		await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrl#$u$_ctrl", button);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(keyDownFired, "KeyDown event should have been fired");
		Assert.AreEqual(VirtualKey.Control, receivedKey, "Key should be Control, not None");
	}

	[TestMethod]
#if !__WASM__ && !__SKIA__
	[Ignore("This test is specific to WASM and Skia platforms where BrowserVirtualKeyHelper is used")]
#endif
	public async Task When_Shift_Key_Pressed()
	{
		var button = new Button { Content = "Test Button" };
		VirtualKey? receivedKey = null;
		var keyDownFired = false;

		button.KeyDown += (s, e) =>
		{
			receivedKey = e.Key;
			keyDownFired = true;
		};

		await UITestHelper.Load(button);
		button.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		// Simulate Shift key press using TestServices.KeyboardHelper
		await TestServices.KeyboardHelper.PressKeySequence("$d$_shift#$u$_shift", button);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(keyDownFired, "KeyDown event should have been fired");
		Assert.AreEqual(VirtualKey.Shift, receivedKey, "Key should be Shift");
	}

	[TestMethod]
#if !__WASM__ && !__SKIA__
	[Ignore("This test is specific to WASM and Skia platforms where BrowserVirtualKeyHelper is used")]
#endif
	public async Task When_Alt_Key_Pressed()
	{
		var button = new Button { Content = "Test Button" };
		VirtualKey? receivedKey = null;
		var keyDownFired = false;

		button.KeyDown += (s, e) =>
		{
			receivedKey = e.Key;
			keyDownFired = true;
		};

		await UITestHelper.Load(button);
		button.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		// Simulate Alt key press using TestServices.KeyboardHelper
		await TestServices.KeyboardHelper.PressKeySequence("$d$_alt#$u$_alt", button);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(keyDownFired, "KeyDown event should have been fired");
		Assert.AreEqual(VirtualKey.Menu, receivedKey, "Key should be Menu (Alt)");
	}

	[TestMethod]
#if !__WASM__ && !__SKIA__
	[Ignore("This test is specific to WASM and Skia platforms where BrowserVirtualKeyHelper is used")]
#endif
	public async Task When_Ctrl_Plus_A_Pressed()
	{
		var button = new Button { Content = "Test Button" };
		VirtualKey? receivedKey = null;
		VirtualKeyModifiers? receivedModifiers = null;
		var keyDownCount = 0;

		button.KeyDown += (s, e) =>
		{
			// Store the last key event (which should be 'A' with Ctrl modifier)
			receivedKey = e.Key;
			receivedModifiers = e.KeyModifiers;
			keyDownCount++;
		};

		await UITestHelper.Load(button);
		button.Focus(FocusState.Programmatic);
		await TestServices.WindowHelper.WaitForIdle();

		// Simulate Ctrl+A key press
		await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrl#$d$_a#$u$_a#$u$_ctrl", button);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(keyDownCount >= 2, "KeyDown event should have been fired at least twice (Ctrl and A)");
		Assert.AreEqual(VirtualKey.A, receivedKey, "Last key should be A");
		Assert.IsTrue(receivedModifiers.HasValue && (receivedModifiers.Value & VirtualKeyModifiers.Control) != 0, 
			"Control modifier should be present");
	}
}
