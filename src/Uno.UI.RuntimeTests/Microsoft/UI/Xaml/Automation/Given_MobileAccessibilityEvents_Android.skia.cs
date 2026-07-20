#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

/// <summary>
/// Verifies that the Android accessibility adapter records the expected
/// <see cref="AccessibilityNativeEventRecord"/> entries and sends native signals
/// for property changes, structure changes, and automation events.
/// No Android types are referenced; tests run only on SkiaAndroid.
/// </summary>
[TestClass]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid)]
public class Given_MobileAccessibilityEvents_Android
{
	private static AccessibilityNativeEventRecord[] GetAndClearEvents(XamlRoot root)
	{
		var events = AccessibilityPeerHelper.AndroidAccessibilityEventsAccessor?.Invoke(root)
			?? System.Array.Empty<AccessibilityNativeEventRecord>();
		AccessibilityPeerHelper.AndroidClearAccessibilityEventsAction?.Invoke(root);
		return events;
	}

	// Ensures the element has a stable virtual ID by triggering a tree scan via
	// the snapshot accessor.  Tests that assert NodeInvalidated (recorded on the
	// native flush) must call this before mutating properties.
	private static void EnsureVirtualId(UIElement element)
	{
		var snapshot = AccessibilityPeerHelper.AndroidAccessibilityNodeSnapshotAccessor?.Invoke(element);
		Assert.IsNotNull(snapshot, "Element must have a virtual ID before the test mutates it.");
	}

	// Hook registration ---------------------------------------------------------

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Event_Hooks_Are_Registered_Then_Accessors_Are_Not_Null()
	{
		var button = new Button { Content = "Hook Probe" };
		await UITestHelper.Load(button);

		Assert.IsNotNull(AccessibilityPeerHelper.AndroidAccessibilityEventsAccessor);
		Assert.IsNotNull(AccessibilityPeerHelper.AndroidClearAccessibilityEventsAction);
	}

