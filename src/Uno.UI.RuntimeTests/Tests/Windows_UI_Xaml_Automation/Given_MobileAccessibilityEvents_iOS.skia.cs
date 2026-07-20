#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

/// <summary>
/// Verifies <see cref="AccessibilityNativeEventRecord"/> entries produced by the iOS adapter
/// for property changes, structural mutations, announcements, text changes, and selection changes.
/// Gated to SkiaIOS; no UIKit references.
/// </summary>
[TestClass]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaIOS)]
public class Given_MobileAccessibilityEvents_iOS
{
	private static AccessibilityNativeEventRecord[] GetEvents(XamlRoot root)
		=> AccessibilityPeerHelper.IOSAccessibilityEventsAccessor?.Invoke(root)
			?? Array.Empty<AccessibilityNativeEventRecord>();

	private static void ClearEvents(XamlRoot root)
		=> AccessibilityPeerHelper.IOSClearAccessibilityEventsAction?.Invoke(root);

	private static AccessibilityNativeNodeSnapshot? GetSnapshot(UIElement element)
		=> AccessibilityPeerHelper.IOSAccessibilityNodeSnapshotAccessor?.Invoke(element);

	// Hook registration

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_iOS_Event_Hooks_Are_Registered_Then_Accessors_Are_Not_Null()
	{
		var button = new Button { Content = "Probe" };
		await UITestHelper.Load(button);

		Assert.IsNotNull(AccessibilityPeerHelper.IOSAccessibilityEventsAccessor);
		Assert.IsNotNull(AccessibilityPeerHelper.IOSClearAccessibilityEventsAction);
	}

