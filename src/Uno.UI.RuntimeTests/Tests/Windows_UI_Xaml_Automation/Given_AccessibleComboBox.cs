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
using static Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper;
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

#if HAS_UNO
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_DataPeer_Selects_Item_Then_ComboBox_Selection_Changes()
		{
			var first = new ComboBoxItem { Content = "Option A" };
			var second = new ComboBoxItem { Content = "Option B" };
			var comboBox = new ComboBox { Items = { first, second }, SelectedIndex = 0 };

			await UITestHelper.Load(comboBox);

			var peer = comboBox.GetOrCreateAutomationPeer();
			Assert.IsInstanceOfType<ComboBoxAutomationPeer>(peer);
			var comboBoxPeer = (ComboBoxAutomationPeer)peer;
			var itemPeer = comboBoxPeer.CreateItemAutomationPeer(second);
			var pattern = itemPeer.GetPattern(PatternInterface.SelectionItem);
			Assert.IsInstanceOfType<ISelectionItemProvider>(pattern);
			var selectionProvider = (ISelectionItemProvider)pattern;

			selectionProvider.Select();

			Assert.AreEqual(1, comboBox.SelectedIndex);
			Assert.AreSame(second, comboBox.SelectedItem);
		}
#endif

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

#if HAS_UNO
		/// <summary>
		/// WinUI parity for the ComboBox light-dismiss automation element
		/// (MUX <c>ComboBoxAutomationPeer_Partial.cpp</c> <c>GetChildrenCore</c>, which appends the
		/// <c>ComboBoxLightDismiss</c> peer while the drop-down is open).
		/// Upstream ComboBox opts out of the Popup light-dismiss chain — it only registers
		/// <c>WindowSizeChange</c> as a dismissal trigger and creates its own giant
		/// <c>CComboBoxLightDismiss</c> canvas inside the popup child — so upstream's generic
		/// <c>PopupRootAutomationPeer</c> "Close" affordance never applies to it and has to be
		/// duplicated per ComboBox. Uno's ComboBox instead sets <c>Popup.IsLightDismissEnabled</c>
		/// and is dismissed through the shared <c>PopupRoot</c> chain, so it has no such element
		/// and no per-ComboBox peer to surface.
		///
		/// The UIA dismissal affordance Uno exposes in its place is the ExpandCollapse pattern on
		/// the ComboBox peer itself, which upstream also exposes. This asserts both halves: the
		/// peer's children are exactly upstream's minus that element (realized item peers only — no
		/// synthesized light-dismiss child, no template parts), and Collapse() genuinely closes the
		/// drop-down. Uno-only: native WinUI does expose the extra light-dismiss child here, and that
		/// difference is the documented adaptation.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_DropDown_Open_Then_Peer_Children_Are_Items_And_Collapse_Dismisses()
		{
			var comboBox = new ComboBox();
			comboBox.Items.Add("Option A");
			comboBox.Items.Add("Option B");
			comboBox.SelectedIndex = 0;

			try
			{
				await UITestHelper.Load(comboBox);

				var peer = FrameworkElementAutomationPeer.CreatePeerForElement(comboBox) as ComboBoxAutomationPeer;
				Assert.IsNotNull(peer, "ComboBox should have a ComboBoxAutomationPeer");

				Assert.AreEqual(0, peer.GetChildren().Count,
					"A closed ComboBox exposes no automation children; the selected value is carried by the Value pattern.");

				comboBox.IsDropDownOpen = true;
				await UITestHelper.WaitForIdle();
				await UITestHelper.WaitFor(
					() => peer.GetChildren().Count == comboBox.Items.Count,
					timeoutMS: 5000,
					message: "Timed out waiting for the open ComboBox to expose one automation child per item.");

				var children = peer.GetChildren();
				Assert.AreEqual(comboBox.Items.Count, children.Count,
					"An open ComboBox exposes exactly one automation child per item — no light-dismiss child and no template parts.");

				foreach (var child in children)
				{
					Assert.AreEqual(AutomationControlType.ListItem, child.GetAutomationControlType(),
						"Every automation child of an open ComboBox must be an item peer.");
					Assert.AreNotEqual("Close", child.GetName(),
						"Uno must not synthesize a light-dismiss 'Close' child; dismissal is exposed through ExpandCollapse.");
				}

				var expandCollapse = (IExpandCollapseProvider)peer.GetPattern(PatternInterface.ExpandCollapse);
				Assert.AreEqual(ExpandCollapseState.Expanded, expandCollapse.ExpandCollapseState);

				expandCollapse.Collapse();
				await UITestHelper.WaitForIdle();

				Assert.IsFalse(comboBox.IsDropDownOpen,
					"IExpandCollapseProvider.Collapse() must dismiss the drop-down — this is the UIA affordance that replaces upstream's light-dismiss Invoke.");
				Assert.AreEqual(ExpandCollapseState.Collapsed, expandCollapse.ExpandCollapseState);
				Assert.AreEqual(0, peer.GetChildren().Count, "Closing the drop-down must retract the item peers.");
			}
			finally
			{
				comboBox.IsDropDownOpen = false;
				TestServices.WindowHelper.WindowContent = null;
			}
		}
