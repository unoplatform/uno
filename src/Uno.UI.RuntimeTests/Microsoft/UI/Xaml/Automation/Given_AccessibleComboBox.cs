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
using Uno.UI.Runtime;
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
#if HAS_UNO
		[TestCleanup]
		public void DisableAccessibilityAfterTest() => DisableAccessibility();
#endif

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

		/// <summary>
		/// WinUI gives the items of a ComboBox a ComboBoxItemDataAutomationPeer, whose Select selects
		/// the item and collapses the ComboBox (ComboBoxItemDataAutomationPeer::SelectImpl).
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Item_Selected_Through_Peer_Then_ComboBox_Collapses()
		{
			var comboBox = new ComboBox();
			comboBox.Items.Add("Option A");
			comboBox.Items.Add("Option B");
			comboBox.Items.Add("Option C");

			try
			{
				await UITestHelper.Load(comboBox);

				comboBox.IsDropDownOpen = true;
				await TestServices.WindowHelper.WaitForIdle();

				var peer = (ComboBoxAutomationPeer)FrameworkElementAutomationPeer.CreatePeerForElement(comboBox);
				var itemPeer = peer.CreateItemAutomationPeer(comboBox.Items[2]);
				Assert.IsInstanceOfType(itemPeer, typeof(ComboBoxItemDataAutomationPeer));

				if (itemPeer.GetPattern(PatternInterface.SelectionItem) is not ISelectionItemProvider selectionItemProvider)
				{
					Assert.Fail("A ComboBox item should support ISelectionItemProvider");
					return;
				}

				selectionItemProvider.Select();
				await TestServices.WindowHelper.WaitForIdle();

				Assert.AreEqual(2, comboBox.SelectedIndex, "Select should select the item");
				Assert.IsFalse(comboBox.IsDropDownOpen, "Select should collapse the ComboBox");
			}
			finally
			{
				comboBox.IsDropDownOpen = false;
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		/// <summary>
		/// As in WinUI (ItemAutomationPeer::GetNameCore), an item peer without a container, or whose container
		/// provides no name, is named after the item itself: a templated item has no text of its own.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Item_Is_Templated_Then_Item_Peer_Name_Is_Item_Text()
		{
			var comboBox = new ComboBox
			{
				ItemTemplate = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(
					"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><StackPanel><TextBlock Text='Row'/></StackPanel></DataTemplate>"),
				ItemsSource = new[] { new NamedItem("Alice") },
			};

			await UITestHelper.Load(comboBox);

			var peer = (ComboBoxAutomationPeer)FrameworkElementAutomationPeer.CreatePeerForElement(comboBox);
			Assert.AreEqual("Alice", peer.CreateItemAutomationPeer(comboBox.Items[0]).GetName());
		}

		private sealed class NamedItem
		{
			private readonly string _name;

			public NamedItem(string name) => _name = name;

			public override string ToString() => _name;
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

		/// <summary>
		/// The XAML ComboBox owns the keyboard, as in WinUI: a key pressed on the semantic head must be
		/// handled once. Handling it in the semantic layer as well reopened or closed the dropdown within
		/// the same key press, so Enter stopped opening the dropdown once its items had been created.
		/// ComboBoxItem items are used because they take the focus as soon as the dropdown opens.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Keys_Pressed_On_Head_Then_Handled_Once()
		{
			var comboBox = await LoadAccessibleComboBox(itemsAsContainers: true);

			try
			{
				foreach (var (openKey, expectedIndex) in new[] { ("Enter", 1), (" ", 2), ("F4", 2) })
				{
					await FocusHead(comboBox);

					PressKeyOnFocusedSemanticElement(openKey);
					await UITestHelper.WaitFor(() => GetListBoxOptionCount(comboBox) == 3, timeoutMS: 5000, message: $"Timed out waiting for '{openKey}' to open the dropdown.");
					await UITestHelper.WaitForIdle();
					Assert.IsTrue(comboBox.IsDropDownOpen, $"'{openKey}' on the head must leave the dropdown open.");

					if (expectedIndex != comboBox.SelectedIndex)
					{
						PressKeyOnFocusedSemanticElement("ArrowDown");
						await UITestHelper.WaitForIdle();
					}

					PressKeyOnFocusedSemanticElement("Enter");
					await UITestHelper.WaitFor(() => !comboBox.IsDropDownOpen, timeoutMS: 5000, message: "Timed out waiting for Enter to close the dropdown.");
					Assert.AreEqual(expectedIndex, comboBox.SelectedIndex, "Enter must commit the focused option.");
				}
			}
			finally
			{
				comboBox.IsDropDownOpen = false;
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		/// <summary>
		/// WAI-ARIA combobox pattern: the head keeps DOM focus while the dropdown is open and follows the
		/// XAML-focused item through aria-activedescendant. As in WinUI, moving through the items does not
		/// select them: aria-selected only changes once the focused item is committed.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_DropDown_Navigated_Then_Head_Tracks_Focused_Option()
		{
			var comboBox = await LoadAccessibleComboBox();
			comboBox.SelectedIndex = 0;

			try
			{
				await FocusHead(comboBox);
				Assert.AreEqual("Option A", GetSemanticText(comboBox), "A closed combobox must expose its selected item as its value.");
				var dialogCount = GetDialogCount();

				PressKeyOnFocusedSemanticElement("Enter");
				await UITestHelper.WaitFor(() => GetListBoxOptionCount(comboBox) == 3, timeoutMS: 5000, message: "Timed out waiting for the dropdown options.");
				Assert.AreEqual(dialogCount, GetDialogCount(), "An open dropdown must not add a dialog: its options live in the listbox.");

				var first = (ComboBoxItem)comboBox.ContainerFromIndex(0);
				var second = (ComboBoxItem)comboBox.ContainerFromIndex(1);
				await UITestHelper.WaitFor(
					() => GetSemanticAttribute(comboBox, "aria-activedescendant") == GetSemanticElementId(first),
					timeoutMS: 5000,
					message: "Timed out waiting for the head to reference the selected option when the dropdown opens.");
				Assert.AreEqual(GetSemanticElementId(comboBox), GetActiveElementId(), "DOM focus must stay on the combobox head while its dropdown is open.");
				Assert.AreEqual("true", GetSemanticAttribute(first, "aria-selected"), "The selected option must report aria-selected=true.");
				Assert.AreEqual("false", GetSemanticAttribute(second, "aria-selected"), "The other options must report aria-selected=false.");

				PressKeyOnFocusedSemanticElement("ArrowDown");
				await UITestHelper.WaitFor(
					() => GetSemanticAttribute(comboBox, "aria-activedescendant") == GetSemanticElementId(second),
					timeoutMS: 5000,
					message: "Timed out waiting for the head to reference the option focused with ArrowDown.");
				Assert.AreEqual(GetSemanticElementId(comboBox), GetActiveElementId(), "DOM focus must stay on the combobox head while moving through the options.");
				Assert.AreEqual(0, comboBox.SelectedIndex, "Moving through the options must not select them before they are committed.");
				Assert.AreEqual("false", GetSemanticAttribute(second, "aria-selected"), "A focused option is not selected until it is committed.");

				PressKeyOnFocusedSemanticElement("Enter");
				await UITestHelper.WaitFor(() => !comboBox.IsDropDownOpen, timeoutMS: 5000, message: "Timed out waiting for Enter to close the dropdown.");
				await UITestHelper.WaitForIdle();

				Assert.AreEqual(1, comboBox.SelectedIndex, "Enter must commit the focused option.");
				Assert.AreEqual("Option B", GetSemanticText(comboBox), "The head must expose the committed item as its value.");
				Assert.IsFalse(SemanticElementHasAttribute(comboBox, "aria-activedescendant"), "A closed combobox must not reference an option.");
				Assert.AreEqual(GetSemanticElementId(comboBox), GetActiveElementId(), "DOM focus must still be on the combobox head once the dropdown has closed.");
			}
			finally
			{
				comboBox.IsDropDownOpen = false;
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		/// <summary>
		/// A screen reader activates an option by clicking its semantic node. As in WinUI
		/// (ComboBoxItemDataAutomationPeer::SelectImpl), that selects the item and collapses the ComboBox.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Active_Option_Changes_Before_Deferred_Update_Then_Latest_Wins()
		{
			var comboBox = await LoadAccessibleComboBox();
			comboBox.SelectedIndex = 0;

			try
			{
				await FocusHead(comboBox);
				PressKeyOnFocusedSemanticElement("Enter");
				await UITestHelper.WaitFor(() => GetListBoxOptionCount(comboBox) == 3, timeoutMS: 5000, message: "Timed out waiting for the dropdown options.");

				var first = (ComboBoxItem)comboBox.ContainerFromIndex(0);
				var second = (ComboBoxItem)comboBox.ContainerFromIndex(1);
				var third = (ComboBoxItem)comboBox.ContainerFromIndex(2);
				await UITestHelper.WaitFor(
					() => GetSemanticAttribute(comboBox, "aria-activedescendant") == GetSemanticElementId(first),
					timeoutMS: 5000,
					message: "Timed out waiting for the head to reference the selected option when the dropdown opens.");

				// The second option is requested while it is not in the DOM yet, so that request is deferred;
				// the third one is requested right after and is applied at once.
				RequestActiveDescendantWhileMissingThenAnother(comboBox, second, third);
				await Task.Delay(250);
				await UITestHelper.WaitForIdle();

				Assert.AreEqual(GetSemanticElementId(third), GetSemanticAttribute(comboBox, "aria-activedescendant"), "A deferred request must not overwrite a newer one.");
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
		public async Task When_Option_Clicked_Then_Selected_And_DropDown_Closed()
		{
			var comboBox = await LoadAccessibleComboBox();

			try
			{
				comboBox.IsDropDownOpen = true;
				await UITestHelper.WaitFor(() => GetListBoxOptionCount(comboBox) == 3, timeoutMS: 5000, message: "Timed out waiting for the dropdown options.");

				var third = (ComboBoxItem)comboBox.ContainerFromIndex(2);
				await UITestHelper.WaitFor(() => SemanticElementExists(third), timeoutMS: 5000, message: "Timed out waiting for the third option.");
				InvokeBrowserJs($"(function(){{document.getElementById('{GetSemanticElementId(third)}').click(); return 'ok';}})()");

				await UITestHelper.WaitFor(() => !comboBox.IsDropDownOpen, timeoutMS: 5000, message: "Timed out waiting for the clicked option to close the dropdown.");
				Assert.AreEqual(2, comboBox.SelectedIndex, "Clicking an option must select it.");
			}
			finally
			{
				comboBox.IsDropDownOpen = false;
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		/// <summary>
		/// Up/Down change the selection of a closed ComboBox (WinUI ComboBox::MainKeyDown). No option exists
		/// in the DOM at that point, so the change has to be exposed as the value of the head.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Selection_Changes_While_Closed_Then_Head_Value_Updated()
		{
			var comboBox = await LoadAccessibleComboBox();

			try
			{
				Assert.AreEqual(string.Empty, GetSemanticText(comboBox), "A combobox without selection has no value.");

				await FocusHead(comboBox);

				PressKeyOnFocusedSemanticElement("ArrowDown");
				await UITestHelper.WaitFor(() => GetSemanticText(comboBox) == "Option A", timeoutMS: 5000, message: "Timed out waiting for the head to expose the item selected with ArrowDown.");

				PressKeyOnFocusedSemanticElement("ArrowDown");
				await UITestHelper.WaitFor(() => GetSemanticText(comboBox) == "Option B", timeoutMS: 5000, message: "Timed out waiting for the head to expose the next item selected with ArrowDown.");

				Assert.IsFalse(comboBox.IsDropDownOpen, "Up/Down must not open the dropdown.");
				Assert.IsFalse(SemanticElementHasAttribute(comboBox, "aria-activedescendant"), "A closed combobox must not reference an option.");
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		private static async Task<ComboBox> LoadAccessibleComboBox(bool itemsAsContainers = false)
		{
			var comboBox = new ComboBox();
			foreach (var option in new[] { "Option A", "Option B", "Option C" })
			{
				comboBox.Items.Add(itemsAsContainers ? new ComboBoxItem { Content = option } : option);
			}

			await UITestHelper.Load(comboBox);
			comboBox.GetOrCreateAutomationPeer();

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => ComboBoxHeadExists(comboBox), timeoutMS: 5000, message: "Timed out waiting for the semantic combobox head element to be created.");
			await UITestHelper.WaitForIdle();

			return comboBox;
		}

		// Keys are delivered to the DOM-focused element, which has to be the combobox head.
		private static async Task FocusHead(ComboBox comboBox)
		{
			comboBox.Focus(FocusState.Keyboard);
			await UITestHelper.WaitFor(
				() => GetActiveElementId() == GetSemanticElementId(comboBox),
				timeoutMS: 5000,
				message: "Timed out waiting for the combobox head to receive DOM focus.");
			await UITestHelper.WaitForIdle();
		}

		private static void RequestActiveDescendantWhileMissingThenAnother(ComboBox comboBox, ComboBoxItem missing, ComboBoxItem latest)
		{
			var prefix = "uno-semantics-";
			var containerHandle = GetSemanticElementId(comboBox).Substring(prefix.Length);
			var missingId = GetSemanticElementId(missing);
			var latestHandle = GetSemanticElementId(latest).Substring(prefix.Length);
			InvokeBrowserJs(
				"(function(){" +
				"const semantics = globalThis.Uno.UI.Runtime.SemanticElements;" +
				$"const option = document.getElementById('{missingId}');" +
				"option.id = '';" +
				$"semantics.updateActiveDescendant({containerHandle}, {missingId.Substring(prefix.Length)});" +
				$"option.id = '{missingId}';" +
				$"semantics.updateActiveDescendant({containerHandle}, {latestHandle});" +
				"return 'ok';" +
				"})()");
		}

		private static string GetDialogCount()
			=> InvokeBrowserJs("(function(){return String(document.querySelectorAll('[role=dialog]').length);})()");

		// The text nodes of the element itself, without the text of its child elements.
		private static string GetSemanticText(UIElement element)
			=> InvokeBrowserJs($"(function(){{const e = document.getElementById('{GetSemanticElementId(element)}'); if (!e) {{ return ''; }} return Array.from(e.childNodes).filter(function(n){{return n.nodeType === 3;}}).map(function(n){{return n.data;}}).join(''); }})()");

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




#endif

	}
}
