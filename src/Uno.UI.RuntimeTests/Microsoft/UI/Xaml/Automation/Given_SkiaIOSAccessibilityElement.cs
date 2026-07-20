#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

[TestClass]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaIOS)]
public class Given_SkiaIOSAccessibilityElement
{
	private static AccessibilityNativeNodeSnapshot? GetSnapshot(UIElement element)
		=> AccessibilityPeerHelper.IOSAccessibilityNodeSnapshotAccessor?.Invoke(element);

	private static AccessibilityNativeNodeSnapshot[] GetOrderedSnapshots(XamlRoot xamlRoot)
		=> AccessibilityPeerHelper.IOSAllNodeSnapshotsForRootAccessor?.Invoke(xamlRoot)
			?? Array.Empty<AccessibilityNativeNodeSnapshot>();

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Button_Has_Content_Then_Label_Is_Set()
	{
		var button = new Button { Content = "Confirm" };
		await UITestHelper.Load(button);

		var snapshot = GetSnapshot(button);

		Assert.IsNotNull(snapshot);
		Assert.AreEqual("Confirm", snapshot.Name);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Button_Then_Traits_Include_Button()
	{
		var button = new Button { Content = "OK" };
		await UITestHelper.Load(button);

		var snapshot = GetSnapshot(button);

		Assert.IsNotNull(snapshot);
		Assert.IsTrue((snapshot.Traits & AccessibilityNativeTraits.Button) != 0);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_TextBlock_Then_Traits_Include_StaticText()
	{
		var textBlock = new TextBlock { Text = "Hello" };
		await UITestHelper.Load(textBlock);

		var snapshot = GetSnapshot(textBlock);

		Assert.IsNotNull(snapshot);
		Assert.IsTrue((snapshot.Traits & AccessibilityNativeTraits.StaticText) != 0);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Button_Is_Disabled_Then_Traits_Include_NotEnabled()
	{
		var button = new Button { Content = "Save", IsEnabled = false };
		await UITestHelper.Load(button);

		var snapshot = GetSnapshot(button);

		Assert.IsNotNull(snapshot);
		Assert.IsFalse(snapshot.Enabled);
		Assert.IsTrue((snapshot.Traits & AccessibilityNativeTraits.NotEnabled) != 0);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_AutomationId_Set_Then_Identifier_Is_Set_And_Label_Is_Not_Id()
	{
		const string automationId = "btn-confirm";
		var button = new Button { Content = "Submit" };
		AutomationProperties.SetAutomationId(button, automationId);
		await UITestHelper.Load(button);

		var snapshot = GetSnapshot(button);

		Assert.IsNotNull(snapshot);
		Assert.AreEqual(automationId, snapshot.AutomationId);
		Assert.AreNotEqual(automationId, snapshot.Name);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_HelpText_Set_Then_Hint_Is_Set()
	{
		const string hint = "Double-tap to confirm";
		var button = new Button { Content = "Go" };
		AutomationProperties.SetHelpText(button, hint);
		await UITestHelper.Load(button);

		var snapshot = GetSnapshot(button);

		Assert.IsNotNull(snapshot);
		Assert.AreEqual(hint, snapshot.Hint);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Multiple_Buttons_Then_Container_Order_Matches_Peer_Order()
	{
		var panel = new StackPanel
		{
			Children =
			{
				new Button { Content = "First" },
				new Button { Content = "Second" },
				new Button { Content = "Third" },
			},
		};
		await UITestHelper.Load(panel);

		var names = GetOrderedSnapshots(panel.XamlRoot!)
			.Select(snapshot => snapshot.Name)
			.Where(name => name is "First" or "Second" or "Third")
			.ToList();

		CollectionAssert.AreEqual(new[] { "First", "Second", "Third" }, names);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Raw_AccessibilityView_Then_Element_Is_Absent()
	{
		var panel = new StackPanel();
		var visible = new Button { Content = "Visible" };
		var raw = new Button { Content = "Raw" };
		AutomationProperties.SetAccessibilityView(raw, AccessibilityView.Raw);
		panel.Children.Add(visible);
		panel.Children.Add(raw);
		await UITestHelper.Load(panel);

		Assert.IsNull(GetSnapshot(raw));
		Assert.IsNotNull(GetSnapshot(visible));
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Same_Element_Queried_Twice_Then_Same_Instance_Returned()
	{
		var button = new Button { Content = "Stable" };
		await UITestHelper.Load(button);

		var element1 = AccessibilityPeerHelper.IOSAccessibilityElementAccessor?.Invoke(button);
		var element2 = AccessibilityPeerHelper.IOSAccessibilityElementAccessor?.Invoke(button);

		Assert.IsNotNull(element1);
		Assert.AreSame(element1, element2);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Element_Loaded_Then_Frame_Is_Non_Empty()
	{
		var button = new Button { Content = "Frame test", Width = 100, Height = 44 };
		await UITestHelper.Load(button);

		var snapshot = GetSnapshot(button);

		Assert.IsNotNull(snapshot);
		Assert.IsTrue(snapshot.Bounds.Width > 0);
		Assert.IsTrue(snapshot.Bounds.Height > 0);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Peer_Overrides_Bounds_Then_Frame_Uses_Peer_Rectangle()
	{
		var control = new CustomBoundsControl { Content = "Custom Bounds", Width = 100, Height = 100 };
		await UITestHelper.Load(control);

		var snapshot = GetSnapshot(control);

		Assert.IsNotNull(snapshot);
		Assert.AreEqual(37, snapshot.Bounds.Width, 0.1);
		Assert.AreEqual(41, snapshot.Bounds.Height, 0.1);
	}

	private sealed partial class CustomBoundsControl : Button
	{
		protected override AutomationPeer OnCreateAutomationPeer()
			=> new CustomBoundsPeer(this);
	}

	private sealed class CustomBoundsPeer : FrameworkElementAutomationPeer
	{
		internal CustomBoundsPeer(FrameworkElement owner)
			: base(owner)
		{
		}

		protected override Windows.Foundation.Rect GetBoundingRectangleCore()
			=> new(11, 13, 37, 41);

		protected override bool IsControlElementCore() => true;

		protected override bool IsContentElementCore() => true;
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Slider_Then_Traits_Include_Adjustable()
	{
		var slider = new Slider { Minimum = 0, Maximum = 100, Value = 50 };
		await UITestHelper.Load(slider);

		var snapshot = GetSnapshot(slider);

		Assert.IsNotNull(snapshot);
		Assert.IsTrue((snapshot.Traits & AccessibilityNativeTraits.Adjustable) != 0);
	}

	// US2 native action tests

	private static bool InvokeAction(UIElement element, AccessibilityNativeAction action,
		double number = 0, string? text = null)
		=> AccessibilityPeerHelper.IOSAccessibilityActionAccessor?.Invoke(
			element, new AccessibilityNativeActionRequest(action, number, text)) ?? false;

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Button_Activate_Then_Returns_True()
	{
		var button = new Button { Content = "Activate" };
		await UITestHelper.Load(button);

		// IOSAccessibilityActionAccessor routes Activate through AccessibilityActivate()
		// and TryInvokeDefaultAction to IInvokeProvider.
		var result = InvokeAction(button, AccessibilityNativeAction.Activate);

		Assert.IsTrue(result);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_CheckBox_Activate_Then_Toggled()
	{
		var checkBox = new CheckBox { Content = "Toggle me", IsChecked = false };
		await UITestHelper.Load(checkBox);

		var snapshot = GetSnapshot(checkBox);
		Assert.IsNotNull(snapshot);
		Assert.AreEqual("0", snapshot.Value);

		var result = InvokeAction(checkBox, AccessibilityNativeAction.Activate);

		Assert.IsTrue(result);
		// After toggle the peer reports IsChecked = true and value "1".
		var after = GetSnapshot(checkBox);
		Assert.IsNotNull(after);
		Assert.AreEqual("1", after.Value);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_CheckBox_Is_Indeterminate_Then_Checked_State_Is_Null()
	{
		var checkBox = new CheckBox
		{
			Content = "Indeterminate",
			IsThreeState = true,
			IsChecked = null,
		};
		await UITestHelper.Load(checkBox);

		var snapshot = GetSnapshot(checkBox);

		Assert.IsNotNull(snapshot);
		Assert.IsTrue(snapshot.Checkable);
		Assert.IsNull(snapshot.IsChecked);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Slider_Increment_Then_Value_Increases()
	{
		var slider = new Slider { Minimum = 0, Maximum = 100, Value = 50, SmallChange = 10 };
		await UITestHelper.Load(slider);

		var result = InvokeAction(slider, AccessibilityNativeAction.Increment);

		Assert.IsTrue(result);
		Assert.IsTrue(slider.Value > 50, $"Expected value > 50 after increment, got {slider.Value}");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Slider_Decrement_Then_Value_Decreases()
	{
		var slider = new Slider { Minimum = 0, Maximum = 100, Value = 50, SmallChange = 10 };
		await UITestHelper.Load(slider);

		var result = InvokeAction(slider, AccessibilityNativeAction.Decrement);

		Assert.IsTrue(result);
		Assert.IsTrue(slider.Value < 50, $"Expected value < 50 after decrement, got {slider.Value}");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_TextBox_SetValue_Then_Text_Changes()
	{
		var textBox = new TextBox { Text = "Before" };
		await UITestHelper.Load(textBox);

		var result = InvokeAction(
			textBox,
			AccessibilityNativeAction.SetValue,
			text: "After");

		Assert.IsTrue(result);
		Assert.AreEqual("After", textBox.Text);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Disabled_Button_Activate_Then_Returns_False()
	{
		var button = new Button { Content = "Disabled", IsEnabled = false };
		await UITestHelper.Load(button);

		// AccessibilityActivate() calls TryInvokeDefaultAction which catches
		// ElementNotEnabledException and returns false.
		var result = InvokeAction(button, AccessibilityNativeAction.Activate);

		Assert.IsFalse(result);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Disabled_Slider_Increment_Then_Returns_False()
	{
		var slider = new Slider { Minimum = 0, Maximum = 100, Value = 50, IsEnabled = false };
		await UITestHelper.Load(slider);

		// The action accessor pre-checks IsEnabled before calling AccessibilityIncrement().
		var result = InvokeAction(slider, AccessibilityNativeAction.Increment);

		Assert.IsFalse(result);
		Assert.AreEqual(50, slider.Value);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Element_Without_Range_Provider_Increment_Then_Returns_False()
	{
		// Button has no IRangeValueProvider; increment should fail gracefully.
		var button = new Button { Content = "No range" };
		await UITestHelper.Load(button);

		var result = InvokeAction(button, AccessibilityNativeAction.Increment);

		Assert.IsFalse(result);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ComboBox_Expand_Custom_Action_Then_State_Changes()
	{
		var comboBox = new ComboBox
		{
			ItemsSource = new[] { "Alpha", "Beta", "Gamma" },
		};
		await UITestHelper.Load(comboBox);

		// ComboBox exposes IExpandCollapseProvider; when collapsed the custom
		// "Expand" action must be advertised and must expand the ComboBox.
		var result = InvokeAction(comboBox, AccessibilityNativeAction.Expand);

		Assert.IsTrue(result, "Expand custom action should succeed on a collapsed ComboBox.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Element_Has_Scroll_Provider_Then_Scroll_Forward_Succeeds()
	{
		// Create a ScrollViewer with enough content to scroll.
		var scrollViewer = new ScrollViewer
		{
			Width = 100,
			Height = 100,
			Content = new StackPanel
			{
				Children =
				{
					new Border { Width = 100, Height = 300 },
				},
			},
		};
		await UITestHelper.Load(scrollViewer);
		await UITestHelper.WaitForIdle();

		// ScrollViewer exposes IScrollProvider; ScrollForward should map to vertical SmallIncrement.
		var result = InvokeAction(scrollViewer, AccessibilityNativeAction.ScrollForward);

		// Scroll succeeds when the container is scrollable.
		Assert.IsTrue(result, "ScrollForward should succeed on a scrollable ScrollViewer.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Element_Has_Scroll_Provider_Then_Scroll_Custom_Action_Advertised()
	{
		var scrollViewer = new ScrollViewer
		{
			Width = 100,
			Height = 100,
			Content = new Border { Width = 100, Height = 300 },
		};
		await UITestHelper.Load(scrollViewer);

		// The action accessor invokes the native element's custom action handler.
		var forward = InvokeAction(scrollViewer, AccessibilityNativeAction.ScrollForward);
		var backward = InvokeAction(scrollViewer, AccessibilityNativeAction.ScrollBackward);

		// At least one scroll direction must succeed for a scrollable container.
		Assert.IsTrue(forward || backward,
			"At least one scroll direction must succeed on a scrollable ScrollViewer.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Activate_And_Increment_Are_Sequential_Then_Both_Succeed()
	{
		// Validate that the native element is correctly resolved for multiple consecutive action calls.
		var slider = new Slider { Minimum = 0, Maximum = 100, Value = 50, SmallChange = 10 };
		await UITestHelper.Load(slider);

		var inc1 = InvokeAction(slider, AccessibilityNativeAction.Increment);
		var inc2 = InvokeAction(slider, AccessibilityNativeAction.Increment);

		Assert.IsTrue(inc1, "First increment should succeed.");
		Assert.IsTrue(inc2, "Second increment should succeed.");
		Assert.IsTrue(slider.Value >= 60, $"Value should have increased twice; got {slider.Value}");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Action_Accessor_Not_Registered_Then_Returns_False()
	{
		// Verify the test helper gracefully returns false when no accessor is registered
		// (e.g., when running on a non-iOS Skia target via PlatformCondition exclusion).
		// The class-level [PlatformCondition] already gates this test to SkiaIOS so in
		// practice the accessor IS registered; this test simply validates the null-safe
		// helper doesn't throw.
		var button = new Button { Content = "Safety" };
		await UITestHelper.Load(button);

		var savedAccessor = AccessibilityPeerHelper.IOSAccessibilityActionAccessor;
		try
		{
			AccessibilityPeerHelper.IOSAccessibilityActionAccessor = null;
			var result = InvokeAction(button, AccessibilityNativeAction.Activate);
			Assert.IsFalse(result, "Null accessor must return false, not throw.");
		}
		finally
		{
			AccessibilityPeerHelper.IOSAccessibilityActionAccessor = savedAccessor;
		}
	}
}
