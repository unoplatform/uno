using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

#if HAS_UNO
using static Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation
{
	[TestClass]
	public class Given_AccessibleSplitButton
	{
#if HAS_UNO
		[TestCleanup]
		public void DisableAccessibilityAfterTest() => DisableAccessibility();

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_SplitButton_Then_Group_Of_Primary_And_Menu_Buttons()
		{
			var flyout = new MenuFlyout { Items = { new MenuFlyoutItem { Text = "As PDF" } } };
			var splitButton = new SplitButton { Content = "Export", Flyout = flyout };
			AutomationProperties.SetName(splitButton, "Export actions");
			var primaryInvoked = false;
			splitButton.Click += (s, e) => primaryInvoked = true;

			await UITestHelper.Load(splitButton);
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(splitButton), timeoutMS: 5000, message: "Timed out waiting for the split button semantic element.");

			var id = GetSemanticElementId(splitButton);
			Assert.AreEqual("group", GetSemanticAttribute(splitButton, "role"));
			Assert.AreEqual("Export actions", GetSemanticAttribute(splitButton, "aria-label"));
			Assert.IsFalse(SemanticElementHasAttribute(splitButton, "aria-expanded"), "The group must not carry the menu button's state.");
			Assert.IsFalse(SemanticElementHasAttribute(splitButton, "aria-haspopup"), "The group must not carry the menu button's state.");

			Assert.AreEqual("BUTTON|Export||", PartInfo(id, "primary"));
			Assert.AreEqual("BUTTON|More Options|menu|false", PartInfo(id, "secondary"));

			ClickPart(id, "secondary");
			await UITestHelper.WaitFor(() => splitButton.IsFlyoutOpen && PartAttribute(id, "secondary", "aria-expanded") == "true", timeoutMS: 5000,
				message: "Activating the menu button must open the flyout and report aria-expanded=true.");

			flyout.Hide();
			await UITestHelper.WaitFor(() => PartAttribute(id, "secondary", "aria-expanded") == "false", timeoutMS: 5000,
				message: "Closing the flyout must report aria-expanded=false on the menu button.");

			ClickPart(id, "primary");
			await UITestHelper.WaitFor(() => primaryInvoked, timeoutMS: 5000, message: "Activating the primary button must run the primary action.");
			Assert.IsFalse(splitButton.IsFlyoutOpen, "The primary button must not open the menu.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_ToggleSplitButton_Then_Primary_Is_A_Toggle_Button()
		{
			var splitButton = new ToggleSplitButton { Content = "Bold", Flyout = new MenuFlyout { Items = { new MenuFlyoutItem { Text = "Options" } } } };

			await UITestHelper.Load(splitButton);
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(splitButton), timeoutMS: 5000, message: "Timed out waiting for the split button semantic element.");

			var id = GetSemanticElementId(splitButton);
			Assert.AreEqual("false", PartAttribute(id, "primary", "aria-pressed"));

			ClickPart(id, "primary");
			await UITestHelper.WaitFor(() => splitButton.IsChecked && PartAttribute(id, "primary", "aria-pressed") == "true", timeoutMS: 5000,
				message: "Activating the primary button must toggle the split button and report aria-pressed=true.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Content_Changes_Then_Primary_Label_Follows_And_Group_Keeps_Its_Name()
		{
			var splitButton = new SplitButton { Content = "Export", Flyout = new MenuFlyout { Items = { new MenuFlyoutItem { Text = "As PDF" } } } };
			AutomationProperties.SetName(splitButton, "Export actions");

			await UITestHelper.Load(splitButton);
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(splitButton), timeoutMS: 5000, message: "Timed out waiting for the split button semantic element.");
			var id = GetSemanticElementId(splitButton);

			splitButton.Content = "Export everything now";
			await UITestHelper.WaitFor(() => PartAttribute(id, "primary", "aria-label") == "Export everything now", timeoutMS: 5000,
				message: "The primary button must be renamed when the content changes.");
			Assert.AreEqual("Export actions", GetSemanticAttribute(splitButton, "aria-label"), "The group must keep its explicit name.");
			Assert.AreEqual("More Options", PartAttribute(id, "secondary", "aria-label"));
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_SplitButton_Then_Primary_Is_The_Only_Tab_Stop()
		{
			var splitButton = new SplitButton { Content = "Export", Flyout = new MenuFlyout { Items = { new MenuFlyoutItem { Text = "As PDF" } } } };

			await UITestHelper.Load(splitButton);
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(splitButton), timeoutMS: 5000, message: "Timed out waiting for the split button semantic element.");
			var id = GetSemanticElementId(splitButton);

			Assert.AreEqual("0", PartAttribute(id, "primary", "tabindex"));
			Assert.AreEqual("-1", PartAttribute(id, "secondary", "tabindex"), "As in WinUI, the menu part is not a separate tab stop.");

			splitButton.Focus(FocusState.Keyboard);
			await UITestHelper.WaitFor(() => InvokeBrowserJs("(function(){return document.activeElement ? document.activeElement.id : '';})()") == id + "-primary", timeoutMS: 5000,
				message: "Focusing the split button must focus its primary part.");
			Assert.AreEqual("-1", GetSemanticAttribute(splitButton, "tabindex"), "The group itself must never become a tab stop.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_NonTabStop_SplitButton_Is_Reenabled_Then_It_Stays_Out_Of_Tab_Order()
		{
			var splitButton = new SplitButton { Content = "Export", IsTabStop = false, Flyout = new MenuFlyout { Items = { new MenuFlyoutItem { Text = "As PDF" } } } };

			await UITestHelper.Load(splitButton);
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(splitButton), timeoutMS: 5000, message: "Timed out waiting for the split button semantic element.");
			var id = GetSemanticElementId(splitButton);
			Assert.AreEqual("-1", PartAttribute(id, "primary", "tabindex"));

			splitButton.IsEnabled = false;
			await UITestHelper.WaitFor(() => PartAttribute(id, "primary", "aria-disabled") == "true", timeoutMS: 5000, message: "The parts must be disabled.");

			splitButton.IsEnabled = true;
			await UITestHelper.WaitFor(() => PartAttribute(id, "primary", "aria-disabled") == "", timeoutMS: 5000, message: "The parts must be re-enabled.");
			Assert.AreEqual("-1", PartAttribute(id, "primary", "tabindex"), "Re-enabling must not make a non-tab-stop split button tabbable.");
			Assert.AreEqual("-1", PartAttribute(id, "secondary", "tabindex"));
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Space_On_Menu_Part_Then_Primary_Action_Does_Not_Run()
		{
			var splitButton = new SplitButton { Content = "Export", Flyout = new MenuFlyout { Items = { new MenuFlyoutItem { Text = "As PDF" } } } };
			var primaryInvoked = false;
			splitButton.Click += (s, e) => primaryInvoked = true;

			await UITestHelper.Load(splitButton);
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(splitButton), timeoutMS: 5000, message: "Timed out waiting for the split button semantic element.");
			var id = GetSemanticElementId(splitButton);

			splitButton.Focus(FocusState.Keyboard);
			await UITestHelper.WaitForIdle();
			InvokeBrowserJs($"(function(){{const e = document.getElementById('{id}-secondary'); e.focus(); for (const t of ['keydown', 'keyup']) {{ e.dispatchEvent(new KeyboardEvent(t, {{ key: ' ', code: 'Space', bubbles: true }})); }} return 'ok'; }})()");
			await UITestHelper.WaitForIdle();

			Assert.IsFalse(primaryInvoked, "Space on the menu part must not reach the managed SplitButton and run the primary action.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_SplitButton_Resized_Then_Parts_Follow()
		{
			var splitButton = new SplitButton { Content = "Export", Width = 120, Flyout = new MenuFlyout { Items = { new MenuFlyoutItem { Text = "As PDF" } } } };

			await UITestHelper.Load(splitButton);
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(splitButton), timeoutMS: 5000, message: "Timed out waiting for the split button semantic element.");
			var id = GetSemanticElementId(splitButton);

			splitButton.Width = 260;
			await UITestHelper.WaitFor(() => RightEdgeGap(id) == "0", timeoutMS: 5000,
				message: "The parts must keep covering the resized split button (menu part at the right edge, primary part next to it).");
		}

		private static string RightEdgeGap(string id)
			=> InvokeBrowserJs($"(function(){{const g = document.getElementById('{id}'), s = document.getElementById('{id}-secondary'), p = document.getElementById('{id}-primary'); if (!g || !s || !p) return 'missing'; const gr = g.getBoundingClientRect(), sr = s.getBoundingClientRect(), pr = p.getBoundingClientRect(); return String(Math.round(Math.abs(gr.right - sr.right) + Math.abs(pr.right - sr.left))); }})()");

		private static string PartInfo(string id, string part)
			=> InvokeBrowserJs($"(function(){{const e = document.getElementById('{id}-{part}'); return e ? [e.tagName, e.getAttribute('aria-label') || '', e.getAttribute('aria-haspopup') || '', e.getAttribute('aria-expanded') || ''].join('|') : 'missing'; }})()");

		private static string PartAttribute(string id, string part, string attribute)
			=> InvokeBrowserJs($"(function(){{const e = document.getElementById('{id}-{part}'); return e ? (e.getAttribute('{attribute}') || '') : 'missing'; }})()");

		private static void ClickPart(string id, string part)
			=> InvokeBrowserJs($"(function(){{document.getElementById('{id}-{part}').click(); return 'ok'; }})()");
#endif
	}
}
