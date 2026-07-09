#if HAS_UNO
#nullable enable
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Input;

[TestClass]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
public class Given_BrowserKeyboardInputSource
{
	// Verifies the full Web Skia keyboard ingress: OnNativeKeyboardEvent
	// (the [JSExport] entry-point JS calls into) raises KeyDown with the
	// VirtualKey produced by BrowserVirtualKeyHelper.FromCode. Regression
	// guard for OEM-row keys mapping to VirtualKey.None.
	[TestMethod]
	[DataRow("Equal", 0xBB)]
	[DataRow("Minus", 0xBD)]
	[DataRow("Comma", 0xBC)]
	[DataRow("Period", 0xBE)]
	[DataRow("Semicolon", 0xBA)]
	[DataRow("Quote", 0xDE)]
	[DataRow("BracketLeft", 0xDB)]
	[DataRow("BracketRight", 0xDD)]
	[DataRow("Backslash", 0xDC)]
	[DataRow("Backquote", 0xC0)]
	[DataRow("Slash", 0xBF)]
	[DataRow("KeyA", (int)VirtualKey.A)]
	[DataRow("Digit0", (int)VirtualKey.Number0)]
	[RunsOnUIThread]
	public void When_OnNativeKeyboardEvent_KeyDown_Has_Expected_VirtualKey(string code, int expectedVirtualKey)
	{
		var sourceType = Type.GetType("Uno.UI.Runtime.Skia.BrowserKeyboardInputSource, Uno.UI.Runtime.Skia.WebAssembly.Browser", throwOnError: false);
		Assert.IsNotNull(sourceType, "BrowserKeyboardInputSource type was not found in Uno.UI.Runtime.Skia.WebAssembly.Browser.");

		// GetUninitializedObject skips the ctor (which would re-attach JS DOM
		// listeners and pollute later tests). The auto-implemented KeyDown
		// event delegate field is still nullable-default, which is what we want.
		var instance = RuntimeHelpers.GetUninitializedObject(sourceType);

		KeyEventArgs? captured = null;
		var keyDownEvent = sourceType.GetEvent("KeyDown", BindingFlags.Public | BindingFlags.Instance);
		Assert.IsNotNull(keyDownEvent, "KeyDown event not found.");
		var handler = new TypedEventHandler<object, KeyEventArgs>((_, args) => captured = args);
		keyDownEvent.AddEventHandler(instance, handler);

		var onNative = sourceType.GetMethod("OnNativeKeyboardEvent", BindingFlags.NonPublic | BindingFlags.Static);
		Assert.IsNotNull(onNative, "OnNativeKeyboardEvent JSExport method not found.");

		// Signature: (object inputSource, bool down, bool ctrl, bool shift, bool alt, bool meta, string code, string key)
		onNative.Invoke(null, new object[] { instance, true, false, false, false, false, code, string.Empty });

		Assert.IsNotNull(captured, "KeyDown event was not raised.");
		Assert.AreEqual(expectedVirtualKey, (int)captured!.VirtualKey,
			$"event.code='{code}' should map to VirtualKey 0x{expectedVirtualKey:X2}, got 0x{(int)captured.VirtualKey:X2}.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_OnNativeKeyboardEvent_ShiftEqual_Modifiers_Flow_Through()
	{
		var sourceType = Type.GetType("Uno.UI.Runtime.Skia.BrowserKeyboardInputSource, Uno.UI.Runtime.Skia.WebAssembly.Browser", throwOnError: false);
		Assert.IsNotNull(sourceType);

		var instance = RuntimeHelpers.GetUninitializedObject(sourceType);

		KeyEventArgs? captured = null;
		var keyDownEvent = sourceType.GetEvent("KeyDown", BindingFlags.Public | BindingFlags.Instance);
		var handler = new TypedEventHandler<object, KeyEventArgs>((_, args) => captured = args);
		keyDownEvent!.AddEventHandler(instance, handler);

		var onNative = sourceType.GetMethod("OnNativeKeyboardEvent", BindingFlags.NonPublic | BindingFlags.Static);
		// Shift+Equal — code is shift-independent (still "Equal"), modifier should propagate.
		onNative!.Invoke(null, new object[] { instance, true, false, true, false, false, "Equal", "+" });

		Assert.IsNotNull(captured);
		Assert.AreEqual(0xBB, (int)captured!.VirtualKey);
		Assert.IsTrue((captured.KeyboardModifiers & VirtualKeyModifiers.Shift) == VirtualKeyModifiers.Shift,
			"Shift modifier should be set on KeyEventArgs.");
	}

	// Regression guard for Shift+Tab focus routing while the "Enable Accessibility"
	// button is present. Exercises the pure decision the TS host applies (extracted
	// as BrowserKeyboardInputSource.shouldLetBrowserHandleTabKeydown) rather than a
	// flaky browser focus-escape assertion. The key case: once focus has entered the
	// app, Shift+Tab on the button must route to the managed FocusManager (return
	// false) instead of exiting to the browser.
	[TestMethod]
	[RunsOnUIThread]
	public void When_ShiftTab_On_AccessibilityButton_Routing_Respects_App_Entry()
	{
		static string B(bool value) => value ? "true" : "false";
		static bool LetBrowserHandle(bool shift, bool onButton, bool unfocused, bool entered)
		{
			var result = WasmSemanticDomHelper.InvokeBrowserJs(
				$"String(Uno.UI.Runtime.Skia.BrowserKeyboardInputSource.shouldLetBrowserHandleTabKeydown({B(shift)}, {B(onButton)}, {B(unfocused)}, {B(entered)}))");
			Assert.IsTrue(result is "true" or "false",
				$"shouldLetBrowserHandleTabKeydown was not reachable/returned unexpected value: '{result}'.");
			return result == "true";
		}

		// Nothing focused yet — always let the browser Tab onto the prepended button.
		Assert.IsTrue(LetBrowserHandle(shift: false, onButton: false, unfocused: true, entered: false));
		Assert.IsTrue(LetBrowserHandle(shift: true, onButton: false, unfocused: true, entered: false));

		// Button focused + Shift+Tab BEFORE entering the app — exit to the browser.
		Assert.IsTrue(LetBrowserHandle(shift: true, onButton: true, unfocused: false, entered: false));

		// Button focused + Shift+Tab AFTER entering the app — route to managed (regression).
		Assert.IsFalse(LetBrowserHandle(shift: true, onButton: true, unfocused: false, entered: true));

		// Button focused + Tab (no shift) — always route to managed.
		Assert.IsFalse(LetBrowserHandle(shift: false, onButton: true, unfocused: false, entered: false));
		Assert.IsFalse(LetBrowserHandle(shift: false, onButton: true, unfocused: false, entered: true));
	}
}
#endif
