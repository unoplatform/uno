#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

[TestClass]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid | RuntimeTestPlatforms.SkiaIOS)]
public class Given_MobileAccessibilityAutomation
{
	private static AccessibilityNativeNodeSnapshot? GetSnapshot(UIElement element)
		=> MobileAccessibilityTestHelper.TryGetNativeSnapshot(element);

	private static object? GetMachineIdentity(UIElement element)
		=> (object?)AccessibilityPeerHelper.AndroidAccessibilityVirtualIdAccessor?.Invoke(element)
			?? AccessibilityPeerHelper.IOSAccessibilityElementAccessor?.Invoke(element);

	private static bool InvokeAction(UIElement element, AccessibilityNativeActionRequest request)
		=> AccessibilityPeerHelper.AndroidAccessibilityActionAccessor?.Invoke(element, request)
			?? AccessibilityPeerHelper.IOSAccessibilityActionAccessor?.Invoke(element, request)
			?? false;

	private static AccessibilityNativeEventRecord[] GetEvents(XamlRoot root)
		=> AccessibilityPeerHelper.AndroidAccessibilityEventsAccessor?.Invoke(root)
			?? AccessibilityPeerHelper.IOSAccessibilityEventsAccessor?.Invoke(root)
			?? Array.Empty<AccessibilityNativeEventRecord>();

