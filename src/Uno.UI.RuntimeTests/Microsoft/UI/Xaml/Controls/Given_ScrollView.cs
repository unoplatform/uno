#nullable enable

#if __SKIA__

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.Interactions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.DevTools.Input;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using Windows.UI.Input.Preview.Injection;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_ScrollView
{
	/// <summary>A precision touchpad reports wheel deltas finer than one 120-unit detent, and each must still scroll.</summary>
	[TestMethod]
	public async Task When_Wheel_Delta_Below_A_Detent_Then_Scrolls()
	{
		var (sut, bounds) = await LoadTallScrollView();

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var mouse = injector.GetMouse();
		mouse.MoveTo(Center(bounds));
		mouse.Wheel(-30);

		await TestServices.WindowHelper.WaitFor(() => sut.VerticalOffset > 0, message: "a sub-detent wheel delta should scroll");
		await UITestHelper.WaitForIdle(waitForCompositionAnimations: true);

		Assert.AreEqual(12, sut.VerticalOffset, 0.5, "a quarter detent should scroll a quarter of the 48px a detent scrolls");
	}

	/// <summary>
	/// The tracker's position is what the frame recorded in the same tick has to show: raising its change from a
	/// dispatcher continuation instead left the content a hop behind the tracker.
	/// </summary>
	[TestMethod]
	public async Task When_Wheel_Inertia_Then_Recorded_Frame_Shows_Tracker_Position()
	{
		var (sut, bounds) = await LoadTallScrollView();
		var presenter = sut.ScrollPresenter!;
		var content = presenter.Content!;
		var tracker = GetTracker(presenter);
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

		Assert.IsTrue(frames >= 5, $"expected the wheel inertia to span several frames, got {frames}");
		Assert.AreEqual(0, mismatches, $"{mismatches} of {frames} recorded frames showed a position the tracker had already left");
	}

	/// <summary>A finger pressed and held on coasting content stops it, without having to move first.</summary>
	[TestMethod]
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

	private static async Task WaitForFrames(int count)
	{
		for (var i = 0; i < count; i++)
		{
			await TestServices.WindowHelper.WaitForIdle();
			await Task.Delay(16);
		}
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

	private static InteractionTracker GetTracker(ScrollPresenter presenter)
		=> (InteractionTracker)typeof(ScrollPresenter)
			.GetField("m_interactionTracker", BindingFlags.Instance | BindingFlags.NonPublic)!
			.GetValue(presenter)!;
}
#endif