	// Property changes produce one coalesced native invalidation per dispatcher cycle.

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Button_Name_Changed_Then_NodeInvalidated_Recorded()
	{
		var button = new Button { Content = "Original" };
		await UITestHelper.Load(button);
		var root = button.XamlRoot!;

		ClearEvents(root);
		AutomationProperties.SetName(button, "Updated");
		await UITestHelper.WaitForIdle();

		Assert.IsTrue(
			GetEvents(root).Any(e => e.Kind == AccessibilityNativeEventKind.NodeInvalidated),
			"Expected NodeInvalidated after name change.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Two_Properties_Changed_On_Same_Element_Then_Exactly_One_NodeInvalidated()
	{
		// Two properties on the same handle share one LayoutChanged notification.
		var button = new Button { Content = "Multi" };
		await UITestHelper.Load(button);
		var root = button.XamlRoot!;

		ClearEvents(root);
		AutomationProperties.SetName(button, "Name A");
		AutomationProperties.SetHelpText(button, "Hint");
		await UITestHelper.WaitForIdle();

		var count = GetEvents(root).Count(e => e.Kind == AccessibilityNativeEventKind.NodeInvalidated);
		Assert.AreEqual(1, count,
			"Two properties on one element must produce exactly one coalesced NodeInvalidated.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Two_Elements_Changed_In_One_Cycle_Then_One_Coalesced_Signal()
	{
		// Synchronous changes share one LayoutChanged(null) without moving focus.
		var b1 = new Button { Content = "B1" };
		var b2 = new Button { Content = "B2" };
		var panel = new StackPanel { Children = { b1, b2 } };
		await UITestHelper.Load(panel);
		var root = panel.XamlRoot!;

		ClearEvents(root);
		AutomationProperties.SetName(b1, "New B1");
		AutomationProperties.SetName(b2, "New B2");
		await UITestHelper.WaitForIdle();

		var count = GetEvents(root).Count(e => e.Kind == AccessibilityNativeEventKind.NodeInvalidated);
		Assert.AreEqual(1, count,
			"Two elements changed in one burst must produce exactly one coalesced NodeInvalidated.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Property_Changed_Then_Snapshot_Reflects_Final_State()
	{
		var button = new Button { Content = "Before" };
		await UITestHelper.Load(button);
		ClearEvents(button.XamlRoot!);

		button.Content = "After";
		await UITestHelper.WaitForIdle();

		Assert.AreEqual("After", GetSnapshot(button)?.Name,
			"Live snapshot must reflect the final content after coalescing.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Enabled_Changes_Then_NodeInvalidated_Recorded()
	{
		var button = new Button { Content = "Toggle" };
		await UITestHelper.Load(button);
		var root = button.XamlRoot!;

		ClearEvents(root);
		button.IsEnabled = false;
		await UITestHelper.WaitForIdle();

		Assert.IsTrue(GetEvents(root).Any(e => e.Kind == AccessibilityNativeEventKind.NodeInvalidated));
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Slider_Value_Changed_Then_NodeInvalidated_Recorded()
	{
		var slider = new Slider { Minimum = 0, Maximum = 100, Value = 50 };
		await UITestHelper.Load(slider);
		var root = slider.XamlRoot!;

		ClearEvents(root);
		slider.Value = 75;
		await UITestHelper.WaitForIdle();

		Assert.IsTrue(GetEvents(root).Any(e => e.Kind == AccessibilityNativeEventKind.NodeInvalidated));
	}

	// ClearEvents

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ClearEvents_Called_Then_Log_Is_Empty()
	{
		var button = new Button { Content = "Clear" };
		await UITestHelper.Load(button);
		var root = button.XamlRoot!;

		AutomationProperties.SetName(button, "Pre-clear");
		await UITestHelper.WaitForIdle();
		Assert.IsTrue(GetEvents(root).Length > 0, "Setup: expected events before clear.");

		ClearEvents(root);

		Assert.AreEqual(0, GetEvents(root).Length, "Log must be empty after ClearEvents.");
	}

	// LayoutChanged(null) must not move VoiceOver focus

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Property_Changed_Then_Focused_Node_Is_Not_Moved()
	{
		var focusTarget = new Button { Content = "Focus" };
		var other = new Button { Content = "Other" };
		var panel = new StackPanel { Children = { focusTarget, other } };
		await UITestHelper.Load(panel);
		var root = panel.XamlRoot!;

		// Establish a native focus baseline via the focus hook.
		var focused = AccessibilityPeerHelper.IOSAccessibilityFocusAccessor?.Invoke(focusTarget);
		Assert.IsTrue(focused is true, "Setup: focus must be requestable on focusTarget.");

		var nodeBefore = AccessibilityPeerHelper.IOSFocusedNativeNodeAccessor?.Invoke(root);
		Assert.IsNotNull(nodeBefore, "Setup: focused node must be set after requesting focus.");

		// Mutate a property on a different element.
		AutomationProperties.SetName(other, "Updated");
		await UITestHelper.WaitForIdle();

		var nodeAfter = AccessibilityPeerHelper.IOSFocusedNativeNodeAccessor?.Invoke(root);
		Assert.AreSame(nodeBefore, nodeAfter,
			"Property change on a sibling must not move VoiceOver focus.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Child_Added_Then_Focused_Node_Is_Not_Moved()
	{
		var focusTarget = new Button { Content = "Focus" };
		var panel = new StackPanel { Children = { focusTarget } };
		await UITestHelper.Load(panel);
		var root = panel.XamlRoot!;

		AccessibilityPeerHelper.IOSAccessibilityFocusAccessor?.Invoke(focusTarget);
		var nodeBefore = AccessibilityPeerHelper.IOSFocusedNativeNodeAccessor?.Invoke(root);
		Assert.IsNotNull(nodeBefore, "Setup: focused node must be set.");

		panel.Children.Add(new Button { Content = "New" });
		await UITestHelper.WaitForIdle();

		var nodeAfter = AccessibilityPeerHelper.IOSFocusedNativeNodeAccessor?.Invoke(root);
		Assert.AreSame(nodeBefore, nodeAfter,
			"Adding a child must not move VoiceOver focus away from the currently focused element.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_No_Focus_Set_Then_Property_Change_Leaves_Focused_Node_Null()
	{
		var button = new Button { Content = "No focus" };
		await UITestHelper.Load(button);
		var root = button.XamlRoot!;

		// No focus was requested, so the accessor should remain null.
		var before = AccessibilityPeerHelper.IOSFocusedNativeNodeAccessor?.Invoke(root);

		AutomationProperties.SetName(button, "Changed");
		await UITestHelper.WaitForIdle();

		var after = AccessibilityPeerHelper.IOSFocusedNativeNodeAccessor?.Invoke(root);
		Assert.AreSame(before, after,
			"Property change must leave IOSFocusedNativeNodeAccessor unchanged when no focus was set.");
	}

	// Structure records are emitted after the rebuilt native order actually changes.

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Child_Added_Then_StructureChanged_Recorded()
	{
		var panel = new StackPanel { Children = { new Button { Content = "Existing" } } };
		await UITestHelper.Load(panel);
		var root = panel.XamlRoot!;

		ClearEvents(root);
		panel.Children.Add(new Button { Content = "New" });
		await UITestHelper.WaitForIdle();

		Assert.IsTrue(
			GetEvents(root).Any(e => e.Kind == AccessibilityNativeEventKind.StructureChanged),
			"Expected StructureChanged after child added.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Child_Removed_Then_StructureChanged_Recorded()
	{
		var child = new Button { Content = "Remove" };
		var panel = new StackPanel { Children = { child } };
		await UITestHelper.Load(panel);
		var root = panel.XamlRoot!;

		ClearEvents(root);
		panel.Children.Remove(child);
		await UITestHelper.WaitForIdle();

		Assert.IsTrue(
			GetEvents(root).Any(e => e.Kind == AccessibilityNativeEventKind.StructureChanged),
			"Expected StructureChanged after child removed.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Children_Reordered_Then_StructureChanged_Recorded()
	{
		var b1 = new Button { Content = "First" };
		var b2 = new Button { Content = "Second" };
		var panel = new StackPanel { Children = { b1, b2 } };
		await UITestHelper.Load(panel);
		var root = panel.XamlRoot!;

		// Wait for initial build to stabilize.
		await UITestHelper.WaitForIdle();
		ClearEvents(root);

		// Move b1 to after b2.
		panel.Children.Remove(b1);
		panel.Children.Add(b1);
		await UITestHelper.WaitForIdle();

		Assert.IsTrue(
			GetEvents(root).Any(e => e.Kind == AccessibilityNativeEventKind.StructureChanged),
			"Expected StructureChanged after reordering children.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Multiple_Children_Added_Then_One_Coalesced_StructureChanged()
	{
		var panel = new StackPanel();
		await UITestHelper.Load(panel);
		var root = panel.XamlRoot!;

		ClearEvents(root);
		panel.Children.Add(new Button { Content = "A" });
		panel.Children.Add(new Button { Content = "B" });
		await UITestHelper.WaitForIdle();

		// Two additions share one coalesced rebuild.
		var count = GetEvents(root).Count(e => e.Kind == AccessibilityNativeEventKind.StructureChanged);
		Assert.AreEqual(1, count,
			"Multiple adds in one cycle must produce exactly one StructureChanged.");
	}

	// StructureChanged via automation event path

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_StructureChanged_AutomationEvent_Raised_Then_StructureChanged_Recorded()
	{
		var panel = new StackPanel { Children = { new Button { Content = "Item" } } };
		await UITestHelper.Load(panel);
		var root = panel.XamlRoot!;

		ClearEvents(root);
		panel.GetOrCreateAutomationPeer()?.RaiseAutomationEvent(AutomationEvents.StructureChanged);
		await UITestHelper.WaitForIdle();

		Assert.IsTrue(
			GetEvents(root).Any(e => e.Kind == AccessibilityNativeEventKind.StructureChanged),
			"Expected StructureChanged when StructureChanged automation event is raised.");
	}

	// Announcements

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Notification_Event_Raised_Then_Announcement_Recorded()
	{
		var button = new Button { Content = "Announce" };
		await UITestHelper.Load(button);
		var root = button.XamlRoot!;

		ClearEvents(root);
		button.GetOrCreateAutomationPeer()?.RaiseNotificationEvent(
			AutomationNotificationKind.ActionCompleted,
			AutomationNotificationProcessing.ImportantAll,
			"Operation complete",
			"ios-us4-1");

		// Debounce delay before posting.
		await Task.Delay(300);
		await UITestHelper.WaitForIdle();

		Assert.IsTrue(
			GetEvents(root).Any(e => e.Kind == AccessibilityNativeEventKind.Announcement),
			"Expected Announcement event after RaiseNotificationEvent.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Notification_Raised_Then_Announcement_Carries_Text()
	{
		const string text = "File saved";
		var button = new Button { Content = "Save" };
		await UITestHelper.Load(button);
		var root = button.XamlRoot!;

		ClearEvents(root);
		button.GetOrCreateAutomationPeer()?.RaiseNotificationEvent(
			AutomationNotificationKind.ActionCompleted,
			AutomationNotificationProcessing.ImportantAll,
			text,
			"ios-us4-2");

		await Task.Delay(300);
		await UITestHelper.WaitForIdle();

		var rec = GetEvents(root).FirstOrDefault(e => e.Kind == AccessibilityNativeEventKind.Announcement);
		Assert.IsNotNull(rec, "Expected an Announcement event.");
		Assert.AreEqual(text, rec.Text, "Announcement text must match the notification display string.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Modal_Is_Active_Then_Background_Live_Region_Is_Suppressed()
	{
		var background = new Button { Content = "Background update" };
		var modalContent = new Button { Content = "Modal update" };
		AutomationProperties.SetLiveSetting(background, AutomationLiveSetting.Assertive);
		AutomationProperties.SetLiveSetting(modalContent, AutomationLiveSetting.Assertive);

		var popup = new Popup
		{
			IsLightDismissEnabled = false,
			Child = new Border { Child = modalContent },
		};
		var root = new Grid { Children = { background, popup } };
		await UITestHelper.Load(root);

		try
		{
			ClearEvents(root.XamlRoot!);
			var backgroundPeer = background.GetOrCreateAutomationPeer()!;
			backgroundPeer.SetAPEventsSource(new OwnerlessLiveRegionAutomationPeer("Background update"));

			backgroundPeer.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
			await Task.Delay(300);
			await UITestHelper.WaitForIdle();
			Assert.IsTrue(
				GetEvents(root.XamlRoot!).Any(e =>
					e.Kind == AccessibilityNativeEventKind.Announcement &&
					e.Text?.Contains("Background update") is true),
				"An ownerless EventsSource must route through its rooted source peer when no modal is active.");
			ClearEvents(root.XamlRoot!);

			// Queue before the modal opens to verify delivery revalidates modal scope.
			backgroundPeer.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
			popup.IsOpen = true;
			await Task.Delay(300);
			await UITestHelper.WaitForIdle();

			Assert.IsFalse(
				GetEvents(root.XamlRoot!).Any(e =>
					e.Kind == AccessibilityNativeEventKind.Announcement &&
					e.Text?.Contains("Background update") is true),
				"Queued background live regions must be suppressed when a modal opens before delivery.");
			ClearEvents(root.XamlRoot!);

			backgroundPeer.RaiseNotificationEvent(
				AutomationNotificationKind.Other,
				AutomationNotificationProcessing.ImportantAll,
				"Background notification",
				"modal-background");
			await Task.Delay(300);
			await UITestHelper.WaitForIdle();

			Assert.IsFalse(
				GetEvents(root.XamlRoot!).Any(e =>
					e.Kind == AccessibilityNativeEventKind.Announcement &&
					e.Text == "Background notification"),
				"Background notifications must not bypass the active modal.");
			ClearEvents(root.XamlRoot!);

			modalContent.GetOrCreateAutomationPeer()!.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
			await Task.Delay(300);
			await UITestHelper.WaitForIdle();

			Assert.IsTrue(
				GetEvents(root.XamlRoot!).Any(e =>
					e.Kind == AccessibilityNativeEventKind.Announcement &&
					e.Text?.Contains("Modal update") is true),
				"Live regions inside the popup modal must remain announceable.");
		}
		finally
		{
			popup.IsOpen = false;
		}
	}

	// Text changes

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_TextEdit_TextChanged_Raised_Then_TextChanged_Recorded()
	{
		var textBox = new TextBox { Text = "Hello" };
		await UITestHelper.Load(textBox);
		var root = textBox.XamlRoot!;

		ClearEvents(root);

		var peer = textBox.GetOrCreateAutomationPeer();
		peer?.RaiseTextEditTextChangedEvent(
			AutomationTextEditChangeType.None,
			new List<string> { "World" });

		await UITestHelper.WaitForIdle();

		Assert.IsTrue(
			GetEvents(root).Any(e => e.Kind == AccessibilityNativeEventKind.TextChanged),
			"Expected TextChanged after RaiseTextEditTextChangedEvent.");
	}

	// Live snapshot after mutation

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Button_Disabled_Then_Snapshot_Shows_Not_Enabled()
	{
		var button = new Button { Content = "Disable me" };
		await UITestHelper.Load(button);

		Assert.IsTrue(GetSnapshot(button)?.Enabled, "Button should start enabled.");

		button.IsEnabled = false;
		await UITestHelper.WaitForIdle();

		Assert.IsFalse(GetSnapshot(button)?.Enabled,
			"Snapshot must report not-enabled after IsEnabled=false.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_AutomationId_Set_After_Load_Then_Snapshot_Reflects_Id()
	{
		const string id = "dyn-id";
		var button = new Button { Content = "Id test" };
		await UITestHelper.Load(button);

		Assert.IsNull(GetSnapshot(button)?.AutomationId, "No AutomationId initially.");

		AutomationProperties.SetAutomationId(button, id);
		await UITestHelper.WaitForIdle();

		Assert.AreEqual(id, GetSnapshot(button)?.AutomationId,
			"Snapshot AutomationId must reflect the dynamically set value.");
	}

}