	private static void ClearEvents(XamlRoot root)
	{
		AccessibilityPeerHelper.AndroidClearAccessibilityEventsAction?.Invoke(root);
		AccessibilityPeerHelper.IOSClearAccessibilityEventsAction?.Invoke(root);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_AutomationId_Is_Set_Then_Snapshot_AutomationId_Ends_With_Id_And_Name_Is_Content()
	{
		const string automationId = "machine_probe_1";
		var button = new Button { Content = "Spoken label" };
		AutomationProperties.SetAutomationId(button, automationId);
		await UITestHelper.Load(button);

		var snapshot = GetSnapshot(button);

		Assert.IsNotNull(snapshot, "Native snapshot was not produced.");
		// Android may package-qualify the ID (e.g. "com.example:id/machine-probe-1").
		Assert.IsTrue(
			snapshot.AutomationId?.EndsWith(automationId, StringComparison.Ordinal) is true,
			$"Expected AutomationId ending '{automationId}', got '{snapshot.AutomationId}'.");
		Assert.AreEqual("Spoken label", snapshot.Name,
			"Name must equal the accessible content, not the machine identifier.");
		Assert.AreNotEqual(automationId, snapshot.Name,
			"Name must never equal the raw AutomationId string.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Only_AutomationId_Is_Set_Then_Name_Does_Not_Borrow_Identifier()
	{
		const string automationId = "machine_only_id";
		var button = new Button();
		AutomationProperties.SetAutomationId(button, automationId);
		await UITestHelper.Load(button);

		var snapshot = GetSnapshot(button);

		Assert.IsNotNull(snapshot, "Native snapshot was not produced.");
		Assert.IsTrue(
			snapshot.AutomationId?.EndsWith(automationId, StringComparison.Ordinal) is true,
			$"Expected AutomationId ending '{automationId}', got '{snapshot.AutomationId}'.");
		Assert.IsFalse(
			snapshot.Name?.Contains(automationId, StringComparison.Ordinal) is true,
			"An unnamed element must not borrow AutomationId as its spoken name.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_AutomationId_Is_Absent_Then_Name_Is_Content()
	{
		var button = new Button { Content = "No-id label" };
		await UITestHelper.Load(button);

		var snapshot = GetSnapshot(button);

		Assert.IsNotNull(snapshot, "Native snapshot was not produced.");
		Assert.AreEqual("No-id label", snapshot.Name,
			"Name must be the accessible content even when AutomationId is absent.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_AutomationId_Is_Updated_Then_Snapshot_Reflects_New_Id_And_Node_Identity_Is_Stable()
	{
		const string firstId = "identity_first";
		const string secondId = "identity_second";
		var button = new Button { Content = "Stable label" };
		AutomationProperties.SetAutomationId(button, firstId);
		await UITestHelper.Load(button);

		var identityBefore = GetMachineIdentity(button);
		var snapshotBefore = GetSnapshot(button);

		Assert.IsNotNull(identityBefore, "Machine identity token must be available before update.");
		Assert.IsNotNull(snapshotBefore);
		Assert.IsTrue(
			snapshotBefore.AutomationId?.EndsWith(firstId, StringComparison.Ordinal) is true,
			$"Initial AutomationId expected to end with '{firstId}', got '{snapshotBefore.AutomationId}'.");

		AutomationProperties.SetAutomationId(button, secondId);
		await UITestHelper.WaitForIdle();

		var identityAfter = GetMachineIdentity(button);
		var snapshotAfter = GetSnapshot(button);

		Assert.IsNotNull(identityAfter, "Machine identity token must remain available after update.");
		Assert.AreEqual(identityBefore, identityAfter,
			"The native node/virtual-ID must not change when only AutomationId is updated.");
		Assert.IsNotNull(snapshotAfter);
		Assert.IsTrue(
			snapshotAfter.AutomationId?.EndsWith(secondId, StringComparison.Ordinal) is true,
			$"Updated AutomationId expected to end with '{secondId}', got '{snapshotAfter.AutomationId}'.");
		Assert.AreEqual("Stable label", snapshotAfter.Name,
			"Accessible Name must remain unchanged after an AutomationId update.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Two_Controls_Share_Name_But_Have_Unique_Ids_Then_Machine_AutomationIds_Differ()
	{
		const string sharedName = "Submit";
		var primary = new Button { Content = sharedName };
		var secondary = new Button { Content = sharedName };
		AutomationProperties.SetAutomationId(primary, "submit_primary");
		AutomationProperties.SetAutomationId(secondary, "submit_secondary");
		var panel = new StackPanel { Children = { primary, secondary } };
		await UITestHelper.Load(panel);

		var snapshotA = GetSnapshot(primary);
		var snapshotB = GetSnapshot(secondary);

		Assert.IsNotNull(snapshotA, "Snapshot for primary must be available.");
		Assert.IsNotNull(snapshotB, "Snapshot for secondary must be available.");
		Assert.AreEqual(sharedName, snapshotA.Name, "Primary Name must be the spoken label.");
		Assert.AreEqual(sharedName, snapshotB.Name, "Secondary Name must be the spoken label.");
		Assert.AreNotEqual(snapshotA.AutomationId, snapshotB.AutomationId,
			"Controls with distinct AutomationIds must have distinct native identifiers.");
		Assert.IsTrue(
			snapshotA.AutomationId?.EndsWith("submit_primary", StringComparison.Ordinal) is true,
			$"Primary AutomationId expected to end with 'submit_primary', got '{snapshotA.AutomationId}'.");
		Assert.IsTrue(
			snapshotB.AutomationId?.EndsWith("submit_secondary", StringComparison.Ordinal) is true,
			$"Secondary AutomationId expected to end with 'submit_secondary', got '{snapshotB.AutomationId}'.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_PasswordBox_Is_Snapshotted_Then_Password_Flag_Is_True_And_No_Plaintext_Escapes()
	{
		var fixture = string.Concat("mobile-", "secret-", "sentinel-42");
		var pwBox = new PasswordBox();
		AutomationProperties.SetAutomationId(pwBox, "secure_field");
		AutomationProperties.SetName(pwBox, "Account password");
		await UITestHelper.Load(pwBox);

		ClearEvents(pwBox.XamlRoot!);
		pwBox.Password = fixture;
		await UITestHelper.WaitForIdle();

		var snapshot = GetSnapshot(pwBox);

		Assert.IsNotNull(snapshot, "Native snapshot was not produced for PasswordBox.");
		Assert.IsTrue(snapshot.Password, "Password flag must be true for a PasswordBox.");

		Assert.IsFalse(snapshot.Name?.Contains(fixture) is true,
			"Name must not expose the plaintext password.");
		Assert.IsFalse(snapshot.Value?.Contains(fixture) is true,
			"Value must not expose the plaintext password.");
		Assert.IsFalse(snapshot.Hint?.Contains(fixture) is true,
			"Hint must not expose the plaintext password.");
		Assert.IsTrue(
			snapshot.AutomationId?.EndsWith("secure_field", StringComparison.Ordinal) is true,
			$"AutomationId must be the machine identifier, got '{snapshot.AutomationId}'.");
		Assert.IsFalse(snapshot.AutomationId?.Contains(fixture) is true,
			"AutomationId must not expose the plaintext password.");

		if (snapshot.Details is { } details)
		{
			var detailText = new[]
			{
				details.ItemStatus,
				details.ItemType,
				details.LocalizedControlType,
				details.FullDescription,
				details.LocalizedLandmarkType,
			};
			Assert.IsFalse(
				detailText.Any(value => value?.Contains(fixture, StringComparison.Ordinal) is true),
				"Rich native details must not expose the plaintext password.");
			Assert.IsFalse(
				details.Relations is { } relations &&
				relations.LabeledByIds
					.Concat(relations.DescribedByIds)
					.Concat(relations.ControlledPeerIds)
					.Concat(relations.FlowsFromIds)
					.Concat(relations.FlowsToIds)
					.Concat(relations.AnnotationTypeNames)
					.Any(value => value.Contains(fixture, StringComparison.Ordinal)),
				"Relation metadata must not expose the plaintext password.");
		}

		Assert.IsFalse(
			GetEvents(pwBox.XamlRoot!).Any(record =>
				record.Name?.Contains(fixture, StringComparison.Ordinal) is true ||
				record.Text?.Contains(fixture, StringComparison.Ordinal) is true),
			"Native event records must not expose the plaintext password.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Id_Bearing_Button_Is_Invoked_Then_Click_Fires()
	{
		var button = new Button { Content = "Invoke target" };
		AutomationProperties.SetAutomationId(button, "MobileAutomationInvoke");
		var clicked = false;
		button.Click += (_, _) => clicked = true;
		await UITestHelper.Load(button);

		var result = InvokeAction(button, new AccessibilityNativeActionRequest(AccessibilityNativeAction.Activate));

		Assert.IsTrue(result, "Activate action must return true for an enabled ID-bearing button.");
		Assert.IsTrue(clicked, "Button.Click must fire after the Activate action.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Id_Bearing_CheckBox_Is_Toggled_Then_State_Changes()
	{
		var checkBox = new CheckBox { Content = "Enable fixture option", IsChecked = false };
		AutomationProperties.SetAutomationId(checkBox, "MobileAutomationToggle");
		await UITestHelper.Load(checkBox);

		var result = InvokeAction(checkBox, new AccessibilityNativeActionRequest(AccessibilityNativeAction.Activate));

		Assert.IsTrue(result, "Activate (toggle) action must succeed on an ID-bearing CheckBox.");
		Assert.IsTrue(checkBox.IsChecked == true, "CheckBox must be checked after toggle.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Id_Bearing_Slider_Has_Range_Action_Then_Value_Adjusts()
	{
		var slider = new Slider { Minimum = 0, Maximum = 100, Value = 40, SmallChange = 1 };
		AutomationProperties.SetAutomationId(slider, "MobileAutomationRange");
		await UITestHelper.Load(slider);

		if (RuntimeTestsPlatformHelper.CurrentPlatform == RuntimeTestPlatforms.SkiaAndroid)
		{
			var setResult = InvokeAction(
				slider,
				new AccessibilityNativeActionRequest(
					AccessibilityNativeAction.SetRangeValue,
					number: 75));
			Assert.IsTrue(setResult, "Android must expose the set-progress action for a writable Slider.");
			Assert.AreEqual(75d, slider.Value,
				"Slider value must be 75 after SetRangeValue on an ID-bearing Slider.");
		}
		else
		{
			var incResult = InvokeAction(
				slider, new AccessibilityNativeActionRequest(AccessibilityNativeAction.Increment));
			Assert.IsTrue(incResult,
				"iOS must expose Increment for a writable Slider.");
			Assert.IsTrue(slider.Value > 40,
				$"Value must have increased; got {slider.Value}.");
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Id_Bearing_TextBox_Has_SetValue_Action_Then_Text_Updates()
	{
		var textBox = new TextBox { Text = "Ada" };
		AutomationProperties.SetAutomationId(textBox, "MobileAutomationText");
		await UITestHelper.Load(textBox);

		var result = InvokeAction(
			textBox, new AccessibilityNativeActionRequest(AccessibilityNativeAction.SetValue, text: "Bob"));

		Assert.IsTrue(result, "SetValue action must return true for a writable ID-bearing TextBox.");
		Assert.AreEqual("Bob", textBox.Text,
			"TextBox.Text must match the value set via the SetValue action.");
	}
}
