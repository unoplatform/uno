#if HAS_UNO
#nullable enable
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
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
		var sourceType = AppDomain.CurrentDomain
			.GetAssemblies()
			.FirstOrDefault(a => a.GetName().Name == "Uno.UI.Runtime.Skia.WebAssembly.Browser")
			?.GetType("Uno.UI.Runtime.Skia.BrowserKeyboardInputSource");

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
		var sourceType = AppDomain.CurrentDomain
			.GetAssemblies()
			.FirstOrDefault(a => a.GetName().Name == "Uno.UI.Runtime.Skia.WebAssembly.Browser")
			?.GetType("Uno.UI.Runtime.Skia.BrowserKeyboardInputSource");
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
}
#endif
