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
using Uno.UI.Toolkit.DevTools.Input;
using Windows.UI.Input.Preview.Injection;

#if HAS_UNO
using DirectUI;
#endif

using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia | RuntimeTestPlatforms.NativeWinUI)]
public partial class Given_SemanticZoom
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
			peer.InvalidatePeer();
			await WindowHelper.WaitFor(() => peer.GetChildren().Any(child => child.GetName() == "Zoomed out"));
			var activeChildren = peer.GetChildren();

			Assert.AreEqual(ToggleState.Off, toggleProvider.ToggleState);
			Assert.IsTrue(activeChildren.Any(child => child.GetName() == "Zoomed out"));
			Assert.IsFalse(activeChildren.Any(child => child.GetName() == "Zoomed in"));
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
		var scrollViewer = new ScrollViewer();

		Assert.IsTrue(sut.IsZoomedInViewActive);
		Assert.IsTrue(sut.CanChangeViews);
		Assert.IsTrue(sut.IsZoomOutButtonEnabled);
		Assert.IsNull(sut.ZoomedInView);
		Assert.IsNull(sut.ZoomedOutView);
		Assert.AreEqual(ZoomMode.Enabled, scrollViewer.ZoomMode);
		Assert.AreEqual(ScrollBarVisibility.Visible, scrollViewer.VerticalScrollBarVisibility);
		Assert.IsFalse(scrollViewer.CanContentRenderOutsideBounds);
		Assert.IsFalse(scrollViewer.IsDeferredScrollingEnabled);
		Assert.IsTrue(scrollViewer.IsZoomChainingEnabled);
		Assert.IsTrue(scrollViewer.IsZoomInertiaEnabled);
		Assert.IsFalse(scrollViewer.ReduceViewportForCoreInputViewOcclusions);
		Assert.IsNull(scrollViewer.LeftHeader);
		Assert.IsNull(scrollViewer.TopHeader);
		Assert.IsNull(scrollViewer.TopLeftHeader);
		Assert.AreEqual(SnapPointsType.Optional, scrollViewer.ZoomSnapPointsType);
		Assert.IsNotNull(scrollViewer.ZoomSnapPoints);
		Assert.AreSame(scrollViewer.ZoomSnapPoints, scrollViewer.ZoomSnapPoints);
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
			await WindowHelper.WaitForIdle();

			await KeyboardHelper.PressKeySequence(
				"$d$_ctrl#$d$_-#$u$_-#$u$_ctrl",
				zoomedInView);
			await WindowHelper.WaitFor(() => !sut.IsZoomedInViewActive);

			await KeyboardHelper.PressKeySequence(
				"$d$_ctrl#$d$_+#$u$_+#$u$_ctrl",
				zoomedOutView);
			await WindowHelper.WaitFor(() => sut.IsZoomedInViewActive);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_ControlMouseWheel_ChangesActiveView()
	{
		var sut = CreateSemanticZoom();

		try
		{
			await UITestHelper.Load(sut, element => element.IsLoaded);
			var scrollViewer = FindNamedDescendant<ScrollViewer>(sut, "ScrollViewer")
				?? throw new InvalidOperationException("SemanticZoom did not contain its ScrollViewer template part");
			scrollViewer.ZoomMode = ZoomMode.Enabled;
			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();
			mouse.MoveTo(sut.TransformToVisual(null).TransformPoint(new Point(sut.ActualWidth / 2, sut.ActualHeight / 2)));

			try
			{
				await KeyboardHelper.PressKeySequence("$d$_ctrl", sut);

				mouse.WheelDown();
				await WindowHelper.WaitFor(() => !sut.IsZoomedInViewActive);

				mouse.WheelUp();
				await WindowHelper.WaitFor(() => sut.IsZoomedInViewActive);
			}
			finally
			{
				await KeyboardHelper.PressKeySequence("$u$_ctrl", sut);
			}
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public void When_CanChangeViewsIsFalse_ActiveViewCannotChange()
	{
		var sut = new SemanticZoom
		{
			CanChangeViews = false,
		};

		Assert.ThrowsExactly<InvalidOperationException>(() => sut.IsZoomedInViewActive = false);
		Assert.IsFalse(sut.IsZoomedInViewActive);
	}

#if HAS_UNO
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_BackPressed_ZoomedOutViewReturnsToZoomedInView()
	{
		var sut = CreateSemanticZoom();
		var completedChanges = 0;
		sut.ViewChangeCompleted += (sender, args) => completedChanges++;

		try
		{
			await UITestHelper.Load(sut, element => element.IsLoaded);

			ToggleThroughAutomation(sut);
			await WindowHelper.WaitFor(() => completedChanges == 1);
			Assert.IsFalse(sut.IsZoomedInViewActive);

			Assert.IsTrue(sut.OnBackButtonPressedImpl());
			await WindowHelper.WaitFor(() => completedChanges == 2);
			Assert.IsTrue(sut.IsZoomedInViewActive);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_DirectManipulationCrossesThreshold_TogglesBothDirections()
	{
		var sut = CreateSemanticZoom();
		((FrameworkElement)sut.ZoomedInView).Width = 600;
		((FrameworkElement)sut.ZoomedInView).Height = 600;
		((FrameworkElement)sut.ZoomedOutView).Width = 600;
		((FrameworkElement)sut.ZoomedOutView).Height = 600;
		ScrollViewer.SetZoomMode(sut, ZoomMode.Enabled);
		var completedChanges = 0;
		sut.ViewChangeCompleted += (sender, args) => completedChanges++;

		try
		{
			await UITestHelper.Load(sut, element => element.IsLoaded);
			var scrollViewer = FindNamedDescendant<ScrollViewer>(sut, "ScrollViewer")
				?? throw new InvalidOperationException("SemanticZoom did not contain its ScrollViewer template part");
			Assert.AreEqual(ZoomMode.Enabled, scrollViewer.ZoomMode);
			var zoomRange = await Pinch(sut, scrollViewer, startHalfSpan: 80, endHalfSpan: 25);
			Assert.IsLessThan(0.9f, zoomRange.minimum,
				$"Pinch did not lower ZoomFactor; range={zoomRange.minimum}..{zoomRange.maximum}, extent={scrollViewer.ExtentWidth}x{scrollViewer.ExtentHeight}, viewport={scrollViewer.ViewportWidth}x{scrollViewer.ViewportHeight}");

			await WindowHelper.WaitFor(() => completedChanges == 1);
			Assert.IsFalse(sut.IsZoomedInViewActive);

			zoomRange = await Pinch(sut, scrollViewer, startHalfSpan: 25, endHalfSpan: 80);
			Assert.IsGreaterThan(0.6f, zoomRange.maximum,
				$"Pinch did not raise ZoomFactor; range={zoomRange.minimum}..{zoomRange.maximum}");

			await WindowHelper.WaitFor(() => completedChanges == 2);
			Assert.IsTrue(sut.IsZoomedInViewActive);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
#endif

#if HAS_UNO
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_SizeChanges_LayoutSizeAndActiveZoomAreReset()
	{
		var sut = CreateSemanticZoom();

		try
		{
			await UITestHelper.Load(sut, element => element.IsLoaded);
			var scrollViewer = FindNamedDescendant<ScrollViewer>(sut, "ScrollViewer");
			Assert.IsNotNull(scrollViewer);

			sut.Width = 420;
			sut.Height = 260;
			await WindowHelper.WaitForIdle();

			var layoutSize = scrollViewer.GetLayoutSize();
			Assert.IsGreaterThan(0, layoutSize.Width);
			Assert.IsGreaterThan(0, layoutSize.Height);
			Assert.AreEqual(1.0f, scrollViewer.ZoomFactor);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
#endif

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_ViewIsReplaced_RolesMoveToReplacement()
	{
		var originalCalls = new List<string>();
		var replacementCalls = new List<string>();
		var original = new TestSemanticZoomView("original", originalCalls);
		var replacement = new TestSemanticZoomView("replacement", replacementCalls);
		var sut = new SemanticZoom
		{
			Width = 300,
			Height = 300,
			ZoomedInView = new TestSemanticZoomView("in", new List<string>()),
			ZoomedOutView = original,
		};
		var completed = false;
		sut.ViewChangeCompleted += (sender, args) => completed = true;

		try
		{
			await UITestHelper.Load(sut, element => element.IsLoaded);

			sut.ZoomedOutView = replacement;

			Assert.IsNull(original.SemanticZoomOwner);
			Assert.IsFalse(original.IsActiveView);
			Assert.AreSame(sut, replacement.SemanticZoomOwner);
			Assert.IsFalse(replacement.IsZoomedInView);

			ToggleThroughAutomation(sut);
			await WindowHelper.WaitFor(() => completed);

			Assert.IsTrue(replacement.IsActiveView);
			CollectionAssert.Contains(replacementCalls, "replacement.MakeVisible");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_UnloadedAndReloaded_TransitionSubscriptionsRemainSingle()
	{
		var sut = CreateSemanticZoom();
		var completedChanges = 0;
		sut.ViewChangeCompleted += (sender, args) => completedChanges++;

		try
		{
			await UITestHelper.Load(sut, element => element.IsLoaded);
			WindowHelper.WindowContent = null;
			await WindowHelper.WaitFor(() => !sut.IsLoaded);
			await UITestHelper.Load(sut, element => element.IsLoaded);

			ToggleThroughAutomation(sut);
			await WindowHelper.WaitFor(() => completedChanges == 1);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, completedChanges);
			Assert.IsFalse(sut.IsZoomedInViewActive);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_Retemplated_TransitionSubscriptionsRemainSingle()
	{
		var sut = CreateSemanticZoom();
		var completedChanges = 0;
		sut.ViewChangeCompleted += (sender, args) => completedChanges++;

		try
		{
			await UITestHelper.Load(sut, element => element.IsLoaded);
			var template = sut.Template;
			Assert.IsNotNull(template);

			sut.Template = null;
			sut.Template = template;
			sut.ApplyTemplate();
			await WindowHelper.WaitForIdle();

			var zoomOutButton = FindNamedDescendant<Button>(sut, "ZoomOutButton");
			Assert.IsNotNull(zoomOutButton);
			new ButtonAutomationPeer(zoomOutButton).Invoke();
			await WindowHelper.WaitFor(() => completedChanges == 1);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, completedChanges);
			Assert.IsFalse(sut.IsZoomedInViewActive);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_AutomationToggle_RaisesPropertyChanged()
	{
		var sut = new SemanticZoom
		{
			Width = 300,
			Height = 300,
			ZoomedInView = new TestSemanticZoomView("in", new List<string>()),
			ZoomedOutView = new TestSemanticZoomView("out", new List<string>()),
		};
		var completedChanges = 0;
		sut.ViewChangeCompleted += (sender, args) => completedChanges++;

		try
		{
			await UITestHelper.Load(sut, element => element.IsLoaded);
			var peer = (SemanticZoomAutomationPeer)FrameworkElementAutomationPeer.CreatePeerForElement(sut);
			Assert.AreEqual(ToggleState.On, peer.ToggleState);

#if __SKIA__
			var listener = new CapturingAutomationListener();
			AutomationPeer.TestAutomationPeerListener = listener;
#endif

			((IToggleProvider)peer).Toggle();
			await WindowHelper.WaitFor(() => completedChanges == 1);

#if __SKIA__
			Assert.AreSame(TogglePatternIdentifiers.ToggleStateProperty, listener.Property);
			Assert.AreEqual(ToggleState.Off, listener.OldValue);
			Assert.AreEqual(ToggleState.On, listener.NewValue);
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
		Assert.IsTrue(typeof(SemanticZoom).IsSealed);
		Assert.IsTrue(typeof(SemanticZoomLocation).IsSealed);
		Assert.IsTrue(typeof(SemanticZoomViewChangedEventArgs).IsSealed);
		Assert.IsTrue(typeof(ScrollContentPresenter).IsSealed);
		Assert.IsTrue(typeof(ScrollViewerView).IsSealed);
		Assert.IsTrue(typeof(ScrollViewerViewChangingEventArgs).IsSealed);
		Assert.AreEqual(typeof(object), typeof(SemanticZoomViewChangedEventArgs).BaseType);

		var defaultLocation = new SemanticZoomLocation();
		Assert.IsNull(defaultLocation.Item);
		Assert.AreEqual(0, defaultLocation.Bounds.X);
		Assert.AreEqual(0, defaultLocation.Bounds.Y);
		Assert.AreEqual(-1, defaultLocation.Bounds.Width);
		Assert.AreEqual(-1, defaultLocation.Bounds.Height);
#if HAS_UNO
		Assert.AreEqual(default(Point), defaultLocation.ZoomPoint);
		Assert.AreEqual(default(Rect), defaultLocation.Remainder);
		Assert.IsTrue(defaultLocation.IsBottomAlignment);
#endif

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

			ToggleThroughAutomation(sut);
			await WindowHelper.WaitFor(() => completed);

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
			Assert.AreEqual(callsBeforeCompletion.Length, calls.IndexOf("completed"));
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
		var destinationGroupData = groups[1];
		var destinationGroup = collectionViewSource.View.CollectionGroups
			.Cast<ICollectionViewGroup>()
			.Single(group => ReferenceEquals(group.Group, destinationGroupData));
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

			var scrollViewer = FindNamedDescendant<ScrollViewer>(sut, "ScrollViewer")
				?? throw new InvalidOperationException("SemanticZoom did not contain its ScrollViewer template part");
			scrollViewer.ZoomMode = ZoomMode.Enabled;
			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();
			mouse.MoveTo(sourceContainer.TransformToVisual(null).TransformPoint(
				new Point(sourceContainer.ActualWidth / 2, sourceContainer.ActualHeight / 2)));
			try
			{
				await KeyboardHelper.PressKeySequence("$d$_ctrl", sut);
				mouse.WheelDown();
				await WindowHelper.WaitFor(() => completedChanges == 1);
			}
			finally
			{
				await KeyboardHelper.PressKeySequence("$u$_ctrl", sut);
			}

			Assert.AreSame(destinationGroup, startedChanges[0].DestinationItem.Item);

			await WindowHelper.WaitFor(
				() => zoomedOutView.ContainerFromItem(destinationGroup) is Control);
			var destinationContainer = (Control)zoomedOutView.ContainerFromItem(destinationGroup);
			await WindowHelper.WaitFor(
				() => ReferenceEquals(
					destinationContainer,
					FocusManager.GetFocusedElement(sut.XamlRoot)));

			Assert.IsTrue(destinationContainer.Focus(FocusState.Keyboard));
			await KeyboardHelper.PressKeySequence("$d$_enter#$u$_enter", destinationContainer);
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
	public async Task When_ZoomedOutItemInvoked_TogglesToZoomedInView()
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

			ToggleThroughAutomation(sut);
			await WindowHelper.WaitFor(() => completedChanges == 1);
			await WindowHelper.WaitFor(() => zoomedOutView.ContainerFromIndex(1) is Control);

			var destinationContainer = (Control)zoomedOutView.ContainerFromIndex(1);
			Assert.IsTrue(destinationContainer.Focus(FocusState.Keyboard));
			await KeyboardHelper.PressKeySequence("$d$_enter#$u$_enter", destinationContainer);
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

			ToggleThroughAutomation(sut);
			await WindowHelper.WaitFor(() => completed);

			Assert.IsFalse(zoomedInView.IsActiveView);
			Assert.IsTrue(zoomedOutView.IsActiveView);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	private static SemanticZoom CreateSemanticZoom() =>
		new()
		{
			Width = 300,
			Height = 300,
			ZoomedInView = new TestSemanticZoomView("in", new List<string>()),
			ZoomedOutView = new TestSemanticZoomView("out", new List<string>()),
		};

	private static void ToggleThroughAutomation(SemanticZoom semanticZoom)
	{
		var peer = FrameworkElementAutomationPeer.CreatePeerForElement(semanticZoom)
			?? throw new InvalidOperationException("Failed to create the SemanticZoom automation peer");
		((IToggleProvider)peer.GetPattern(PatternInterface.Toggle)).Toggle();
	}

	private static async Task<(float minimum, float maximum)> Pinch(
		FrameworkElement element,
		ScrollViewer scrollViewer,
		double startHalfSpan,
		double endHalfSpan)
	{
		var center = element.TransformToVisual(null).TransformPoint(
			new Point(element.ActualWidth / 2, element.ActualHeight / 2));
		var injector1 = InputInjector.TryCreate()
			?? throw new InvalidOperationException("Failed to initialize the first touch injector");
		var injector2 = InputInjector.TryCreate()
			?? throw new InvalidOperationException("Failed to initialize the second touch injector");
		using var finger1 = injector1.GetFinger(id: 101);
		using var finger2 = injector2.GetFinger(id: 102);
		var minimumZoomFactor = scrollViewer.ZoomFactor;
		var maximumZoomFactor = scrollViewer.ZoomFactor;

		finger1.Press(new Point(center.X - startHalfSpan, center.Y));
		await WindowHelper.WaitForIdle();
		finger2.Press(new Point(center.X + startHalfSpan, center.Y));
		await WindowHelper.WaitForIdle();

		const int stepCount = 10;
		for (var step = 1; step <= stepCount; step++)
		{
			var halfSpan = startHalfSpan + (endHalfSpan - startHalfSpan) * step / stepCount;
			finger1.MoveTo(new Point(center.X - halfSpan, center.Y), steps: 2);
			await WindowHelper.WaitForIdle();
			finger2.MoveTo(new Point(center.X + halfSpan, center.Y), steps: 2);
			await WindowHelper.WaitForIdle();
			minimumZoomFactor = Math.Min(minimumZoomFactor, scrollViewer.ZoomFactor);
			maximumZoomFactor = Math.Max(maximumZoomFactor, scrollViewer.ZoomFactor);
		}

		finger2.Release();
		finger1.Release();
		await WindowHelper.WaitForIdle();
		return (minimumZoomFactor, maximumZoomFactor);
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

		return default;
	}

	private sealed partial class TestSemanticZoomView : ContentControl, ISemanticZoomInformation
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
