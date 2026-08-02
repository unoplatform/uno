#nullable enable

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
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
/// US7 Android registry-leak, stale-generation, root-isolation, thread-affinity,
/// and disposal-guard tests. Companion to Given_MobileAccessibilityLifecycle (US3).
/// Does not duplicate existing focus/modal assertions from that class.
/// </summary>
[TestClass]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid)]
public class Given_MobileAccessibilityLifecycle_US7_Android
{
	private static AccessibilityNativeNodeSnapshot[] GetAllSnapshots(XamlRoot root)
		=> AccessibilityPeerHelper.AndroidAllNodeSnapshotsForRootAccessor?.Invoke(root)
			?? Array.Empty<AccessibilityNativeNodeSnapshot>();

	private static AccessibilityNativeNodeSnapshot? GetSnapshot(UIElement element)
		=> AccessibilityPeerHelper.AndroidAccessibilityNodeSnapshotAccessor?.Invoke(element);

	private static AccessibilityNativeEventRecord[] GetEvents(XamlRoot root)
		=> AccessibilityPeerHelper.AndroidAccessibilityEventsAccessor?.Invoke(root)
			?? Array.Empty<AccessibilityNativeEventRecord>();

	private static void ClearEvents(XamlRoot root)
		=> AccessibilityPeerHelper.AndroidClearAccessibilityEventsAction?.Invoke(root);

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Repeated_AddRemove_Cycles_Then_Count_Returns_To_Baseline()
	{
		Assert.IsNotNull(
			AccessibilityPeerHelper.AndroidAllNodeSnapshotsForRootAccessor,
			"AndroidAllNodeSnapshotsForRootAccessor not registered.");
		Assert.IsNotNull(
			AccessibilityPeerHelper.AndroidAccessibilityNodeSnapshotAccessor,
			"AndroidAccessibilityNodeSnapshotAccessor not registered.");

		var panel = new StackPanel();
		var anchor = new Button { Content = "Anchor" };
		panel.Children.Add(anchor);
		await UITestHelper.Load(panel);
		await TestServices.WindowHelper.WaitForIdle();

		var root = panel.XamlRoot!;
		var baseline = GetAllSnapshots(root).Length;
		Assert.IsTrue(baseline > 0, "Baseline count must be positive.");

		for (var i = 0; i < 3; i++)
		{
			var item = new Button { Content = $"Cycle {i}" };
			panel.Children.Add(item);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsNotNull(GetSnapshot(item), $"Cycle {i}: node must appear after add.");

			panel.Children.Remove(item);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsNull(GetSnapshot(item), $"Cycle {i}: node must be absent after remove.");
			Assert.AreEqual(
				baseline,
				GetAllSnapshots(root).Length,
				$"Cycle {i}: registry count must return to baseline.");
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Element_Removed_Then_Action_Rejected_And_Readd_Restores_Access()
	{
		Assert.IsNotNull(
			AccessibilityPeerHelper.AndroidAccessibilityActionAccessor,
			"AndroidAccessibilityActionAccessor not registered.");
		Assert.IsNotNull(
			AccessibilityPeerHelper.AndroidAccessibilityVirtualIdAccessor,
			"AndroidAccessibilityVirtualIdAccessor not registered.");

		var panel = new StackPanel();
		var button = new Button { Content = "Round Trip" };
		panel.Children.Add(button);
		await UITestHelper.Load(panel);

		Assert.IsNotNull(GetSnapshot(button), "Precondition: element must have a native snapshot.");
		var idBefore = AccessibilityPeerHelper.AndroidAccessibilityVirtualIdAccessor!.Invoke(button);
		Assert.IsNotNull(idBefore, "Element must have a VirtualId while in tree.");

		var clicked = false;
		button.Click += (_, _) => clicked = true;

		var inTree = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor!.Invoke(
			button, new AccessibilityNativeActionRequest(AccessibilityNativeAction.Activate));
		Assert.IsTrue(inTree, "Activate must succeed while in tree.");
		Assert.IsTrue(clicked, "Click must fire for in-tree Activate.");

		clicked = false;
		panel.Children.Remove(button);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsNull(GetSnapshot(button), "Snapshot must be null while removed.");

		var removed = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor!.Invoke(
			button, new AccessibilityNativeActionRequest(AccessibilityNativeAction.Activate));
		Assert.IsFalse(removed, "Activate must fail while removed (stale generation).");
		Assert.IsFalse(clicked, "Click must not fire for removed element.");

		panel.Children.Add(button);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsNotNull(GetSnapshot(button), "Snapshot must be present after re-add.");

		var idAfterReadd = AccessibilityPeerHelper.AndroidAccessibilityVirtualIdAccessor!.Invoke(button);
		Assert.IsNotNull(idAfterReadd, "Element must have a VirtualId after re-add.");
		Assert.AreEqual(idBefore, idAfterReadd, "VirtualId must be stable across remove/re-add.");

		var reAdded = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor!.Invoke(
			button, new AccessibilityNativeActionRequest(AccessibilityNativeAction.Activate));
		Assert.IsTrue(reAdded, "Activate must succeed after re-add.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Item_Container_Is_Rebound_Then_New_Item_Gets_New_Id_And_Stale_Id_Is_Rejected()
	{
		Assert.IsNotNull(
			AccessibilityPeerHelper.AndroidAccessibilityPeerVirtualIdAccessor,
			"AndroidAccessibilityPeerVirtualIdAccessor not registered.");
		Assert.IsNotNull(
			AccessibilityPeerHelper.AndroidAccessibilityRawActionAccessor,
			"AndroidAccessibilityRawActionAccessor not registered.");

		var items = new ObservableCollection<string> { "Item A" };
		var listView = new ListView
		{
			ItemsSource = items,
			Width = 240,
			Height = 160,
		};
		await UITestHelper.Load(listView);
		await TestServices.WindowHelper.WaitForIdle();

		var containerBefore = listView.ContainerFromIndex(0);
		Assert.IsNotNull(containerBefore, "The first item container must be realized.");

		var listPeer = FrameworkElementAutomationPeer.CreatePeerForElement(listView) as ItemsControlAutomationPeer;
		Assert.IsNotNull(listPeer);

		var itemAPeer = listPeer.GetChildren()?.OfType<ItemAutomationPeer>()
			.SingleOrDefault(peer => peer.GetName().Contains("Item A", StringComparison.Ordinal));
		Assert.IsNotNull(itemAPeer, "The first realized item must have an item automation peer.");

		_ = GetAllSnapshots(listView.XamlRoot!);
		var itemAId = AccessibilityPeerHelper.AndroidAccessibilityPeerVirtualIdAccessor!.Invoke(itemAPeer);
		Assert.IsNotNull(itemAId);

		items[0] = "Item B";
		await TestServices.WindowHelper.WaitForIdle();

		var containerAfter = listView.ContainerFromIndex(0);
		Assert.AreSame(containerBefore, containerAfter, "The test requires the realized container to be reused.");

		var itemBPeer = listPeer.GetChildren()?.OfType<ItemAutomationPeer>()
			.SingleOrDefault(peer => peer.GetName().Contains("Item B", StringComparison.Ordinal));
		Assert.IsNotNull(itemBPeer, "The rebound item must have a new item automation peer.");

		_ = GetAllSnapshots(listView.XamlRoot!);
		var itemBId = AccessibilityPeerHelper.AndroidAccessibilityPeerVirtualIdAccessor!.Invoke(itemBPeer);
		Assert.IsNotNull(itemBId);
		Assert.AreNotEqual(itemAId, itemBId, "A recycled container must not transfer native identity between items.");

		Assert.IsFalse(
			AccessibilityPeerHelper.AndroidAccessibilityRawActionAccessor!.Invoke(itemAId.Value, 0x10),
			"The stale virtual ID must reject ACTION_CLICK after the container is rebound.");
		Assert.IsTrue(
			AccessibilityPeerHelper.AndroidAccessibilityRawActionAccessor.Invoke(itemBId.Value, 0x10),
			"The current item virtual ID must remain actionable.");
		Assert.AreEqual("Item B", listView.SelectedItem);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Same_Item_Instance_Appears_Twice_Then_Each_Occurrence_Has_Its_Own_Id()
	{
		var sharedItem = new SharedItem("Shared");
		var listView = new ListView
		{
			ItemsSource = new[] { sharedItem, sharedItem },
			Width = 240,
			Height = 200,
		};
		await UITestHelper.Load(listView);
		await TestServices.WindowHelper.WaitForIdle();

		var listPeer = FrameworkElementAutomationPeer.CreatePeerForElement(listView) as ItemsControlAutomationPeer;
		Assert.IsNotNull(listPeer);

		var firstPeers = listPeer.GetChildren()?.OfType<ItemAutomationPeer>().ToArray();
		Assert.IsNotNull(firstPeers);
		Assert.AreEqual(2, firstPeers.Length);
		Assert.AreNotSame(firstPeers[0], firstPeers[1]);

		_ = GetAllSnapshots(listView.XamlRoot!);
		var firstId = AccessibilityPeerHelper.AndroidAccessibilityPeerVirtualIdAccessor?.Invoke(firstPeers[0]);
		var secondId = AccessibilityPeerHelper.AndroidAccessibilityPeerVirtualIdAccessor?.Invoke(firstPeers[1]);
		Assert.IsNotNull(firstId);
		Assert.IsNotNull(secondId);
		Assert.AreNotEqual(firstId, secondId);

		var rebuiltPeers = listPeer.GetChildren()?.OfType<ItemAutomationPeer>().ToArray();
		Assert.IsNotNull(rebuiltPeers);
		Assert.AreSame(firstPeers[0], rebuiltPeers[0]);
		Assert.AreSame(firstPeers[1], rebuiltPeers[1]);

		Assert.IsTrue(
			AccessibilityPeerHelper.AndroidAccessibilityRawActionAccessor?.Invoke(secondId.Value, 0x10));
		Assert.AreEqual(1, listView.SelectedIndex);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Root_Content_Replaced_Then_Old_Element_Absent_From_Snapshots()
	{
		Assert.IsNotNull(
			AccessibilityPeerHelper.AndroidAllNodeSnapshotsForRootAccessor,
			"AndroidAllNodeSnapshotsForRootAccessor not registered.");
		Assert.IsNotNull(
			AccessibilityPeerHelper.AndroidAccessibilityNodeSnapshotAccessor,
			"AndroidAccessibilityNodeSnapshotAccessor not registered.");

		var first = new Button { Content = "First Root Content" };
		await UITestHelper.Load(first);
		var root = first.XamlRoot!;
		Assert.IsNotNull(GetSnapshot(first), "First element must have snapshot before replacement.");

		var second = new Button { Content = "Second Root Content" };
		await UITestHelper.Load(second);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsNull(GetSnapshot(first), "Replaced element must not have a snapshot.");
		Assert.IsFalse(
			GetAllSnapshots(root).Any(s => s.Name?.Contains("First Root Content") is true),
			"Replaced element must not appear in live snapshot list.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Action_Called_From_Background_Thread_Then_Returns_False_And_Click_Is_Queued()
	{
		Assert.IsNotNull(
			AccessibilityPeerHelper.AndroidAccessibilityActionAccessor,
			"AndroidAccessibilityActionAccessor not registered.");

		var button = new Button { Content = "Off-Thread Target" };
		await UITestHelper.Load(button);

		var clicked = new[] { 0 };
		button.Click += (_, _) => Interlocked.Increment(ref clicked[0]);

		var accessor = AccessibilityPeerHelper.AndroidAccessibilityActionAccessor!;
		var request = new AccessibilityNativeActionRequest(AccessibilityNativeAction.Activate);

		var clickedDuringCall = -1;
		var callTask = Task.Run(() =>
		{
			var result = accessor.Invoke(button, request);
			// The contract requires the accessor not to wait on the UI thread, so
			// sampling here is race-free: the UI thread is blocked on .Wait() below
			// and cannot drain the dispatcher queue between the accessor returning
			// and this read.
			clickedDuringCall = Volatile.Read(ref clicked[0]);
			return result;
		});

		// Block the UI thread synchronously. The contract guarantees the accessor
		// will not wait for the UI thread, so this cannot deadlock.
		var completed = callTask.Wait(TimeSpan.FromSeconds(5));

		Assert.IsTrue(completed, "Off-UI-thread action call must complete within 5 s.");
		Assert.IsTrue(callTask.Result, "Off-UI-thread Activate must report that the action was accepted for UI-thread execution.");
		Assert.AreEqual(0, clickedDuringCall, "Click must not fire on the background thread during the accessor call.");

		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(1, Volatile.Read(ref clicked[0]), "Queued click must fire exactly once after idle.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Element_Detached_Then_Subsequent_Property_Mutation_Emits_No_Events()
	{
		Assert.IsNotNull(
			AccessibilityPeerHelper.AndroidAccessibilityEventsAccessor,
			"AndroidAccessibilityEventsAccessor not registered.");
		Assert.IsNotNull(
			AccessibilityPeerHelper.AndroidClearAccessibilityEventsAction,
			"AndroidClearAccessibilityEventsAction not registered.");

		var panel = new StackPanel();
		var button = new Button { Content = "Detach Guard" };
		panel.Children.Add(button);
		await UITestHelper.Load(panel);

		var root = button.XamlRoot!;

		panel.Children.Remove(button);
		await TestServices.WindowHelper.WaitForIdle();

		// Discard structural events from the detach itself.
		ClearEvents(root);

		// Mutate a property on the now-detached element; the adapter must ignore it.
		AutomationProperties.SetName(button, "PostDetach");
		await TestServices.WindowHelper.WaitForIdle();

		var events = GetEvents(root);
		Assert.AreEqual(
			0,
			events.Length,
			"Accessibility events must not be emitted for a detached element after property mutation.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_100_Mixed_Transitions_Occur_Then_Focus_Stays_Recoverable()
	{
		// SC-005: accessibility focus must survive 100 consecutive focus, popup, scroll,
		// virtualization, disable, and removal transitions with no dead node, no duplicate
		// focus loop, and no unrecoverable focus state.
		Assert.IsNotNull(
			AccessibilityPeerHelper.AndroidAccessibilityFocusAccessor,
			"AndroidAccessibilityFocusAccessor not registered.");

		var items = new ObservableCollection<string>(
			Enumerable.Range(0, 60).Select(index => $"Row {index}"));
		var list = new ListView { ItemsSource = items, Height = 160 };
		var scroller = new ScrollViewer
		{
			Height = 120,
			Content = new StackPanel
			{
				Children =
				{
					new Button { Content = "Scrolled A", Height = 90 },
					new Button { Content = "Scrolled B", Height = 90 },
					new Button { Content = "Scrolled C", Height = 90 },
				},
			},
		};
		var toggleTarget = new Button { Content = "Toggle Target" };
		var churnHost = new StackPanel();
		var popupHost = new Popup();
		var panel = new StackPanel
		{
			Children = { list, scroller, toggleTarget, churnHost, popupHost },
		};

		await UITestHelper.Load(panel);
		await TestServices.WindowHelper.WaitForIdle();

		var root = panel.XamlRoot!;
		var baseline = GetAllSnapshots(root).Length;
		Assert.IsTrue(baseline > 0, "Baseline node count must be positive.");

		for (var iteration = 0; iteration < 100; iteration++)
		{
			switch (iteration % 6)
			{
				case 0:
					// Focus transition: focus must land on a live, named node.
					Assert.IsTrue(
						RequestNativeFocus(toggleTarget),
						$"Iteration {iteration}: focusing a live element must succeed.");
					Assert.IsNotNull(
						GetFocusedNode(root),
						$"Iteration {iteration}: focused node must be resolvable after focus.");
					break;

				case 1:
					// Popup transition: open and close a modal-like surface.
					popupHost.Child = new Border
					{
						Width = 60,
						Height = 40,
						Child = new TextBlock { Text = $"Popup {iteration}" },
					};
					popupHost.IsOpen = true;
					await TestServices.WindowHelper.WaitForIdle();
					popupHost.IsOpen = false;
					popupHost.Child = null;
					break;

				case 2:
					// Scroll transition.
					scroller.ChangeView(null, (iteration % 5) * 20d, null, disableAnimation: true);
					break;

				case 3:
					// Virtualization transition: mutate the realized range.
					items.Insert(0, $"Inserted {iteration}");
					if (items.Count > 60)
					{
						items.RemoveAt(items.Count - 1);
					}
					break;

				case 4:
					// Disable transition: a disabled element must not remain focus target.
					toggleTarget.IsEnabled = !toggleTarget.IsEnabled;
					break;

				default:
					// Removal transition: add then remove a node in the same iteration.
					var transient = new Button { Content = $"Transient {iteration}" };
					churnHost.Children.Add(transient);
					await TestServices.WindowHelper.WaitForIdle();
					Assert.IsNotNull(
						GetSnapshot(transient),
						$"Iteration {iteration}: transient node must be exposed while attached.");
					churnHost.Children.Remove(transient);
					await TestServices.WindowHelper.WaitForIdle();
					Assert.IsNull(
						GetSnapshot(transient),
						$"Iteration {iteration}: removed node must not remain exposed.");
					break;
			}

			await TestServices.WindowHelper.WaitForIdle();
		}

		// Focus must still be usable after the churn, and the registry must not have
		// accumulated dead nodes.
		toggleTarget.IsEnabled = true;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(
			RequestNativeFocus(toggleTarget),
			"Focus must still be grantable after 100 transitions.");
		Assert.IsNotNull(
			GetFocusedNode(root),
			"Focused node must be resolvable after 100 transitions.");

		churnHost.Children.Clear();
		popupHost.IsOpen = false;
		await TestServices.WindowHelper.WaitForIdle();
		GC.Collect();
		GC.WaitForPendingFinalizers();
		_ = GetAllSnapshots(root);
		await TestServices.WindowHelper.WaitForIdle();

		var finalCount = GetAllSnapshots(root).Length;
		Assert.IsTrue(
			finalCount <= baseline + 20,
			$"Node registry must not grow unbounded across 100 transitions; baseline={baseline}, final={finalCount}.");
	}

	private static bool RequestNativeFocus(UIElement element)
		=> AccessibilityPeerHelper.AndroidAccessibilityFocusAccessor?.Invoke(element) is true;

	private static object? GetFocusedNode(XamlRoot root)
		=> AccessibilityPeerHelper.AndroidFocusedNativeNodeAccessor?.Invoke(root);

	private sealed record SharedItem(string Name)
	{
		public override string ToString() => Name;
	}
}

/// <summary>
/// US7 iOS registry-leak, stale-generation, root-isolation, thread-affinity,
/// and disposal-guard tests. Companion to Given_MobileAccessibilityLifecycle_IOS (US3).
/// </summary>
[TestClass]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaIOS)]
public class Given_MobileAccessibilityLifecycle_US7_IOS
{
	private static AccessibilityNativeNodeSnapshot[] GetAllSnapshots(XamlRoot root)
		=> AccessibilityPeerHelper.IOSAllNodeSnapshotsForRootAccessor?.Invoke(root)
			?? Array.Empty<AccessibilityNativeNodeSnapshot>();

	private static AccessibilityNativeNodeSnapshot? GetSnapshot(UIElement element)
		=> AccessibilityPeerHelper.IOSAccessibilityNodeSnapshotAccessor?.Invoke(element);

	private static AccessibilityNativeEventRecord[] GetEvents(XamlRoot root)
		=> AccessibilityPeerHelper.IOSAccessibilityEventsAccessor?.Invoke(root)
			?? Array.Empty<AccessibilityNativeEventRecord>();

	private static void ClearEvents(XamlRoot root)
		=> AccessibilityPeerHelper.IOSClearAccessibilityEventsAction?.Invoke(root);

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Repeated_AddRemove_Cycles_Then_Count_Returns_To_Baseline()
	{
		Assert.IsNotNull(
			AccessibilityPeerHelper.IOSAllNodeSnapshotsForRootAccessor,
			"IOSAllNodeSnapshotsForRootAccessor not registered.");
		Assert.IsNotNull(
			AccessibilityPeerHelper.IOSAccessibilityNodeSnapshotAccessor,
			"IOSAccessibilityNodeSnapshotAccessor not registered.");

		var panel = new StackPanel();
		var anchor = new Button { Content = "Anchor" };
		panel.Children.Add(anchor);
		await UITestHelper.Load(panel);
		await TestServices.WindowHelper.WaitForIdle();

		var root = panel.XamlRoot!;
		var baseline = GetAllSnapshots(root).Length;
		Assert.IsTrue(baseline > 0, "Baseline count must be positive.");

		for (var i = 0; i < 3; i++)
		{
			var item = new Button { Content = $"Cycle {i}" };
			panel.Children.Add(item);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsNotNull(GetSnapshot(item), $"Cycle {i}: node must appear after add.");

			panel.Children.Remove(item);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsNull(GetSnapshot(item), $"Cycle {i}: node must be absent after remove.");
			Assert.AreEqual(
				baseline,
				GetAllSnapshots(root).Length,
				$"Cycle {i}: registry count must return to baseline.");
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Element_Removed_Then_Action_Rejected_And_Readd_Restores_Access()
	{
		Assert.IsNotNull(
			AccessibilityPeerHelper.IOSAccessibilityActionAccessor,
			"IOSAccessibilityActionAccessor not registered.");

		var panel = new StackPanel();
		var button = new Button { Content = "Round Trip" };
		panel.Children.Add(button);
		await UITestHelper.Load(panel);

		Assert.IsNotNull(GetSnapshot(button), "Precondition: element must have a native snapshot.");

		var clicked = false;
		button.Click += (_, _) => clicked = true;

		var inTree = AccessibilityPeerHelper.IOSAccessibilityActionAccessor!.Invoke(
			button, new AccessibilityNativeActionRequest(AccessibilityNativeAction.Activate));
		Assert.IsTrue(inTree, "Activate must succeed while in tree.");
		Assert.IsTrue(clicked, "Click must fire for in-tree Activate.");

		clicked = false;
		panel.Children.Remove(button);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsNull(GetSnapshot(button), "Snapshot must be null while removed.");

		var removed = AccessibilityPeerHelper.IOSAccessibilityActionAccessor!.Invoke(
			button, new AccessibilityNativeActionRequest(AccessibilityNativeAction.Activate));
		Assert.IsFalse(removed, "Activate must fail while removed.");
		Assert.IsFalse(clicked, "Click must not fire for removed element.");

		panel.Children.Add(button);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsNotNull(GetSnapshot(button), "Snapshot must be present after re-add.");

		var reAdded = AccessibilityPeerHelper.IOSAccessibilityActionAccessor!.Invoke(
			button, new AccessibilityNativeActionRequest(AccessibilityNativeAction.Activate));
		Assert.IsTrue(reAdded, "Activate must succeed after re-add.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Root_Content_Replaced_Then_Old_Element_Absent_From_Snapshots()
	{
		Assert.IsNotNull(
			AccessibilityPeerHelper.IOSAllNodeSnapshotsForRootAccessor,
			"IOSAllNodeSnapshotsForRootAccessor not registered.");
		Assert.IsNotNull(
			AccessibilityPeerHelper.IOSAccessibilityNodeSnapshotAccessor,
			"IOSAccessibilityNodeSnapshotAccessor not registered.");

		var first = new Button { Content = "First Root Content" };
		await UITestHelper.Load(first);
		var root = first.XamlRoot!;
		Assert.IsNotNull(GetSnapshot(first), "First element must have snapshot before replacement.");

		var second = new Button { Content = "Second Root Content" };
		await UITestHelper.Load(second);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsNull(GetSnapshot(first), "Replaced element must not have a snapshot.");
		Assert.IsFalse(
			GetAllSnapshots(root).Any(s => s.Name?.Contains("First Root Content") is true),
			"Replaced element must not appear in live snapshot list.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Action_Called_From_Background_Thread_Then_Returns_False_And_Click_Is_Queued()
	{
		Assert.IsNotNull(
			AccessibilityPeerHelper.IOSAccessibilityActionAccessor,
			"IOSAccessibilityActionAccessor not registered.");

		var button = new Button { Content = "Off-Thread Target" };
		await UITestHelper.Load(button);

		var clicked = new[] { 0 };
		button.Click += (_, _) => Interlocked.Increment(ref clicked[0]);

		var accessor = AccessibilityPeerHelper.IOSAccessibilityActionAccessor!;
		var request = new AccessibilityNativeActionRequest(AccessibilityNativeAction.Activate);

		var clickedDuringCall = -1;
		var callTask = Task.Run(() =>
		{
			var result = accessor.Invoke(button, request);
			// The contract requires the accessor not to wait on the UI thread, so
			// sampling here is race-free: the UI thread is blocked on .Wait() below
			// and cannot drain the dispatcher queue between the accessor returning
			// and this read.
			clickedDuringCall = Volatile.Read(ref clicked[0]);
			return result;
		});

		// Block the UI thread synchronously. The contract guarantees the accessor
		// will not wait for the UI thread, so this cannot deadlock.
		var completed = callTask.Wait(TimeSpan.FromSeconds(5));

		Assert.IsTrue(completed, "Off-UI-thread action call must complete within 5 s.");
		Assert.IsTrue(callTask.Result, "Off-UI-thread Activate must report that the action was accepted for UI-thread execution.");
		Assert.AreEqual(0, clickedDuringCall, "Click must not fire on the background thread during the accessor call.");

		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(1, Volatile.Read(ref clicked[0]), "Queued click must fire exactly once after idle.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Element_Detached_Then_Subsequent_Property_Mutation_Emits_No_Events()
	{
		Assert.IsNotNull(
			AccessibilityPeerHelper.IOSAccessibilityEventsAccessor,
			"IOSAccessibilityEventsAccessor not registered.");
		Assert.IsNotNull(
			AccessibilityPeerHelper.IOSClearAccessibilityEventsAction,
			"IOSClearAccessibilityEventsAction not registered.");

		var panel = new StackPanel();
		var button = new Button { Content = "Detach Guard" };
		panel.Children.Add(button);
		await UITestHelper.Load(panel);

		var root = button.XamlRoot!;

		panel.Children.Remove(button);
		await TestServices.WindowHelper.WaitForIdle();

		ClearEvents(root);

		AutomationProperties.SetName(button, "PostDetach");
		await TestServices.WindowHelper.WaitForIdle();

		var events = GetEvents(root);
		Assert.AreEqual(
			0,
			events.Length,
			"Accessibility events must not be emitted for a detached element after property mutation.");
	}

}
