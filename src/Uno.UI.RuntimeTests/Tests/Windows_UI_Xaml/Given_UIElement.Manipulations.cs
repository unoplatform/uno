using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI.Input.Preview.Injection;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.Extensions;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Toolkit.Extensions;
using Uno.UI.Toolkit.DevTools.Input;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

public partial class Given_UIElement
{
	[TestMethod]
	[RunsOnUIThread]
#if !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#endif
	public async Task When_ManipulationEvents_Then_PositionIsRelative()
	{
		var sut = new Border
		{
			Width = 200,
			Height = 200,
			ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY,
			Background = new SolidColorBrush(Colors.DeepPink)
		};

		Point started = default, delta = default, completed = default;
		sut.ManipulationStarted += (snd, args) => started = args.Position;
		sut.ManipulationDelta += (snd, args) => delta = args.Position;
		sut.ManipulationCompleted += (snd, args) => completed = args.Position;

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var finger = injector.GetFinger();

		var bounds = await UITestHelper.Load(sut);

		finger.Drag(bounds.GetLocation().Offset(10, 10), bounds.GetLocation().Offset(-100, 10), steps: 2);

		started.X.Should().BeInRange(-100 - 1, 10 + 1);
		started.Y.Should().BeApproximately(10, precision: 1);

		delta.X.Should().BeInRange(-100 - 1, 10 + 1);
		delta.Y.Should().BeApproximately(10, precision: 1);

		completed.X.Should().BeApproximately(-100, precision: 1);
		completed.Y.Should().BeApproximately(10, precision: 1);
	}

	[TestMethod]
	[RunsOnUIThread]
#if !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#endif
	public async Task When_PinchOnScaleGrid_InsideScrollViewer_Then_ManipulationDoesNotCompletePrematurely()
	{
		// Two conditions are required to actually reproduce the bug:
		//
		//   1. Content MUST be bigger than the viewport — otherwise ScrollContentPresenter.OnStarting
		//      returns ManipulationModes.None and DirectManipulation.ProcessDown completes the recognizer
		//      immediately (see DirectManipulation.cs), so there's no live DM to misbehave.
		//
		//   2. The pressed element MUST be a child of the Scale grid (not the grid itself) — the bug
		//      is specific to the managed-bubbling case where Grid.OnPointerDown runs with
		//      ctx=OnManagedBubbling during the bubble, and its CancelDirectManipulations() fires
		//      BEFORE ScrollContentPresenter (further up the tree) has registered its DM.
		// Rectangle pinned to the visible portion of the ScrollViewer viewport (top-left of the
		// 600x600 Grid) so that the centre-of-viewport touches used below actually hit it —
		// otherwise the Grid itself becomes the OriginalSource and the bug doesn't reproduce.
		var rectangle = new Rectangle
		{
			Width = 300,
			Height = 300,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Fill = new SolidColorBrush(Colors.Red),
		};

		var grid = new Grid
		{
			Width = 600,
			Height = 600,
			Background = new SolidColorBrush(Colors.Blue),
			ManipulationMode = ManipulationModes.Scale,
			Children = { rectangle },
		};

		var sut = new ScrollViewer
		{
			Width = 300,
			Height = 300,
			Background = new SolidColorBrush(Colors.Black),
			Content = grid,
		};

		var releasing = false;
		var completedDuringGesture = false;
		var deltaCount = 0;
		var maxScaleDeviation = 0f;
		var stagesWithDeltas = 0;
		var stageDeltaCount = 0;

		grid.ManipulationDelta += (_, e) =>
		{
			deltaCount++;
			stageDeltaCount++;
			var deviation = Math.Abs(e.Delta.Scale - 1f);
			if (deviation > maxScaleDeviation)
			{
				maxScaleDeviation = deviation;
			}
		};
		grid.ManipulationCompleted += (_, _) =>
		{
			if (!releasing)
			{
				completedDuringGesture = true;
			}
		};

		var bounds = await UITestHelper.Load(sut);

		// Two independent injectors so each Finger owns its own injection state and can hold a distinct
		// PointerId — the default single-injector Finger API is single-touch only.
		var injector1 = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init injector 1");
		var injector2 = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init injector 2");
		using var finger1 = injector1.GetFinger(id: 101);
		using var finger2 = injector2.GetFinger(id: 102);

		// The Grid is 600x600 but only its top-left 300x300 is visible through the ScrollViewer.
		// Use the ScrollViewer bounds so touches land inside the viewport.
		var center = new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);

		finger1.Press(new Point(center.X - 20, center.Y));
		await TestServices.WindowHelper.WaitForIdle();
		finger2.Press(new Point(center.X + 20, center.Y));
		await TestServices.WindowHelper.WaitForIdle();

		// Fan the fingers outward in stages. A perfectly symmetric spread keeps the midpoint fixed
		// and never trips DM; real touches always drift, so the test deliberately introduces Y
		// drift on both fingers (finger1 down, finger2 down a smaller amount) so that the
		// SCP DM's TranslateY threshold (15 px on touch) is crossed by the second pointer
		// partway through the gesture — exactly the Android repro.
		const int stageCount = 60;

		for (var stage = 1; stage <= stageCount; stage++)
		{
			stageDeltaCount = 0;

			var spread = stage * 25;
			finger1.MoveTo(new Point(center.X - 20 - spread - stage * 3, center.Y + stage * 2), steps: 5);
			await TestServices.WindowHelper.WaitForIdle();
			finger2.MoveTo(new Point(center.X + 20 + spread, center.Y + stage), steps: 5);
			await TestServices.WindowHelper.WaitForIdle();

			if (stageDeltaCount > 0)
			{
				stagesWithDeltas++;
			}
		}

		releasing = true;
		finger2.Release();
		await TestServices.WindowHelper.WaitForIdle();
		finger1.Release();
		await TestServices.WindowHelper.WaitForIdle();

		// Sanity: the gesture did something at all.
		deltaCount.Should().BeGreaterThan(0, "Grid.ManipulationDelta should fire at least at the start of a two-finger pinch");
		maxScaleDeviation.Should().BeGreaterThan(0.1f, "the first few deltas of a widening pinch should report a Scale noticeably different from 1");

		// The actual regression assertions — these are what fail while the bug is live:
		completedDuringGesture.Should().BeFalse("Grid.ManipulationCompleted must not fire while both fingers are still pressed; if it does, ScrollViewer's DirectManipulation has cancelled the pointers mid-gesture");
		stagesWithDeltas.Should().Be(stageCount, $"Grid.ManipulationDelta should keep firing through every stage of the pinch; it stopped firing after {stagesWithDeltas}/{stageCount} stages because the pointers were redirected to ScrollViewer's DirectManipulation");
	}
}
