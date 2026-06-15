using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

#if HAS_UNO
using Uno.UI.Runtime.Skia;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation
{
	/// <summary>
	/// Runtime tests for accessible combobox behavior.
	/// Tests automation peer properties, expand/collapse pattern, and ARIA attribute mapping.
	/// </summary>
	[TestClass]
	public class Given_AccessibleComboBox
	{
		/// <summary>
		/// T057: Verifies that a closed ComboBox reports aria-expanded="false".
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_ComboBox_Closed_Then_AriaExpanded_IsFalse()
		{
			// Arrange
			var comboBox = new ComboBox();
			comboBox.Items.Add("Option A");
			comboBox.Items.Add("Option B");
			comboBox.Items.Add("Option C");

			await UITestHelper.Load(comboBox);

			// Act
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(comboBox);
			var expandCollapseProvider = peer?.GetPattern(PatternInterface.ExpandCollapse) as IExpandCollapseProvider;

			// Assert
			Assert.IsNotNull(peer, "ComboBox should have an automation peer");
			Assert.IsNotNull(expandCollapseProvider, "ComboBox should support IExpandCollapseProvider");
			Assert.AreEqual(ExpandCollapseState.Collapsed, expandCollapseProvider.ExpandCollapseState, "Closed ComboBox should report Collapsed state");
		}

		/// <summary>
		/// T058: Verifies that calling Expand on the ComboBox opens it.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Enter_Pressed_Then_ComboBox_Opens()
		{
			// Arrange
			var comboBox = new ComboBox();
			comboBox.Items.Add("Option A");
			comboBox.Items.Add("Option B");

			await UITestHelper.Load(comboBox);

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(comboBox);
			var expandCollapseProvider = peer?.GetPattern(PatternInterface.ExpandCollapse) as IExpandCollapseProvider;

			Assert.IsNotNull(expandCollapseProvider, "ComboBox should support IExpandCollapseProvider");

			// Act - Simulate Enter press expanding the ComboBox via automation peer
			expandCollapseProvider.Expand();
			await TestServices.WindowHelper.WaitForIdle();

			// Assert
			Assert.AreEqual(ExpandCollapseState.Expanded, expandCollapseProvider.ExpandCollapseState, "After Expand, state should be Expanded");
		}

		/// <summary>
		/// T059: Verifies that selecting an item via automation updates the selection.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Item_Selected_Then_Selection_Announced()
		{
			// Arrange
			var comboBox = new ComboBox();
			comboBox.Items.Add("Option A");
			comboBox.Items.Add("Option B");
			comboBox.Items.Add("Option C");
			comboBox.SelectedIndex = 0;

			await UITestHelper.Load(comboBox);

			// Act
			comboBox.SelectedIndex = 2;
			await TestServices.WindowHelper.WaitForIdle();

			// Assert
			Assert.AreEqual(2, comboBox.SelectedIndex);
			Assert.AreEqual("Option C", comboBox.SelectedItem);
		}

		/// <summary>
		/// Verifies that ComboBox automation peer has correct control type.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_ComboBox_Created_Then_Has_ComboBox_ControlType()
		{
			// Arrange
			var comboBox = new ComboBox();
			comboBox.Items.Add("A");
			await UITestHelper.Load(comboBox);

			// Act
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(comboBox);
			var controlType = peer?.GetAutomationControlType();

			// Assert
			Assert.AreEqual(AutomationControlType.ComboBox, controlType);
		}

#if __SKIA__
		/// <summary>
		/// Verifies that an open ComboBox dropdown exposes its options as a proper WAI-ARIA
		/// listbox: a role="listbox" node referenced by the combobox head via aria-controls,
		/// with the options parented under it (so the browser honors role="option" instead of
		/// invalidating the orphaned options to "paragraph"), each carrying aria-posinset and
		/// aria-setsize. Regression test for the pre-existing gap where ComboBox options were
		/// emitted directly under the Popup's role="dialog" and were therefore unreachable.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_DropDown_Opened_Then_Options_Form_Accessible_Listbox()
		{
			var comboBox = new ComboBox();
			comboBox.Items.Add("Option A");
			comboBox.Items.Add("Option B");
			comboBox.Items.Add("Option C");
			comboBox.SelectedIndex = 0;

			try
			{
				await UITestHelper.Load(comboBox);
				comboBox.GetOrCreateAutomationPeer();

				EnableAccessibilityThroughDom();
				await UITestHelper.WaitFor(
					() => ComboBoxHeadExists(comboBox),
					timeoutMS: 5000,
					message: "Timed out waiting for the semantic combobox head element to be created.");

				comboBox.IsDropDownOpen = true;
				await UITestHelper.WaitForIdle();

				await UITestHelper.WaitFor(
					() => GetListBoxOptionCount(comboBox) == 3,
					timeoutMS: 5000,
					message: "Timed out waiting for the 3 dropdown options to be exposed under a role=listbox.");

				Assert.AreEqual(
					"ok",
					VerifyOptionsParentedUnderListBox(comboBox),
					"Options must be role=option direct children of the listbox referenced by the combobox head's aria-controls, each with aria-posinset/aria-setsize.");
			}
			finally
			{
				comboBox.IsDropDownOpen = false;
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		/// <summary>
		/// Regression for the listbox-fix residuals: an open ComboBox dropdown must NOT (a) re-emit each
		/// option's content as a standalone <p> alongside its role=option, nor (b) leave a role=dialog Popup
		/// wrapper around the options. Both are suppressed once the items live in the listbox region.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_DropDown_Opened_Then_No_Duplicate_Option_Paragraphs_Nor_Dialog()
		{
			var comboBox = new ComboBox();
			comboBox.Items.Add("Option A");
			comboBox.Items.Add("Option B");
			comboBox.Items.Add("Option C");
			comboBox.SelectedIndex = 0;

			try
			{
				await UITestHelper.Load(comboBox);
				comboBox.GetOrCreateAutomationPeer();

				EnableAccessibilityThroughDom();
				await UITestHelper.WaitFor(
					() => ComboBoxHeadExists(comboBox),
					timeoutMS: 5000,
					message: "Timed out waiting for the semantic combobox head element to be created.");

				comboBox.IsDropDownOpen = true;
				await UITestHelper.WaitForIdle();

				await UITestHelper.WaitFor(
					() => GetListBoxOptionCount(comboBox) == 3,
					timeoutMS: 5000,
					message: "Timed out waiting for the 3 dropdown options to be exposed under a role=listbox.");

				Assert.AreEqual(
					"0|0",
					GetDuplicateParagraphsAndOptionDialogs(),
					"Open dropdown must not duplicate option text as standalone <p>, nor wrap options in a role=dialog popup.");
			}
			finally
			{
				comboBox.IsDropDownOpen = false;
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		// Returns "dupP|dialogs": count of standalone <p> whose text matches an option label (duplicate
		// emission), and count of role=dialog nodes containing any role=option (un-suppressed popup).
		// "0|0" once both residuals are fixed.
		private static string GetDuplicateParagraphsAndOptionDialogs()
		{
			var js =
				"(function(){" +
				"var labels = ['Option A','Option B','Option C'];" +
				"var dupP = Array.from(document.querySelectorAll('p')).filter(function(p){return labels.indexOf((p.textContent||'').trim()) >= 0;}).length;" +
				"var dlg = Array.from(document.querySelectorAll('[role=dialog]')).filter(function(d){return d.querySelector('[role=option]') !== null;}).length;" +
				"return String(dupP) + '|' + String(dlg);" +
				"})()";
			return InvokeBrowserJs(js);
		}

		private static string GetSemanticElementId(ComboBox comboBox)
			=> "uno-semantics-" + ((long)comboBox.Visual.Handle).ToString(System.Globalization.CultureInfo.InvariantCulture);

		private static bool ComboBoxHeadExists(ComboBox comboBox)
		{
			var id = GetSemanticElementId(comboBox);
			return InvokeBrowserJs("(function(){return document.getElementById('" + id + "') ? '1' : '0';})()") == "1";
		}

		// Returns the number of role=option direct children of the listbox referenced by the
		// combobox head's aria-controls, or a negative sentinel describing what was missing.
		private static int GetListBoxOptionCount(ComboBox comboBox)
		{
			var id = GetSemanticElementId(comboBox);
			var js =
				"(function(){" +
				"const head = document.getElementById('" + id + "');" +
				"if (!head) { return '-1'; }" +
				"const controls = head.getAttribute('aria-controls');" +
				"if (!controls) { return '-2'; }" +
				"const listbox = document.getElementById(controls);" +
				"if (!listbox || listbox.getAttribute('role') !== 'listbox') { return '-3'; }" +
				"return String(listbox.querySelectorAll(':scope > [role=\"option\"]').length);" +
				"})()";
			return int.TryParse(InvokeBrowserJs(js), out var count) ? count : -99;
		}

		// Returns "ok" when every option is a role=option direct child of the listbox and
		// carries a valid aria-posinset/aria-setsize; otherwise a short diagnostic token.
		private static string VerifyOptionsParentedUnderListBox(ComboBox comboBox)
		{
			var id = GetSemanticElementId(comboBox);
			var js =
				"(function(){" +
				"const head = document.getElementById('" + id + "');" +
				"if (!head) { return 'no-head'; }" +
				"const controls = head.getAttribute('aria-controls');" +
				"const listbox = controls ? document.getElementById(controls) : null;" +
				"if (!listbox || listbox.getAttribute('role') !== 'listbox') { return 'no-listbox'; }" +
				"const options = Array.from(listbox.querySelectorAll(':scope > [role=\"option\"]'));" +
				"if (options.length === 0) { return 'no-options'; }" +
				"for (let i = 0; i < options.length; i++) {" +
				"const o = options[i];" +
				"if (o.parentElement !== listbox) { return 'wrong-parent'; }" +
				"const pos = parseInt(o.getAttribute('aria-posinset'));" +
				"if (isNaN(pos) || pos < 1) { return 'bad-posinset'; }" +
				"if (o.getAttribute('aria-setsize') !== String(options.length)) { return 'bad-setsize'; }" +
				"}" +
				"return 'ok';" +
				"})()";
			return InvokeBrowserJs(js);
		}

		private static void EnableAccessibilityThroughDom()
		{
			InvokeBrowserJs("(function(){const button = document.getElementById('uno-enable-accessibility'); if (button) { button.click(); } return 'ok';})()");
		}

		private static string InvokeBrowserJs(string javascript)
		{
			var runtimeType = Type.GetType("Uno.Foundation.WebAssemblyRuntime, Uno.Foundation.Runtime.WebAssembly", throwOnError: false);
			Assert.IsNotNull(runtimeType, "Unable to locate Uno.Foundation.WebAssemblyRuntime at runtime.");

			var invokeJs = runtimeType.GetMethod("InvokeJS", new[] { typeof(string) });
			Assert.IsNotNull(invokeJs, "Unable to locate Uno.Foundation.WebAssemblyRuntime.InvokeJS(string).");

			return invokeJs.Invoke(obj: null, parameters: new object[] { javascript }) as string ?? string.Empty;
		}
#endif

#if HAS_UNO
		/// <summary>
		/// Verifies that AriaMapper correctly identifies ComboBox semantic element type.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_ComboBox_Mapped_Then_SemanticElementType_Is_ComboBox()
		{
			// Arrange
			var comboBox = new ComboBox();
			comboBox.Items.Add("A");
			await UITestHelper.Load(comboBox);

			// Act
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(comboBox);
			var elementType = AriaMapper.GetSemanticElementType(peer);

			// Assert
			Assert.AreEqual(SemanticElementType.ComboBox, elementType);
		}

		/// <summary>
		/// Verifies that AriaMapper produces correct ARIA attributes for ComboBox.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_ComboBox_Mapped_Then_AriaAttributes_Are_Correct()
		{
			// Arrange
			var comboBox = new ComboBox();
			comboBox.Items.Add("A");
			await UITestHelper.Load(comboBox);

			// Act
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(comboBox);
			var attributes = AriaMapper.GetAriaAttributes(peer);

			// Assert
			Assert.AreEqual("combobox", attributes.Role);
			Assert.AreEqual("listbox", attributes.HasPopup);
		}

		/// <summary>
		/// Verifies that AriaMapper correctly detects expand/collapse capability.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_ComboBox_Mapped_Then_PatternCapabilities_CanExpandCollapse_Is_True()
		{
			// Arrange
			var comboBox = new ComboBox();
			comboBox.Items.Add("A");
			await UITestHelper.Load(comboBox);

			// Act
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(comboBox);
			var capabilities = AriaMapper.GetPatternCapabilities(peer);

			// Assert
			Assert.IsTrue(capabilities.CanExpandCollapse, "ComboBox should have CanExpandCollapse capability");
		}
#endif
#if __SKIA__

		/// <summary>
		/// T057/FR-016 (WASM DOM): a closed ComboBox emits role="combobox" with aria-expanded="false" and
		/// aria-haspopup="listbox" on its semantic node.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_ComboBox_Closed_Then_Dom_AriaExpanded_Is_False()
		{
			var comboBox = new ComboBox();
			comboBox.Items.Add("Option A");
			comboBox.Items.Add("Option B");
			comboBox.Items.Add("Option C");

			await UITestHelper.Load(comboBox);
			comboBox.GetOrCreateAutomationPeer();

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(comboBox), timeoutMS: 5000, message: "Timed out waiting for the combobox semantic element to be created.");
			await UITestHelper.WaitForIdle();

			Assert.AreEqual("combobox", GetSemanticAttribute(comboBox, "role"), "A ComboBox must emit role=combobox.");
			Assert.AreEqual("false", GetSemanticAttribute(comboBox, "aria-expanded"), "A closed ComboBox must emit aria-expanded=\"false\".");
			Assert.AreEqual("listbox", GetSemanticAttribute(comboBox, "aria-haspopup"), "A ComboBox must emit aria-haspopup=\"listbox\".");
		}

		// Targets the exact semantic element for a given element via its visual handle, mirroring the
		// id scheme used by the WASM runtime (uno-semantics-{handle}).
		private static string GetSemanticElementId(UIElement element)
			=> $"uno-semantics-{((long)element.Visual.Handle)}";

		private static bool SemanticElementExists(UIElement element)
			=> InvokeBrowserJs($"(function(){{return document.getElementById('{GetSemanticElementId(element)}') ? '1' : '0';}})()") == "1";

		private static string GetSemanticAttribute(UIElement element, string attribute)
			=> InvokeBrowserJs($"(function(){{const e = document.getElementById('{GetSemanticElementId(element)}'); return e ? (e.getAttribute('{attribute}') ?? '') : '';}})()");

#endif

	}
}
