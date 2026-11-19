using System;
using System.Threading.Tasks;
using System.Windows.Input;
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

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Command_Cannot_Execute_Then_Only_Primary_Part_Is_Disabled()
		{
			var command = new ToggleableCommand { IsExecutable = false };
			var splitButton = new SplitButton { Content = "Export", Command = command, Flyout = new MenuFlyout { Items = { new MenuFlyoutItem { Text = "As PDF" } } } };

			await UITestHelper.Load(splitButton);
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(splitButton), timeoutMS: 5000, message: "Timed out waiting for the split button semantic element.");
			var id = GetSemanticElementId(splitButton);

			Assert.IsTrue(splitButton.IsEnabled, "CanExecute == false disables only the primary template part.");
			Assert.AreEqual("true", PartAttribute(id, "primary", "aria-disabled"), "The primary part must mirror its own disabled state.");
			Assert.AreEqual("", PartAttribute(id, "secondary", "aria-disabled"), "The menu part must stay enabled.");
			Assert.AreEqual("0", PartAttribute(id, "secondary", "tabindex"), "The menu part must take the tab stop while the primary part is disabled.");

			command.IsExecutable = true;
			await UITestHelper.WaitFor(() => PartAttribute(id, "primary", "aria-disabled") == "", timeoutMS: 5000,
				message: "The primary part must be re-enabled when the command can execute again.");
			Assert.AreEqual("0", PartAttribute(id, "primary", "tabindex"));
			Assert.AreEqual("-1", PartAttribute(id, "secondary", "tabindex"));
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_SplitButton_Added_After_Accessibility_Enabled_Then_Menu_Part_Is_Measured()
		{
			// EnsureAccessibilityEnabledAsync replaces the window content with its probe, so it must run before the host is loaded.
			await EnsureAccessibilityEnabledAsync();
			var host = new Grid { Width = 400, Height = 200, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
			await UITestHelper.Load(host);

			var splitButton = new SplitButton { Content = "Export with a long label", Flyout = new MenuFlyout { Items = { new MenuFlyoutItem { Text = "As PDF" } } } };
			host.Children.Add(splitButton);
			await UITestHelper.WaitFor(() => splitButton.SecondaryButtonPart is { ActualWidth: > 0 }, timeoutMS: 5000,
				message: "The split button added after accessibility was enabled must be laid out.");
			await UITestHelper.WaitFor(() => SemanticElementExists(splitButton), timeoutMS: 5000, message: "Timed out waiting for the split button semantic element.");
			var id = GetSemanticElementId(splitButton);

			var expected = Math.Round(splitButton.SecondaryButtonPart.ActualWidth).ToString(System.Globalization.CultureInfo.InvariantCulture);
			await UITestHelper.WaitFor(() => PartStyle(id, "secondary", "width") == expected && RightEdgeGap(id) == "0", timeoutMS: 5000,
				message: $"The menu part must take the measured width of the template's menu button ({expected}px) once laid out, got {PartStyle(id, "secondary", "width")}px.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Space_On_Primary_Part_Of_ToggleSplitButton_Then_Click_And_Command_Run()
		{
			var command = new ToggleableCommand { IsExecutable = true };
			var splitButton = new ToggleSplitButton { Content = "Bold", Command = command, Flyout = new MenuFlyout { Items = { new MenuFlyoutItem { Text = "Options" } } } };
			var clicks = 0;
			splitButton.Click += (s, e) => clicks++;

			await UITestHelper.Load(splitButton);
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(splitButton), timeoutMS: 5000, message: "Timed out waiting for the split button semantic element.");
			var id = GetSemanticElementId(splitButton);

			splitButton.Focus(FocusState.Keyboard);
			await UITestHelper.WaitForIdle();
			var defaultPrevented = InvokeBrowserJs($"(function(){{const e = document.getElementById('{id}-primary'); e.focus(); let prevented = true; for (const t of ['keydown', 'keyup']) {{ const k = new KeyboardEvent(t, {{ key: ' ', code: 'Space', bubbles: true, cancelable: true }}); e.dispatchEvent(k); prevented = prevented && k.defaultPrevented; }} return String(prevented); }})()");

			await UITestHelper.WaitFor(() => splitButton.IsChecked, timeoutMS: 5000, message: "Space on the primary part must reach the managed ToggleSplitButton and toggle it.");
			await UITestHelper.WaitForIdle();
			Assert.AreEqual(1, clicks, "Space on the primary part must raise Click once.");
			Assert.AreEqual(1, command.ExecuteCount, "Space on the primary part must execute the Command once.");
			Assert.AreEqual("true", defaultPrevented, "The native click of the primary part must be suppressed so the action does not run twice.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Focus_Trap_Contains_Split_Buttons_Then_Shift_Tab_Moves_From_Focused_Part()
		{
			SplitButton Create(string text) => new SplitButton { Content = text, Flyout = new MenuFlyout { Items = { new MenuFlyoutItem { Text = "Option" } } } };
			var first = Create("First");
			var second = Create("Second");
			var third = Create("Third");

			await UITestHelper.Load(new StackPanel { Children = { first, second, third } });
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(first) && SemanticElementExists(second) && SemanticElementExists(third), timeoutMS: 5000,
				message: "Timed out waiting for the split button semantic elements.");

			string Handle(UIElement e) => GetSemanticElementId(e).Substring("uno-semantics-".Length);
			const string FakeModal = "987654321";
			try
			{
				var focusedOnActivate = InvokeBrowserJs($"(function(){{Uno.UI.Runtime.FocusTrap.activateFocusTrap({FakeModal}, 0, [{Handle(first)}, {Handle(second)}, {Handle(third)}]); return document.activeElement ? document.activeElement.id : ''; }})()");
				Assert.AreEqual(GetSemanticElementId(first) + "-primary", focusedOnActivate, "Activating the trap must focus the first split button's primary part.");

				var wrapped = InvokeBrowserJs($"(function(){{document.getElementById('{GetSemanticElementId(second)}-primary').focus(); return String(Uno.UI.Runtime.FocusTrap.handleTrapTab({FakeModal}, true)); }})()");
				Assert.AreEqual("false", wrapped, "Shift+Tab from the second split button must not wrap to the last one.");

				var wrappedFromFirst = InvokeBrowserJs($"(function(){{document.getElementById('{GetSemanticElementId(first)}-primary').focus(); const w = Uno.UI.Runtime.FocusTrap.handleTrapTab({FakeModal}, true); return w + '|' + (document.activeElement ? document.activeElement.id : ''); }})()");
				Assert.AreEqual("true|" + GetSemanticElementId(third) + "-primary", wrappedFromFirst, "Shift+Tab from the first split button must wrap to the last one's primary part.");
			}
			finally
			{
				InvokeBrowserJs($"(function(){{Uno.UI.Runtime.FocusTrap.deactivateFocusTrap({FakeModal}); return 'ok'; }})()");
			}
		}

		private sealed class ToggleableCommand : ICommand
		{
			private bool _isExecutable;

			public event EventHandler CanExecuteChanged;

			public int ExecuteCount { get; private set; }

			public bool IsExecutable
			{
				get => _isExecutable;
				set
				{
					_isExecutable = value;
					CanExecuteChanged?.Invoke(this, EventArgs.Empty);
				}
			}

			public bool CanExecute(object parameter) => _isExecutable;

			public void Execute(object parameter) => ExecuteCount++;
		}

		private static string PartStyle(string id, string part, string property)
			=> InvokeBrowserJs($"(function(){{const e = document.getElementById('{id}-{part}'); return e ? String(Math.round(parseFloat(e.style['{property}']) || 0)) : 'missing'; }})()");

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
