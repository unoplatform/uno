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
	/// Runtime tests for accessible text box behavior.
	/// Tests automation peer properties, value pattern, and ARIA attribute mapping.
	/// </summary>
	[TestClass]
	public class Given_AccessibleTextBox
	{
		/// <summary>
		/// T046: Verifies that a focused textbox exposes its text value
		/// via the IValueProvider pattern.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_TextBox_Focused_Then_Value_Exposed()
		{
			// Arrange
			var textBox = new TextBox
			{
				Text = "Hello world"
			};

			await UITestHelper.Load(textBox);

			// Act
			var peer = textBox.GetOrCreateAutomationPeer();
			var valueProvider = peer?.GetPattern(PatternInterface.Value) as IValueProvider;

			// Assert
			Assert.IsNotNull(peer, "TextBox should have an automation peer");
			Assert.IsNotNull(valueProvider, "TextBox should support IValueProvider");
			Assert.AreEqual("Hello world", valueProvider.Value, "Value should match TextBox.Text");
		}

		/// <summary>
		/// T047: Verifies that setting text via IValueProvider.SetValue()
		/// updates the underlying TextBox. This is the path used when text
		/// is entered in the semantic input element.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Text_Entered_Then_Value_Syncs()
		{
			// Arrange
			var textBox = new TextBox { Text = "" };

			await UITestHelper.Load(textBox);

			var peer = textBox.GetOrCreateAutomationPeer();
			var valueProvider = peer?.GetPattern(PatternInterface.Value) as IValueProvider;

			Assert.IsNotNull(valueProvider, "TextBox should support IValueProvider");

			// Act - Simulate text input via automation peer
			valueProvider.SetValue("New text");

			// Assert
			Assert.AreEqual("New text", textBox.Text, "TextBox.Text should be updated");
			Assert.AreEqual("New text", valueProvider.Value, "ValueProvider should report updated value");
		}

		/// <summary>
		/// T048: Verifies that a PasswordBox has correct automation properties.
		/// When mapped to semantic DOM, this should create input[type=password].
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_PasswordBox_Then_Input_Type_Is_Password()
		{
			// Arrange
			var passwordBox = new PasswordBox
			{
				Password = "secret"
			};

			await UITestHelper.Load(passwordBox);

			// Act
			var peer = passwordBox.GetOrCreateAutomationPeer();

			// Assert
			Assert.IsNotNull(peer, "PasswordBox should have an automation peer");
			Assert.IsTrue(peer.IsPassword(), "PasswordBox peer should report IsPassword=true");

#if HAS_UNO
			// Verify AriaMapper produces Password element type
			var elementType = AriaMapper.GetSemanticElementType(peer);
			Assert.AreEqual(SemanticElementType.Password, elementType, "PasswordBox should map to Password element type");
#endif
		}

		/// <summary>
		/// Verifies that TextBox automation peer has correct control type.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_TextBox_Created_Then_Has_Edit_ControlType()
		{
			// Arrange
			var textBox = new TextBox();
			await UITestHelper.Load(textBox);

			// Act
			var peer = textBox.GetOrCreateAutomationPeer();
			var controlType = peer?.GetAutomationControlType();

			// Assert
			Assert.AreEqual(AutomationControlType.Edit, controlType);
		}

		/// <summary>
		/// Verifies that TextBox with AutomationProperties.Name exposes correct name.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_TextBox_Has_AutomationName_Then_Name_Is_Exposed()
		{
			// Arrange
			var textBox = new TextBox();
			AutomationProperties.SetName(textBox, "Enter your name");

			await UITestHelper.Load(textBox);

			// Act
			var peer = textBox.GetOrCreateAutomationPeer();
			var name = peer?.GetName();

			// Assert
			Assert.AreEqual("Enter your name", name);
		}

		/// <summary>
		/// Verifies that a read-only TextBox reports correct state.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_TextBox_IsReadOnly_Then_ValueProvider_IsReadOnly()
		{
			// Arrange
			var textBox = new TextBox
			{
				Text = "Read only text",
				IsReadOnly = true
			};
			await UITestHelper.Load(textBox);

			// Act
			var peer = textBox.GetOrCreateAutomationPeer();
			var valueProvider = peer?.GetPattern(PatternInterface.Value) as IValueProvider;

			// Assert
			Assert.IsNotNull(valueProvider);
			Assert.IsTrue(valueProvider.IsReadOnly, "Read-only TextBox should report IsReadOnly=true");
		}

#if HAS_UNO
		/// <summary>
		/// Verifies that AriaMapper correctly identifies TextBox semantic element type.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_TextBox_Mapped_Then_SemanticElementType_Is_TextBox()
		{
			// Arrange
			var textBox = new TextBox();
			await UITestHelper.Load(textBox);

			// Act
			var peer = textBox.GetOrCreateAutomationPeer();
			var elementType = AriaMapper.GetSemanticElementType(peer);

			// Assert
			Assert.AreEqual(SemanticElementType.TextBox, elementType);
		}

		/// <summary>
		/// Verifies that multiline TextBox maps to TextArea element type.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_TextBox_AcceptsReturn_Then_SemanticElementType_Is_TextArea()
		{
			// Arrange
			var textBox = new TextBox { AcceptsReturn = true };
			await UITestHelper.Load(textBox);

			// Act
			var peer = textBox.GetOrCreateAutomationPeer();
			var elementType = AriaMapper.GetSemanticElementType(peer);

			// Assert
			Assert.AreEqual(SemanticElementType.TextArea, elementType);
		}

		/// <summary>
		/// Verifies that AriaMapper correctly detects value capability.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_TextBox_Mapped_Then_PatternCapabilities_CanValue_Is_True()
		{
			// Arrange
			var textBox = new TextBox();
			await UITestHelper.Load(textBox);

			// Act
			var peer = textBox.GetOrCreateAutomationPeer();
			var capabilities = AriaMapper.GetPatternCapabilities(peer);

			// Assert
			Assert.IsTrue(capabilities.CanValue, "TextBox should have CanValue capability");
		}
#endif
	}
}
