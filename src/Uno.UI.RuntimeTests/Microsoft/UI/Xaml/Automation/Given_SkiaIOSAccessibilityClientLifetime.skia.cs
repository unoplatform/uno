#nullable enable

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using Uno.UI.Hosting;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

/// <summary>
/// A client's query turns the iOS tree on; VoiceOver and Switch Control turning off must turn it off again, so one
/// query doesn't leave tree tracking on for the rest of the process.
/// </summary>
[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaIOS)]
public class Given_SkiaIOSAccessibilityClientLifetime
{
	[TestMethod]
	public async Task When_Assistive_Technology_Stops_Then_Tree_Is_No_Longer_Tracked()
	{
		var button = new Button { Content = "Tracked" };
		await UITestHelper.Load(button);
		using var adapter = AdapterState.Capture(button.XamlRoot!);

		_ = QuerySnapshots(button.XamlRoot!);
		Assert.IsTrue(adapter.IsEnabled, "A client's query must enable the tree.");

		adapter.RaiseAssistiveTechnologyStatusChanged();

		Assert.IsFalse(adapter.IsEnabled, "With no assistive technology left, the tree must stop being tracked.");
	}

	[TestMethod]
	public async Task When_Client_Queries_After_Release_Then_Tree_Is_Rebuilt()
	{
		var panel = new StackPanel();
		panel.Children.Add(new Button { Content = "Existing" });
		await UITestHelper.Load(panel);
		var xamlRoot = panel.XamlRoot!;
		using var adapter = AdapterState.Capture(xamlRoot);

		_ = QuerySnapshots(xamlRoot);
		adapter.RaiseAssistiveTechnologyStatusChanged();
		Assert.IsFalse(adapter.IsEnabled);

		panel.Children.Add(new Button { Content = "Added while released" });
		await TestServices.WindowHelper.WaitForIdle();

		var names = QuerySnapshots(xamlRoot).Select(s => s.Name).ToArray();

		Assert.IsTrue(adapter.IsEnabled, "A later query must enable the tree again.");
		CollectionAssert.Contains(names, "Existing");
		CollectionAssert.Contains(names, "Added while released");
	}

	[TestMethod]
	public async Task When_Events_Are_Recorded_Then_Tree_Stays_Tracked()
	{
		var button = new Button { Content = "Recorded" };
		await UITestHelper.Load(button);
		using var adapter = AdapterState.Capture(button.XamlRoot!);

		_ = AccessibilityPeerHelper.IOSAccessibilityEventsAccessor?.Invoke(button.XamlRoot!);
		Assert.IsTrue(adapter.IsEnabled);

		adapter.RaiseAssistiveTechnologyStatusChanged();

		Assert.IsTrue(adapter.IsEnabled, "A client recording events must keep the tree tracked.");
	}

	private static AccessibilityNativeNodeSnapshot[] QuerySnapshots(XamlRoot xamlRoot)
		=> AccessibilityPeerHelper.IOSAllNodeSnapshotsForRootAccessor?.Invoke(xamlRoot)
			?? Array.Empty<AccessibilityNativeNodeSnapshot>();

	// Starts from a tree no client asked for and no test records, and restores both afterwards.
	private sealed class AdapterState : IDisposable
	{
		private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

		private readonly object _adapter;
		private readonly FieldInfo _requestedField;
		private readonly FieldInfo _recordingField;
		private readonly bool _wasRequested;
		private readonly bool _wasRecording;

		private AdapterState(object adapter)
		{
			var type = adapter.GetType();
			_adapter = adapter;
			_requestedField = type.GetField("_clientRequestedTree", Flags) ?? throw new InvalidOperationException("_clientRequestedTree not found.");
			_recordingField = type.GetField("_recordEvents", Flags) ?? throw new InvalidOperationException("_recordEvents not found.");
			_wasRequested = _requestedField.GetValue(adapter) is true;
			_wasRecording = _recordingField.GetValue(adapter) is true;
			_recordingField.SetValue(adapter, false);
		}

		public static AdapterState Capture(XamlRoot xamlRoot)
		{
			var host = XamlRootMap.GetHostForRoot(xamlRoot);
			var adapter = host?.GetType().GetProperty("Accessibility", Flags)?.GetValue(host)
				?? throw new InvalidOperationException("The iOS host must expose its accessibility adapter.");

			if (adapter.GetType().GetProperty("IsAssistiveTechnologyRunning", Flags)?.GetValue(null) is true)
			{
				Assert.Inconclusive("VoiceOver or Switch Control is running on this device.");
			}

			return new AdapterState(adapter);
		}

		public bool IsEnabled
			=> _adapter.GetType().GetProperty("IsAccessibilityEnabled", Flags)?.GetValue(_adapter) is true;

		// UIKit raises this on VoiceOver or Switch Control status changes, which a test cannot toggle.
		public void RaiseAssistiveTechnologyStatusChanged()
			=> (_adapter.GetType().GetMethod("OnAssistiveTechnologyStatusChanged", Flags)
				?? throw new InvalidOperationException("OnAssistiveTechnologyStatusChanged not found."))
				.Invoke(_adapter, new object?[] { null, null });

		public void Dispose()
		{
			_recordingField.SetValue(_adapter, _wasRecording);
			if (_wasRequested)
			{
				_adapter.GetType().GetMethod("EnsureTreeRequested", Flags)?.Invoke(_adapter, null);
			}
		}
	}
}
