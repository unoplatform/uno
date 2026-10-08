#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
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
