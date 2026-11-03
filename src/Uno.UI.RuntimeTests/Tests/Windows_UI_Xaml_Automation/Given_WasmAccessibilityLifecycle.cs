#if HAS_UNO
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using static Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
public class Given_WasmAccessibilityLifecycle
{
	[TestCleanup]
	public void Cleanup() => DisableAccessibility();

	[TestMethod]
	public async Task When_Disabled_Semantic_Dom_Is_Removed()
	{
		var button = new Button { Content = "Target" };
		await UITestHelper.Load(button);

		EnableAccessibilityThroughDom();
		await UITestHelper.WaitFor(() => SemanticElementExists(button), message: "The semantic node should be created once accessibility is enabled.");

		DisableAccessibility();
		await UITestHelper.WaitForIdle();

		Assert.IsFalse(SemanticElementExists(button), "The semantic node should be removed once accessibility is disabled.");
		Assert.AreEqual("0", InvokeBrowserJs("(function(){return String(document.getElementById('uno-semantics-root').childElementCount);})()"));
		Assert.AreEqual("1", InvokeBrowserJs("(function(){return document.getElementById('uno-enable-accessibility') ? '1' : '0';})()"), "The enable button should be back so accessibility can be turned on again.");
	}

	[TestMethod]
	public async Task When_Disabled_TextBox_Focus_Does_Not_Bounce()
	{
		EnableAccessibilityThroughDom();
		await UITestHelper.WaitForIdle();
		DisableAccessibility();

		var textBox = new TextBox();
		var otherButton = new Button { Content = "Other" };
		await UITestHelper.Load(new StackPanel { Children = { otherButton, textBox } });

		otherButton.Focus(FocusState.Programmatic);
		await UITestHelper.WaitForIdle();

		var gotFocusCount = 0;
		var lostFocusCount = 0;
		textBox.GotFocus += (s, e) => gotFocusCount++;
		textBox.LostFocus += (s, e) => lostFocusCount++;

		textBox.Focus(FocusState.Programmatic);
		await UITestHelper.WaitForIdle();

		Assert.AreEqual(1, gotFocusCount, "A single Focus call should raise GotFocus once.");
		Assert.AreEqual(0, lostFocusCount, "Focus should not bounce off the TextBox.");
		// The bounce re-focuses through the semantic DOM, which reports Keyboard.
		Assert.AreNotEqual(FocusState.Keyboard, textBox.FocusState);
	}

	[TestMethod]
	public async Task When_Reenabled_Semantic_Dom_Is_Rebuilt()
	{
		var button = new Button { Content = "Target" };
		await UITestHelper.Load(button);

		EnableAccessibilityThroughDom();
		await UITestHelper.WaitForIdle();
		DisableAccessibility();
		await UITestHelper.WaitForIdle();

		EnableAccessibilityThroughDom();
		await UITestHelper.WaitFor(() => SemanticElementExists(button), message: "Re-enabling should rebuild the semantic node.");

		Assert.AreEqual("1", InvokeBrowserJs("(function(){return String(document.querySelectorAll('#uno-live-region-polite').length);})()"), "Re-enabling should not duplicate the live regions.");
		Assert.AreEqual("1", InvokeBrowserJs("(function(){return String(document.querySelectorAll('#uno-focus-sentinel-start').length);})()"), "Re-enabling should not duplicate the focus sentinels.");
	}
}
#endif
