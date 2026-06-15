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
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(button);

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

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(button);
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
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(button);

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
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(button);
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
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(button);
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
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(button);
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
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(button);
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
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(button);
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
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(button);
			var capabilities = AriaMapper.GetPatternCapabilities(peer);

			// Assert
			Assert.IsTrue(capabilities.CanInvoke, "Button should have CanInvoke capability");
		}
#endif
#if __SKIA__

		/// <summary>
		/// T016/FR-016 (WASM DOM): a focusable Button emits a native &lt;button&gt; semantic element that is a
		/// real tab stop (tabindex="0"). The native &lt;button&gt; carries the implicit ARIA button role, so the
		/// assertion is on the tag plus focusability rather than an explicit role attribute.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Button_Is_Focusable_Then_Dom_Is_Button_With_Tabindex()
		{
			var button = new Button { Content = "Click Me", IsTabStop = true };

			await UITestHelper.Load(button);
			button.GetOrCreateAutomationPeer();

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(button), timeoutMS: 5000, message: "Timed out waiting for the button semantic element to be created.");
			await UITestHelper.WaitForIdle();

			Assert.AreEqual("button", GetSemanticElementTagName(button), "A Button must emit a native <button> semantic element.");
			Assert.AreEqual("0", GetSemanticAttribute(button, "tabindex"), "A focusable Button must be a tab stop (tabindex=\"0\").");
		}

		/// <summary>
		/// T018/FR-016 (WASM DOM): a disabled Button emits aria-disabled="true" on its semantic element and is
		/// not a tab stop (tabindex must not be "0").
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Button_Is_Disabled_Then_Dom_AriaDisabled_Is_True()
		{
			var button = new Button { Content = "Disabled Button", IsEnabled = false };

			await UITestHelper.Load(button);
			button.GetOrCreateAutomationPeer();

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(button), timeoutMS: 5000, message: "Timed out waiting for the disabled button semantic element to be created.");
			await UITestHelper.WaitForIdle();

			Assert.AreEqual("true", GetSemanticAttribute(button, "aria-disabled"), "A disabled Button must emit aria-disabled=\"true\".");
			Assert.AreNotEqual("0", GetSemanticAttribute(button, "tabindex"), "A disabled Button must not be a tab stop (tabindex must not be \"0\").");
		}

		/// <summary>
		/// FR-016 (WASM DOM): a Button with AutomationProperties.Name exposes the name as aria-label on its
		/// semantic element so screen readers announce it.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Button_Has_AutomationName_Then_Dom_AriaLabel_Is_Set()
		{
			var button = new Button { Content = "Click" };
			AutomationProperties.SetName(button, "Submit Form");

			await UITestHelper.Load(button);
			button.GetOrCreateAutomationPeer();

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(button), timeoutMS: 5000, message: "Timed out waiting for the named button semantic element to be created.");
			await UITestHelper.WaitForIdle();

			Assert.AreEqual("Submit Form", GetSemanticAttribute(button, "aria-label"), "A Button's AutomationProperties.Name must surface as aria-label on the DOM node.");
		}

		private static void EnableAccessibilityThroughDom()
		{
			InvokeBrowserJs("(function(){const button = document.getElementById('uno-enable-accessibility'); if (button) { button.click(); } return 'ok';})()");
		}

		// Targets the exact semantic element for a given element via its visual handle, mirroring the
		// id scheme used by the WASM runtime (uno-semantics-{handle}).
		private static string GetSemanticElementId(UIElement element)
			=> $"uno-semantics-{((long)element.Visual.Handle)}";

		private static bool SemanticElementExists(UIElement element)
			=> InvokeBrowserJs($"(function(){{return document.getElementById('{GetSemanticElementId(element)}') ? '1' : '0';}})()") == "1";

		private static string GetSemanticAttribute(UIElement element, string attribute)
			=> InvokeBrowserJs($"(function(){{const e = document.getElementById('{GetSemanticElementId(element)}'); return e ? (e.getAttribute('{attribute}') ?? '') : '';}})()");

		private static string GetSemanticElementTagName(UIElement element)
			=> InvokeBrowserJs($"(function(){{const e = document.getElementById('{GetSemanticElementId(element)}'); return e ? e.tagName.toLowerCase() : '';}})()");

		private static string InvokeBrowserJs(string javascript)
		{
			var runtimeType = Type.GetType("Uno.Foundation.WebAssemblyRuntime, Uno.Foundation.Runtime.WebAssembly", throwOnError: false);
			Assert.IsNotNull(runtimeType, "Unable to locate Uno.Foundation.WebAssemblyRuntime at runtime.");

			var invokeJs = runtimeType.GetMethod("InvokeJS", new[] { typeof(string) });
			Assert.IsNotNull(invokeJs, "Unable to locate Uno.Foundation.WebAssemblyRuntime.InvokeJS(string).");

			return invokeJs.Invoke(obj: null, parameters: new object[] { javascript }) as string ?? string.Empty;
		}
#endif

	}
}
