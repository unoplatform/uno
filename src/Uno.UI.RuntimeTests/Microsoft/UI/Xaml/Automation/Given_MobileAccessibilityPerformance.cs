#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

/// <summary>
/// US7 performance tests for native accessibility snapshot enumeration, incremental
/// update bounds, virtualized-list node counts, and registry compaction under
/// repeated add/remove cycles.
/// </summary>
[TestClass]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid | RuntimeTestPlatforms.SkiaIOS)]
public class Given_MobileAccessibilityPerformance
{
	// 10 s is deliberately generous to tolerate slow CI emulators; the assertion
	// is that traversal of 500 nodes does not require seconds of work, not that
	// it is fast in absolute terms.
	private static readonly TimeSpan EnumerationCeiling = TimeSpan.FromSeconds(10);

	private static AccessibilityNativeNodeSnapshot[] GetAllSnapshots(XamlRoot root)
		=> AccessibilityPeerHelper.AndroidAllNodeSnapshotsForRootAccessor?.Invoke(root)
			?? AccessibilityPeerHelper.IOSAllNodeSnapshotsForRootAccessor?.Invoke(root)
			?? Array.Empty<AccessibilityNativeNodeSnapshot>();

	private static AccessibilityNativeNodeSnapshot? GetSnapshot(UIElement element)
		=> MobileAccessibilityTestHelper.TryGetNativeSnapshot(element);

	private static AccessibilityNativeEventRecord[] GetAndClearEvents(XamlRoot root)
	{
		var events = AccessibilityPeerHelper.AndroidAccessibilityEventsAccessor?.Invoke(root)
			?? AccessibilityPeerHelper.IOSAccessibilityEventsAccessor?.Invoke(root)
			?? Array.Empty<AccessibilityNativeEventRecord>();
		AccessibilityPeerHelper.AndroidClearAccessibilityEventsAction?.Invoke(root);
		AccessibilityPeerHelper.IOSClearAccessibilityEventsAction?.Invoke(root);
		return events;
	}

