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
	/// Runtime tests for accessible checkbox behavior.
	/// Tests automation peer properties, toggle pattern, and ARIA attribute mapping.
	/// </summary>
	[TestClass]
	public class Given_AccessibleCheckBox
	{
		/// <summary>
		/// T036: Verifies that a focused checkbox exposes its checked state
		/// via the IToggleProvider pattern. This maps to aria-checked.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Checkbox_Focused_Then_Checked_State_Exposed()
		{
			// Arrange
			var checkBox = new CheckBox
			{
				Content = "Accept terms",
				IsChecked = true
			};

			await UITestHelper.Load(checkBox);

			// Act
			var peer = checkBox.GetOrCreateAutomationPeer();
			var toggleProvider = peer?.GetPattern(PatternInterface.Toggle) as IToggleProvider;

			// Assert
			Assert.IsNotNull(peer, "CheckBox should have an automation peer");
			Assert.IsNotNull(toggleProvider, "CheckBox should support IToggleProvider");
			Assert.AreEqual(ToggleState.On, toggleProvider.ToggleState, "Checked checkbox should report ToggleState.On");
		}

		/// <summary>
		/// T037: Verifies that toggling a checkbox via automation peer changes its state.
		/// This tests the critical path for screen reader checkbox activation via Space.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Space_Pressed_Then_Checkbox_Toggles()
		{
			// Arrange
			var checkBox = new CheckBox
			{
				Content = "Enable notifications",
				IsChecked = false
			};

			await UITestHelper.Load(checkBox);

			var peer = checkBox.GetOrCreateAutomationPeer();
			var toggleProvider = peer?.GetPattern(PatternInterface.Toggle) as IToggleProvider;

			Assert.IsNotNull(toggleProvider, "CheckBox should support IToggleProvider");
			Assert.AreEqual(ToggleState.Off, toggleProvider.ToggleState, "Initial state should be Off");

			// Act - Simulate Space press toggling the checkbox via automation peer
			toggleProvider.Toggle();

			// Assert
			Assert.AreEqual(ToggleState.On, toggleProvider.ToggleState, "After toggle, state should be On");
			Assert.IsTrue(checkBox.IsChecked == true, "CheckBox.IsChecked should be true after toggle");
		}

		/// <summary>
		/// T038: Verifies that a tri-state checkbox correctly reports the mixed/indeterminate state.
		/// This maps to aria-checked="mixed".
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_TriState_Then_AriaChecked_IsMixed()
		{
			// Arrange
			var checkBox = new CheckBox
			{
				Content = "Select all",
				IsThreeState = true,
				IsChecked = null // Indeterminate
			};

			await UITestHelper.Load(checkBox);

			// Act
			var peer = checkBox.GetOrCreateAutomationPeer();
			var toggleProvider = peer?.GetPattern(PatternInterface.Toggle) as IToggleProvider;

			// Assert
			Assert.IsNotNull(toggleProvider, "CheckBox should support IToggleProvider");
			Assert.AreEqual(ToggleState.Indeterminate, toggleProvider.ToggleState, "Indeterminate checkbox should report ToggleState.Indeterminate");

#if HAS_UNO
			// Verify AriaMapper produces correct attribute
			var attributes = AriaMapper.GetAriaAttributes(peer);
			Assert.AreEqual("mixed", attributes.Checked, "AriaMapper should report Checked='mixed' for indeterminate checkbox");
#endif
		}

		/// <summary>
		/// Verifies that checkbox automation peer has correct control type.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_CheckBox_Created_Then_Has_CheckBox_ControlType()
		{
			// Arrange
			var checkBox = new CheckBox { Content = "Test" };
			await UITestHelper.Load(checkBox);

			// Act
			var peer = checkBox.GetOrCreateAutomationPeer();
			var controlType = peer?.GetAutomationControlType();

			// Assert
			Assert.AreEqual(AutomationControlType.CheckBox, controlType);
		}

		/// <summary>
		/// Verifies that unchecked checkbox reports correct toggle state.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_CheckBox_Is_Unchecked_Then_ToggleState_Is_Off()
		{
			// Arrange
			var checkBox = new CheckBox
			{
				Content = "Test",
				IsChecked = false
			};
			await UITestHelper.Load(checkBox);

			// Act
			var peer = checkBox.GetOrCreateAutomationPeer();
			var toggleProvider = peer?.GetPattern(PatternInterface.Toggle) as IToggleProvider;

			// Assert
			Assert.IsNotNull(toggleProvider);
			Assert.AreEqual(ToggleState.Off, toggleProvider.ToggleState);
		}

		/// <summary>
		/// Verifies that checkbox with AutomationProperties.Name exposes correct name.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_CheckBox_Has_AutomationName_Then_Name_Is_Exposed()
		{
			// Arrange
			var checkBox = new CheckBox { Content = "Agree" };
			AutomationProperties.SetName(checkBox, "I agree to the terms");

			await UITestHelper.Load(checkBox);

			// Act
			var peer = checkBox.GetOrCreateAutomationPeer();
			var name = peer?.GetName();

			// Assert
			Assert.AreEqual("I agree to the terms", name, "Automation name should be exposed");
		}

		/// <summary>
		/// Verifies that toggling a tri-state checkbox cycles through all states.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_TriState_Toggle_Then_Cycles_Through_States()
		{
			// Arrange
			var checkBox = new CheckBox
			{
				Content = "Select",
				IsThreeState = true,
				IsChecked = false // Start unchecked
			};
			await UITestHelper.Load(checkBox);

			var peer = checkBox.GetOrCreateAutomationPeer();
			var toggleProvider = peer?.GetPattern(PatternInterface.Toggle) as IToggleProvider;
			Assert.IsNotNull(toggleProvider);

			// Act & Assert - Cycle: Off -> On -> Indeterminate -> Off
			Assert.AreEqual(ToggleState.Off, toggleProvider.ToggleState, "Initial state should be Off");

			toggleProvider.Toggle();
			Assert.AreEqual(ToggleState.On, toggleProvider.ToggleState, "After first toggle should be On");

			toggleProvider.Toggle();
			Assert.AreEqual(ToggleState.Indeterminate, toggleProvider.ToggleState, "After second toggle should be Indeterminate");

			toggleProvider.Toggle();
			Assert.AreEqual(ToggleState.Off, toggleProvider.ToggleState, "After third toggle should be Off");
		}

#if HAS_UNO
		/// <summary>
		/// Verifies that AriaMapper correctly identifies checkbox semantic element type.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_CheckBox_Mapped_Then_SemanticElementType_Is_Checkbox()
		{
			// Arrange
			var checkBox = new CheckBox { Content = "Test" };
			await UITestHelper.Load(checkBox);

			// Act
			var peer = checkBox.GetOrCreateAutomationPeer();
			var elementType = AriaMapper.GetSemanticElementType(peer);

			// Assert
			Assert.AreEqual(SemanticElementType.Checkbox, elementType);
		}

		/// <summary>
		/// Verifies that AriaMapper produces correct ARIA attributes for checked checkbox.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_CheckBox_Checked_Then_AriaChecked_Is_True()
		{
			// Arrange
			var checkBox = new CheckBox
			{
				Content = "Test",
				IsChecked = true
			};
			await UITestHelper.Load(checkBox);

			// Act
			var peer = checkBox.GetOrCreateAutomationPeer();
			var attributes = AriaMapper.GetAriaAttributes(peer);

			// Assert
			Assert.AreEqual("checkbox", attributes.Role);
			Assert.AreEqual("true", attributes.Checked);
		}

		/// <summary>
		/// Verifies that AriaMapper correctly detects toggle capability.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_CheckBox_Mapped_Then_PatternCapabilities_CanToggle_Is_True()
		{
			// Arrange
			var checkBox = new CheckBox { Content = "Test" };
			await UITestHelper.Load(checkBox);

			// Act
			var peer = checkBox.GetOrCreateAutomationPeer();
			var capabilities = AriaMapper.GetPatternCapabilities(peer);

			// Assert
			Assert.IsTrue(capabilities.CanToggle, "CheckBox should have CanToggle capability");
		}

		/// <summary>
		/// Verifies that RadioButton has correct semantic element type.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_RadioButton_Mapped_Then_SemanticElementType_Is_RadioButton()
		{
			// Arrange
			var radioButton = new RadioButton { Content = "Option A" };
			await UITestHelper.Load(radioButton);

			// Act
			var peer = radioButton.GetOrCreateAutomationPeer();
			var elementType = AriaMapper.GetSemanticElementType(peer);

			// Assert
			Assert.AreEqual(SemanticElementType.RadioButton, elementType);
		}
#endif
	}
}
