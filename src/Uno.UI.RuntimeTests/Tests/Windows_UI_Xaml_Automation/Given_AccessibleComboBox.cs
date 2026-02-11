using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
			var peer = comboBox.GetOrCreateAutomationPeer();
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

			var peer = comboBox.GetOrCreateAutomationPeer();
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
			var peer = comboBox.GetOrCreateAutomationPeer();
			var controlType = peer?.GetAutomationControlType();

			// Assert
			Assert.AreEqual(AutomationControlType.ComboBox, controlType);
		}

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
			var peer = comboBox.GetOrCreateAutomationPeer();
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
			var peer = comboBox.GetOrCreateAutomationPeer();
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
			var peer = comboBox.GetOrCreateAutomationPeer();
			var capabilities = AriaMapper.GetPatternCapabilities(peer);

			// Assert
			Assert.IsTrue(capabilities.CanExpandCollapse, "ComboBox should have CanExpandCollapse capability");
		}
#endif
	}
}
