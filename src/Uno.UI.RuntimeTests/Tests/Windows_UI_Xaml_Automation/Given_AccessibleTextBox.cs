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
		[Ignore("Temporarily disabled - not yet validated")]
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
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(textBox);
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
		[Ignore("Temporarily disabled - not yet validated")]
		[RunsOnUIThread]
		public async Task When_Text_Entered_Then_Value_Syncs()
		{
			// Arrange
			var textBox = new TextBox { Text = "" };

			await UITestHelper.Load(textBox);

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(textBox);
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
		[Ignore("Temporarily disabled - not yet validated")]
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
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(passwordBox);

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
		[Ignore("Temporarily disabled - not yet validated")]
		[RunsOnUIThread]
		public async Task When_TextBox_Created_Then_Has_Edit_ControlType()
		{
			// Arrange
			var textBox = new TextBox();
			await UITestHelper.Load(textBox);

			// Act
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(textBox);
			var controlType = peer?.GetAutomationControlType();

			// Assert
			Assert.AreEqual(AutomationControlType.Edit, controlType);
		}

		/// <summary>
		/// Verifies that TextBox with AutomationProperties.Name exposes correct name.
		/// </summary>
		[TestMethod]
		[Ignore("Temporarily disabled - not yet validated")]
		[RunsOnUIThread]
		public async Task When_TextBox_Has_AutomationName_Then_Name_Is_Exposed()
		{
			// Arrange
			var textBox = new TextBox();
			AutomationProperties.SetName(textBox, "Enter your name");

			await UITestHelper.Load(textBox);

			// Act
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(textBox);
			var name = peer?.GetName();

			// Assert
			Assert.AreEqual("Enter your name", name);
		}

		/// <summary>
		/// Verifies that a read-only TextBox reports correct state.
		/// </summary>
		[TestMethod]
		[Ignore("Temporarily disabled - not yet validated")]
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
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(textBox);
			var valueProvider = peer?.GetPattern(PatternInterface.Value) as IValueProvider;

			// Assert
			Assert.IsNotNull(valueProvider);
			Assert.IsTrue(valueProvider.IsReadOnly, "Read-only TextBox should report IsReadOnly=true");
		}

