using System;
using System.Collections.Generic;
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

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Selection_Changes_Then_Item_Data_Peers_Raise_IsSelected()
		{
#if HAS_UNO
			var red = new ComboBoxItem { Content = "Red" };
			var green = new ComboBoxItem { Content = "Green" };
			var comboBox = new ComboBox { Items = { red, green }, SelectedIndex = 0 };
			var previousListener = AutomationPeer.TestAutomationPeerListener;
			try
			{
				await UITestHelper.Load(comboBox);
				comboBox.IsDropDownOpen = true;
				await UITestHelper.WaitForIdle();
				var ownerPeer = (ComboBoxAutomationPeer)comboBox.GetOrCreateAutomationPeer();
				var redPeer = ownerPeer.CreateItemAutomationPeer(red);
				var greenPeer = ownerPeer.CreateItemAutomationPeer(green);
				var listener = new SelectionPropertyListener();
				AutomationPeer.TestAutomationPeerListener = listener;

				comboBox.SelectedIndex = 1;

				Assert.AreEqual(2, listener.Changes.Count);
				Assert.AreEqual(1, listener.Changes.FindAll(change => change.Peer == redPeer && change.OldValue && !change.NewValue).Count);
				Assert.AreEqual(1, listener.Changes.FindAll(change => change.Peer == greenPeer && !change.OldValue && change.NewValue).Count);
			}
			finally
			{
				AutomationPeer.TestAutomationPeerListener = previousListener;
				comboBox.IsDropDownOpen = false;
				TestServices.WindowHelper.WindowContent = null;
			}
#endif
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public void When_Unparented_Item_Selection_Changes_Then_No_Owner_Event_Is_Fabricated()
		{
#if HAS_UNO
			var previousListener = AutomationPeer.TestAutomationPeerListener;
			var listener = new SelectionPropertyListener();
			try
			{
				AutomationPeer.TestAutomationPeerListener = listener;
				new ComboBoxItem().IsSelected = true;
				Assert.AreEqual(0, listener.Changes.Count);
			}
			finally
			{
				AutomationPeer.TestAutomationPeerListener = previousListener;
			}
#endif
		}

#if HAS_UNO
		private sealed class SelectionPropertyListener : IAutomationPeerListener
		{
			public List<(AutomationPeer Peer, bool OldValue, bool NewValue)> Changes { get; } = new();
			public bool ListenerExistsHelper(AutomationEvents eventId) => true;
			public void NotifyPropertyChangedEvent(AutomationPeer peer, AutomationProperty property, object oldValue, object newValue)
			{
				if (property == SelectionItemPatternIdentifiers.IsSelectedProperty)
				{
					Changes.Add((peer, (bool)oldValue, (bool)newValue));
				}
			}
			public void OnAutomationEvent(AutomationPeer peer, AutomationEvents eventId) { }
			public void NotifyAutomationEvent(AutomationPeer peer, AutomationEvents eventId) { }
			public void NotifyStructureChangedEvent(AutomationPeer peer, AutomationStructureChangeType changeType, AutomationPeer child) { }
			public void NotifyInvalidatePeer(AutomationPeer peer) { }
			public void NotifyNotificationEvent(AutomationPeer peer, AutomationNotificationKind kind, AutomationNotificationProcessing processing, string displayString, string activityId) { }
			public void NotifyTextEditTextChangedEvent(AutomationPeer peer, AutomationTextEditChangeType changeType, IReadOnlyList<string> changedData) { }
		}
#endif

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Options_Realized_Then_AutomationId_And_Selection_Stay_In_Sync()
		{
#if __SKIA__
			var red = new ComboBoxItem { Content = "Red" };
			var green = new ComboBoxItem { Content = "Green" };
			AutomationProperties.SetAutomationId(red, "red-option");
			AutomationProperties.SetAutomationId(green, "green-option");
			var comboBox = new ComboBox { Header = "Favorite color", Items = { red, green }, SelectedIndex = 0 };

			try
			{
				await UITestHelper.Load(comboBox);
				EnableAccessibilityThroughDom();
				await UITestHelper.WaitFor(() => ComboBoxHeadExists(comboBox), timeoutMS: 5000);

				comboBox.IsDropDownOpen = true;
				await UITestHelper.WaitFor(
					() => GetSemanticAttribute(red, "aria-selected") == "true" &&
						GetSemanticAttribute(green, "aria-selected") == "false",
					timeoutMS: 5000,
					message: "Realized options must publish their initial selection state.");
				Assert.AreEqual("red-option", GetSemanticAttribute(red, "xamlautomationid"));
				Assert.AreEqual("green-option", GetSemanticAttribute(green, "xamlautomationid"));

				Assert.AreEqual("clicked", InvokeBrowserJs("""
					(function() {
						const option = document.querySelector('#uno-semantics-root [xamlautomationid="green-option"]');
						if (!option) { return 'missing'; }
						option.click();
						return 'clicked';
					})()
					"""));
				await UITestHelper.WaitFor(
					() => comboBox.SelectedIndex == 1 && !comboBox.IsDropDownOpen &&
						GetComboBoxSemanticValue(comboBox) == "Green",
					timeoutMS: 5000,
					message: "ID-based option activation must select Green and update the collapsed value.");

				comboBox.IsDropDownOpen = true;
				await UITestHelper.WaitFor(
					() => GetSemanticAttribute(red, "aria-selected") == "false" &&
						GetSemanticAttribute(green, "aria-selected") == "true",
					timeoutMS: 5000,
					message: "Reopening must preserve the old and new selected states.");
				Assert.AreEqual("green-option", GetSemanticAttribute(green, "xamlautomationid"));

				comboBox.SelectedIndex = 0;
				await UITestHelper.WaitFor(
					() => GetSemanticAttribute(red, "aria-selected") == "true" &&
						GetSemanticAttribute(green, "aria-selected") == "false" &&
						GetSemanticAttribute(comboBox, "aria-activedescendant") == WasmSemanticDomHelper.GetSemanticElementId(red),
					timeoutMS: 5000,
					message: "Programmatic selection must update both realized options and the active descendant.");

				comboBox.SelectedIndex = -1;
				await UITestHelper.WaitFor(
					() => GetSemanticAttribute(red, "aria-selected") == "false" &&
						GetSemanticAttribute(green, "aria-selected") == "false" &&
						!SemanticElementHasAttribute(comboBox, "aria-activedescendant"),
					timeoutMS: 5000,
					message: "Clearing selection must not leave a stale active descendant.");
			}
			finally
			{
				comboBox.IsDropDownOpen = false;
				TestServices.WindowHelper.WindowContent = null;
			}
#endif
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Removed_Item_Raises_Selection_Event_Then_Owner_Is_Not_Selected()
		{
#if __SKIA__
			var comboBox = new ComboBox { Header = "Choice", Items = { "Retained", "Removed" }, SelectedIndex = 0 };
			try
			{
				await UITestHelper.Load(comboBox);
				EnableAccessibilityThroughDom();
				await UITestHelper.WaitFor(() => ComboBoxHeadExists(comboBox), timeoutMS: 5000);
				var ownerPeer = (ComboBoxAutomationPeer)comboBox.GetOrCreateAutomationPeer();
				var removedPeer = ownerPeer.CreateItemAutomationPeer("Removed");
				comboBox.Items.Remove("Removed");
				Assert.IsNull(removedPeer.GetContainer());

				removedPeer.RaisePropertyChangedEvent(SelectionItemPatternIdentifiers.IsSelectedProperty, false, true);
				await UITestHelper.WaitForIdle();

				Assert.IsFalse(SemanticElementHasAttribute(comboBox, "aria-selected"));
				Assert.IsFalse(SemanticElementHasAttribute(comboBox, "aria-activedescendant"));
				Assert.AreEqual("Retained", GetComboBoxSemanticValue(comboBox));
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
			}
#endif
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		[DataRow(false)]
		[DataRow(true)]
		public async Task When_Closed_Selection_Changes_Then_Name_And_Value_Remain_Distinct(bool useLabeledBy)
		{
#if __SKIA__
			var label = new TextBlock { Text = "Favorite fruit" };
			var comboBox = new ComboBox
			{
				Header = "Favorite fruit",
				DisplayMemberPath = nameof(ComboBoxValue.Name),
				Items = { new ComboBoxValue("Apple"), new ComboBoxValue("Pear") },
				SelectedIndex = 0,
			};
			if (useLabeledBy)
			{
				AutomationProperties.SetLabeledBy(comboBox, label);
			}

			try
			{
				await UITestHelper.Load(new StackPanel { Children = { label, comboBox } });
				var peer = FrameworkElementAutomationPeer.CreatePeerForElement(comboBox);
				Assert.IsNull(peer.GetPattern(PatternInterface.Value), "A select-only ComboBox must not advertise WinUI's editable Value pattern.");
				EnableAccessibilityThroughDom();
				await UITestHelper.WaitFor(() => GetComboBoxSemanticValue(comboBox) == "Apple", timeoutMS: 5000);

				var nameAttribute = useLabeledBy ? "aria-labelledby" : "aria-label";
				var nameValue = useLabeledBy ? WasmSemanticDomHelper.GetSemanticElementId(label) : "Favorite fruit";
				await UITestHelper.WaitFor(() => GetSemanticAttribute(comboBox, nameAttribute) == nameValue, timeoutMS: 5000);
				Assert.IsFalse(SemanticElementHasAttribute(comboBox, "aria-valuetext"), "A combobox is not an ARIA range widget.");

				comboBox.SelectedIndex = 1;
				await UITestHelper.WaitFor(() => GetComboBoxSemanticValue(comboBox) == "Pear", timeoutMS: 5000);
				Assert.AreEqual(nameValue, GetSemanticAttribute(comboBox, nameAttribute));
				Assert.IsFalse(SemanticElementHasAttribute(comboBox, "aria-activedescendant"), "A closed dropdown has no realized active option.");
				Assert.IsFalse(SemanticElementHasAttribute(comboBox, "aria-selected"), "An item's selected state must not be applied to its ComboBox owner.");

				comboBox.SelectedIndex = -1;
				await UITestHelper.WaitFor(() => GetComboBoxSemanticValue(comboBox) == string.Empty, timeoutMS: 5000);
				Assert.AreEqual(nameValue, GetSemanticAttribute(comboBox, nameAttribute));
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
			}
#endif
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_DropDown_And_Authored_ControlledPeers_Change_Then_Controls_Compose()
		{
#if __SKIA__
			var firstTarget = new TextBlock { Text = "First controlled target" };
			var secondTarget = new TextBlock { Text = "Second controlled target" };
			var comboBox = new ComboBox { Header = "Choice", Items = { "One", "Two" }, SelectedIndex = 0 };
			comboBox.SetValue(AutomationProperties.ControlledPeersProperty, new List<UIElement> { firstTarget });
			var panel = new StackPanel { Children = { comboBox, firstTarget, secondTarget } };

			try
			{
				await UITestHelper.Load(panel);
				EnableAccessibilityThroughDom();
				var firstId = WasmSemanticDomHelper.GetSemanticElementId(firstTarget);
				var secondId = WasmSemanticDomHelper.GetSemanticElementId(secondTarget);
				await UITestHelper.WaitFor(() => GetSemanticAttribute(comboBox, "aria-controls") == firstId, timeoutMS: 5000);

				comboBox.IsDropDownOpen = true;
				await UITestHelper.WaitFor(() => GetListBoxOptionCount(comboBox) == 2, timeoutMS: 5000);
				var controls = GetSemanticAttribute(comboBox, "aria-controls").Split(' ', StringSplitOptions.RemoveEmptyEntries);
				Assert.AreEqual(2, controls.Length);
				CollectionAssert.Contains(controls, firstId);
				var popupId = controls[0] == firstId ? controls[1] : controls[0];

				comboBox.SetValue(AutomationProperties.ControlledPeersProperty, new List<UIElement> { secondTarget });
				await UITestHelper.WaitFor(
					() => GetSemanticAttribute(comboBox, "aria-controls") == $"{secondId} {popupId}",
					timeoutMS: 5000,
					message: "Authored relationship updates must not erase the generated popup relationship.");

				panel.Children.Remove(secondTarget);
				await UITestHelper.WaitFor(() => GetSemanticAttribute(comboBox, "aria-controls") == popupId, timeoutMS: 5000);
				panel.Children.Add(secondTarget);
				await UITestHelper.WaitFor(() => GetSemanticAttribute(comboBox, "aria-controls") == $"{secondId} {popupId}", timeoutMS: 5000);

				comboBox.IsDropDownOpen = false;
				await UITestHelper.WaitFor(() => GetSemanticAttribute(comboBox, "aria-controls") == secondId, timeoutMS: 5000);
				Assert.IsFalse(SemanticElementHasAttribute(comboBox, "aria-activedescendant"));

				comboBox.IsDropDownOpen = true;
				await UITestHelper.WaitFor(() => GetListBoxOptionCount(comboBox) == 2, timeoutMS: 5000);
				comboBox.ClearValue(AutomationProperties.ControlledPeersProperty);
				await UITestHelper.WaitFor(
					() => GetSemanticAttribute(comboBox, "aria-controls") == popupId,
					timeoutMS: 5000,
					message: "Clearing authored peers must preserve the open popup.");
				comboBox.IsDropDownOpen = false;
				await UITestHelper.WaitFor(() => !SemanticElementHasAttribute(comboBox, "aria-controls"), timeoutMS: 5000);
			}
			finally
			{
				comboBox.IsDropDownOpen = false;
				TestServices.WindowHelper.WindowContent = null;
			}
#endif
		}

#if __SKIA__
		private sealed record ComboBoxValue(string Name);

		private static string GetComboBoxSemanticValue(ComboBox comboBox)
			=> InvokeBrowserJs($"document.getElementById('{GetSemanticElementId(comboBox)}')?.textContent || ''");

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
				"const controls = (head.getAttribute('aria-controls') || '').split(/\\s+/).filter(Boolean);" +
				"if (!controls.length) { return '-2'; }" +
				"const listbox = controls.map(id => document.getElementById(id)).find(e => e && e.getAttribute('role') === 'listbox');" +
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
				"const listbox = controls.map(id => document.getElementById(id)).find(e => e && e.getAttribute('role') === 'listbox');" +
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
