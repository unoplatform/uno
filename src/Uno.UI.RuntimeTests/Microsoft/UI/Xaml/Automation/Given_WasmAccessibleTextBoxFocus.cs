#if HAS_UNO
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using static Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

/// <summary>
/// Mirroring XAML focus into the semantic DOM must not feed back into XAML focus.
/// </summary>
[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
public class Given_WasmAccessibleTextBoxFocus
{
	[TestCleanup]
	public void Cleanup()
	{
		DisableAccessibility();
		TestServices.WindowHelper.WindowContent = null;
	}

	[TestMethod]
	[DataRow(FocusState.Programmatic)]
	[DataRow(FocusState.Keyboard)]
	[DataRow(FocusState.Pointer)]
	public async Task When_TextBox_Focused_Then_Focus_Does_Not_Bounce(FocusState focusState)
	{
		var (textBox, log) = await LoadAsync(new TextBox { Text = "hello" });

		textBox.Focus(focusState);
		await SettleAsync();

		CollectionAssert.AreEqual(new[] { "GotFocus" }, log, string.Join(", ", log));
		if (focusState != FocusState.Programmatic)
		{
			// The bounce re-applies focus from the semantic node as Keyboard.
			Assert.AreEqual(focusState, textBox.FocusState);
		}
		Assert.AreEqual(GetSemanticElementId(textBox), InvokeBrowserJs("(function(){return document.activeElement ? document.activeElement.id : '';})()"), "The semantic input should own DOM focus.");
	}

	[TestMethod]
	public async Task When_PasswordBox_Focused_Then_Focus_Does_Not_Bounce()
	{
		var (passwordBox, log) = await LoadAsync(new PasswordBox { Password = "secret" });

		passwordBox.Focus(FocusState.Programmatic);
		await SettleAsync();

		CollectionAssert.AreEqual(new[] { "GotFocus" }, log, string.Join(", ", log));
	}

	[TestMethod]
	public async Task When_Semantic_Input_Focused_By_Browser_Then_TextBox_Gets_Focus_Once()
	{
		var (textBox, log) = await LoadAsync(new TextBox { Text = "hello" });

		// What a screen reader (or Tab in the semantic DOM) does: focus the semantic node itself.
		InvokeBrowserJs($"(function(){{document.getElementById('{GetSemanticElementId(textBox)}').focus(); return 'ok';}})()");
		await SettleAsync();

		CollectionAssert.AreEqual(new[] { "GotFocus" }, log, string.Join(", ", log));
		Assert.AreEqual(FocusState.Keyboard, textBox.FocusState);
	}

	[TestMethod]
	public async Task When_Focused_Radio_Gets_Browser_Focus_Then_Roving_Tab_Stop_Follows()
	{
		var first = new RadioButton { Content = "First", GroupName = "roving" };
		var second = new RadioButton { Content = "Second", GroupName = "roving" };
		var (_, log) = await LoadAsync(second, new StackPanel { Children = { first, second } });

		second.Focus(FocusState.Keyboard);
		await SettleAsync();

		// Make the tab stop stale, then have the browser (a screen reader, or Tab) land on the focused radio.
		var firstId = GetSemanticElementId(first);
		var secondId = GetSemanticElementId(second);
		InvokeBrowserJs($"(function(){{document.getElementById('{firstId}').tabIndex = 0; document.getElementById('{secondId}').tabIndex = -1; document.getElementById('{secondId}').focus(); return 'ok';}})()");
		await SettleAsync();

		CollectionAssert.AreEqual(new[] { "GotFocus" }, log, string.Join(", ", log));
		Assert.AreEqual("0", InvokeBrowserJs($"(function(){{return String(document.getElementById('{secondId}').tabIndex);}})()"), "The focused radio should be the tab stop.");
		Assert.AreEqual("-1", InvokeBrowserJs($"(function(){{return String(document.getElementById('{firstId}').tabIndex);}})()"), "The other radio should leave the tab order.");
	}

	private static Task<(T Control, List<string> Log)> LoadAsync<T>(T control) where T : Control
		=> LoadAsync(control, null);

	private static async Task<(T Control, List<string> Log)> LoadAsync<T>(T control, Panel container) where T : Control
	{
		var other = new Button { Content = "Other" };
		await UITestHelper.Load(container is null ? new StackPanel { Children = { other, control } } : new StackPanel { Children = { other, container } });

		EnableAccessibilityThroughDom();
		await UITestHelper.WaitFor(() => SemanticElementExists(control), message: "The semantic node should be created once accessibility is enabled.");

		other.Focus(FocusState.Programmatic);
		await SettleAsync();

		var log = new List<string>();
		control.GotFocus += (_, _) => log.Add("GotFocus");
		control.LostFocus += (_, _) => log.Add("LostFocus");
		return (control, log);
	}

	private static async Task SettleAsync()
	{
		await UITestHelper.WaitForIdle();
		// The bounce this guards against comes back through browser focus events and then the dispatcher, a few
		// frames later; there is no event to wait on for "nothing else happened".
		await Task.Delay(300);
		await UITestHelper.WaitForIdle();
	}
}
#endif