#if __SKIA__
		/// <summary>
		/// Verifies that browser-originated typing through the semantic textbox keeps
		/// the caret at the browser-managed position instead of jumping back to the start.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_SemanticTextBox_Receives_Input_Then_Text_Order_And_Caret_Are_Preserved()
		{
			var textBox = new TextBox();

			await UITestHelper.Load(textBox);
			textBox.GetOrCreateAutomationPeer();

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticTextBoxExists(textBox), timeoutMS: 5000, message: "Timed out waiting for the semantic textbox to be created.");
			await UITestHelper.WaitForIdle();

			TypeCharacterIntoSemanticTextBox(textBox, "a");
			await UITestHelper.WaitForIdle();
			TypeCharacterIntoSemanticTextBox(textBox, "b");
			await UITestHelper.WaitForIdle();
			TypeCharacterIntoSemanticTextBox(textBox, "c");
			await UITestHelper.WaitForIdle();

			Assert.AreEqual("abc", textBox.Text, "Semantic typing should preserve text order in the managed TextBox.");
			Assert.AreEqual("abc", GetSemanticTextBoxValue(textBox), "Semantic textbox DOM value should stay in sync with managed text.");
			Assert.AreEqual("3", GetSemanticTextBoxCaret(textBox), "Caret should remain at the end after sequential semantic input.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Accessibility_Is_Enabled_After_Text_Exists_Then_SemanticTyping_Appends_At_Current_Caret()
		{
			var textBox = new TextBox
			{
				Text = "test"
			};

			await UITestHelper.Load(textBox);
			textBox.Focus(FocusState.Programmatic);
			textBox.Select(textBox.Text.Length, 0);
			textBox.GetOrCreateAutomationPeer();

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticTextBoxExists(textBox), timeoutMS: 5000, message: "Timed out waiting for the semantic textbox to be created.");
			await UITestHelper.WaitForIdle();

			Assert.AreEqual("test", GetSemanticTextBoxValue(textBox), "Semantic textbox should mirror the existing managed value when accessibility is enabled.");
			Assert.AreEqual("4", GetSemanticTextBoxCaret(textBox), "Semantic textbox should inherit the managed caret position when created.");
			Assert.IsFalse(HiddenNativeTextBoxExists(), "The hidden browser textbox should be detached once the semantic textbox owns focus.");

			TypeTextIntoSemanticTextBox(textBox, "test");
			await UITestHelper.WaitForIdle();

			Assert.AreEqual("testtest", textBox.Text, "Semantic typing should append at the current caret instead of inserting in reverse at the start.");
			Assert.AreEqual("testtest", GetSemanticTextBoxValue(textBox), "Semantic textbox DOM value should stay aligned after appending text.");
			Assert.AreEqual("8", GetSemanticTextBoxCaret(textBox), "Caret should remain at the end after appending to preexisting text.");
		}

		private static void EnableAccessibilityThroughDom()
		{
			InvokeBrowserJs("(function(){const button = document.getElementById('uno-enable-accessibility'); if (button) { button.click(); } return 'ok';})()");
		}

		// Targets the exact semantic element for a given TextBox. Using a generic
		// '#uno-semantics-root input' selector would match the first semantic input in the
		// document, which is usually not the TextBox under test (e.g. the test runner header).
		private static string GetSemanticElementId(TextBox textBox)
			=> $"uno-semantics-{((long)textBox.Visual.Handle)}";

		private static bool SemanticTextBoxExists(TextBox textBox)
			=> InvokeBrowserJs($"(function(){{return document.getElementById('{GetSemanticElementId(textBox)}') ? '1' : '0';}})()") == "1";

		private static string GetSemanticTextBoxValue(TextBox textBox)
			=> InvokeBrowserJs($"(function(){{const element = document.getElementById('{GetSemanticElementId(textBox)}'); return element ? element.value : '';}})()");

		private static string GetSemanticTextBoxCaret(TextBox textBox)
			=> InvokeBrowserJs($"(function(){{const element = document.getElementById('{GetSemanticElementId(textBox)}'); return element ? String(element.selectionStart ?? -1) : '-1';}})()");

		private static bool HiddenNativeTextBoxExists()
			=> InvokeBrowserJs("(function(){return document.getElementById('uno-input') ? '1' : '0';})()") == "1";

		private static void TypeCharacterIntoSemanticTextBox(TextBox textBox, string character)
		{
			var escapedCharacter = character
				.Replace("\\", "\\\\")
				.Replace("'", "\\'");

			InvokeBrowserJs($"(function(){{const element = document.getElementById('{GetSemanticElementId(textBox)}'); if (!element) {{ return 'missing'; }} element.focus(); const start = element.selectionStart ?? 0; const end = element.selectionEnd ?? start; const character = '{escapedCharacter}'; element.value = element.value.slice(0, start) + character + element.value.slice(end); const caret = start + character.length; element.setSelectionRange(caret, caret); element.dispatchEvent(new Event('input', {{ bubbles: true }})); return element.value; }})()");
		}

		private static void TypeTextIntoSemanticTextBox(TextBox textBox, string text)
		{
			foreach (var character in text)
			{
				TypeCharacterIntoSemanticTextBox(textBox, character.ToString());
			}
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
		/// Verifies that AriaMapper correctly identifies TextBox semantic element type.
		/// </summary>
		[TestMethod]
		[Ignore("Temporarily disabled - not yet validated")]
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
		[Ignore("Temporarily disabled - not yet validated")]
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
		[Ignore("Temporarily disabled - not yet validated")]
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
