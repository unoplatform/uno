#if __SKIA__
using System;
using System.Threading.Tasks;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using Uno.Disposables;
using Uno.Extensions;
using Uno.UI.DevTools.Input;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI.Input.Preview.Injection;
using static Private.Infrastructure.TestServices;
using static Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

/// <summary>
/// By default the soft keyboard of iOS only comes up for an input that is focused while the user is interacting
/// with the page. These tests make the hidden native input's bridge take the browser for an iOS one.
/// </summary>
public partial class Given_TextBox
{
	private const string HiddenInputBridge = "globalThis.Uno.UI.Runtime.BrowserInvisibleTextBoxViewExtension";

	// Past the delay within which the gesture recognizer takes a second tap for a double tap (see Tap).
	private static readonly TimeSpan MultiTapDelay = TimeSpan.FromMicroseconds(GestureRecognizer.MultiTapMaxDelayMicroseconds) + TimeSpan.FromMilliseconds(100);

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25183")]
	public async Task When_Focused_TextBox_Tapped_On_iOS_Then_Input_Focused_Again()
	{
		using var content = UITestHelper.ResetWindowContent();
		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		var SUT = new TextBox();
		await UITestHelper.Load(SUT);

		using var platform = SimulateIOS(true);

		// Focused from code, the input has no soft keyboard on iOS.
		await FocusFromCode(SUT);
		CountHiddenInputFocusCalls();

		// The tap finds the TextBox focused already. The input is focused again for it, and not blurred.
		Tap(injector, SUT);
		await WindowHelper.WaitForIdle();

		var (blurs, focuses) = GetHiddenInputFocusCalls();
		Assert.AreEqual(0, blurs);
		Assert.IsTrue(focuses > 0, "The tap should focus the hidden input again.");
		Assert.IsTrue(IsHiddenInputFocused());
		Assert.AreEqual(FocusState.Pointer, SUT.FocusState);

		// So is it for any later tap, which no change of focus state comes with, and the trailing-click guard
		// is armed for it.
		await Task.Delay(MultiTapDelay);
		CountHiddenInputFocusCalls();
		DisarmTrailingClickGuard();
		Tap(injector, SUT);
		await WindowHelper.WaitForIdle();

		(blurs, focuses) = GetHiddenInputFocusCalls();
		Assert.AreEqual(0, blurs);
		Assert.AreEqual(1, focuses);
		Assert.IsTrue(IsTrailingClickGuardArmed());
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25183")]
	public async Task When_Focused_TextBox_Tapped_Off_iOS_Then_Input_Left_Alone()
	{
		using var content = UITestHelper.ResetWindowContent();
		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		var SUT = new TextBox();
		await UITestHelper.Load(SUT);

		using var platform = SimulateIOS(false);

		// Other browsers show the keyboard for a focus from code as they see fit.
		await FocusFromCode(SUT);
		CountHiddenInputFocusCalls();
		DisarmTrailingClickGuard();

		Tap(injector, SUT);
		await WindowHelper.WaitForIdle();
		await Task.Delay(MultiTapDelay);
		Tap(injector, SUT);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual((0, 0), GetHiddenInputFocusCalls());
		Assert.IsTrue(IsHiddenInputFocused());
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25183")]
	public async Task When_Another_TextBox_Tapped_On_iOS_Then_Input_Focused_Again()
	{
		using var content = UITestHelper.ResetWindowContent();
		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		var first = new TextBox();
		var SUT = new TextBox();
		await UITestHelper.Load(new StackPanel { Children = { first, SUT } });

		using var platform = SimulateIOS(true);

		// The input is handed over from one TextBox to the other without losing focus, so the tap that moves
		// focus has to focus it again as well.
		await FocusFromCode(first);
		CountHiddenInputFocusCalls();

		Tap(injector, SUT);
		await WindowHelper.WaitForIdle();

		var (blurs, focuses) = GetHiddenInputFocusCalls();
		Assert.AreEqual(FocusState.Pointer, SUT.FocusState);
		Assert.AreEqual(0, blurs);
		Assert.IsTrue(focuses > 0, "The tap should focus the hidden input again.");
		Assert.IsTrue(IsHiddenInputFocused());
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25183")]
	public async Task When_Focused_TextBox_Clicked_With_Mouse_On_iOS_Then_Next_Click_Left_Alone()
	{
		using var content = UITestHelper.ResetWindowContent();
		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var mouse = injector.GetMouse();
		var SUT = new TextBox();
		await UITestHelper.Load(SUT);

		using var platform = SimulateIOS(true, pointerType: "mouse");

		await FocusFromCode(SUT);
		CountHiddenInputFocusCalls();
		DisarmTrailingClickGuard();

		// A mouse, as an iPad can have. The input is focused again for its click as well, but no mousedown trails
		// a mouse's own: armed, the guard would swallow the next click on the page.
		mouse.MoveTo(SUT.GetAbsoluteBoundsRect().GetCenter());
		await WindowHelper.WaitForIdle();
		mouse.Press();
		mouse.Release();
		await WindowHelper.WaitForIdle();

		var (blurs, focuses) = GetHiddenInputFocusCalls();
		Assert.AreEqual(0, blurs);
		Assert.IsTrue(focuses > 0, "The click should focus the hidden input again.");
		Assert.IsFalse(IsTrailingClickGuardArmed());
	}

	// A tap with a finger of its own: injected touches take their time from when the finger was created, so the
	// taps of one finger would count as a double tap however long apart.
	private static void Tap(InputInjector injector, FrameworkElement element)
	{
		using var finger = injector.GetFinger();
		finger.Press(element.GetAbsoluteBoundsRect().GetCenter());
		finger.Release();
	}

	// Focuses the control as a dialog does the field it opens on: from code, with no tap going on.
	private static async Task FocusFromCode(Control control)
	{
		control.Focus(FocusState.Programmatic);
		await WindowHelper.WaitForIdle();
		Assert.IsTrue(IsHiddenInputFocused(), "The hidden native input should be focused for the focused control.");
	}

	// Makes the hidden input's bridge take the browser for an iOS one, or not, and the pointer for the kind a tap or
	// a click on the page would have it be: injected input raises no DOM event.
	private static IDisposable SimulateIOS(bool isIOS, string pointerType = "touch")
	{
		// Accessibility, which an earlier test may have left on, routes text entry through the semantic inputs
		// and detaches the shared one.
		DisableAccessibility();

		InvokeBrowserJs($$"""
			(function() {
				const bridge = {{HiddenInputBridge}};
				bridge.detach();
				window.__unoBridge ??= { isIOS: bridge.isIOS, lastPointerType: bridge.lastPointerType, swallowNextCanvasClick: bridge.swallowNextCanvasClick };
				bridge.isIOS = {{(isIOS ? "true" : "false")}};
				bridge.lastPointerType = '{{pointerType}}';
				return '';
			})()
			""");
		return Disposable.Create(() => InvokeBrowserJs($"(function(){{ const bridge = {HiddenInputBridge}; bridge.detach(); Object.assign(bridge, window.__unoBridge); return ''; }})()"));
	}

	// Counts the calls that blur or focus the hidden input from here on. Calls rather than events: a tab in the
	// background raises no focus events.
	private static void CountHiddenInputFocusCalls()
		=> InvokeBrowserJs($$"""
			(function() {
				const input = {{HiddenInputBridge}}.inputElement;
				const calls = window.__unoFocusCalls = { blur: 0, focus: 0 };
				const prototype = Object.getPrototypeOf(input);
				input.blur = function() { calls.blur++; prototype.blur.call(input); };
				input.focus = function(options) { calls.focus++; prototype.focus.call(input, options); };
				return '';
			})()
			""");

	private static (int Blurs, int Focuses) GetHiddenInputFocusCalls()
	{
		var calls = InvokeBrowserJs("(function(){ return window.__unoFocusCalls.blur + ',' + window.__unoFocusCalls.focus; })()").Split(',');
		return (int.Parse(calls[0]), int.Parse(calls[1]));
	}

	private static bool IsHiddenInputFocused()
		=> InvokeBrowserJs($"(function(){{ const input = {HiddenInputBridge}.inputElement; return input && document.activeElement === input ? '1' : '0'; }})()") == "1";

	private static void DisarmTrailingClickGuard()
		=> InvokeBrowserJs($"(function(){{ {HiddenInputBridge}.swallowNextCanvasClick = false; return ''; }})()");

	private static bool IsTrailingClickGuardArmed()
		=> InvokeBrowserJs($"(function(){{ return {HiddenInputBridge}.swallowNextCanvasClick === true ? '1' : '0'; }})()") == "1";
}
#endif
