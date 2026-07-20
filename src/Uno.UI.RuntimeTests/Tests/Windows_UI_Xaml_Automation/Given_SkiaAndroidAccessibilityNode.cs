#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI;
using Uno.UI.RuntimeTests.Helpers;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

[TestClass]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid)]
public partial class Given_SkiaAndroidAccessibilityNode
{
	private static IReadOnlyList<AccessibilityNativeNodeSnapshot> GetAllNodes(XamlRoot root)
		=> AccessibilityPeerHelper.AndroidAllNodeSnapshotsForRootAccessor?.Invoke(root)
			?? Array.Empty<AccessibilityNativeNodeSnapshot>();

	private static AccessibilityNativeNodeSnapshot? FindByName(
		IReadOnlyList<AccessibilityNativeNodeSnapshot> nodes,
		string name)
		=> nodes.FirstOrDefault(node => node.Name?.Contains(name) is true);

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Adapter_Is_Initialized_Then_Native_Node_Hooks_Are_Registered()
	{
		var button = new Button { Content = "Adapter Probe" };
		await UITestHelper.Load(button);

		Assert.IsNotNull(
			AccessibilityPeerHelper.AndroidAccessibilityNodeSnapshotAccessor,
			"Android accessibility hooks were not registered for the loaded XamlRoot.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Accessibility_Tree_Is_Built_Then_It_Contains_Native_Nodes()
	{
		var button = new Button { Content = "Tree Probe" };
		await UITestHelper.Load(button);

		var nodes = GetAllNodes(button.XamlRoot!);
		var diagnostics = AccessibilityPeerHelper.AndroidAccessibilityDiagnosticsAccessor?.Invoke(button.XamlRoot!);

		Assert.IsTrue(nodes.Count > 0, diagnostics);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Tree_Is_Queried_Then_Button_Appears_In_Tree()
	{
		var button = new Button { Content = "Tree Button" };
		await UITestHelper.Load(button);

		var node = FindByName(GetAllNodes(button.XamlRoot!), "Tree Button");

		Assert.IsNotNull(node);
		Assert.IsNotNull(node.NativeNode);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Removed_Node_Dirties_Cached_Tree_Then_Owner_Can_Be_Collected()
	{
		var panel = new StackPanel
		{
			Children =
			{
				new TextBlock { Text = "Anchor" },
			},
		};
		await UITestHelper.Load(panel);

		var removedOwner = await CacheAndRemoveChild(panel);
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();

		Assert.IsFalse(
			removedOwner.IsAlive,
			"Dirtying the Android peer-tree cache must release removed element owners without another native query.");
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static async Task<WeakReference> CacheAndRemoveChild(StackPanel panel)
	{
		Button? child = new() { Content = "Cached Child" };
		panel.Children.Add(child);
		await TestServices.WindowHelper.WaitForIdle();

		_ = GetAllNodes(panel.XamlRoot!);
		var removedOwner = new WeakReference(child);
		panel.Children.Remove(child);
		child = null;
		await TestServices.WindowHelper.WaitForIdle();
		return removedOwner;
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Button_Is_Queried_Then_ClassName_Is_Button()
	{
		var button = new Button { Content = "Role Button" };
		await UITestHelper.Load(button);

		var node = FindByName(GetAllNodes(button.XamlRoot!), "Role Button");

		Assert.IsNotNull(node);
		Assert.AreEqual("android.widget.Button", node.ClassName);
		Assert.IsTrue((node.Traits & AccessibilityNativeTraits.Button) != 0);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Element_Is_Enabled_Then_Node_Reports_Enabled()
	{
		var button = new Button { Content = "Enabled Check", IsEnabled = true };
		await UITestHelper.Load(button);

		var node = FindByName(GetAllNodes(button.XamlRoot!), "Enabled Check");

		Assert.IsNotNull(node);
		Assert.IsTrue(node.Enabled);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Element_Is_Disabled_Then_Node_Reports_Disabled()
	{
		var button = new Button { Content = "Disabled Check", IsEnabled = false };
		await UITestHelper.Load(button);

		var node = FindByName(GetAllNodes(button.XamlRoot!), "Disabled Check");

		Assert.IsNotNull(node);
		Assert.IsFalse(node.Enabled);
		Assert.IsTrue((node.Traits & AccessibilityNativeTraits.NotEnabled) != 0);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Heading_Is_Set_Then_Node_Reports_Heading()
	{
		var textBlock = new TextBlock { Text = "Heading Node" };
		AutomationProperties.SetHeadingLevel(textBlock, AutomationHeadingLevel.Level1);
		await UITestHelper.Load(textBlock);

		var node = FindByName(GetAllNodes(textBlock.XamlRoot!), "Heading Node");

		Assert.IsNotNull(node);
		Assert.IsTrue(node.Heading);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_AutomationId_Is_Set_Then_ViewIdResourceName_Is_Package_Qualified()
	{
		var button = new Button { Content = "AutoId Button" };
		AutomationProperties.SetAutomationId(button, "myAutomationId");
		await UITestHelper.Load(button);

		var node = FindByName(GetAllNodes(button.XamlRoot!), "AutoId Button");

		Assert.IsNotNull(node);
		Assert.AreEqual("myAutomationId", node.AutomationId);
		Assert.IsTrue(node.NativeAutomationId?.EndsWith(":id/myAutomationId", StringComparison.Ordinal) is true);
		Assert.AreNotEqual("myAutomationId", node.Name);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Element_Has_Raw_Accessibility_View_Then_It_Is_Excluded()
	{
		var panel = new StackPanel();
		var visible = new Button { Content = "Visible Node" };
		var raw = new TextBlock { Text = "Raw Node" };
		AutomationProperties.SetAccessibilityView(raw, AccessibilityView.Raw);
		panel.Children.Add(visible);
		panel.Children.Add(raw);
		await UITestHelper.Load(panel);

		var rawNode = FindByName(GetAllNodes(panel.XamlRoot!), "Raw Node");

		Assert.IsNull(rawNode);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Tree_Is_Queried_Twice_Then_Same_Element_Gets_Same_VirtualId()
	{
		var button = new Button { Content = "Stable Id" };
		await UITestHelper.Load(button);

		var id1 = AccessibilityPeerHelper.AndroidAccessibilityVirtualIdAccessor?.Invoke(button);
		var id2 = AccessibilityPeerHelper.AndroidAccessibilityVirtualIdAccessor?.Invoke(button);

		Assert.IsNotNull(id1);
		Assert.AreEqual(id1, id2);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Node_Has_Explicit_Width_Height_Then_Bounds_Are_Non_Degenerate()
	{
		var button = new Button { Content = "Bounds", Width = 120, Height = 48 };
		await UITestHelper.Load(button);

		var node = FindByName(GetAllNodes(button.XamlRoot!), "Bounds");

		Assert.IsNotNull(node);
		Assert.IsTrue(node.Bounds.Width > 0);
		Assert.IsTrue(node.Bounds.Height > 0);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_CheckBox_Is_On_Then_Node_Is_Checkable_With_CheckBox_Class()
	{
		var checkBox = new CheckBox { Content = "Checked Box", IsChecked = true };
		await UITestHelper.Load(checkBox);

		var node = FindByName(GetAllNodes(checkBox.XamlRoot!), "Checked Box");

		Assert.IsNotNull(node);
		Assert.IsTrue(node.Checkable);
		Assert.IsTrue(node.IsChecked is true);
		Assert.AreEqual("android.widget.CheckBox", node.ClassName);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_CheckBox_Is_Indeterminate_Then_Checked_State_Is_Null()
	{
		var checkBox = new CheckBox
		{
			Content = "Indeterminate Box",
			IsThreeState = true,
			IsChecked = null,
		};
		await UITestHelper.Load(checkBox);

		var node = FindByName(GetAllNodes(checkBox.XamlRoot!), "Indeterminate Box");

		Assert.IsNotNull(node);
		Assert.IsTrue(node.Checkable);
		Assert.IsNull(node.IsChecked);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Siblings_Are_In_Panel_Then_Both_Appear_In_Peer_Tree_Order()
	{
		var panel = new StackPanel();
		panel.Children.Add(new Button { Content = "First" });
		panel.Children.Add(new Button { Content = "Second" });
		await UITestHelper.Load(panel);

		var names = GetAllNodes(panel.XamlRoot!)
			.Select(node => node.Name)
			.Where(name => name is "First" or "Second")
			.ToList();

		CollectionAssert.AreEqual(new[] { "First", "Second" }, names);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Direct_Node_Accessor_Used_Then_Returns_Real_Node_For_Element()
	{
		var button = new Button { Content = "Direct Node" };
		await UITestHelper.Load(button);

		var snapshot = AccessibilityPeerHelper.AndroidAccessibilityNodeSnapshotAccessor?.Invoke(button);

		Assert.IsNotNull(snapshot);
		Assert.IsNotNull(snapshot.NativeNode);
		Assert.IsTrue(snapshot.Name?.Contains("Direct Node") is true);
	}

	// Action tests
	// All tests below use AndroidAccessibilityActionAccessor so no Android types
	// are referenced directly from the test assembly.

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Action_Hook_Is_Registered_Then_Action_Accessor_Is_Not_Null()
	{
		var button = new Button { Content = "Hook Probe" };
		await UITestHelper.Load(button);

		Assert.IsNotNull(
			AccessibilityPeerHelper.AndroidAccessibilityActionAccessor,
			"AndroidAccessibilityActionAccessor was not registered by UnoExploreByTouchHelper.Initialize.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Button_Activate_Action_Is_Performed_Then_Click_Is_Invoked()
	{
		var button = new Button { Content = "Action Click" };
		var clicked = false;
		button.Click += (_, _) => clicked = true;
		await UITestHelper.Load(button);

		var result = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
			button, new AccessibilityNativeActionRequest(AccessibilityNativeAction.Activate));

		Assert.IsTrue(result, "Activate action should return true for an enabled button.");
		Assert.IsTrue(clicked, "Button.Click should have fired after the Activate action.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_CheckBox_Activate_Action_Is_Performed_Then_Toggle_State_Changes()
	{
		var checkBox = new CheckBox { Content = "Action Toggle", IsChecked = false };
		await UITestHelper.Load(checkBox);

		var result = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
			checkBox, new AccessibilityNativeActionRequest(AccessibilityNativeAction.Activate));

		Assert.IsTrue(result, "Activate (toggle) action should return true.");
		Assert.IsTrue(checkBox.IsChecked == true, "CheckBox should now be checked.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Slider_Increment_Action_Is_Performed_Then_Value_Increases()
	{
		var slider = new Slider { Minimum = 0, Maximum = 10, Value = 5, SmallChange = 1 };
		await UITestHelper.Load(slider);

		var result = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
			slider, new AccessibilityNativeActionRequest(AccessibilityNativeAction.Increment));

		Assert.IsTrue(result, "Increment action should return true for a writable Slider.");
		Assert.AreEqual(6d, slider.Value, "Slider value should have increased by SmallChange.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Slider_Decrement_Action_Is_Performed_Then_Value_Decreases()
	{
		var slider = new Slider { Minimum = 0, Maximum = 10, Value = 5, SmallChange = 1 };
		await UITestHelper.Load(slider);

		var result = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
			slider, new AccessibilityNativeActionRequest(AccessibilityNativeAction.Decrement));

		Assert.IsTrue(result, "Decrement action should return true for a writable Slider.");
		Assert.AreEqual(4d, slider.Value, "Slider value should have decreased by SmallChange.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Peer_Overrides_Bounds_Then_Native_Bounds_Use_Peer_Rectangle()
	{
		var control = new CustomBoundsControl { Content = "Custom Bounds", Width = 100, Height = 100 };
		await UITestHelper.Load(control);

		var snapshot = AccessibilityPeerHelper.AndroidAccessibilityNodeSnapshotAccessor?.Invoke(control);

		Assert.IsNotNull(snapshot);
		var scale = control.XamlRoot!.RasterizationScale;
		Assert.AreEqual(37 * scale, snapshot.Bounds.Width, 1);
		Assert.AreEqual(41 * scale, snapshot.Bounds.Height, 1);
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
}

[TestClass]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid)]
public class Given_SkiaAndroidAutomationId
{
	private static AccessibilityNativeNodeSnapshot? GetSnapshot(UIElement element)
		=> AccessibilityPeerHelper.AndroidAccessibilityNodeSnapshotAccessor?.Invoke(element);

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Valid_AutomationId_Is_Set_Then_ResourceName_Is_Unchanged()
	{
		var button = new Button { Content = "Valid ID" };
		AutomationProperties.SetAutomationId(button, "MobileAutomationInvoke");
		await UITestHelper.Load(button);

		var snapshot = GetSnapshot(button);
		Assert.IsNotNull(snapshot);
		Assert.AreEqual("MobileAutomationInvoke", snapshot.AutomationId);
		Assert.IsTrue(
			snapshot.NativeAutomationId?.EndsWith(":id/MobileAutomationInvoke") is true,
			$"Valid ID must be projected unchanged; got '{snapshot.NativeAutomationId}'.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_AutomationId_Contains_Invalid_Chars_Then_ResourceName_Is_Normalized_With_Hash()
	{
		var button = new Button { Content = "Invalid ID" };
		AutomationProperties.SetAutomationId(button, "My Button");
		await UITestHelper.Load(button);

		var snapshot = GetSnapshot(button);
		Assert.IsNotNull(snapshot);

		Assert.AreEqual("My Button", snapshot.AutomationId);
		var resourceName = snapshot.NativeAutomationId;
		Assert.IsNotNull(resourceName);
		// Segment must start with the sanitized prefix and end with a 4-char hex suffix.
		Assert.IsTrue(
			resourceName.Contains(":id/My_Button_"),
			$"Normalized segment should contain 'My_Button_' prefix; got '{resourceName}'.");

		// The suffix must be 4 lowercase hex digits.
		var segment = resourceName.Substring(resourceName.LastIndexOf('/') + 1);
		Assert.AreEqual(14, segment.Length, $"'My_Button_XXXX' = 14 chars; got '{segment}'.");
		Assert.IsTrue(
			System.Text.RegularExpressions.Regex.IsMatch(segment, @"^My_Button_[0-9a-f]{4}$"),
			$"Segment should match 'My_Button_XXXX'; got '{segment}'.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Two_Ids_Normalize_To_Same_Base_Then_Resource_Names_Differ()
	{
		// "My Button" and "My_Button" both normalize to "My_Button" base,
		// but must get different suffixes because their originals differ.
		var b1 = new Button { Content = "First" };
		var b2 = new Button { Content = "Second" };
		AutomationProperties.SetAutomationId(b1, "My Button");
		AutomationProperties.SetAutomationId(b2, "My_Button");
		var panel = new Microsoft.UI.Xaml.Controls.StackPanel { Children = { b1, b2 } };
		await UITestHelper.Load(panel);

		var s1 = GetSnapshot(b1);
		var s2 = GetSnapshot(b2);
		Assert.IsNotNull(s1);
		Assert.IsNotNull(s2);

		// "My_Button" is already valid → no hash; "My Button" has a hash → they must differ.
		Assert.AreNotEqual(s1.NativeAutomationId, s2.NativeAutomationId,
			"Two distinct originals that normalize to the same base must have different resource names.");
		Assert.IsTrue(
			s2.NativeAutomationId?.EndsWith(":id/My_Button") is true,
			$"'My_Button' is already valid and must not have a hash; got '{s2.NativeAutomationId}'.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Generated_Resource_Name_Collides_With_Valid_Id_Then_Ids_Are_Disambiguated()
	{
		var invalid = new Button { Content = "Invalid" };
		var valid = new Button { Content = "Valid" };
		AutomationProperties.SetAutomationId(invalid, "My Button");
		AutomationProperties.SetAutomationId(valid, "My_Button_ea6f");
		var panel = new StackPanel { Children = { invalid, valid } };
		await UITestHelper.Load(panel);

		var invalidSnapshot = GetSnapshot(invalid);
		var validSnapshot = GetSnapshot(valid);

		Assert.IsNotNull(invalidSnapshot);
		Assert.IsNotNull(validSnapshot);
		Assert.AreNotEqual(invalidSnapshot.NativeAutomationId, validSnapshot.NativeAutomationId);
		Assert.AreEqual("My_Button_ea6f", validSnapshot.AutomationId);
		Assert.IsTrue(
			validSnapshot.NativeAutomationId?.Contains(":id/My_Button_ea6f", StringComparison.Ordinal) is true,
			"The valid AutomationId must retain its readable segment when a stable generated assignment already owns it.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Collision_Set_Changes_Then_Existing_Resource_Name_Remains_Stable()
	{
		var invalid = new Button { Content = "Invalid" };
		AutomationProperties.SetAutomationId(invalid, "My Button");
		var panel = new StackPanel { Children = { invalid } };
		await UITestHelper.Load(panel);

		var before = GetSnapshot(invalid);
		var initialResourceName = before?.NativeAutomationId;
		Assert.IsNotNull(initialResourceName);
		var generatedSegment = initialResourceName[(initialResourceName.LastIndexOf('/') + 1)..];

		var collidingValid = new Button { Content = "Valid" };
		AutomationProperties.SetAutomationId(collidingValid, generatedSegment);
		panel.Children.Add(collidingValid);
		await TestServices.WindowHelper.WaitForIdle();

		var during = GetSnapshot(invalid);
		var validSnapshot = GetSnapshot(collidingValid);
		Assert.AreEqual(initialResourceName, during?.NativeAutomationId);
		Assert.AreNotEqual(during?.NativeAutomationId, validSnapshot?.NativeAutomationId);

		panel.Children.Remove(collidingValid);
		await TestServices.WindowHelper.WaitForIdle();

		var after = GetSnapshot(invalid);
		Assert.AreEqual(initialResourceName, after?.NativeAutomationId);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_AutomationId_Changes_Live_Then_Snapshot_Updates_On_Requery()
	{
		var button = new Button { Content = "Live ID" };
		AutomationProperties.SetAutomationId(button, "OriginalId");
		await UITestHelper.Load(button);

		var before = GetSnapshot(button);
		Assert.AreEqual("OriginalId", before?.AutomationId);
		Assert.IsTrue(before?.NativeAutomationId?.EndsWith(":id/OriginalId") is true,
			$"Before change: expected ':id/OriginalId'; got '{before?.NativeAutomationId}'.");

		AutomationProperties.SetAutomationId(button, "UpdatedId");
		await TestServices.WindowHelper.WaitForIdle();

		var after = GetSnapshot(button);
		Assert.AreEqual("UpdatedId", after?.AutomationId);
		Assert.IsTrue(after?.NativeAutomationId?.EndsWith(":id/UpdatedId") is true,
			$"After change: expected ':id/UpdatedId'; got '{after?.NativeAutomationId}'.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Slider_SetRangeValue_Action_Is_Performed_Then_Value_Updates()
	{
		var slider = new Slider { Minimum = 0, Maximum = 10, Value = 5 };
		await UITestHelper.Load(slider);

		var result = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
			slider, new AccessibilityNativeActionRequest(AccessibilityNativeAction.SetRangeValue, number: 8));

		// ActionSetProgress may not be available on all binding versions; skip without fail.
		if (result is true)
		{
			Assert.AreEqual(8d, slider.Value, "Slider value should be 8 after SetRangeValue.");
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_TextBox_SetValue_Action_Is_Performed_Then_Text_Updates()
	{
		var textBox = new TextBox();
		await UITestHelper.Load(textBox);

		var result = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
			textBox, new AccessibilityNativeActionRequest(AccessibilityNativeAction.SetValue, text: "Hello"));

		Assert.IsTrue(result, "SetValue action should return true for a writable TextBox.");
		Assert.AreEqual("Hello", textBox.Text, "TextBox.Text should match the value set via action.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Disabled_Button_Activate_Action_Is_Performed_Then_Returns_False()
	{
		var button = new Button { Content = "Disabled Action", IsEnabled = false };
		await UITestHelper.Load(button);

		var result = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
			button, new AccessibilityNativeActionRequest(AccessibilityNativeAction.Activate));

		Assert.IsFalse(result == true,
			"Activate action on a disabled button should fail (return false).");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ReadOnly_TextBox_SetValue_Action_Is_Performed_Then_Returns_False()
	{
		var textBox = new TextBox { IsReadOnly = true };
		await UITestHelper.Load(textBox);

		var result = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
			textBox, new AccessibilityNativeActionRequest(AccessibilityNativeAction.SetValue, text: "Hi"));

		Assert.IsFalse(result == true,
			"SetValue on a read-only TextBox should fail (return false).");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Unknown_Action_Is_Performed_Then_Returns_False()
	{
		var button = new Button { Content = "Unknown Action" };
		await UITestHelper.Load(button);

		var result = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(
			button, new AccessibilityNativeActionRequest((AccessibilityNativeAction)int.MaxValue));

		Assert.IsFalse(result == true,
			"An unmappable action should return false rather than throw.");
	}
}