	// Property changes → NodeInvalidated ----------------------------------------

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Name_Property_Changes_Then_NodeInvalidated_Is_Recorded()
	{
		var button = new Button { Content = "Initial" };
		await UITestHelper.Load(button);
		EnsureVirtualId(button);
		GetAndClearEvents(button.XamlRoot!);

		AutomationProperties.SetName(button, "Updated");
		await TestServices.WindowHelper.WaitForIdle();

		var events = GetAndClearEvents(button.XamlRoot!);
		Assert.IsTrue(
			events.Any(e => e.Kind == AccessibilityNativeEventKind.NodeInvalidated),
			"Expected NodeInvalidated after Name property change.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_IsEnabled_Changes_Then_NodeInvalidated_Is_Recorded()
	{
		var button = new Button { Content = "Toggle" };
		await UITestHelper.Load(button);
		EnsureVirtualId(button);
		GetAndClearEvents(button.XamlRoot!);

		button.IsEnabled = false;
		await TestServices.WindowHelper.WaitForIdle();

		var events = GetAndClearEvents(button.XamlRoot!);
		Assert.IsTrue(
			events.Any(e => e.Kind == AccessibilityNativeEventKind.NodeInvalidated),
			"Expected NodeInvalidated after IsEnabled change.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_AutomationId_Changes_Then_NodeInvalidated_Is_Recorded()
	{
		var button = new Button { Content = "ID Button" };
		await UITestHelper.Load(button);
		EnsureVirtualId(button);
		GetAndClearEvents(button.XamlRoot!);

		AutomationProperties.SetAutomationId(button, "new-id");
		await TestServices.WindowHelper.WaitForIdle();

		var events = GetAndClearEvents(button.XamlRoot!);
		Assert.IsTrue(
			events.Any(e => e.Kind == AccessibilityNativeEventKind.NodeInvalidated),
			"Expected NodeInvalidated after AutomationId change.");
	}

	// Coalescing: N rapid changes → exactly 1 NodeInvalidated per handle ---------

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Multiple_Name_Changes_Fire_Then_Exactly_One_NodeInvalidated_And_Final_State()
	{
		var button = new Button { Content = "Base" };
		await UITestHelper.Load(button);
		EnsureVirtualId(button);
		GetAndClearEvents(button.XamlRoot!);

		AutomationProperties.SetName(button, "A");
		AutomationProperties.SetName(button, "B");
		AutomationProperties.SetName(button, "C");
		await TestServices.WindowHelper.WaitForIdle();

		var events = GetAndClearEvents(button.XamlRoot!);
		var invalidated = events.Where(e => e.Kind == AccessibilityNativeEventKind.NodeInvalidated).ToArray();
		Assert.AreEqual(1, invalidated.Length,
			"Three Name changes to the same node should produce exactly one coalesced NodeInvalidated.");

		var snapshot = AccessibilityPeerHelper.AndroidAccessibilityNodeSnapshotAccessor?.Invoke(button);
		Assert.IsNotNull(snapshot);
		Assert.AreEqual("C", snapshot.Name,
			"Snapshot should reflect the final Name value after coalescing.");
	}

	// Snapshot reflects updated state -------------------------------------------

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Content_Changes_Then_Snapshot_Reflects_New_Name()
	{
		var button = new Button { Content = "Old" };
		await UITestHelper.Load(button);

		button.Content = "New";
		await TestServices.WindowHelper.WaitForIdle();

		var snapshot = AccessibilityPeerHelper.AndroidAccessibilityNodeSnapshotAccessor?.Invoke(button);
		Assert.IsNotNull(snapshot);
		Assert.IsTrue(snapshot.Name?.Contains("New") is true,
			$"Snapshot Name should contain 'New', got '{snapshot.Name}'.");
	}

	// Child add/remove → StructureChanged ---------------------------------------

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Child_Is_Added_Then_StructureChanged_Is_Recorded()
	{
		var panel = new StackPanel { Width = 100, Height = 100 };
		await UITestHelper.Load(panel);
		await TestServices.WindowHelper.WaitForIdle();
		GetAndClearEvents(panel.XamlRoot!);

		panel.Children.Add(new Button { Content = "New Child" });
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(
			GetAndClearEvents(panel.XamlRoot!).Any(e => e.Kind == AccessibilityNativeEventKind.StructureChanged),
			"Expected StructureChanged after child added.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Child_Is_Removed_Then_StructureChanged_Is_Recorded()
	{
		var panel = new StackPanel { Width = 100, Height = 100 };
		var child = new Button { Content = "Removable" };
		panel.Children.Add(child);
		await UITestHelper.Load(panel);
		GetAndClearEvents(panel.XamlRoot!);

		panel.Children.Remove(child);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(
			GetAndClearEvents(panel.XamlRoot!).Any(e => e.Kind == AccessibilityNativeEventKind.StructureChanged),
			"Expected StructureChanged after child removed.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Child_Is_Removed_Then_Removed_Child_Is_Not_In_Snapshots()
	{
		var panel = new StackPanel();
		var child = new Button { Content = "Will Disappear" };
		panel.Children.Add(child);
		await UITestHelper.Load(panel);

		var root = panel.XamlRoot!;
		panel.Children.Remove(child);
		await TestServices.WindowHelper.WaitForIdle();

		var snapshots = AccessibilityPeerHelper.AndroidAllNodeSnapshotsForRootAccessor?.Invoke(root)
			?? System.Array.Empty<AccessibilityNativeNodeSnapshot>();
		Assert.IsFalse(
			snapshots.Any(s => s.Name?.Contains("Will Disappear") is true),
			"Removed child should not appear in the snapshot list.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Nested_Children_Are_Removed_Then_Descendants_Are_Not_In_Snapshots()
	{
		// Nested structure: panel → outer → [inner1, inner2]
		var inner1 = new Button { Content = "Inner1" };
		var inner2 = new Button { Content = "Inner2" };
		var outer = new StackPanel { Children = { inner1, inner2 } };
		var panel = new StackPanel { Children = { outer } };
		await UITestHelper.Load(panel);

		// Trigger scans so all elements get virtual IDs.
		var root = panel.XamlRoot!;
		AccessibilityPeerHelper.AndroidAllNodeSnapshotsForRootAccessor?.Invoke(root);

		panel.Children.Remove(outer);
		await TestServices.WindowHelper.WaitForIdle();

		var snapshots = AccessibilityPeerHelper.AndroidAllNodeSnapshotsForRootAccessor?.Invoke(root)
			?? System.Array.Empty<AccessibilityNativeNodeSnapshot>();
		Assert.IsFalse(
			snapshots.Any(s => s.Name?.Contains("Inner1") is true || s.Name?.Contains("Inner2") is true),
			"Descendants of the removed parent should not appear in the snapshot list.");
	}

	// Notification event → Announcement ----------------------------------------

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_RaiseNotificationEvent_Then_Announcement_Is_Recorded()
	{
		var button = new Button { Content = "Announce" };
		await UITestHelper.Load(button);
		GetAndClearEvents(button.XamlRoot!);

		button.GetOrCreateAutomationPeer()!.RaiseNotificationEvent(
			AutomationNotificationKind.ActionCompleted,
			AutomationNotificationProcessing.ImportantAll,
			"Test announcement text",
			"test-activity");

		// SkiaAccessibilityBase debounces announcements by 100 ms; wait past that
		// before reading the log so the timer fires on the main thread first.
		await Task.Delay(150);
		await TestServices.WindowHelper.WaitForIdle();

		var events = GetAndClearEvents(button.XamlRoot!);
		Assert.IsTrue(
			events.Any(e =>
				e.Kind == AccessibilityNativeEventKind.Announcement &&
				e.Text?.Contains("Test announcement text") is true),
			"Expected Announcement event with the notification text.");
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
			GetAndClearEvents(root.XamlRoot!);
			var backgroundPeer = background.GetOrCreateAutomationPeer()!;
			backgroundPeer.SetAPEventsSource(new OwnerlessLiveRegionAutomationPeer("Background update"));

			backgroundPeer.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
			await Task.Delay(300);
			await TestServices.WindowHelper.WaitForIdle();
			Assert.IsTrue(
				GetAndClearEvents(root.XamlRoot!).Any(e =>
					e.Kind == AccessibilityNativeEventKind.Announcement &&
					e.Text?.Contains("Background update") is true),
				"An ownerless EventsSource must route through its rooted source peer when no modal is active.");

			// Queue before the modal opens to verify delivery revalidates modal scope.
			backgroundPeer.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
			popup.IsOpen = true;
			await Task.Delay(300);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsFalse(
				GetAndClearEvents(root.XamlRoot!).Any(e =>
					e.Kind == AccessibilityNativeEventKind.Announcement &&
					e.Text?.Contains("Background update") is true),
				"Queued background live regions must be suppressed when a modal opens before delivery.");

			backgroundPeer.RaiseNotificationEvent(
				AutomationNotificationKind.Other,
				AutomationNotificationProcessing.ImportantAll,
				"Background notification",
				"modal-background");
			await Task.Delay(300);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsFalse(
				GetAndClearEvents(root.XamlRoot!).Any(e =>
					e.Kind == AccessibilityNativeEventKind.Announcement &&
					e.Text == "Background notification"),
				"Background notifications must not bypass the active modal.");

			modalContent.GetOrCreateAutomationPeer()!.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
			await Task.Delay(300);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsTrue(
				GetAndClearEvents(root.XamlRoot!).Any(e =>
					e.Kind == AccessibilityNativeEventKind.Announcement &&
					e.Text?.Contains("Modal update") is true),
				"Live regions inside the popup modal must remain announceable.");
		}
		finally
		{
			popup.IsOpen = false;
		}
	}

	// RaiseTextEditTextChangedEvent → TextChanged --------------------------------

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_RaiseTextEditTextChangedEvent_Then_TextChanged_Is_Recorded()
	{
		var textBox = new TextBox { Text = "Hello" };
		await UITestHelper.Load(textBox);
		EnsureVirtualId(textBox);
		GetAndClearEvents(textBox.XamlRoot!);

		textBox.GetOrCreateAutomationPeer()!.RaiseTextEditTextChangedEvent(
			AutomationTextEditChangeType.AutoCorrect,
			new List<string> { "helo", "hello" });
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(
			GetAndClearEvents(textBox.XamlRoot!).Any(e => e.Kind == AccessibilityNativeEventKind.TextChanged),
			"Expected TextChanged after RaiseTextEditTextChangedEvent.");
	}

	// TextPatternOnTextChanged → TextChanged ------------------------------------

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_TextPatternOnTextChanged_Is_Raised_Then_TextChanged_Is_Recorded()
	{
		var textBox = new TextBox { Text = "World" };
		await UITestHelper.Load(textBox);
		EnsureVirtualId(textBox);
		GetAndClearEvents(textBox.XamlRoot!);

		textBox.GetOrCreateAutomationPeer()!.RaiseAutomationEvent(AutomationEvents.TextPatternOnTextChanged);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(
			GetAndClearEvents(textBox.XamlRoot!).Any(e => e.Kind == AccessibilityNativeEventKind.TextChanged),
			"Expected TextChanged after TextPatternOnTextChanged event.");
	}

	// Selection automation events → SelectionChanged ----------------------------

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_SelectionItemPatternOnElementSelected_Is_Raised_Then_SelectionChanged_Is_Recorded()
	{
		var button = new Button { Content = "Selectable" };
		await UITestHelper.Load(button);
		EnsureVirtualId(button);
		GetAndClearEvents(button.XamlRoot!);

		button.GetOrCreateAutomationPeer()!.RaiseAutomationEvent(
			AutomationEvents.SelectionItemPatternOnElementSelected);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(
			GetAndClearEvents(button.XamlRoot!).Any(e => e.Kind == AccessibilityNativeEventKind.SelectionChanged),
			"Expected SelectionChanged after SelectionItemPatternOnElementSelected.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_SelectionPatternOnInvalidated_Is_Raised_Then_SelectionChanged_Is_Recorded()
	{
		var button = new Button { Content = "Container" };
		await UITestHelper.Load(button);
		EnsureVirtualId(button);
		GetAndClearEvents(button.XamlRoot!);

		button.GetOrCreateAutomationPeer()!.RaiseAutomationEvent(AutomationEvents.SelectionPatternOnInvalidated);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(
			GetAndClearEvents(button.XamlRoot!).Any(e => e.Kind == AccessibilityNativeEventKind.SelectionChanged),
			"Expected SelectionChanged after SelectionPatternOnInvalidated.");
	}

	// StructureChanged / LayoutInvalidated → StructureChanged -------------------

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_StructureChanged_Is_Raised_Then_StructureChanged_Is_Recorded()
	{
		var button = new Button { Content = "Structure" };
		await UITestHelper.Load(button);
		GetAndClearEvents(button.XamlRoot!);

		button.GetOrCreateAutomationPeer()!.RaiseAutomationEvent(AutomationEvents.StructureChanged);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(
			GetAndClearEvents(button.XamlRoot!).Any(e => e.Kind == AccessibilityNativeEventKind.StructureChanged),
			"Expected StructureChanged after RaiseAutomationEvent(StructureChanged).");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_LayoutInvalidated_Is_Raised_Then_StructureChanged_Is_Recorded()
	{
		var button = new Button { Content = "Layout" };
		await UITestHelper.Load(button);
		GetAndClearEvents(button.XamlRoot!);

		button.GetOrCreateAutomationPeer()!.RaiseAutomationEvent(AutomationEvents.LayoutInvalidated);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(
			GetAndClearEvents(button.XamlRoot!).Any(e => e.Kind == AccessibilityNativeEventKind.StructureChanged),
			"Expected StructureChanged after LayoutInvalidated.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_AsyncContentLoaded_Is_Raised_Then_StructureChanged_Is_Recorded()
	{
		var button = new Button { Content = "Async" };
		await UITestHelper.Load(button);
		GetAndClearEvents(button.XamlRoot!);

		button.GetOrCreateAutomationPeer()!.RaiseAutomationEvent(AutomationEvents.AsyncContentLoaded);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(
			GetAndClearEvents(button.XamlRoot!).Any(e => e.Kind == AccessibilityNativeEventKind.StructureChanged),
			"Expected StructureChanged (not WindowChanged) after AsyncContentLoaded.");
	}

	// Window events → WindowChanged ---------------------------------------------

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_WindowOpened_Is_Raised_Then_WindowChanged_Is_Recorded()
	{
		var button = new Button { Content = "Window" };
		await UITestHelper.Load(button);
		GetAndClearEvents(button.XamlRoot!);

		button.GetOrCreateAutomationPeer()!.RaiseAutomationEvent(AutomationEvents.WindowOpened);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(
			GetAndClearEvents(button.XamlRoot!).Any(e => e.Kind == AccessibilityNativeEventKind.WindowChanged),
			"Expected WindowChanged after WindowOpened.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_MenuOpened_Is_Raised_Then_WindowChanged_Is_Recorded()
	{
		var button = new Button { Content = "Menu" };
		await UITestHelper.Load(button);
		GetAndClearEvents(button.XamlRoot!);

		button.GetOrCreateAutomationPeer()!.RaiseAutomationEvent(AutomationEvents.MenuOpened);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(
			GetAndClearEvents(button.XamlRoot!).Any(e => e.Kind == AccessibilityNativeEventKind.WindowChanged),
			"Expected WindowChanged after MenuOpened.");
	}

	// ClearEvents resets the log ------------------------------------------------

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Cleared_Then_Subsequent_Events_Start_From_Empty_Log()
	{
		var button = new Button { Content = "Clear Test" };
		await UITestHelper.Load(button);
		EnsureVirtualId(button);

		AutomationProperties.SetName(button, "First");
		await TestServices.WindowHelper.WaitForIdle();

		AccessibilityPeerHelper.AndroidClearAccessibilityEventsAction?.Invoke(button.XamlRoot!);

		AutomationProperties.SetName(button, "Second");
		await TestServices.WindowHelper.WaitForIdle();

		var events = GetAndClearEvents(button.XamlRoot!);
		Assert.IsTrue(events.Length > 0, "Expected events after the second change.");
		Assert.IsTrue(
			events.All(e => e.Kind == AccessibilityNativeEventKind.NodeInvalidated),
			"Only events since the last Clear should be present.");
	}

	// Root-invalidation coalescing: burst adds/removes → one StructureChanged ----

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Two_Children_Are_Added_In_Burst_Then_StructureChanged_Is_Bounded()
	{
		var panel = new StackPanel { Width = 100, Height = 100 };
		await UITestHelper.Load(panel);
		await TestServices.WindowHelper.WaitForIdle();
		GetAndClearEvents(panel.XamlRoot!);

		// Two synchronous additions — both land in the same looper iteration.
		panel.Children.Add(new Button { Content = "First" });
		panel.Children.Add(new Button { Content = "Second" });
		await TestServices.WindowHelper.WaitForIdle();

		var structured = GetAndClearEvents(panel.XamlRoot!)
			.Where(e => e.Kind == AccessibilityNativeEventKind.StructureChanged)
			.ToArray();
		Assert.IsTrue(
			structured.Length is >= 1 and <= 2,
			$"Two synchronous child additions should produce at most two bounded StructureChanged signals; got {structured.Length}.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Child_Removed_And_New_Child_Added_In_Burst_Then_StructureChanged_Is_Bounded()
	{
		var panel = new StackPanel { Width = 100, Height = 100 };
		var original = new Button { Content = "Original" };
		panel.Children.Add(original);
		await UITestHelper.Load(panel);
		await TestServices.WindowHelper.WaitForIdle();
		GetAndClearEvents(panel.XamlRoot!);

		// Remove one and add a replacement in the same synchronous block.
		panel.Children.Remove(original);
		panel.Children.Add(new Button { Content = "Replacement" });
		await TestServices.WindowHelper.WaitForIdle();

		var structured = GetAndClearEvents(panel.XamlRoot!)
			.Where(e => e.Kind == AccessibilityNativeEventKind.StructureChanged)
			.ToArray();
		Assert.IsTrue(
			structured.Length is >= 1 and <= 2,
			$"Remove + Add in the same cycle should produce at most two bounded StructureChanged signals; got {structured.Length}.");
	}

}
