#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.Helpers;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

/// <summary>
/// With no accessibility client, the framework must not create automation peers or maintain a native tree: most users
/// never turn on a screen reader and should not pay for one.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_AccessibilityOnDemand
{
	[TestMethod]
	// A macOS window an earlier test queried keeps its native tree, and so its peers, in sync.
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaMacOS)]
	public async Task When_ItemsSource_Changes_Then_ListView_Peer_Is_Not_Created()
	{
		using var _ = await SuspendAccessibilityClient();

		var items = new ObservableCollection<string> { "One", "Two", "Three" };
		var listView = new ListView { ItemsSource = items };
		await UITestHelper.Load(listView);
		if (OperatingSystem.IsWindows() && listView.CachedAutomationPeer is not null)
		{
			// A desktop UIA client (e.g. one tracking focus) walks the new content as soon as it loads.
			Assert.Inconclusive("An accessibility client created the list's peer when it loaded.");
		}

		items.Add("Four");
		items.RemoveAt(0);
		items[0] = "Replaced";
		items.Clear();
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsNull(listView.CachedAutomationPeer, "Only an accessibility client may create the list's peer.");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid | RuntimeTestPlatforms.SkiaIOS)]
	public async Task When_No_Client_Then_Scrolling_A_ListView_Creates_No_Automation_Peers()
	{
		using var _ = await SuspendAccessibilityClient();

		var items = new ObservableCollection<string>(Enumerable.Range(0, 200).Select(i => $"Item {i}"));
		var listView = new ListView { ItemsSource = items, Height = 300 };
		await UITestHelper.Load(listView);

		var scrollViewer = listView.FindFirstDescendant<ScrollViewer>();
		Assert.IsNotNull(scrollViewer);
		Assert.IsGreaterThan(0d, scrollViewer.ScrollableHeight, "The list must scroll to recycle containers.");
		for (var offset = 0d; offset < scrollViewer.ScrollableHeight; offset += 400)
		{
			scrollViewer.ChangeView(null, offset, null, disableAnimation: true);
			await TestServices.WindowHelper.WaitForIdle();
		}

		items.Insert(150, "Inserted");
		items.RemoveAt(10);
		await TestServices.WindowHelper.WaitForIdle();

		var withPeer = EnumerateSubtree(listView).Where(element => element.CachedAutomationPeer is not null).ToArray();
		Assert.AreEqual(
			0,
			withPeer.Length,
			$"Created peers for: {string.Join(", ", withPeer.Select(element => element.GetType().Name).Distinct())}");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid | RuntimeTestPlatforms.SkiaIOS)]
	public async Task When_First_Queried_Then_Tree_Includes_Content_Added_Without_A_Client()
	{
		using var _ = await SuspendAccessibilityClient();

		var button = new Button { Content = "Added without a client" };
		await UITestHelper.Load(button);

		var xamlRoot = button.XamlRoot!;
		var snapshots = AccessibilityPeerHelper.AndroidAllNodeSnapshotsForRootAccessor?.Invoke(xamlRoot)
			?? AccessibilityPeerHelper.IOSAllNodeSnapshotsForRootAccessor?.Invoke(xamlRoot)
			?? Array.Empty<AccessibilityNativeNodeSnapshot>();

		Assert.IsTrue(AccessibilityBridgeHelper.IsMobileAccessibilityEnabled(xamlRoot), "Reading the tree must enable the bridge.");
		Assert.IsTrue(
			snapshots.Any(snapshot => snapshot.Name == "Added without a client"),
			"The first read must see content added while no client listened.");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid | RuntimeTestPlatforms.SkiaIOS | RuntimeTestPlatforms.SkiaMacOS | RuntimeTestPlatforms.SkiaWin32)]
	public void When_Checking_For_Listeners_Then_Nothing_Is_Allocated()
	{
		// TextBlock asks on every text change.
		AutomationPeer.ListenerExists(AutomationEvents.PropertyChanged);

		var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
		for (var i = 0; i < 100; i++)
		{
			AutomationPeer.ListenerExists(AutomationEvents.PropertyChanged);
		}

		Assert.AreEqual(0, GC.GetAllocatedBytesForCurrentThread() - allocatedBefore);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid | RuntimeTestPlatforms.SkiaIOS)]
	public async Task When_No_Client_Then_Visual_Tree_Changes_Are_Not_Routed_Until_The_First_Query()
	{
		using var suspended = await SuspendAccessibilityClient();

		// Earlier tests enabled the routing for the rest of the run: start again as an app no client ever queried.
		var childAdded = UIElementAccessibilityHelper.ExternalOnChildAdded;
		var childRemoved = UIElementAccessibilityHelper.ExternalOnChildRemoved;
		var visualChanged = VisualAccessibilityHelper.ExternalOnVisualOffsetOrSizeChanged;
		UIElementAccessibilityHelper.ExternalOnChildAdded = null;
		UIElementAccessibilityHelper.ExternalOnChildRemoved = null;
		VisualAccessibilityHelper.ExternalOnVisualOffsetOrSizeChanged = null;
		try
		{
			var button = new Button { Content = "Moved without a client" };
			await UITestHelper.Load(button);
			button.Margin = new Thickness(20);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsNull(VisualAccessibilityHelper.ExternalOnVisualOffsetOrSizeChanged, "Without a client, layout must not pay for routing.");
			Assert.IsNull(UIElementAccessibilityHelper.ExternalOnChildAdded);

			var xamlRoot = button.XamlRoot!;
			_ = AccessibilityPeerHelper.AndroidAllNodeSnapshotsForRootAccessor?.Invoke(xamlRoot)
				?? AccessibilityPeerHelper.IOSAllNodeSnapshotsForRootAccessor?.Invoke(xamlRoot);

			Assert.IsNotNull(VisualAccessibilityHelper.ExternalOnVisualOffsetOrSizeChanged, "The first query must start routing.");
			Assert.IsNotNull(UIElementAccessibilityHelper.ExternalOnChildAdded);
			Assert.IsNotNull(UIElementAccessibilityHelper.ExternalOnChildRemoved);
		}
		finally
		{
			UIElementAccessibilityHelper.ExternalOnChildAdded = childAdded;
			UIElementAccessibilityHelper.ExternalOnChildRemoved = childRemoved;
			VisualAccessibilityHelper.ExternalOnVisualOffsetOrSizeChanged = visualChanged;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid | RuntimeTestPlatforms.SkiaIOS)]
	public async Task When_No_Bridge_Was_Ever_Enabled_Then_The_Router_Short_Circuits_Until_The_First_Query()
	{
		using var suspended = await SuspendAccessibilityClient();
		using var router = RouterBridgeState.ResetToNeverEnabled();

		var button = new Button { Content = "Changed without a bridge" };
		await UITestHelper.Load(button);

		Assert.IsFalse(router.IsAnyBridgeEnabled);
		Assert.IsFalse(AutomationPeer.ListenerExists(AutomationEvents.PropertyChanged), "No bridge can listen before one is enabled.");

		AutomationProperties.SetHelpText(button, "Help");
		Assert.IsNull(button.CachedAutomationPeer, "Without an enabled bridge, a property change must not create a peer.");

		var xamlRoot = button.XamlRoot!;
		_ = AccessibilityPeerHelper.AndroidAllNodeSnapshotsForRootAccessor?.Invoke(xamlRoot)
			?? AccessibilityPeerHelper.IOSAllNodeSnapshotsForRootAccessor?.Invoke(xamlRoot);

		Assert.IsTrue(router.IsAnyBridgeEnabled, "The first query must enable routing.");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid | RuntimeTestPlatforms.SkiaIOS)]
	public async Task When_Changed_Before_Any_Bridge_Was_Enabled_Then_The_First_Query_Sees_The_Change()
	{
		using var suspended = await SuspendAccessibilityClient();
		using var router = RouterBridgeState.ResetToNeverEnabled();

		var renamed = new Button { Content = "Original name" };
		var hidden = new Button { Content = "Hidden before the first query" };
		await UITestHelper.Load(new StackPanel { Children = { renamed, hidden } });

		// The router drops both signals: no bridge is enabled yet.
		AutomationProperties.SetName(renamed, "Renamed before the first query");
		AutomationProperties.SetAccessibilityView(hidden, AccessibilityView.Raw);
		await TestServices.WindowHelper.WaitForIdle();
		Assert.IsFalse(router.IsAnyBridgeEnabled);

		var xamlRoot = renamed.XamlRoot!;
		_ = AccessibilityPeerHelper.AndroidAllNodeSnapshotsForRootAccessor?.Invoke(xamlRoot)
			?? AccessibilityPeerHelper.IOSAllNodeSnapshotsForRootAccessor?.Invoke(xamlRoot);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(router.IsAnyBridgeEnabled, "The first query must enable routing.");
		Assert.AreEqual("Renamed before the first query", MobileAccessibilityTestHelper.TryGetNativeSnapshot(renamed)?.Name);
		Assert.IsNull(
			MobileAccessibilityTestHelper.TryGetNativeSnapshot(hidden),
			"A raw view set before the first query must stay out of the tree.");
	}

	// Earlier tests enabled a bridge, which the router remembers for the rest of the run.
	private sealed class RouterBridgeState : IDisposable
	{
		private readonly bool _wasEnabled;

		private RouterBridgeState()
		{
			_wasEnabled = IsAnyBridgeEnabled;
			AccessibilityPeerHelper.SetAnyAccessibilityBridgeEnabledAction!(false);
		}

		public bool IsAnyBridgeEnabled => AccessibilityPeerHelper.IsAnyAccessibilityBridgeEnabledAccessor?.Invoke() is true;

		public static RouterBridgeState ResetToNeverEnabled()
			=> AccessibilityPeerHelper.IsAnyAccessibilityBridgeEnabledAccessor is null || AccessibilityPeerHelper.SetAnyAccessibilityBridgeEnabledAction is null
				? throw new InvalidOperationException("The accessibility router is not initialized.")
				: new RouterBridgeState();

		public void Dispose()
		{
			if (_wasEnabled)
			{
				AccessibilityPeerHelper.SetAnyAccessibilityBridgeEnabledAction?.Invoke(true);
			}
		}
	}

	private static Task<IDisposable> SuspendAccessibilityClient() => AccessibilityBridgeHelper.SuspendMobileAccessibilityClient();

	private static IEnumerable<UIElement> EnumerateSubtree(UIElement root)
	{
		var pending = new Stack<UIElement>();
		pending.Push(root);
		while (pending.Count > 0)
		{
			var element = pending.Pop();
			yield return element;

			for (var i = VisualTreeHelper.GetChildrenCount(element) - 1; i >= 0; i--)
			{
				if (VisualTreeHelper.GetChild(element, i) is UIElement child)
				{
					pending.Push(child);
				}
			}
		}
	}
}
