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
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(checkBox);
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

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(checkBox);
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
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(checkBox);
			var toggleProvider = peer?.GetPattern(PatternInterface.Toggle) as IToggleProvider;

			// Assert
			Assert.IsNotNull(toggleProvider, "CheckBox should support IToggleProvider");
			Assert.AreEqual(ToggleState.Indeterminate, toggleProvider.ToggleState, "Indeterminate checkbox should report ToggleState.Indeterminate");

#if HAS_UNO
			// Verify AriaMapper produces correct attribute
			var attributes = Uno.UI.Runtime.Skia.AriaMapper.GetAriaAttributes(peer);
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
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(checkBox);
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
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(checkBox);
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
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(checkBox);
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

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(checkBox);
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
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(checkBox);
			var elementType = Uno.UI.Runtime.Skia.AriaMapper.GetSemanticElementType(peer);

			// Assert
			Assert.AreEqual(Uno.UI.Runtime.Skia.SemanticElementType.Checkbox, elementType);
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
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(checkBox);
			var attributes = Uno.UI.Runtime.Skia.AriaMapper.GetAriaAttributes(peer);

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
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(checkBox);
			var capabilities = Uno.UI.Runtime.Skia.AriaMapper.GetPatternCapabilities(peer);

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
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(radioButton);
			var elementType = Uno.UI.Runtime.Skia.AriaMapper.GetSemanticElementType(peer);

			// Assert
			Assert.AreEqual(Uno.UI.Runtime.Skia.SemanticElementType.RadioButton, elementType);
		}

		/// <summary>
		/// US1/FR-001: A checked RadioButton must map to checked="true". RadioButtonAutomationPeer
		/// exposes ONLY ISelectionItemProvider (not Toggle), so Checked must be derived from
		/// IsSelected — otherwise the radio always renders unchecked. Fails before the US1 fix.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_RadioButton_Checked_Then_AriaMapper_Checked_Is_True()
		{
			var radioButton = new RadioButton { Content = "Option A", IsChecked = true };
			await UITestHelper.Load(radioButton);

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(radioButton);
			var attributes = Uno.UI.Runtime.Skia.AriaMapper.GetAriaAttributes(peer);

			Assert.AreEqual("true", attributes.Checked, "Checked RadioButton must map to checked='true'");
		}

		/// <summary>
		/// US1/FR-001: An unchecked RadioButton must map to checked="false" (not null).
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_RadioButton_Unchecked_Then_AriaMapper_Checked_Is_False()
		{
			var radioButton = new RadioButton { Content = "Option B", IsChecked = false };
			await UITestHelper.Load(radioButton);

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(radioButton);
			var attributes = Uno.UI.Runtime.Skia.AriaMapper.GetAriaAttributes(peer);

			Assert.AreEqual("false", attributes.Checked, "Unchecked RadioButton must map to checked='false', not null");
		}

		/// <summary>
		/// US1/FR-002+FR-003: DOM activation routes through OnSelection → ISelectionItemProvider.Select().
		/// Verifies Select() checks the RadioButton and the mapped Checked state follows. Fails before
		/// the US1 fix (radio activation was a no-op via the absent Toggle pattern).
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_RadioButton_Selected_Via_Peer_Then_IsChecked_And_Mapping_Update()
		{
			var radioButton = new RadioButton { Content = "Option C", IsChecked = false };
			await UITestHelper.Load(radioButton);

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(radioButton);
			var selectionItem = peer?.GetPattern(PatternInterface.SelectionItem) as ISelectionItemProvider;

			Assert.IsNotNull(selectionItem, "RadioButton must expose ISelectionItemProvider");

			selectionItem.Select();

			Assert.IsTrue(radioButton.IsChecked == true, "Select() must check the RadioButton");
			var attributes = Uno.UI.Runtime.Skia.AriaMapper.GetAriaAttributes(peer);
			Assert.AreEqual("true", attributes.Checked, "After Select(), Checked must be 'true'");
		}
#endif
#if __SKIA__

		/// <summary>
		/// T036/FR-016 (WASM DOM): a checked CheckBox emits a native &lt;input type="checkbox"&gt; with
		/// aria-checked="true", matching its visual state (WCAG 4.1.2).
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Checkbox_Checked_Then_Dom_AriaChecked_Is_True()
		{
			var checkBox = new CheckBox { Content = "Accept terms", IsChecked = true };

			await UITestHelper.Load(checkBox);
			checkBox.GetOrCreateAutomationPeer();

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(checkBox), timeoutMS: 5000, message: "Timed out waiting for the checkbox semantic element to be created.");
			await UITestHelper.WaitForIdle();

			Assert.AreEqual("input", GetSemanticElementTagName(checkBox), "A CheckBox must emit a native <input> semantic element.");
			Assert.AreEqual("checkbox", GetSemanticInputType(checkBox), "A CheckBox must emit input[type=checkbox].");
			Assert.AreEqual("true", GetSemanticAttribute(checkBox, "aria-checked"), "A checked CheckBox must emit aria-checked=\"true\".");
		}

		/// <summary>
		/// FR-016 (WASM DOM): an unchecked CheckBox emits aria-checked="false".
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Checkbox_Unchecked_Then_Dom_AriaChecked_Is_False()
		{
			var checkBox = new CheckBox { Content = "Enable notifications", IsChecked = false };

			await UITestHelper.Load(checkBox);
			checkBox.GetOrCreateAutomationPeer();

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(checkBox), timeoutMS: 5000, message: "Timed out waiting for the checkbox semantic element to be created.");
			await UITestHelper.WaitForIdle();

			Assert.AreEqual("false", GetSemanticAttribute(checkBox, "aria-checked"), "An unchecked CheckBox must emit aria-checked=\"false\".");
		}

		/// <summary>
		/// T038/FR-016 (WASM DOM): a tri-state CheckBox in the indeterminate state emits aria-checked="mixed".
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_TriState_Checkbox_Then_Dom_AriaChecked_Is_Mixed()
		{
			var checkBox = new CheckBox { Content = "Select all", IsThreeState = true, IsChecked = null };

			await UITestHelper.Load(checkBox);
			checkBox.GetOrCreateAutomationPeer();

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(checkBox), timeoutMS: 5000, message: "Timed out waiting for the tri-state checkbox semantic element to be created.");
			await UITestHelper.WaitForIdle();

			Assert.AreEqual("mixed", GetSemanticAttribute(checkBox, "aria-checked"), "An indeterminate tri-state CheckBox must emit aria-checked=\"mixed\".");
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

		private static string GetSemanticInputType(UIElement element)
			=> InvokeBrowserJs($"(function(){{const e = document.getElementById('{GetSemanticElementId(element)}'); return e ? (e.getAttribute('type') ?? '') : '';}})()");

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
