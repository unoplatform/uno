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
	/// Runtime tests for accessible button behavior.
	/// Tests automation peer properties, patterns, and ARIA attribute mapping.
	/// </summary>
	[TestClass]
	public class Given_AccessibleButton
	{
		/// <summary>
		/// T016: Verifies that a focusable button has correct keyboard focusability settings.
		/// When rendered as a semantic element, this translates to tabindex="0".
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Button_Is_Focusable_Then_Has_Tabindex()
		{
			// Arrange
			var button = new Button
			{
				Content = "Click Me",
				IsTabStop = true
			};

			await UITestHelper.Load(button);

			// Act
			var peer = button.GetOrCreateAutomationPeer();

			// Assert
			Assert.IsNotNull(peer, "Button should have an automation peer");
			Assert.IsTrue(peer.IsKeyboardFocusable(), "Button should be keyboard focusable");
			Assert.AreEqual(AutomationControlType.Button, peer.GetAutomationControlType(), "Control type should be Button");
		}

		/// <summary>
		/// T017: Verifies that invoking a button via automation peer fires the Click event.
		/// This tests the critical path for screen reader button activation.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Button_Is_Invoked_Then_Click_Handler_Fires()
		{
			// Arrange
			var button = new Button { Content = "Submit" };
			var clickFired = false;
			button.Click += (s, e) => clickFired = true;

			await UITestHelper.Load(button);

			var peer = button.GetOrCreateAutomationPeer();
			var invokeProvider = peer?.GetPattern(PatternInterface.Invoke) as IInvokeProvider;

			// Act
			Assert.IsNotNull(invokeProvider, "Button should support IInvokeProvider");
			invokeProvider.Invoke();

			// Assert
			Assert.IsTrue(clickFired, "Click handler should have fired when button was invoked");
		}

		/// <summary>
		/// T018: Verifies that a disabled button reports correct enabled state.
		/// When mapped to ARIA, this translates to aria-disabled="true".
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Button_Is_Disabled_Then_AriaDisabled_Is_True()
		{
			// Arrange
			var button = new Button
			{
				Content = "Disabled Button",
				IsEnabled = false
			};

			await UITestHelper.Load(button);

			// Act
			var peer = button.GetOrCreateAutomationPeer();

			// Assert
			Assert.IsNotNull(peer, "Button should have an automation peer");
			Assert.IsFalse(peer.IsEnabled(), "Disabled button's peer should report IsEnabled=false");

#if HAS_UNO
			// Verify AriaMapper produces correct attribute
			var attributes = AriaMapper.GetAriaAttributes(peer);
			Assert.IsTrue(attributes.Disabled, "AriaMapper should report Disabled=true for disabled button");
#endif
		}

		/// <summary>
		/// Verifies that button automation peer has correct control type.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Button_Created_Then_Has_Button_ControlType()
		{
			// Arrange
			var button = new Button { Content = "Test" };
			await UITestHelper.Load(button);

			// Act
			var peer = button.GetOrCreateAutomationPeer();
			var controlType = peer?.GetAutomationControlType();

			// Assert
			Assert.AreEqual(AutomationControlType.Button, controlType);
		}

		/// <summary>
		/// Verifies that button with AutomationProperties.Name set exposes correct name.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Button_Has_AutomationName_Then_Name_Is_Exposed()
		{
			// Arrange
			var button = new Button { Content = "Click" };
			AutomationProperties.SetName(button, "Submit Form");

			await UITestHelper.Load(button);

			// Act
			var peer = button.GetOrCreateAutomationPeer();
			var name = peer?.GetName();

			// Assert
			Assert.AreEqual("Submit Form", name, "Automation name should be exposed");
		}

		/// <summary>
		/// Verifies that button supports IInvokeProvider pattern.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Button_Created_Then_Supports_Invoke_Pattern()
		{
			// Arrange
			var button = new Button { Content = "Test" };
			await UITestHelper.Load(button);

			// Act
			var peer = button.GetOrCreateAutomationPeer();
			var pattern = peer?.GetPattern(PatternInterface.Invoke);

			// Assert
			Assert.IsNotNull(pattern, "Button should support Invoke pattern");
			Assert.IsInstanceOfType(pattern, typeof(IInvokeProvider));
		}

#if HAS_UNO
		/// <summary>
		/// Verifies that AriaMapper correctly identifies button semantic element type.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Button_Mapped_Then_SemanticElementType_Is_Button()
		{
			// Arrange
			var button = new Button { Content = "Test" };
			await UITestHelper.Load(button);

			// Act
			var peer = button.GetOrCreateAutomationPeer();
			var elementType = AriaMapper.GetSemanticElementType(peer);

			// Assert
			Assert.AreEqual(SemanticElementType.Button, elementType);
		}

		/// <summary>
		/// Verifies that AriaMapper produces correct ARIA role for buttons.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Button_Mapped_Then_AriaRole_Is_Button()
		{
			// Arrange
			var button = new Button { Content = "Test" };
			await UITestHelper.Load(button);

			// Act
			var peer = button.GetOrCreateAutomationPeer();
			var attributes = AriaMapper.GetAriaAttributes(peer);

			// Assert
			Assert.AreEqual("button", attributes.Role);
		}

		/// <summary>
		/// Verifies that AriaMapper correctly detects invoke capability.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Button_Mapped_Then_PatternCapabilities_CanInvoke_Is_True()
		{
			// Arrange
			var button = new Button { Content = "Test" };
			await UITestHelper.Load(button);

			// Act
			var peer = button.GetOrCreateAutomationPeer();
			var capabilities = AriaMapper.GetPatternCapabilities(peer);

			// Assert
			Assert.IsTrue(capabilities.CanInvoke, "Button should have CanInvoke capability");
		}
#endif
	}
}
