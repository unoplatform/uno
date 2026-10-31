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

		private static string PartInfo(string id, string part)
			=> InvokeBrowserJs($"(function(){{const e = document.getElementById('{id}-{part}'); return e ? [e.tagName, e.getAttribute('aria-label') || '', e.getAttribute('aria-haspopup') || '', e.getAttribute('aria-expanded') || ''].join('|') : 'missing'; }})()");

		private static string PartAttribute(string id, string part, string attribute)
			=> InvokeBrowserJs($"(function(){{const e = document.getElementById('{id}-{part}'); return e ? (e.getAttribute('{attribute}') || '') : 'missing'; }})()");

		private static void ClickPart(string id, string part)
			=> InvokeBrowserJs($"(function(){{document.getElementById('{id}-{part}').click(); return 'ok'; }})()");
#endif
	}
}