	// Registers every element in the tree so virtual IDs / native handles are
	// assigned before event-based assertions.
	private static void EnsureRegistration(XamlRoot root) => _ = GetAllSnapshots(root);

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_500_Node_Tree_Enumerated_Then_Completes_Under_Ceiling_With_Expected_Count()
	{
		Assert.IsTrue(
			AccessibilityPeerHelper.AndroidAllNodeSnapshotsForRootAccessor is not null
				|| AccessibilityPeerHelper.IOSAllNodeSnapshotsForRootAccessor is not null,
			"No snapshot accessor registered; adapter may not have initialized.");

		const int nodeCount = 500;
		var panel = new StackPanel();
		for (var i = 0; i < nodeCount; i++)
		{
			panel.Children.Add(new TextBlock { Text = $"Node {i}" });
		}

		await UITestHelper.Load(panel);
		await TestServices.WindowHelper.WaitForIdle();

		var root = panel.XamlRoot!;

		var sw = Stopwatch.StartNew();
		var snapshots = GetAllSnapshots(root);
		sw.Stop();

		Assert.IsTrue(
			snapshots.Length >= nodeCount,
			$"Expected at least {nodeCount} snapshots, got {snapshots.Length}.");
		Assert.IsTrue(
			sw.Elapsed < EnumerationCeiling,
			$"Enumeration of {snapshots.Length} nodes took {sw.Elapsed.TotalSeconds:F2} s; ceiling is {EnumerationCeiling.TotalSeconds} s.");

		var nodeNames = snapshots
			.Select(s => s.Name)
			.Where(n => n?.StartsWith("Node ", StringComparison.Ordinal) is true)
			.Take(3)
			.ToArray();

		CollectionAssert.AreEqual(
			new[] { "Node 0", "Node 1", "Node 2" },
			nodeNames,
			"First three accessible nodes must appear in document order.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_One_Node_Updated_In_500_Node_Tree_Then_Invalidation_Count_Is_Bounded()
	{
		Assert.IsTrue(
			AccessibilityPeerHelper.AndroidAllNodeSnapshotsForRootAccessor is not null
				|| AccessibilityPeerHelper.IOSAllNodeSnapshotsForRootAccessor is not null,
			"No snapshot accessor registered.");
		Assert.IsTrue(
			AccessibilityPeerHelper.AndroidAccessibilityEventsAccessor is not null
				|| AccessibilityPeerHelper.IOSAccessibilityEventsAccessor is not null,
			"No event accessor registered.");

		const int nodeCount = 500;
		const int targetIndex = 250;
		var targets = new TextBlock[nodeCount];
		var panel = new StackPanel();
		for (var i = 0; i < nodeCount; i++)
		{
			targets[i] = new TextBlock { Text = $"Node {i}" };
			panel.Children.Add(targets[i]);
		}

		await UITestHelper.Load(panel);
		await TestServices.WindowHelper.WaitForIdle();

		var root = panel.XamlRoot!;

		// Ensure all virtual IDs / handles are registered before observing events.
		EnsureRegistration(root);
		await TestServices.WindowHelper.WaitForIdle();
		GetAndClearEvents(root);

		AutomationProperties.SetName(targets[targetIndex], "Updated Node");
		await TestServices.WindowHelper.WaitForIdle();

		var events = GetAndClearEvents(root);

		Assert.IsTrue(
			events.Any(e => e.Kind == AccessibilityNativeEventKind.NodeInvalidated),
			"At least one NodeInvalidated event must be emitted for the mutated node.");
		Assert.IsTrue(
			events.Length >= 1,
			$"Name mutation must emit at least one event; got 0.");
		Assert.IsTrue(
			events.Length <= 2,
			$"Single-node update must emit at most 2 events for a {nodeCount}-node tree; got {events.Length}.");

		var snap = GetSnapshot(targets[targetIndex]);
		Assert.IsNotNull(snap, "Updated node must still have a native snapshot.");
		Assert.AreEqual("Updated Node", snap.Name, "Snapshot name must reflect the AutomationProperties.Name change.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_1000_Item_ListView_Loaded_Then_Native_Node_Count_Is_Bounded()
	{
		Assert.IsTrue(
			AccessibilityPeerHelper.AndroidAllNodeSnapshotsForRootAccessor is not null
				|| AccessibilityPeerHelper.IOSAllNodeSnapshotsForRootAccessor is not null,
			"No snapshot accessor registered.");

		const int itemCount = 1000;
		var placeholder = new Border { Width = 100, Height = 300 };
		await UITestHelper.Load(placeholder);
		await TestServices.WindowHelper.WaitForIdle();
		var baselineCount = GetAllSnapshots(placeholder.XamlRoot!).Length;

		var items = new List<string>(itemCount);
		for (var i = 0; i < itemCount; i++)
		{
			items.Add($"List Item {i}");
		}

		// Height constraint causes the ListView to virtualize. Root snapshots also
		// include SamplesApp test-host chrome, so compare against its baseline.
		var listView = new ListView
		{
			ItemsSource = items,
			Height = 300,
			SelectionMode = ListViewSelectionMode.None,
		};

		await UITestHelper.Load(listView);
		await TestServices.WindowHelper.WaitForIdle();

		var snapshots = GetAllSnapshots(listView.XamlRoot!);

		Assert.IsTrue(
			snapshots.Length > 0,
			"At least one realized item must appear as a native node.");
		Assert.IsTrue(
			snapshots.Any(snapshot =>
				snapshot.Name?.StartsWith("List Item ", StringComparison.Ordinal) is true),
			"At least one realized ListView item must appear as a native node.");
		Assert.IsTrue(
			snapshots.Length < baselineCount + 200,
			$"Virtualized ListView must add fewer than 200 native nodes for {itemCount} items; baseline={baselineCount}, got {snapshots.Length}.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_AddRemove_Cycles_With_GC_Then_Registry_Does_Not_Grow_Monotonically()
	{
		Assert.IsTrue(
			AccessibilityPeerHelper.AndroidAllNodeSnapshotsForRootAccessor is not null
				|| AccessibilityPeerHelper.IOSAllNodeSnapshotsForRootAccessor is not null,
			"No snapshot accessor registered.");

		var panel = new StackPanel();
		var anchor = new Button { Content = "Anchor" };
		panel.Children.Add(anchor);
		await UITestHelper.Load(panel);
		await TestServices.WindowHelper.WaitForIdle();

		var root = panel.XamlRoot!;

		EnsureRegistration(root);
		var baseline = GetAllSnapshots(root).Length;
		Assert.IsTrue(baseline > 0, "Baseline count must be positive.");

		const int batchSize = 5;
		const int cycles = 3;

		for (var cycle = 0; cycle < cycles; cycle++)
		{
			var batch = new Button[batchSize];
			for (var j = 0; j < batchSize; j++)
			{
				batch[j] = new Button { Content = $"Batch {cycle}-{j}" };
				panel.Children.Add(batch[j]);
			}

			await TestServices.WindowHelper.WaitForIdle();

			var afterAdd = GetAllSnapshots(root).Length;
			Assert.IsTrue(
				afterAdd >= baseline + batchSize,
				$"Cycle {cycle}: count after add ({afterAdd}) must be >= baseline+{batchSize} ({baseline + batchSize}).");

			for (var j = 0; j < batchSize; j++)
			{
				panel.Children.Remove(batch[j]);
			}

			await TestServices.WindowHelper.WaitForIdle();

			// GC to release weak references so the adapter's weak maps compact on
			// the next scan call below.
			GC.Collect();
			GC.WaitForPendingFinalizers();

			EnsureRegistration(root);

			var afterRemove = GetAllSnapshots(root).Length;
			Assert.AreEqual(
				baseline,
				afterRemove,
				$"Cycle {cycle}: registry count ({afterRemove}) must return to baseline ({baseline}) after remove + GC.");
		}
	}
}
