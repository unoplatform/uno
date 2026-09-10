using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

#if HAS_UNO
using static Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation
{
	/// <summary>
	/// Runtime tests for the tabindex contract on the WASM accessibility object model (AOM).
	///
	/// tabindex is a DOM/AOM behavior, so the assertions here are gated to Skia WASM
	/// (<see cref="RuntimeTestPlatforms.SkiaWasm"/>) where a semantic DOM is built. They enable
	/// the AOM in-test (the "Enable Accessibility" affordance) and read <c>tabIndex</c> off the
	/// per-element semantic node (<c>document.getElementById("uno-semantics-{handle}")</c>), the
	/// same pattern the active tests in <c>Given_AccessibleTextBox.cs</c> use.
	///
	/// Contract (spec 003, FR-005/006/007):
	/// - heading (&lt;hN&gt;): NO tabindex at all.
	/// - a non-interactive / non-focusable control (IsTabStop=false or IsEnabled=false): not a tab stop.
	/// - a composite container (listbox/tablist/tree/menu/grid): not itself a tab stop (roving model,
	///   tabindex=-1, never 0); focus roves across items so only one roving stop exists.
	///
	/// HAND-OFF: these run on Skia WASM only. They are written test-first against the post-fix
	/// contract (US2/T014/T015), so they are expected to FAIL until the heading hardcoded
	/// <c>tabIndex=0</c> is removed and the composite container moves to the roving model, then PASS.
	/// </summary>
	[TestClass]
	[RunsOnUIThread]
	public class Given_AccessibleTabindex
	{
#if HAS_UNO
		/// <summary>
		/// FR-006: a heading (AutomationProperties.HeadingLevel set) must NOT be a tab stop.
		/// Pre-fix, createHeadingElement hardcodes tabIndex=0; the contract removes the tabindex
		/// line entirely so the heading carries no tabindex attribute.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Heading_Then_Has_No_Tabindex()
		{
			var heading = new TextBlock { Text = "Section title" };
			AutomationProperties.SetHeadingLevel(heading, AutomationHeadingLevel.Level2);

			await UITestHelper.Load(heading);
			heading.GetOrCreateAutomationPeer();

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(heading), timeoutMS: 5000, message: "Timed out waiting for the semantic heading to be created.");
			await UITestHelper.WaitForIdle();

			// A heading must carry no tabindex attribute at all (not even -1): it is static content.
			Assert.IsFalse(HasTabindexAttribute(heading), "A heading must not expose any tabindex attribute.");
		}

		/// <summary>
		/// FR-005: an interactive control that is not focusable (IsTabStop=false) must not be a tab stop.
		/// tabindex is gated on real focusability via updateElementFocusability, so it resolves to -1.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Control_IsTabStop_False_Then_Not_A_Tab_Stop()
		{
			var button = new Button
			{
				Content = "Not tabbable",
				IsTabStop = false
			};

			await UITestHelper.Load(button);
			button.GetOrCreateAutomationPeer();

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(button), timeoutMS: 5000, message: "Timed out waiting for the semantic button to be created.");
			await UITestHelper.WaitForIdle();

			Assert.AreNotEqual("0", GetTabindex(button), "An IsTabStop=false control must not be a tab stop (tabindex must not be 0).");
		}

		/// <summary>
		/// FR-005: a disabled interactive control (IsEnabled=false) must not be a tab stop.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Control_IsDisabled_Then_Not_A_Tab_Stop()
		{
			var button = new Button
			{
				Content = "Disabled",
				IsEnabled = false
			};

			await UITestHelper.Load(button);
			button.GetOrCreateAutomationPeer();

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(button), timeoutMS: 5000, message: "Timed out waiting for the semantic button to be created.");
			await UITestHelper.WaitForIdle();

			Assert.AreNotEqual("0", GetTabindex(button), "A disabled control must not be a tab stop (tabindex must not be 0).");
		}

		/// <summary>
		/// FR-007: a composite container (e.g. ListView → role="listbox") must not itself be a tab stop.
		/// The roving model puts the container at tabindex=-1 (never 0); the single tab stop lives on the
		/// active item. Pre-fix, createListBoxElement hardcodes the container tabIndex=0.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Composite_Container_Then_Not_A_Tab_Stop()
		{
			var listView = new ListView
			{
				ItemsSource = new List<string> { "Alpha", "Beta", "Gamma" }
			};

			await UITestHelper.Load(listView);
			listView.GetOrCreateAutomationPeer();
			await TestServices.WindowHelper.WaitForIdle();

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(listView), timeoutMS: 5000, message: "Timed out waiting for the semantic listbox container to be created.");
			await UITestHelper.WaitForIdle();

			// The container itself must not be a regular tab stop: under the roving model it is -1, and the
			// single tab stop belongs to the active item, not the container.
			Assert.AreNotEqual("0", GetTabindex(listView), "A composite container must not be a tab stop (tabindex must not be 0); the roving stop lives on its active item.");
		}




		// Returns the tabindex as a string ("0", "-1", ...) or empty string when the attribute is absent.
		private static string GetTabindex(UIElement element)
			=> InvokeBrowserJs($"(function(){{const e = document.getElementById('{GetSemanticElementId(element)}'); if (!e) {{ return ''; }} return e.hasAttribute('tabindex') ? e.getAttribute('tabindex') : ''; }})()");

		private static bool HasTabindexAttribute(UIElement element)
			=> InvokeBrowserJs($"(function(){{const e = document.getElementById('{GetSemanticElementId(element)}'); return e && e.hasAttribute('tabindex') ? '1' : '0'; }})()") == "1";

#endif
	}
}