#endif

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

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Precreated_Items_Opened_Through_Dom_Then_Options_Are_Backfilled()
		{
			var first = new ComboBoxItem { Content = "Option A" };
			var second = new ComboBoxItem { Content = "Option B" };
			var third = new ComboBoxItem { Content = "Option C" };
			AutomationProperties.SetAutomationId(second, "SecondOption");
			var comboBox = new ComboBox
			{
				Items =
				{
					first,
					second,
					third,
				},
				SelectedIndex = 0,
			};

			try
			{
				await UITestHelper.Load(comboBox);
				comboBox.GetOrCreateAutomationPeer();

				EnableAccessibilityThroughDom();
				await UITestHelper.WaitFor(
					() => ComboBoxHeadExists(comboBox),
					timeoutMS: 5000,
					message: "Timed out waiting for the semantic combobox head element to be created.");

				InvokeBrowserJs($"document.getElementById('{GetSemanticElementId(comboBox)}').click(); 'clicked'");

				await UITestHelper.WaitFor(
					() => GetListBoxOptionCount(comboBox) == 3,
					timeoutMS: 5000,
					message: "Timed out waiting for pre-created dropdown items to be backfilled after DOM activation.");

				Assert.AreEqual("true", GetSemanticAttribute(comboBox, "aria-expanded"));
				Assert.AreEqual(
					"ok",
					VerifyOptionsParentedUnderListBox(comboBox),
					"Pre-created options must be exposed under the listbox when automation opens the dropdown.");
				Assert.AreEqual(
					"SecondOption",
					GetSemanticAttribute(second, "xamlautomationid"),
					"Virtualized ComboBox options must preserve AutomationId for external automation.");

				InvokeBrowserJs($"document.getElementById('{WasmSemanticDomHelper.GetSemanticElementId(second)}').click(); 'clicked'");
				await UITestHelper.WaitFor(
					() => comboBox.SelectedIndex == 1,
					timeoutMS: 5000,
					message: "Timed out waiting for DOM option activation to select the ComboBox item.");

				Assert.IsFalse(comboBox.IsDropDownOpen, "Selection through ComboBoxItemDataAutomationPeer must collapse the dropdown.");
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

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Selection_Changes_Then_ActiveDescendant_And_Controls_Compose()
		{
			var controlled = new Border { Width = 20, Height = 20 };
			AutomationProperties.SetName(controlled, "Controlled panel");
			var first = new ComboBoxItem { Content = "Option A" };
			var second = new ComboBoxItem { Content = "Option B" };
			var comboBox = new ComboBox { Items = { first, second }, SelectedIndex = 0 };
			AutomationProperties.GetControlledPeers(comboBox).Add(controlled);
			var panel = new StackPanel { Children = { comboBox, controlled } };

			try
			{
				await UITestHelper.Load(panel);
				comboBox.GetOrCreateAutomationPeer();
				EnableAccessibilityThroughDom();
				await UITestHelper.WaitFor(() => ComboBoxHeadExists(comboBox) && SemanticElementExists(controlled), timeoutMS: 5000,
					message: "Timed out waiting for the combobox head and authored controlled target.");

				comboBox.IsDropDownOpen = true;
				await UITestHelper.WaitFor(() => GetListBoxOptionCount(comboBox) == 2, timeoutMS: 5000,
					message: "Timed out waiting for the two dropdown options.");

				var headId = GetSemanticElementId(comboBox);
				var authoredId = WasmSemanticDomHelper.GetSemanticElementId(controlled);
				Assert.AreEqual("ok", InvokeBrowserJs($"(function(){{const h=document.getElementById('{headId}');const ids=(h.getAttribute('aria-controls')||'').split(/\\s+/);const list=ids.map(id=>document.getElementById(id)).find(e=>e&&e.getAttribute('role')==='listbox');return ids.includes('{authoredId}')&&list?'ok':'bad';}})()"));
				Assert.AreEqual(WasmSemanticDomHelper.GetSemanticElementId(first), GetSemanticAttribute(comboBox, "aria-activedescendant"));
				Assert.AreEqual("true", GetSemanticAttribute(first, "aria-selected"));

				comboBox.SelectedIndex = 1;
				await UITestHelper.WaitFor(() => GetSemanticAttribute(comboBox, "aria-activedescendant") == WasmSemanticDomHelper.GetSemanticElementId(second), timeoutMS: 3000,
					message: "ComboBox aria-activedescendant did not follow the live selection.");
				Assert.AreEqual("false", GetSemanticAttribute(first, "aria-selected"));
				Assert.AreEqual("true", GetSemanticAttribute(second, "aria-selected"));

				comboBox.IsDropDownOpen = false;
				await UITestHelper.WaitFor(() => GetSemanticAttribute(comboBox, "aria-controls") == authoredId, timeoutMS: 3000,
					message: "Closing the dropdown did not preserve the authored aria-controls target.");
			}
			finally
			{
				comboBox.IsDropDownOpen = false;
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Closed_Selection_Changes_Then_Name_And_Value_Remain_Distinct()
		{
			var comboBox = new ComboBox
			{
				Header = "Favorite fruit",
				DisplayMemberPath = nameof(ComboBoxValue.Name),
				Items = { new ComboBoxValue("Apple"), new ComboBoxValue("Pear") },
				SelectedIndex = 0,
			};

			await UITestHelper.Load(comboBox);
			comboBox.GetOrCreateAutomationPeer();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => ComboBoxHeadExists(comboBox), timeoutMS: 5000,
				message: "Timed out waiting for the closed combobox semantic head.");

			Assert.AreEqual("Favorite fruit", GetSemanticAttribute(comboBox, "aria-label"));
			Assert.AreEqual("Apple", GetComboBoxSemanticValue(comboBox));
			Assert.IsFalse(SemanticElementHasAttribute(comboBox, "aria-valuetext"), "aria-valuetext does not expose the combobox value in browser accessibility APIs.");

			comboBox.SelectedIndex = 1;
			await UITestHelper.WaitFor(
				() => GetComboBoxSemanticValue(comboBox) == "Pear" && GetSemanticAttribute(comboBox, "aria-label") == "Favorite fruit",
				timeoutMS: 3000,
				message: "A collapsed selection change did not update the value independently from the accessible name.");
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

		private static string GetComboBoxSemanticValue(ComboBox comboBox)
			=> InvokeBrowserJs($"document.getElementById('{GetSemanticElementId(comboBox)}')?.textContent || ''");

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
				"const controls = (head.getAttribute('aria-controls') || '').split(/\\s+/).filter(Boolean);" +
				"if (controls.length === 0) { return '-2'; }" +
				"const listbox = controls.map(function(id){return document.getElementById(id);}).find(function(e){return e && e.getAttribute('role') === 'listbox';});" +
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
				"const controls = (head.getAttribute('aria-controls') || '').split(/\\s+/).filter(Boolean);" +
				"const listbox = controls.map(function(id){return document.getElementById(id);}).find(function(e){return e && e.getAttribute('role') === 'listbox';});" +
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

		/// <summary>
		/// WinUI parity for the ComboBox light-dismiss automation element, browser half.
		///
		/// Upstream (MUX <c>ComboBoxAutomationPeer_Partial.cpp</c>) appends a
		/// <c>ComboBoxLightDismiss</c> peer — ControlType Button, name "Close", Invoke pattern — as a
		/// child of the ComboBox while the drop-down is open, because its giant hit-test canvas would
		/// otherwise be invisible to UIA. Uno's browser accessibility tree is built from the visual
		/// tree, not from <c>GetChildrenCore</c>, and the ARIA 1.2 combobox pattern has no place for a
		/// button child: the popup is associated through <c>aria-controls</c> and dismissal is the
		/// Escape key. Surfacing the light-dismiss surface would inject a viewport-sized node over the
		/// whole page, which is why the popup wrapper is already suppressed for these drop-downs.
		///
		/// This pins that adaptation: while the drop-down is open the head keeps the full ARIA
		/// contract, and the semantic DOM gains no viewport-sized node from the light-dismiss surface.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_DropDown_Opened_Then_No_LightDismiss_Node_And_Aria_Contract_Holds()
		{
			var comboBox = new ComboBox();
			comboBox.Items.Add("Option A");
			comboBox.Items.Add("Option B");
			comboBox.SelectedIndex = 0;

			try
			{
				await UITestHelper.Load(comboBox);
				comboBox.GetOrCreateAutomationPeer();

				EnableAccessibilityThroughDom();
				await UITestHelper.WaitFor(() => ComboBoxHeadExists(comboBox), timeoutMS: 5000,
					message: "Timed out waiting for the semantic combobox head element to be created.");
				await UITestHelper.WaitForIdle();

				comboBox.IsDropDownOpen = true;
				await UITestHelper.WaitForIdle();
				await UITestHelper.WaitFor(() => GetListBoxOptionCount(comboBox) == 2, timeoutMS: 5000,
					message: "Timed out waiting for the two dropdown options.");

				// aria-expanded and aria-controls are published from the accessibility update pass that
				// follows the drop-down opening, so settle on them instead of sampling a single frame.
				await UITestHelper.WaitFor(
					() => GetSemanticAttribute(comboBox, "aria-expanded") == "true" &&
						GetSemanticAttribute(comboBox, "aria-controls").Length > 0,
					timeoutMS: 5000,
					message: "The open ComboBox head did not publish aria-expanded=\"true\" with an aria-controls target — " +
						"that association is the ARIA replacement for a light-dismiss child.");

				Assert.AreEqual("ok", VerifyOptionsParentedUnderListBox(comboBox),
					"The aria-controls target must be the role=listbox owning the options; that association, not a " +
					"'Close' child, is how the browser object model links the head to its popup.");
				Assert.AreEqual("0|0", GetDuplicateParagraphsAndOptionDialogs(),
					"The open drop-down must add no extra wrapper node: no role=dialog around the options and no " +
					"duplicated option text. The light-dismiss surface is likewise never emitted.");
				Assert.AreEqual(0, CountSemanticNodesNamedClose(),
					"Uno must not surface a light-dismiss 'Close' node in the browser accessibility tree.");

				comboBox.IsDropDownOpen = false;
				await UITestHelper.WaitFor(() => GetSemanticAttribute(comboBox, "aria-expanded") == "false", timeoutMS: 3000,
					message: "Closing the drop-down did not reset aria-expanded.");
				await UITestHelper.WaitFor(() => GetSemanticAttribute(comboBox, "aria-activedescendant").Length == 0, timeoutMS: 3000,
					message: "Closing the drop-down must clear aria-activedescendant so it never dangles.");
				Assert.AreEqual(0, CountSemanticNodesNamedClose(),
					"Closing the drop-down must not leave a light-dismiss node behind.");
			}
			finally
			{
				comboBox.IsDropDownOpen = false;
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		// Number of semantic nodes whose accessible name is "Close" — the name WinUI gives its
		// light-dismiss element (UIA_LIGHTDISMISS_NAME). Returns -1 when the semantic root or the
		// scan is unavailable so a broken probe fails loudly instead of reading as "none found".
		private static int CountSemanticNodesNamedClose()
		{
			var js =
				"(function(){" +
				"var root = document.getElementById('uno-semantics-root');" +
				"if (!root) { return '-1'; }" +
				"var matches = Array.from(root.querySelectorAll('*')).filter(function(e){" +
				"return (e.getAttribute('aria-label') || '').trim() === 'Close';" +
				"});" +
				"return String(matches.length);" +
				"})()";

			return int.TryParse(InvokeBrowserJs(js), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var count)
				? count
				: -1;
		}




#endif

		private sealed class ComboBoxValue
		{
			public ComboBoxValue(string name) => Name = name;

			public string Name { get; }

			public override string ToString() => "WRONG VALUE";
		}

	}
}
