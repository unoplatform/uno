using System.Threading.Tasks;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

#if HAS_UNO
using static Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation
{
	[TestClass]
	public class Given_AccessibleRoleOverride
	{
#if __SKIA__
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Generic_Role_Override_Is_Disabled_Then_It_Is_Not_A_Tab_Stop()
		{
			var control = new RoleOverrideControl { Content = "Custom action", IsEnabled = false, IsTabStop = true };
			AutomationProperties.SetRoleOverride(control, "button");
			AutomationProperties.SetName(control, "Custom action");

			await UITestHelper.Load(control);
			var peer = control.GetOrCreateAutomationPeer();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(control), timeoutMS: 5000, message: "Timed out waiting for the role-override semantic node.");

			Assert.AreEqual("button", GetSemanticAttribute(control, "role"));
			Assert.AreEqual("true", GetSemanticAttribute(control, "aria-disabled"));
			Assert.AreEqual("-1", GetSemanticAttribute(control, "tabindex"));

			control.IsEnabled = true;
			peer.RaisePropertyChangedEvent(AutomationElementIdentifiers.IsEnabledProperty, false, true);
			await UITestHelper.WaitFor(
				() => GetSemanticAttribute(control, "aria-disabled") == "false" && GetSemanticAttribute(control, "tabindex") == "0",
				timeoutMS: 3000,
				message: "Re-enabling the role override did not restore its intended tab stop.");

			control.IsEnabled = false;
			peer.RaisePropertyChangedEvent(AutomationElementIdentifiers.IsEnabledProperty, true, false);
			await UITestHelper.WaitFor(
				() => GetSemanticAttribute(control, "aria-disabled") == "true" && GetSemanticAttribute(control, "tabindex") == "-1",
				timeoutMS: 3000,
				message: "Disabling the role override did not remove it from the tab order.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Factory_Control_Role_Is_Overridden_Then_Native_Behavior_And_Validation_Are_Preserved()
		{
			var button = new Button { Content = "Factory action" };
			AutomationProperties.SetName(button, "Named factory action");
			var clickCount = 0;
			button.Click += (_, _) => clickCount++;
			AutomationProperties.SetRoleOverride(button, " TAB ");

			await UITestHelper.Load(button);
			button.GetOrCreateAutomationPeer();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(button), timeoutMS: 5000, message: "Timed out waiting for the factory role override.");

			Assert.AreEqual("button", GetSemanticElementTagName(button), "Role overrides must preserve the native Button element and its invocation callback.");
			Assert.AreEqual("tab", GetSemanticAttribute(button, "role"), "A valid role override must be normalized and applied after factory creation.");

			AutomationProperties.SetRoleOverride(button, "not-a-real-role");
			await UITestHelper.WaitFor(() => !SemanticElementHasAttribute(button, "role"), timeoutMS: 3000,
				message: "An invalid ARIA role token was not rejected in favor of the native Button role.");

			AutomationProperties.SetRoleOverride(button, "link");
			await UITestHelper.WaitFor(() => GetSemanticAttribute(button, "role") == "link", timeoutMS: 3000,
				message: "A live valid role override did not update the factory-created semantic node.");

			AutomationProperties.SetRoleOverride(button, string.Empty);
			await UITestHelper.WaitFor(() => !SemanticElementHasAttribute(button, "role"), timeoutMS: 3000,
				message: "Clearing the override did not restore the native Button's implicit role.");

			InvokeBrowserJs($"document.getElementById('{GetSemanticElementId(button)}').click(); 'ok'");
			await UITestHelper.WaitFor(() => clickCount == 1, timeoutMS: 3000, message: "Role changes broke native Button invocation.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Generic_Invoke_Peer_Has_Button_Role_Then_Automation_Invokes_Once()
		{
			var control = new InvokeRoleOverrideControl { Content = "Custom action", Width = 100, Height = 30 };
			AutomationProperties.SetName(control, "Custom action");
			AutomationProperties.SetRoleOverride(control, "button");

			await UITestHelper.Load(control);
			control.GetOrCreateAutomationPeer();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(control), timeoutMS: 5000, message: "Timed out waiting for the generic invoke semantic node.");

			var elementId = GetSemanticElementId(control);
			InvokeBrowserJs($"(function(){{ const element = document.getElementById('{elementId}'); const bounds = element.getBoundingClientRect(); const eventInit = {{ bubbles: true, cancelable: true, composed: true, pointerId: 1, pointerType: 'mouse', isPrimary: true, button: 0, clientX: bounds.left + bounds.width / 2, clientY: bounds.top + bounds.height / 2 }}; element.dispatchEvent(new PointerEvent('pointerdown', {{ ...eventInit, buttons: 1, pressure: 0.5 }})); element.dispatchEvent(new PointerEvent('pointerup', {{ ...eventInit, buttons: 0, pressure: 0 }})); element.click(); return 'ok'; }})()");

			await UITestHelper.WaitFor(() => control.InvokeCount >= 1, timeoutMS: 3000, message: "The generic invoke peer was not activated.");
			await UITestHelper.WaitForIdle();
			Assert.AreEqual(1, control.InvokeCount, "A semantic automation action must invoke its generic peer exactly once.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Native_Host_Does_Not_Allow_Role_Then_Override_Is_Rejected()
		{
			var slider = new Slider { Value = 50 };
			var textBox = new TextBox { Text = "Value" };
			var checkBox = new CheckBox { Content = "Choice" };
			AutomationProperties.SetRoleOverride(slider, "treeitem");
			AutomationProperties.SetRoleOverride(textBox, "button");
			AutomationProperties.SetRoleOverride(checkBox, "slider");
			var panel = new StackPanel { Children = { slider, textBox, checkBox } };

			await UITestHelper.Load(panel);
			slider.GetOrCreateAutomationPeer();
			textBox.GetOrCreateAutomationPeer();
			checkBox.GetOrCreateAutomationPeer();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(slider) && SemanticElementExists(textBox) && SemanticElementExists(checkBox), timeoutMS: 5000,
				message: "Timed out waiting for native semantic hosts.");

			Assert.AreEqual("range", GetSemanticInputType(slider));
			Assert.AreEqual("text", GetSemanticInputType(textBox));
			Assert.AreEqual("checkbox", GetSemanticInputType(checkBox));
			Assert.IsFalse(SemanticElementHasAttribute(slider, "role"), "input[type=range] permits no role other than its implicit slider role.");
			Assert.IsFalse(SemanticElementHasAttribute(textBox, "role"), "input[type=text] does not permit role=button.");
			Assert.IsFalse(SemanticElementHasAttribute(checkBox, "role"), "input[type=checkbox] does not permit role=slider.");

			AutomationProperties.SetRoleOverride(checkBox, "switch");
			await UITestHelper.WaitFor(() => GetSemanticAttribute(checkBox, "role") == "switch", timeoutMS: 3000,
				message: "An HTML-ARIA-compatible checkbox-to-switch override was rejected.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_ToggleButton_Is_A_Switch_Then_State_Uses_AriaChecked()
		{
			var toggle = new ToggleButton { Content = "Power", IsChecked = true };
			AutomationProperties.SetRoleOverride(toggle, "switch");

			await UITestHelper.Load(toggle);
			var peer = toggle.GetOrCreateAutomationPeer();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(toggle), timeoutMS: 5000, message: "Timed out waiting for the switch override.");

			Assert.AreEqual("switch", GetSemanticAttribute(toggle, "role"));
			Assert.AreEqual("true", GetSemanticAttribute(toggle, "aria-checked"));
			Assert.IsFalse(SemanticElementHasAttribute(toggle, "aria-pressed"));

			toggle.IsChecked = false;
			peer.RaisePropertyChangedEvent(TogglePatternIdentifiers.ToggleStateProperty, ToggleState.On, ToggleState.Off);
			await UITestHelper.WaitFor(() => GetSemanticAttribute(toggle, "aria-checked") == "false" && !SemanticElementHasAttribute(toggle, "aria-pressed"), timeoutMS: 3000,
				message: "The effective switch state did not live-update through aria-checked.");

			AutomationProperties.SetRoleOverride(toggle, string.Empty);
			await UITestHelper.WaitFor(() => !SemanticElementHasAttribute(toggle, "role") && GetSemanticAttribute(toggle, "aria-pressed") == "false" && !SemanticElementHasAttribute(toggle, "aria-checked"), timeoutMS: 3000,
				message: "Clearing the switch override did not restore ToggleButton state semantics.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_ToggleSwitch_Is_A_Button_Then_State_Uses_AriaPressed()
		{
			var toggle = new ToggleSwitch { Header = "Power", IsOn = true };
			AutomationProperties.SetRoleOverride(toggle, "button");

			await UITestHelper.Load(toggle);
			var peer = toggle.GetOrCreateAutomationPeer();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(toggle), timeoutMS: 5000, message: "Timed out waiting for the button override.");

			Assert.AreEqual("button", GetSemanticAttribute(toggle, "role"));
			Assert.AreEqual("true", GetSemanticAttribute(toggle, "aria-pressed"));
			Assert.IsFalse(SemanticElementHasAttribute(toggle, "aria-checked"));

			toggle.IsOn = false;
			peer.RaisePropertyChangedEvent(TogglePatternIdentifiers.ToggleStateProperty, ToggleState.On, ToggleState.Off);
			await UITestHelper.WaitFor(() => GetSemanticAttribute(toggle, "aria-pressed") == "false" && !SemanticElementHasAttribute(toggle, "aria-checked"), timeoutMS: 3000,
				message: "The effective button state did not live-update through aria-pressed.");

			AutomationProperties.SetRoleOverride(toggle, string.Empty);
			await UITestHelper.WaitFor(() => GetSemanticAttribute(toggle, "role") == "switch" && GetSemanticAttribute(toggle, "aria-checked") == "false" && !SemanticElementHasAttribute(toggle, "aria-pressed"), timeoutMS: 3000,
				message: "Clearing the button override did not restore ToggleSwitch state semantics.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_ComboBox_Role_Changes_Then_Expanded_State_Follows_Role()
		{
			var comboBox = new ComboBox { Header = "Choice", Items = { "A", "B" }, SelectedIndex = 0 };
			AutomationProperties.SetRoleOverride(comboBox, "heading");

			await UITestHelper.Load(comboBox);
			comboBox.GetOrCreateAutomationPeer();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(comboBox), timeoutMS: 5000, message: "Timed out waiting for the combobox override.");

			Assert.AreEqual("heading", GetSemanticAttribute(comboBox, "role"));
			Assert.IsFalse(SemanticElementHasAttribute(comboBox, "aria-expanded"));

			AutomationProperties.SetRoleOverride(comboBox, string.Empty);
			await UITestHelper.WaitFor(() => GetSemanticAttribute(comboBox, "role") == "combobox" && GetSemanticAttribute(comboBox, "aria-expanded") == "false", timeoutMS: 3000,
				message: "Clearing the heading override did not restore combobox expansion semantics.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Selection_Changes_During_Role_Override_Then_Clear_Restores_Current_State()
		{
			var control = new SelectableTabControl { Content = "Overview", Width = 100, Height = 30 };
			AutomationProperties.SetName(control, "Overview");

			await UITestHelper.Load(control);
			var peer = (SelectableTabPeer)control.GetOrCreateAutomationPeer();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(control), timeoutMS: 5000, message: "Timed out waiting for the selectable tab semantic node.");

			Assert.AreEqual("tab", GetSemanticAttribute(control, "role"));
			Assert.AreEqual("false", GetSemanticAttribute(control, "aria-selected"));

			AutomationProperties.SetRoleOverride(control, "button");
			await UITestHelper.WaitFor(
				() => GetSemanticAttribute(control, "role") == "button" && !SemanticElementHasAttribute(control, "aria-selected"),
				timeoutMS: 3000,
				message: "The button override retained tab selection state.");

			peer.SetSelected(true);
			await UITestHelper.WaitFor(() => !SemanticElementHasAttribute(control, "aria-selected"), timeoutMS: 3000,
				message: "A selection change emitted aria-selected on role=button.");

			AutomationProperties.SetRoleOverride(control, string.Empty);
			await UITestHelper.WaitFor(
				() => GetSemanticAttribute(control, "role") == "tab" && GetSemanticAttribute(control, "aria-selected") == "true",
				timeoutMS: 3000,
				message: "Clearing the override did not restore the tab's current selected state.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Custom_Peers_Have_No_Override_Then_Generic_Name_Rules_Are_Valid()
		{
			var named = new RoleOverrideControl { Content = "Named custom control", Width = 100, Height = 30 };
			AutomationProperties.SetLocalizedControlType(named, "custom widget");
			var unnamed = new RoleOverrideControl { Width = 100, Height = 30 };
			var panel = new StackPanel { Children = { named, unnamed } };

			await UITestHelper.Load(panel);
			named.GetOrCreateAutomationPeer();
			unnamed.GetOrCreateAutomationPeer();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(named) && SemanticElementExists(unnamed), timeoutMS: 5000,
				message: "Timed out waiting for Custom peer semantic nodes.");

			Assert.AreEqual("group", GetSemanticAttribute(named, "role"), "A named Custom peer must use a nameable group role, not generic.");
			Assert.AreEqual("Named custom control", GetSemanticAttribute(named, "aria-label"));
			Assert.IsFalse(SemanticElementHasAttribute(unnamed, "role"), "An unnamed Custom peer must not force role=generic.");
			Assert.IsFalse(SemanticElementHasAttribute(unnamed, "aria-label"), "An unnamed Custom peer must not emit an empty/prohibited accessible name.");

			AutomationProperties.SetRoleOverride(named, "generic");
			await UITestHelper.WaitFor(
				() => GetSemanticAttribute(named, "role") == "generic" &&
					!SemanticElementHasAttribute(named, "aria-label") &&
					!SemanticElementHasAttribute(named, "aria-roledescription"),
				timeoutMS: 3000,
				message: "Explicit generic did not remove its prohibited accessible name and role description.");

			AutomationProperties.SetRoleOverride(named, string.Empty);
			await UITestHelper.WaitFor(
				() => GetSemanticAttribute(named, "role") == "group" &&
					GetSemanticAttribute(named, "aria-label") == "Named custom control" &&
					GetSemanticAttribute(named, "aria-roledescription") == "custom widget",
				timeoutMS: 3000,
				message: "Clearing generic did not restore the content-derived Custom peer semantics.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Role_Prohibits_Naming_Then_All_Name_Sources_Stay_Suppressed()
		{
			var label = new TextBlock { Text = "Authored label" };
			AutomationProperties.SetName(label, "Authored label");
			var control = new RoleOverrideControl { Content = "Fallback content", Width = 100, Height = 30 };
			AutomationProperties.SetName(control, "Initial name");
			AutomationProperties.SetLabeledBy(control, label);
			AutomationProperties.SetRoleOverride(control, "generic");
			var panel = new StackPanel { Children = { label, control } };

			await UITestHelper.Load(panel);
			control.GetOrCreateAutomationPeer();
			label.GetOrCreateAutomationPeer();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(
				() => SemanticElementExists(label) && SemanticElementExists(control),
				timeoutMS: 5000,
				message: "Timed out waiting for the role-override name-source nodes.");

			Assert.AreEqual("generic", GetSemanticAttribute(control, "role"));
			Assert.IsFalse(SemanticElementHasAttribute(control, "aria-label"));
			Assert.IsFalse(SemanticElementHasAttribute(control, "aria-labelledby"));

			AutomationProperties.SetName(control, "Renamed while generic");
			await UITestHelper.WaitForIdle();
			Assert.IsFalse(SemanticElementHasAttribute(control, "aria-label"),
				"A live Name change must not restore aria-label on a role that prohibits naming.");
			Assert.IsFalse(SemanticElementHasAttribute(control, "aria-labelledby"),
				"A live Name change must not restore aria-labelledby on a role that prohibits naming.");

			AutomationProperties.SetRoleOverride(control, string.Empty);
			await UITestHelper.WaitFor(
				() => GetSemanticAttribute(control, "role") == "group" &&
					GetSemanticAttribute(control, "aria-labelledby") == GetSemanticElementId(label),
				timeoutMS: 3000,
				message: "Clearing generic did not restore the authored labeling relationship.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Custom_Landmark_LabeledBy_Changes_Then_Region_Role_LiveSyncs()
		{
			var label = new TextBlock { Text = "Dynamic region label" };
			AutomationProperties.SetName(label, "Dynamic region label");
			var control = new RoleOverrideControl { Width = 100, Height = 30 };
			AutomationProperties.SetLandmarkType(control, AutomationLandmarkType.Custom);
			var panel = new StackPanel { Children = { label, control } };

			await UITestHelper.Load(panel);
			control.GetOrCreateAutomationPeer();
			label.GetOrCreateAutomationPeer();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(
				() => SemanticElementExists(label) && SemanticElementExists(control),
				timeoutMS: 5000,
				message: "Timed out waiting for the dynamic landmark nodes.");
			Assert.AreNotEqual("region", GetSemanticAttribute(control, "role"));

			AutomationProperties.SetLabeledBy(control, label);
			await UITestHelper.WaitFor(
				() => GetSemanticAttribute(control, "role") == "region" &&
					GetSemanticAttribute(control, "aria-labelledby") == GetSemanticElementId(label),
				timeoutMS: 3000,
				message: "Adding LabeledBy did not promote the custom landmark to a named region.");

			AutomationProperties.SetLabeledBy(control, null);
			await UITestHelper.WaitFor(
				() => !SemanticElementHasAttribute(control, "role") &&
					!SemanticElementHasAttribute(control, "aria-labelledby"),
				timeoutMS: 3000,
				message: "Clearing LabeledBy left an unlabeled custom region.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_IsDialog_Changes_Then_Role_And_Modal_State_Live_Sync()
		{
			var control = new RoleOverrideControl { Content = "Dialog content", Width = 160, Height = 80 };
			AutomationProperties.SetName(control, "Settings");

			await UITestHelper.Load(control);
			control.GetOrCreateAutomationPeer();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(control), timeoutMS: 5000,
				message: "Timed out waiting for the dynamic dialog semantic node.");

			Assert.AreEqual("group", GetSemanticAttribute(control, "role"));
			AutomationProperties.SetIsDialog(control, true);
			await UITestHelper.WaitFor(
				() => GetSemanticAttribute(control, "role") == "dialog" && GetSemanticAttribute(control, "aria-modal") == "true",
				timeoutMS: 3000,
				message: "Setting IsDialog did not expose dialog/modal semantics.");

			AutomationProperties.SetIsDialog(control, false);
			await UITestHelper.WaitFor(
				() => GetSemanticAttribute(control, "role") == "group" && !SemanticElementHasAttribute(control, "aria-modal"),
				timeoutMS: 3000,
				message: "Clearing IsDialog did not restore the intrinsic role and remove aria-modal.");
		}

		private sealed partial class RoleOverrideControl : ContentControl
		{
			protected override AutomationPeer OnCreateAutomationPeer() => new RoleOverridePeer(this);
		}

		private sealed partial class RoleOverridePeer : FrameworkElementAutomationPeer
		{
			public RoleOverridePeer(RoleOverrideControl owner) : base(owner) { }
			protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Custom;
		}

		private sealed partial class InvokeRoleOverrideControl : ContentControl
		{
			public int InvokeCount { get; set; }

			protected override AutomationPeer OnCreateAutomationPeer() => new InvokeRoleOverridePeer(this);
		}

		private sealed partial class InvokeRoleOverridePeer : FrameworkElementAutomationPeer, IInvokeProvider
		{
			public InvokeRoleOverridePeer(InvokeRoleOverrideControl owner) : base(owner) { }

			protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Custom;

			protected override object GetPatternCore(PatternInterface patternInterface)
				=> patternInterface == PatternInterface.Invoke ? this : base.GetPatternCore(patternInterface);

			public void Invoke() => ((InvokeRoleOverrideControl)Owner).InvokeCount++;
		}

		private sealed partial class SelectableTabControl : ContentControl
		{
			protected override AutomationPeer OnCreateAutomationPeer() => new SelectableTabPeer(this);
		}

		private sealed partial class SelectableTabPeer : FrameworkElementAutomationPeer, ISelectionItemProvider
		{
			private bool _isSelected;

			public SelectableTabPeer(SelectableTabControl owner) : base(owner) { }

			protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.TabItem;

			protected override object GetPatternCore(PatternInterface patternInterface)
				=> patternInterface == PatternInterface.SelectionItem ? this : base.GetPatternCore(patternInterface);

			public bool IsSelected => _isSelected;
			public IRawElementProviderSimple SelectionContainer => null;
			public void AddToSelection() => SetSelected(true);
			public void RemoveFromSelection() => SetSelected(false);
			public void Select() => SetSelected(true);

			public void SetSelected(bool value)
			{
				var previous = _isSelected;
				_isSelected = value;
				RaisePropertyChangedEvent(SelectionItemPatternIdentifiers.IsSelectedProperty, previous, value);
			}
		}
#endif
	}
}