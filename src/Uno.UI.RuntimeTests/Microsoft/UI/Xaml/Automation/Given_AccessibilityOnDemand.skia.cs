#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.Helpers;
using Uno.UI.Extensions;
using Uno.UI.Hosting;
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

		Assert.IsTrue(IsAccessibilityEnabled(GetAccessibility(xamlRoot)), "Reading the tree must enable the bridge.");
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
		private readonly FieldInfo _field;
		private readonly bool _wasEnabled;

		private RouterBridgeState(FieldInfo field)
		{
			_field = field;
			_wasEnabled = field.GetValue(null) is true;
			field.SetValue(null, false);
		}

		public bool IsAnyBridgeEnabled => _field.GetValue(null) is true;

		public static RouterBridgeState ResetToNeverEnabled()
		{
			var router = AppDomain.CurrentDomain.GetAssemblies()
				.Select(assembly => assembly.GetType("Uno.UI.Runtime.AccessibilityRouter", throwOnError: false))
				.FirstOrDefault(type => type is not null)
				?? throw new InvalidOperationException("AccessibilityRouter type not found.");
			var field = router.GetField("_anyBridgeEnabled", BindingFlags.Static | BindingFlags.NonPublic)
				?? throw new InvalidOperationException("AccessibilityRouter._anyBridgeEnabled not found.");
			return new RouterBridgeState(field);
		}

		public void Dispose()
		{
			if (_wasEnabled)
			{
				_field.SetValue(null, true);
			}
		}
	}

	// Earlier tests read the tree, which enables the mobile bridges for the rest of the run: start again from an app
	// no client ever queried, and resume afterwards the way a client's next query would.
	private static async Task<IDisposable> SuspendAccessibilityClient()
	{
		// Lets an already queued native tree rebuild run before the content under test exists.
		await TestServices.WindowHelper.WaitForIdle();

		var accessibility = GetAccessibility(TestServices.WindowHelper.XamlRoot);
		var type = accessibility?.GetType();
		var requestedField = type?.GetField("_clientRequestedTree", BindingFlags.Instance | BindingFlags.NonPublic);
		var ensureRequested = type?.GetMethod("EnsureTreeRequested", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
		if (accessibility is null || requestedField is null || ensureRequested is null)
		{
			return new Resume(null);
		}

		var recordingField = type!.GetField("_recordEvents", BindingFlags.Instance | BindingFlags.NonPublic);
		var wasRequested = requestedField.GetValue(accessibility) is true;
		var wasRecording = recordingField?.GetValue(accessibility) is true;
		requestedField.SetValue(accessibility, false);
		recordingField?.SetValue(accessibility, false);

		var resume = new Resume(() =>
		{
			recordingField?.SetValue(accessibility, wasRecording);
			if (wasRequested)
			{
				ensureRequested.Invoke(accessibility, null);
			}
		});

		if (IsAccessibilityEnabled(accessibility))
		{
			resume.Dispose();
			Assert.Inconclusive("An accessibility service is running on this device.");
		}

		return resume;
	}

	private sealed class Resume(Action? resume) : IDisposable
	{
		private Action? _resume = resume;

		public void Dispose()
		{
			_resume?.Invoke();
			_resume = null;
		}
	}

	private static object? GetAccessibility(XamlRoot xamlRoot)
		=> XamlRootMap.GetHostForRoot(xamlRoot) is { } host
			? host.GetType().GetProperty("Accessibility", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(host)
			: null;

	private static bool IsAccessibilityEnabled(object? accessibility)
		=> accessibility?.GetType().GetProperty("IsAccessibilityEnabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(accessibility) is true;

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
