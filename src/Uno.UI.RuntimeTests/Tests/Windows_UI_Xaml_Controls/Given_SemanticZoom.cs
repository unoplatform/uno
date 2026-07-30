using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;

using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia | RuntimeTestPlatforms.NativeWinUI)]
public class Given_SemanticZoom
{
	[TestMethod]
	public async Task When_AutomationToggled_ChildrenFollowActiveView()
	{
		var zoomedInView = new ListView
		{
			ItemsSource = new[] { "in" },
		};
		var zoomedOutView = new ListView
		{
			ItemsSource = new[] { "out" },
		};
		AutomationProperties.SetName(zoomedInView, "Zoomed in");
		AutomationProperties.SetName(zoomedOutView, "Zoomed out");

		var sut = new SemanticZoom
		{
			Width = 300,
			Height = 300,
			ZoomedInView = zoomedInView,
			ZoomedOutView = zoomedOutView,
		};
		var completed = false;
		sut.ViewChangeCompleted += (sender, args) => completed = true;

		try
		{
			await UITestHelper.Load(sut, element => element.IsLoaded);
			var peer = (SemanticZoomAutomationPeer)FrameworkElementAutomationPeer.CreatePeerForElement(sut);
			var toggleProvider = (IToggleProvider)peer.GetPattern(PatternInterface.Toggle);

			Assert.AreEqual(ToggleState.On, toggleProvider.ToggleState);
			Assert.IsTrue(peer.GetChildren().Any(child => child.GetName() == "Zoomed in"));

			toggleProvider.Toggle();
			await WindowHelper.WaitFor(() => completed);

			Assert.AreEqual(ToggleState.Off, toggleProvider.ToggleState);
			Assert.IsTrue(peer.GetChildren().Any(child => child.GetName() == "Zoomed out"));
			Assert.IsFalse(peer.GetChildren().Any(child => child.GetName() == "Zoomed in"));
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public void When_Constructed_DefaultsMatchWinUI()
	{
		var sut = new SemanticZoom();

		Assert.IsTrue(sut.IsZoomedInViewActive);
		Assert.IsTrue(sut.CanChangeViews);
		Assert.IsTrue(sut.IsZoomOutButtonEnabled);
		Assert.IsNull(sut.ZoomedInView);
		Assert.IsNull(sut.ZoomedOutView);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_ControlPlusMinusPressed_TogglesViews()
	{
		var zoomedInView = new TestSemanticZoomView("in", new List<string>());
		var zoomedOutView = new TestSemanticZoomView("out", new List<string>());
		var sut = new SemanticZoom
		{
			Width = 300,
			Height = 300,
			ZoomedInView = zoomedInView,
			ZoomedOutView = zoomedOutView,
		};

		try
		{
			await UITestHelper.Load(sut, element => element.IsLoaded);

			await KeyboardHelper.PressKeySequence(
				"$d$_ctrl#$d$_-#$u$_-#$u$_ctrl",
				zoomedInView);
			Assert.IsFalse(sut.IsZoomedInViewActive);

			await KeyboardHelper.PressKeySequence(
				"$d$_ctrl#$d$_+#$u$_+#$u$_ctrl",
				zoomedOutView);
			Assert.IsTrue(sut.IsZoomedInViewActive);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_DirectPropertyChange_UpdatesAutomationToggleState()
	{
		var sut = new SemanticZoom
		{
			Width = 300,
			Height = 300,
			ZoomedInView = new TestSemanticZoomView("in", new List<string>()),
			ZoomedOutView = new TestSemanticZoomView("out", new List<string>()),
		};

		try
		{
			await UITestHelper.Load(sut, element => element.IsLoaded);
			var peer = (SemanticZoomAutomationPeer)FrameworkElementAutomationPeer.CreatePeerForElement(sut);
			Assert.AreEqual(ToggleState.On, peer.ToggleState);

#if __SKIA__
			var listener = new CapturingAutomationListener();
			AutomationPeer.TestAutomationPeerListener = listener;
#endif

			sut.IsZoomedInViewActive = false;

			Assert.AreEqual(ToggleState.Off, peer.ToggleState);
#if __SKIA__
			Assert.AreSame(TogglePatternIdentifiers.ToggleStateProperty, listener.Property);
			Assert.AreEqual(ToggleState.On, listener.OldValue);
			Assert.AreEqual(ToggleState.Off, listener.NewValue);
#endif
		}
		finally
		{
#if __SKIA__
			AutomationPeer.TestAutomationPeerListener = null;
#endif
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public void When_LocationAndEventArgs_AreMutable()
	{
		var source = new SemanticZoomLocation
		{
			Item = "source",
			Bounds = new Rect(1, 2, 3, 4),
		};
		var destination = new SemanticZoomLocation
		{
			Item = "destination",
			Bounds = new Rect(5, 6, 7, 8),
		};
		var args = new SemanticZoomViewChangedEventArgs
		{
			IsSourceZoomedInView = true,
			SourceItem = source,
			DestinationItem = destination,
		};

		Assert.IsTrue(args.IsSourceZoomedInView);
		Assert.AreSame(source, args.SourceItem);
		Assert.AreSame(destination, args.DestinationItem);
		Assert.AreEqual("source", args.SourceItem.Item);
		Assert.AreEqual(new Rect(5, 6, 7, 8), args.DestinationItem.Bounds);
	}

	[TestMethod]
	public async Task When_ZoomOutButtonInvoked_TogglesToZoomedOutView()
	{
		var sut = new SemanticZoom
		{
			Width = 300,
			Height = 300,
			ZoomedInView = new ListView { ItemsSource = new[] { "in" } },
			ZoomedOutView = new ListView { ItemsSource = new[] { "out" } },
		};
		var completed = false;
		sut.ViewChangeCompleted += (sender, args) => completed = true;

		try
		{
			await UITestHelper.Load(sut, element => element.IsLoaded);
			var zoomOutButton = FindNamedDescendant<Button>(sut, "ZoomOutButton");
			Assert.IsNotNull(zoomOutButton);

			new ButtonAutomationPeer(zoomOutButton).Invoke();
			await WindowHelper.WaitFor(() => completed);

			Assert.IsFalse(sut.IsZoomedInViewActive);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_ViewChangeCompleted_ReentersAndThrows_StateConverges()
	{
		var zoomedInView = new TestSemanticZoomView("in", new List<string>());
		var zoomedOutView = new TestSemanticZoomView("out", new List<string>());
		var sut = new SemanticZoom
		{
			Width = 300,
			Height = 300,
			ZoomedInView = zoomedInView,
			ZoomedOutView = zoomedOutView,
		};
		sut.ViewChangeCompleted += (sender, args) =>
		{
			sut.ToggleActiveView();
			throw new InvalidOperationException("Expected test exception.");
		};

		try
		{
			await UITestHelper.Load(sut, element => element.IsLoaded);

			Assert.ThrowsExactly<InvalidOperationException>(sut.ToggleActiveView);

			Assert.IsTrue(sut.IsZoomedInViewActive);
			Assert.IsTrue(zoomedInView.IsActiveView);
			Assert.IsFalse(zoomedOutView.IsActiveView);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_DestinationViewReplacedDuringStarted_RestartsWithReplacement()
	{
		var zoomedInView = new TestSemanticZoomView("in", new List<string>());
		var originalZoomedOutCalls = new List<string>();
		var replacementZoomedOutCalls = new List<string>();
		var originalZoomedOutView = new TestSemanticZoomView("original", originalZoomedOutCalls);
		var replacementZoomedOutView = new TestSemanticZoomView("replacement", replacementZoomedOutCalls);
		var sut = new SemanticZoom
		{
			Width = 300,
			Height = 300,
			ZoomedInView = zoomedInView,
			ZoomedOutView = originalZoomedOutView,
		};
		var startedChanges = 0;
		var completedChanges = 0;
		sut.ViewChangeStarted += (sender, args) =>
		{
			startedChanges++;
			if (startedChanges == 1)
			{
				sut.ZoomedOutView = replacementZoomedOutView;
			}
		};
		sut.ViewChangeCompleted += (sender, args) => completedChanges++;

		try
		{
			await UITestHelper.Load(sut, element => element.IsLoaded);

			sut.ToggleActiveView();
			await WindowHelper.WaitFor(() => completedChanges == 2);

			Assert.AreEqual(2, startedChanges);
			Assert.IsNull(originalZoomedOutView.SemanticZoomOwner);
			Assert.IsFalse(originalZoomedOutView.IsActiveView);
			Assert.AreSame(sut, replacementZoomedOutView.SemanticZoomOwner);
			Assert.IsTrue(replacementZoomedOutView.IsActiveView);
			CollectionAssert.Contains(replacementZoomedOutCalls, "replacement.MakeVisible");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_ViewChangeStarted_Reenters_LatestViewWins()
	{
		var zoomedInView = new TestSemanticZoomView("in", new List<string>());
		var zoomedOutView = new TestSemanticZoomView("out", new List<string>());
		var sut = new SemanticZoom
		{
			Width = 300,
			Height = 300,
			ZoomedInView = zoomedInView,
			ZoomedOutView = zoomedOutView,
		};
		var startedChanges = 0;
		var completedChanges = 0;
		sut.ViewChangeStarted += (sender, args) =>
		{
			startedChanges++;
			if (startedChanges == 1)
			{
				sut.ToggleActiveView();
			}
		};
		sut.ViewChangeCompleted += (sender, args) => completedChanges++;

		try
		{
			await UITestHelper.Load(sut, element => element.IsLoaded);

			sut.ToggleActiveView();
			await WindowHelper.WaitFor(() => completedChanges == 2);

			Assert.AreEqual(2, startedChanges);
			Assert.IsTrue(sut.IsZoomedInViewActive);
			Assert.IsTrue(zoomedInView.IsActiveView);
			Assert.IsFalse(zoomedOutView.IsActiveView);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Toggled_CoordinatesViewLifecycleAndEvents()
	{
		var calls = new List<string>();
		var zoomedInView = new TestSemanticZoomView("in", calls);
		var zoomedOutView = new TestSemanticZoomView("out", calls);
		var sut = new SemanticZoom
		{
			Width = 300,
			Height = 300,
			ZoomedInView = zoomedInView,
			ZoomedOutView = zoomedOutView,
		};

		var completed = false;
		SemanticZoomViewChangedEventArgs completedArgs = null;

		zoomedInView.StartingFrom = (source, destination) =>
		{
			source.Item = "source";
			destination.Item = "default destination";
		};

		sut.ViewChangeStarted += (sender, args) =>
		{
			calls.Add("started");
			args.DestinationItem.Item = "mapped destination";
		};
		sut.ViewChangeCompleted += (sender, args) =>
		{
			calls.Add("completed");
			completedArgs = args;
			completed = true;
		};

		try
		{
			await UITestHelper.Load(sut, element => element.IsLoaded);
			calls.Clear();

			sut.ToggleActiveView();
			await WindowHelper.WaitForIdle();
			Assert.IsTrue(
				completed,
				$"View change did not complete. Active={sut.IsZoomedInViewActive}; Calls={string.Join(", ", calls)}");

			Assert.IsFalse(sut.IsZoomedInViewActive);
			Assert.IsFalse(zoomedInView.IsActiveView);
			Assert.IsTrue(zoomedOutView.IsActiveView);
			Assert.AreEqual("mapped destination", zoomedOutView.LastMadeVisibleItem);
			Assert.IsNotNull(completedArgs);
			Assert.IsTrue(completedArgs.IsSourceZoomedInView);
			Assert.AreEqual("source", completedArgs.SourceItem.Item);
			Assert.AreEqual("mapped destination", completedArgs.DestinationItem.Item);

			var callsBeforeCompletion = new[]
			{
				"in.Initialize",
				"out.Initialize",
				"in.StartFrom",
				"out.StartTo",
				"started",
				"out.MakeVisible",
				"in.CompleteFrom",
				"out.CompleteTo",
			};

			CollectionAssert.AreEqual(callsBeforeCompletion, calls.GetRange(0, callsBeforeCompletion.Length));
			Assert.IsTrue(calls.IndexOf("completed") >= callsBeforeCompletion.Length);

#if HAS_UNO
			CollectionAssert.AreEqual(
				new[] { "completed", "in.Complete", "out.Complete" },
				calls.GetRange(callsBeforeCompletion.Length, 3));
#endif
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_GroupedListViews_Toggle_RoundTripsGroup()
	{
		var groups = new[]
		{
			new SemanticZoomGroup("A", "A1", "A2"),
			new SemanticZoomGroup("B", "B1", "B2"),
		};
		var collectionViewSource = new CollectionViewSource
		{
			Source = groups,
			IsSourceGrouped = true,
			ItemsPath = new PropertyPath(nameof(SemanticZoomGroup.Items)),
		};
		var zoomedInView = new ListView
		{
			ItemsSource = collectionViewSource.View,
			SelectedItem = "B1",
		};
		var zoomedOutView = new ListView
		{
			ItemsSource = collectionViewSource.View.CollectionGroups,
		};
		var sut = new SemanticZoom
		{
			Width = 300,
			Height = 300,
			ZoomedInView = zoomedInView,
			ZoomedOutView = zoomedOutView,
		};
		var startedChanges = new List<SemanticZoomViewChangedEventArgs>();
		var completedChanges = 0;
		sut.ViewChangeStarted += (sender, args) => startedChanges.Add(args);
		sut.ViewChangeCompleted += (sender, args) => completedChanges++;

		try
		{
			await UITestHelper.Load(sut, element => element.IsLoaded);
			zoomedInView.ScrollIntoView("B1");
			zoomedInView.UpdateLayout();
			var sourceContainer = (Control)zoomedInView.ContainerFromItem("B1");
			Assert.IsTrue(sourceContainer.Focus(FocusState.Programmatic));
			Assert.AreSame(sourceContainer, FocusManager.GetFocusedElement(sut.XamlRoot));
#if HAS_UNO
			Assert.IsTrue(sut.TryGetFocusState(out _), "SemanticZoom did not detect focus in its source view.");
#endif

			sut.ToggleActiveView();
			await WindowHelper.WaitFor(() => completedChanges == 1);

			var mappedDestination = startedChanges[0].DestinationItem.Item;
			var destinationGroupData =
				mappedDestination as SemanticZoomGroup ??
				(mappedDestination as ICollectionViewGroup)?.Group as SemanticZoomGroup;
			Assert.IsNotNull(destinationGroupData);
			Assert.AreEqual("B", destinationGroupData.Key);
			var destinationGroup = collectionViewSource.View.CollectionGroups
				.Cast<ICollectionViewGroup>()
				.Single(group => ReferenceEquals(group.Group, destinationGroupData));

			await WindowHelper.WaitFor(
				() => zoomedOutView.ContainerFromItem(destinationGroup) is Control);
			var destinationContainer = (Control)zoomedOutView.ContainerFromItem(destinationGroup);
			await WindowHelper.WaitFor(
				() => ReferenceEquals(
					destinationContainer,
					FocusManager.GetFocusedElement(sut.XamlRoot)));

			zoomedOutView.SelectedItem = destinationGroup;

			sut.ToggleActiveView();
			await WindowHelper.WaitFor(() => completedChanges == 2);

			var reverseSource = startedChanges[1].SourceItem.Item;
			var reverseSourceData =
				reverseSource as SemanticZoomGroup ??
				(reverseSource as ICollectionViewGroup)?.Group as SemanticZoomGroup;
			Assert.AreSame(destinationGroupData, reverseSourceData);
			Assert.AreSame(destinationGroupData, startedChanges[1].DestinationItem.Item);
			Assert.IsTrue(sut.IsZoomedInViewActive);
			Assert.IsTrue(zoomedInView.IsActiveView);
			await WindowHelper.WaitForIdle();
			var expectedFocusedElement = zoomedInView.ContainerFromItem("B1");
			var actualFocusedElement = FocusManager.GetFocusedElement(sut.XamlRoot);
			Assert.AreSame(
				expectedFocusedElement,
				actualFocusedElement,
				$"Focused element: {actualFocusedElement?.GetType().Name ?? "<null>"}; content: {(actualFocusedElement as ContentControl)?.Content ?? "<none>"}");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_ZoomedOutItemClicked_TogglesToZoomedInView()
	{
		var zoomedInView = new ListView
		{
			ItemsSource = new[] { "A1", "B1" },
		};
		var zoomedOutView = new ListView
		{
			ItemsSource = new[] { "A", "B" },
		};
		var sut = new SemanticZoom
		{
			Width = 300,
			Height = 300,
			ZoomedInView = zoomedInView,
			ZoomedOutView = zoomedOutView,
		};
		var completedChanges = 0;
		sut.ViewChangeCompleted += (sender, args) => completedChanges++;

		try
		{
			await UITestHelper.Load(sut, element => element.IsLoaded);

			sut.ToggleActiveView();
			await WindowHelper.WaitFor(() => completedChanges == 1);
			await WindowHelper.WaitFor(() => zoomedOutView.ContainerFromIndex(1) is not null);

#if HAS_UNO
			zoomedOutView.OnItemClicked(1, default);
#else
			Assert.Inconclusive("This test exercises Uno's internal item interaction path.");
#endif
			await WindowHelper.WaitFor(() => completedChanges == 2);

			Assert.IsTrue(sut.IsZoomedInViewActive);
			Assert.IsTrue(zoomedInView.IsActiveView);
			Assert.IsFalse(zoomedOutView.IsActiveView);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_ListViewsAssigned_RolesAndActiveViewAreUpdated()
	{
		var zoomedInView = new ListView
		{
			ItemsSource = new[] { "one", "two" },
		};
		var zoomedOutView = new ListView
		{
			ItemsSource = new[] { "A", "B" },
		};
		var sut = new SemanticZoom
		{
			Width = 300,
			Height = 300,
			ZoomedInView = zoomedInView,
			ZoomedOutView = zoomedOutView,
		};

		try
		{
			await UITestHelper.Load(sut, element => element.IsLoaded);

			Assert.AreSame(sut, zoomedInView.SemanticZoomOwner);
			Assert.AreSame(sut, zoomedOutView.SemanticZoomOwner);
			Assert.IsTrue(zoomedInView.IsZoomedInView);
			Assert.IsFalse(zoomedOutView.IsZoomedInView);
			Assert.IsTrue(zoomedInView.IsActiveView);
			Assert.IsFalse(zoomedOutView.IsActiveView);

			var completed = false;
			sut.ViewChangeCompleted += (sender, args) => completed = true;

			sut.ToggleActiveView();
			await WindowHelper.WaitForIdle();
			Assert.IsTrue(
				completed,
				$"View change did not complete. Active={sut.IsZoomedInViewActive}; InActive={zoomedInView.IsActiveView}; OutActive={zoomedOutView.IsActiveView}");

			Assert.IsFalse(zoomedInView.IsActiveView);
			Assert.IsTrue(zoomedOutView.IsActiveView);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	private static T FindNamedDescendant<T>(DependencyObject root, string name)
		where T : FrameworkElement
	{
		var childCount = VisualTreeHelper.GetChildrenCount(root);
		for (var index = 0; index < childCount; index++)
		{
			var child = VisualTreeHelper.GetChild(root, index);
			if (child is T element && element.Name == name)
			{
				return element;
			}

			if (FindNamedDescendant<T>(child, name) is { } descendant)
			{
				return descendant;
			}
		}

		return null;
	}

	private sealed class TestSemanticZoomView : ContentControl, ISemanticZoomInformation
	{
		private readonly string _name;
		private readonly List<string> _calls;

		public TestSemanticZoomView(string name, List<string> calls)
		{
			_name = name;
			_calls = calls;
			Width = 200;
			Height = 200;
			Content = name;
		}

		public bool IsActiveView { get; set; }

		public bool IsZoomedInView { get; set; }

		public SemanticZoom SemanticZoomOwner { get; set; }

		public object LastMadeVisibleItem { get; private set; }

		public System.Action<SemanticZoomLocation, SemanticZoomLocation> StartingFrom { get; set; }

		public void InitializeViewChange() => _calls.Add($"{_name}.Initialize");

		public void CompleteViewChange() => _calls.Add($"{_name}.Complete");

		public void MakeVisible(SemanticZoomLocation item)
		{
			_calls.Add($"{_name}.MakeVisible");
			LastMadeVisibleItem = item.Item;
		}

		public void StartViewChangeFrom(SemanticZoomLocation source, SemanticZoomLocation destination)
		{
			_calls.Add($"{_name}.StartFrom");
			StartingFrom?.Invoke(source, destination);
		}

		public void StartViewChangeTo(SemanticZoomLocation source, SemanticZoomLocation destination)
			=> _calls.Add($"{_name}.StartTo");

		public void CompleteViewChangeFrom(SemanticZoomLocation source, SemanticZoomLocation destination)
			=> _calls.Add($"{_name}.CompleteFrom");

		public void CompleteViewChangeTo(SemanticZoomLocation source, SemanticZoomLocation destination)
			=> _calls.Add($"{_name}.CompleteTo");
	}

	private sealed class SemanticZoomGroup
	{
		public SemanticZoomGroup(string key, params string[] items)
		{
			Key = key;
			Items = items;
		}

		public string Key { get; }

		public IReadOnlyList<string> Items { get; }
	}

#if __SKIA__
	private sealed class CapturingAutomationListener : IAutomationPeerListener
	{
		public AutomationProperty Property { get; private set; }

		public object OldValue { get; private set; }

		public object NewValue { get; private set; }

		public bool ListenerExistsHelper(AutomationEvents eventId) => true;

		public void OnAutomationEvent(AutomationPeer peer, AutomationEvents eventId)
		{
		}

		public void NotifyAutomationEvent(AutomationPeer peer, AutomationEvents eventId)
		{
		}

		public void NotifyInvalidatePeer(AutomationPeer peer)
		{
		}

		public void NotifyPropertyChangedEvent(
			AutomationPeer peer,
			AutomationProperty automationProperty,
			object oldValue,
			object newValue)
		{
			Property = automationProperty;
			OldValue = oldValue;
			NewValue = newValue;
		}

		public void NotifyNotificationEvent(
			AutomationPeer peer,
			AutomationNotificationKind notificationKind,
			AutomationNotificationProcessing notificationProcessing,
			string displayString,
			string activityId)
		{
		}
	}
#endif
}
