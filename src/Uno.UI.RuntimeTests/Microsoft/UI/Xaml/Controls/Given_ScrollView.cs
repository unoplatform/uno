#nullable enable

#if __SKIA__

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.Interactions;
using Microsoft.UI.Private.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.Disposables;
using Uno.UI.DevTools.Input;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using Windows.UI.Input.Preview.Injection;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_ScrollView
{
	[TestCleanup]
	public void Cleanup() => TestServices.WindowHelper.WindowContent = null;

	/// <summary>A precision touchpad reports wheel deltas finer than one 120-unit detent, and each must still scroll.</summary>
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24730")]
	public async Task When_Wheel_Delta_Below_A_Detent_Then_Scrolls()
	{
		var (sut, bounds) = await LoadTallScrollView();

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var mouse = injector.GetMouse();
		mouse.MoveTo(Center(bounds));
		mouse.Wheel(-30);

		await TestServices.WindowHelper.WaitFor(() => sut.VerticalOffset > 0, message: "a sub-detent wheel delta should scroll");
		await UITestHelper.WaitForIdle(waitForCompositionAnimations: true);

		Assert.AreEqual(InteractionTracker.PixelsPerWheelDetent / 4, sut.VerticalOffset, 0.5, "a quarter detent should scroll a quarter of what a detent scrolls");
	}

	/// <summary>
	/// The tracker's position is what the frame recorded in the same tick has to show: raising its change from a
	/// dispatcher continuation instead left the content a hop behind the tracker.
	/// </summary>
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24730")]
	public async Task When_Wheel_Inertia_Then_Recorded_Frame_Shows_Tracker_Position()
	{
		var (sut, bounds) = await LoadTallScrollView();
		var presenter = sut.ScrollPresenter!;
		var content = presenter.Content!;
		var tracker = presenter.InteractionTracker;
		var target = (CompositionTarget)content.Visual.CompositionTarget!;

		var frames = 0;
		var mismatches = 0;
		float? offset = null;
		Action onFrameRendered = () =>
		{
			if (content.Visual.Properties.TryGetVector3("Translation", out var translation) != CompositionGetValueStatus.Succeeded)
			{
				return;
			}

			// Whatever constant the expression adds, it is the same in every frame.
			var current = translation.Y + tracker.Position.Y;
			offset ??= current;
			frames++;
			if (Math.Abs(current - offset.Value) > 0.01f)
			{
				mismatches++;
			}
		};

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var mouse = injector.GetMouse();
		mouse.MoveTo(Center(bounds));

		target.FrameRendered += onFrameRendered;
		try
		{
			mouse.Wheel(-120 * 3, steps: 3);
			await TestServices.WindowHelper.WaitFor(() => sut.VerticalOffset > 0, message: "the wheel should scroll");
			await UITestHelper.WaitForIdle(waitForCompositionAnimations: true);
		}
		finally
		{
			target.FrameRendered -= onFrameRendered;
		}

		Assert.AreEqual(0, mismatches, $"{mismatches} of {frames} recorded frames showed a position the tracker had already left");

		// The inertia lasts a fixed time, so a slow agent records only a few frames of it.
		Assert.IsTrue(frames >= 2, $"expected the wheel inertia to span more than one frame, got {frames}");
	}

	/// <summary>
	/// WinUI scrolls a notch with a sine ease-out keyframe animation, D·sin(π/2·t/T) over T = 250ms
	/// (CInteractionTracker::ScrollToPosition in the lifted compositor).
	/// </summary>
	[TestMethod]
	public async Task When_Wheel_Notch_Then_Follows_The_WinUI_Curve()
	{
		var (sut, bounds) = await LoadTallScrollView();

		var samples = await RecordWheelNotch(sut, bounds);

		Assert.AreEqual(InteractionTracker.PixelsPerWheelDetent, sut.VerticalOffset, 0.1, "a notch should scroll one detent");
		AssertOnWheelCurve(samples, from: 0, distance: InteractionTracker.PixelsPerWheelDetent, durationMs: 250);
	}

	/// <summary>A notch clamped at the end shortens the curve by the share of it that is left (CalculatePositionAnimationDuration).</summary>
	[TestMethod]
	public async Task When_Wheel_Notch_Clamped_At_The_End_Then_Curve_Is_Shortened()
	{
		var (sut, bounds) = await LoadTallScrollView();
		var left = InteractionTracker.PixelsPerWheelDetent / 2;
		var from = sut.ScrollableHeight - left;

		sut.ScrollTo(0, from, new ScrollingScrollOptions(ScrollingAnimationMode.Disabled));
		await TestServices.WindowHelper.WaitFor(() => Math.Abs(sut.VerticalOffset - from) < 0.01, message: "the view should reach the start offset");

		var samples = await RecordWheelNotch(sut, bounds);

		Assert.AreEqual(sut.ScrollableHeight, sut.VerticalOffset, 0.1, "the notch should stop at the end");
		AssertOnWheelCurve(samples, from, distance: left, durationMs: 250 * left / InteractionTracker.PixelsPerWheelDetent);
	}

	/// <summary>A notch arriving mid-motion moves the running target, so no notch is lost (ProcessMousewheelManipulation).</summary>
	[TestMethod]
	public async Task When_Wheel_Notches_In_A_Row_Then_Each_Moves_The_Target()
	{
		var (sut, bounds) = await LoadTallScrollView();

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var mouse = injector.GetMouse();
		mouse.MoveTo(Center(bounds));

		mouse.WheelDown();
		await UITestHelper.WaitForRender(2);
		mouse.WheelDown();
		await TestServices.WindowHelper.WaitFor(() => sut.VerticalOffset > 0, message: "the wheel should scroll");
		await UITestHelper.WaitForIdle(waitForCompositionAnimations: true);

		Assert.AreEqual(2 * InteractionTracker.PixelsPerWheelDetent, sut.VerticalOffset, 0.1);
	}

	/// <summary>A notch against the motion moves the running target back rather than stopping it.</summary>
	[TestMethod]
	public async Task When_Wheel_Notch_Reverses_Mid_Motion_Then_Returns_To_Start()
	{
		var (sut, bounds) = await LoadTallScrollView();
		const double From = 1000;
		sut.ScrollTo(0, From, new ScrollingScrollOptions(ScrollingAnimationMode.Disabled));
		await TestServices.WindowHelper.WaitFor(() => Math.Abs(sut.VerticalOffset - From) < 0.01, message: "the view should reach the start offset");

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var mouse = injector.GetMouse();
		mouse.MoveTo(Center(bounds));

		mouse.WheelDown();
		await TestServices.WindowHelper.WaitFor(() => sut.VerticalOffset > From, message: "the first notch should scroll");
		mouse.WheelUp();
		await UITestHelper.WaitForIdle(waitForCompositionAnimations: true);

		Assert.AreEqual(From, sut.VerticalOffset, 0.1);
	}

	private static async Task<List<(double Ms, float Position)>> RecordWheelNotch(ScrollView sut, Rect bounds)
	{
		var tracker = sut.ScrollPresenter!.InteractionTracker;
		var initialOffset = sut.VerticalOffset;

		// Rendering carries the timestamp the tracker was just advanced to, so each sample sits exactly on the curve.
		var samples = new List<(double Ms, float Position)>();
		EventHandler<object> onRendering = (_, args) => samples.Add((((RenderingEventArgs)args).RenderingTime.TotalMilliseconds, tracker.Position.Y));

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var mouse = injector.GetMouse();
		mouse.MoveTo(Center(bounds));

		CompositionTarget.Rendering += onRendering;
		try
		{
			mouse.WheelDown();
			await TestServices.WindowHelper.WaitFor(() => sut.VerticalOffset > initialOffset, message: "the wheel should scroll");
			await UITestHelper.WaitForIdle(waitForCompositionAnimations: true);
		}
		finally
		{
			CompositionTarget.Rendering -= onRendering;
		}

		return samples;
	}

	// On the curve, every moving frame implies the same start time. Another curve spreads it by tens of ms,
	// and a first-frame jump puts the first frame off the others.
	private static void AssertOnWheelCurve(List<(double Ms, float Position)> samples, double from, double distance, double durationMs)
	{
		var starts = samples
			.Select(sample => (sample.Ms, Fraction: (sample.Position - from) / distance))
			.Where(sample => sample.Fraction > 0.005 && sample.Fraction < 0.99)
			.Select(sample => sample.Ms - durationMs * Math.Asin(sample.Fraction) * 2 / Math.PI)
			.ToList();

		Assert.IsTrue(starts.Count >= 2, $"expected the notch to span several frames, got {starts.Count}");
		Assert.IsTrue(
			starts.Max() - starts.Min() < 3,
			$"frames do not lie on the WinUI curve, implied starts (ms): {string.Join(", ", starts.Select(start => start.ToString("F1")))}");
	}

	/// <summary>A finger pressed and held on coasting content stops it, without having to move first.</summary>
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24730")]
	public async Task When_Finger_Held_On_Coasting_Content_Then_Stops()
	{
		var (sut, bounds) = await LoadTallScrollView();
		var center = Center(bounds);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var finger = injector.GetFinger();

		finger.Press(center);
		finger.MoveTo(new Point(center.X, center.Y - 200), steps: 10, stepOffsetInMilliseconds: 5);
		finger.Release();

		await TestServices.WindowHelper.WaitFor(() => sut.VerticalOffset > 250, message: "the fling should coast past where the finger lifted");

		finger.Press(center);
		try
		{
			await TestServices.WindowHelper.WaitForIdle();
			var held = sut.VerticalOffset;

			await Task.Delay(300);

			Assert.AreEqual(held, sut.VerticalOffset, 1, "the content should not move under a finger held still");
		}
		finally
		{
			finger.Release();
		}
	}

	private static readonly TimeSpan CompletionTimeout = TimeSpan.FromSeconds(5);

	[TestMethod]
	public async Task When_ScrollTo_Default_Options_Then_Animates_And_Completes_Once()
	{
		using var animations = ForceAnimationsEnabled();
		var (scrollView, _) = await LoadScrollView();
		var completions = TrackScrollCompleted(scrollView);
		var verticalOffsets = TrackVerticalOffsets(scrollView);

		var correlationId = scrollView.ScrollTo(0, 500);

		await WaitForCompletion(completions, correlationId);
		await WaitForFrames(5);

		Assert.AreEqual(500, scrollView.VerticalOffset, 0.5);
		Assert.AreEqual(1, completions.Count(id => id == correlationId), "ScrollCompleted must be raised exactly once.");
		Assert.IsTrue(
			verticalOffsets.Any(offset => offset > 1 && offset < 499),
			$"Expected intermediate offsets while animating, got: {string.Join(", ", verticalOffsets)}");
	}

	[TestMethod]
	public async Task When_ScrollBy_Default_Options_Then_Animates_To_Target()
	{
		var (scrollView, _) = await LoadScrollView();
		var completions = TrackScrollCompleted(scrollView);

		var firstId = scrollView.ScrollTo(0, 100, new ScrollingScrollOptions(ScrollingAnimationMode.Disabled));
		await WaitForCompletion(completions, firstId);

		var correlationId = scrollView.ScrollBy(0, 250);
		await WaitForCompletion(completions, correlationId);

		Assert.AreEqual(350, scrollView.VerticalOffset, 0.5);
		Assert.AreEqual(1, completions.Count(id => id == correlationId));
	}

	[TestMethod]
	public async Task When_ScrollTo_Interrupted_By_ScrollTo_Then_Ends_At_Second_Target()
	{
		using var animations = ForceAnimationsEnabled();
		var (scrollView, _) = await LoadScrollView();
		var completions = TrackScrollCompleted(scrollView);

		var firstId = scrollView.ScrollTo(0, 1500);
		await WaitForFrames(3);
		var secondId = scrollView.ScrollTo(0, 300);

		await WaitForCompletion(completions, secondId);
		await WaitForCompletion(completions, firstId);
		await WaitForFrames(5);

		Assert.AreEqual(300, scrollView.VerticalOffset, 0.5);
		Assert.AreEqual(1, completions.Count(id => id == firstId), "Interrupted request must complete exactly once.");
		Assert.AreEqual(1, completions.Count(id => id == secondId), "Second request must complete exactly once.");
	}

	[TestMethod]
	public async Task When_Home_End_Keys_Then_Scrolls_To_Extents()
	{
		var (scrollView, _) = await LoadScrollView();
		var completions = TrackScrollCompleted(scrollView);

		scrollView.Focus(FocusState.Keyboard);
		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.KeyboardHelper.PressKeySequence("$d$_end#$u$_end", scrollView);
		await WaitFor(() => completions.Count == 1);
		Assert.AreEqual(scrollView.ScrollableHeight, scrollView.VerticalOffset, 0.5);

		await TestServices.KeyboardHelper.PressKeySequence("$d$_home#$u$_home", scrollView);
		await WaitFor(() => completions.Count == 2);
		Assert.AreEqual(0, scrollView.VerticalOffset, 0.5);
	}

	private static async Task<(ScrollView ScrollView, FrameworkElement Content)> LoadScrollView()
	{
		var content = new Rectangle
		{
			Width = 200,
			Height = 2000,
			Fill = new SolidColorBrush(Microsoft.UI.Colors.SteelBlue),
		};

		var scrollView = new ScrollView
		{
			Width = 300,
			Height = 200,
			Content = content,
		};

		await UITestHelper.Load(scrollView);
		return (scrollView, content);
	}

	private static List<int> TrackScrollCompleted(ScrollView scrollView)
	{
		var completions = new List<int>();
		scrollView.ScrollCompleted += (_, args) => completions.Add(args.CorrelationId);
		return completions;
	}

	private static List<double> TrackVerticalOffsets(ScrollView scrollView)
	{
		var offsets = new List<double>();
		scrollView.ViewChanged += (_, _) => offsets.Add(scrollView.VerticalOffset);
		return offsets;
	}

	private static Task WaitForCompletion(List<int> completions, int correlationId)
		=> WaitFor(() => completions.Contains(correlationId));

	private static async Task WaitFor(Func<bool> condition)
		=> await TestServices.WindowHelper.WaitFor(condition, timeoutMS: (int)CompletionTimeout.TotalMilliseconds);

	private static Task WaitForFrames(int count) => UITestHelper.WaitForRender(count, (int)CompletionTimeout.TotalMilliseconds);

	// Auto resolves against the OS reduced-motion setting, which CI agents may have on.
	private static IDisposable ForceAnimationsEnabled()
	{
		var previous = ScrollPresenterTestHooks.IsAnimationsEnabledOverride;
		ScrollPresenterTestHooks.IsAnimationsEnabledOverride = true;
		return Disposable.Create(() => ScrollPresenterTestHooks.IsAnimationsEnabledOverride = previous);
	}

	private static async Task<(ScrollView ScrollView, Rect Bounds)> LoadTallScrollView()
	{
		var sut = new ScrollView
		{
			Width = 300,
			Height = 300,
			Content = new Border
			{
				Height = 20000,
				Background = new LinearGradientBrush
				{
					StartPoint = new Point(0, 0),
					EndPoint = new Point(0, 1),
					GradientStops =
					{
						new GradientStop { Color = Colors.Red, Offset = 0 },
						new GradientStop { Color = Colors.Blue, Offset = 1 },
					},
				},
			},
		};

		var bounds = await UITestHelper.Load(sut);
		await TestServices.WindowHelper.WaitForIdle();

		return (sut, bounds);
	}

	private static Point Center(Rect bounds) => new(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
}
#endif
