using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;

#if HAS_UNO
using Uno.UI.Runtime.Skia;
using static Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper;
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

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Button_Starts_Disabled_Then_Reenable_Restores_Its_Tab_Stop()
		{
			var button = new MutableEnabledButton { Content = "Initially disabled", IsEnabled = false };

			await UITestHelper.Load(button);
			var peer = button.GetOrCreateAutomationPeer();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(button), timeoutMS: 5000, message: "Timed out waiting for the initially-disabled button semantic element.");

			Assert.AreEqual("true", GetSemanticAttribute(button, "aria-disabled"));
			Assert.AreEqual("-1", GetSemanticAttribute(button, "tabindex"));

			button.IsEnabled = true;
			peer.RaisePropertyChangedEvent(AutomationElementIdentifiers.IsEnabledProperty, false, true);
			await UITestHelper.WaitFor(
				() => GetSemanticAttribute(button, "aria-disabled") == "false" && GetSemanticAttribute(button, "tabindex") == "0",
				timeoutMS: 3000,
				message: "Re-enabling an initially-disabled button did not restore its intended tab stop.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Automation_Invoke_Emits_Pointer_Sequence_Then_Button_Clicks_Once()
		{
			var button = new Button { Content = "Invoke once" };
			var clickCount = 0;
			button.Click += (_, _) => clickCount++;

			await UITestHelper.Load(button);
			button.GetOrCreateAutomationPeer();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(button), timeoutMS: 5000, message: "Timed out waiting for the semantic button.");

			var elementId = GetSemanticElementId(button);
			InvokeBrowserJs($"(function(){{ const element = document.getElementById('{elementId}'); const bounds = element.getBoundingClientRect(); const eventInit = {{ bubbles: true, cancelable: true, composed: true, pointerId: 1, pointerType: 'mouse', isPrimary: true, button: 0, clientX: bounds.left + bounds.width / 2, clientY: bounds.top + bounds.height / 2 }}; element.dispatchEvent(new PointerEvent('pointerdown', {{ ...eventInit, buttons: 1, pressure: 0.5 }})); element.dispatchEvent(new PointerEvent('pointerup', {{ ...eventInit, buttons: 0, pressure: 0 }})); element.click(); return 'ok'; }})()");

			await UITestHelper.WaitFor(() => clickCount >= 1, timeoutMS: 3000, message: "The semantic button was not invoked.");
			await UITestHelper.WaitForIdle();
			Assert.AreEqual(1, clickCount, "Assistive activation pointer events must not also enter the canvas input path.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Detached_Button_Node_Reuses_A_Live_Id_Then_Old_Click_Is_Rejected()
		{
			var button = new Button { Content = "Target" };
			var clickCount = 0;
			button.Click += (_, _) => clickCount++;
			var panel = new StackPanel { Children = { button } };

			await UITestHelper.Load(panel);
			button.GetOrCreateAutomationPeer();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(button), timeoutMS: 5000, message: "Timed out waiting for the button semantic element.");

			var elementId = GetSemanticElementId(button);
			InvokeBrowserJs($"globalThis.__unoStaleSemanticButton = document.getElementById('{elementId}'); 'ok'");
			panel.Children.Remove(button);
			panel.Children.Add(button);
			await UITestHelper.WaitFor(
				() => SemanticElementExists(button) && InvokeBrowserJs($"globalThis.__unoStaleSemanticButton === document.getElementById('{elementId}') ? '1' : '0'") == "0",
				timeoutMS: 3000,
				message: "The semantic button was not recreated with a distinct DOM identity.");

			InvokeBrowserJs("globalThis.__unoStaleSemanticButton.click(); 'ok'");
			await UITestHelper.WaitForIdle();
			Assert.AreEqual(0, clickCount, "A detached superseded semantic node must not dispatch to the live recycled handle.");

			InvokeBrowserJs($"document.getElementById('{elementId}').click(); delete globalThis.__unoStaleSemanticButton; 'ok'");
			await UITestHelper.WaitFor(() => clickCount == 1, timeoutMS: 3000, message: "The current semantic node did not invoke its live owner.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Host_Node_Collides_With_Semantic_Id_Then_It_Is_Not_Removed_Or_Mutated()
		{
			var sibling = new Button { Content = "Existing sibling" };
			var panel = new StackPanel { Children = { sibling } };
			var button = new Button { Content = "Collision target" };
			var elementId = GetSemanticElementId(button);

			await UITestHelper.Load(panel);
			sibling.GetOrCreateAutomationPeer();
			EnableAccessibilityThroughDom();
			InvokeBrowserJs($"(function(){{const host=document.createElement('div'); host.id='{elementId}'; host.dataset.owner='host'; document.body.insertBefore(host, document.body.firstChild); return 'ok';}})()");

			try
			{
				panel.Children.Add(button);
				var peer = button.GetOrCreateAutomationPeer();
				await UITestHelper.WaitFor(
					() => InvokeBrowserJs($"document.querySelector('#uno-semantics-root [id=\"{elementId}\"]') ? '1' : '0'") == "1",
					timeoutMS: 5000,
					message: "Timed out waiting for the owned semantic node with the colliding id.");

				Assert.AreEqual(
					"host",
					InvokeBrowserJs($"document.querySelector('body > [id=\"{elementId}\"]')?.dataset.owner || ''"),
					"Semantic creation must not remove a colliding host node outside the semantic root.");

				button.IsEnabled = false;
				peer.RaisePropertyChangedEvent(AutomationElementIdentifiers.IsEnabledProperty, true, false);
				await UITestHelper.WaitFor(
					() => InvokeBrowserJs($"document.querySelector('#uno-semantics-root [id=\"{elementId}\"]')?.getAttribute('aria-disabled') || ''") == "true",
					timeoutMS: 3000,
					message: "The owned semantic node did not receive its disabled-state update.");

				Assert.AreEqual(
					"",
					InvokeBrowserJs($"document.querySelector('body > [id=\"{elementId}\"]')?.getAttribute('aria-disabled') || ''"),
					"Accessibility updates must not mutate a colliding host node outside the semantic root.");
			}
			finally
			{
				InvokeBrowserJs($"document.querySelector('body > [id=\"{elementId}\"]')?.remove(); 'ok'");
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Invoke_Provider_Throws_Then_Export_Contains_It_And_Sibling_Still_Invokes()
		{
			var throwing = new ThrowingInvokeButton();
			var sibling = new Button { Content = "Sibling" };
			var siblingClicks = 0;
			sibling.Click += (_, _) => siblingClicks++;
			var panel = new StackPanel { Children = { throwing, sibling } };

			await UITestHelper.Load(panel);
			throwing.GetOrCreateAutomationPeer();
			sibling.GetOrCreateAutomationPeer();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(throwing) && SemanticElementExists(sibling), timeoutMS: 5000, message: "Timed out waiting for invoke-provider semantic elements.");

			var result = InvokeBrowserJs($"(function(){{ try {{ document.getElementById('{GetSemanticElementId(throwing)}').click(); return 'contained'; }} catch (e) {{ return 'escaped'; }} }})()");
			Assert.AreEqual("contained", result, "An arbitrary provider exception must not escape the managed JS export.");

			InvokeBrowserJs($"document.getElementById('{GetSemanticElementId(sibling)}').click(); 'ok'");
			await UITestHelper.WaitFor(() => siblingClicks == 1, timeoutMS: 3000, message: "A provider failure poisoned subsequent semantic actions.");
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

		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23802")]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Button_With_PathIcon_And_AutomationName_Then_Dom_AriaLabel_Is_Set()
		{
			var button = new Button
			{
				Width = 48,
				Height = 48,
				Content = new PathIcon { Data = new RectangleGeometry { Rect = new Rect(0, 0, 12, 12) } },
			};
			AutomationProperties.SetName(button, "Refresh");

			try
			{
				await UITestHelper.Load(button);
				button.GetOrCreateAutomationPeer();

				EnableAccessibilityThroughDom();
				await UITestHelper.WaitFor(() => SemanticElementExists(button), timeoutMS: 5000, message: "Timed out waiting for the icon button semantic element to be created.");
				await UITestHelper.WaitFor(
					() => SemanticElementHasNonEmptyBounds(button),
					timeoutMS: 5000,
					message: "Timed out waiting for the icon button semantic element to receive non-zero bounds.");

				Assert.AreEqual("button", GetSemanticElementTagName(button), "An icon-only Button must emit a native <button> semantic element.");
				Assert.AreEqual("Refresh", GetSemanticAttribute(button, "aria-label"), "An icon-only Button's AutomationProperties.Name must surface as aria-label on the DOM node.");
				Assert.IsTrue(SemanticElementHasNonEmptyBounds(button), "The semantic Button must have non-zero bounds so assistive technology can locate it.");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23802")]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Button_With_PathIcon_Gets_AutomationName_Then_Dom_AriaLabel_Is_Updated()
		{
			var button = new Button
			{
				Width = 48,
				Height = 48,
				Content = new PathIcon { Data = new RectangleGeometry { Rect = new Rect(0, 0, 12, 12) } },
			};

			try
			{
				await UITestHelper.Load(button);
				button.GetOrCreateAutomationPeer();

				EnableAccessibilityThroughDom();
				await UITestHelper.WaitFor(() => SemanticElementExists(button), timeoutMS: 5000, message: "Timed out waiting for the unnamed icon button semantic element to be created.");

				Assert.IsFalse(SemanticElementHasAttribute(button, "aria-label"), "An unnamed icon-only Button must not emit aria-label.");

				AutomationProperties.SetName(button, "Refresh");
				await UITestHelper.WaitFor(
					() => GetSemanticAttribute(button, "aria-label") == "Refresh",
					timeoutMS: 5000,
					message: "Timed out waiting for the icon button aria-label to update.");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

#endif

		private sealed partial class MutableEnabledButton : Button
		{
		}

		private sealed partial class ThrowingInvokeButton : Button
		{
			public ThrowingInvokeButton() => Content = "Throwing";
			protected override AutomationPeer OnCreateAutomationPeer() => new ThrowingInvokeButtonPeer(this);
		}

		private sealed partial class ThrowingInvokeButtonPeer : ButtonAutomationPeer, IInvokeProvider
		{
			public ThrowingInvokeButtonPeer(Button owner) : base(owner) { }
			protected override object GetPatternCore(PatternInterface patternInterface)
				=> patternInterface == PatternInterface.Invoke ? this : base.GetPatternCore(patternInterface);
			void IInvokeProvider.Invoke() => throw new InvalidOperationException("Provider failed.");
		}
	}
}
