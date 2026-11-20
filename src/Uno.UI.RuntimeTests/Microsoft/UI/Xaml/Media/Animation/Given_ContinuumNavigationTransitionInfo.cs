#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Private.Infrastructure;
using Windows.Foundation;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media_Animation;

// Expected values come from ThemeTransitions.cpp (winui3/release/2.5.1).
[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
public class Given_ContinuumNavigationTransitionInfo
{
	private const string ScaleX = "(UIElement.RenderTransform).(CompositeTransform.ScaleX)";
	private const string ScaleY = "(UIElement.RenderTransform).(CompositeTransform.ScaleY)";
	private const string TranslateY = "(UIElement.RenderTransform).(CompositeTransform.TranslateY)";
	private const string CenterOfRotationX = "(UIElement.Projection).(PlaneProjection.CenterOfRotationX)";
	private const string CenterOfRotationY = "(UIElement.Projection).(PlaneProjection.CenterOfRotationY)";
	private const string CenterOfRotationZ = "(UIElement.Projection).(PlaneProjection.CenterOfRotationZ)";
	private const string RotationX = "(UIElement.Projection).(PlaneProjection.RotationX)";
	private const string Opacity = nameof(UIElement.Opacity);

	[TestMethod]
	public void When_NavigatingTo_Without_EntranceElement()
	{
#if HAS_UNO
		var page = new Border();
		var timelines = CreateSingleStoryboard(new ContinuumNavigationTransitionInfo(), page, NavigationTrigger.NavigatingTo);

		Assert.HasCount(5, timelines);
		AssertNavigatingToPage(page, timelines);
		AssertTransformOrigin(page, new Point(0.5, 0.5), timelines[3]);
		AssertHitTestDisabled(page, timelines[4]);
		Assert.IsInstanceOfType<CompositeTransform>(page.RenderTransform);
#endif
	}

	[TestMethod]
	public async Task When_NavigatingTo_With_EntranceElement()
	{
#if HAS_UNO
		var (page, first, second) = await LoadPage();
		var outside = new Border();

		try
		{
			ContinuumNavigationTransitionInfo.SetIsEntranceElement(outside, true);
			ContinuumNavigationTransitionInfo.SetIsEntranceElement(first, true);
			ContinuumNavigationTransitionInfo.SetIsEntranceElement(second, true);

			var timelines = CreateSingleStoryboard(new ContinuumNavigationTransitionInfo(), page, NavigationTrigger.NavigatingTo);

			// Only the first entrance element inside the page is animated.
			Assert.HasCount(13, timelines);
			AssertNavigatingToPage(page, timelines);

			var target = timelines.Skip(3).Take(8).ToList();
			Assert.IsTrue(target.All(t => t.Target == first), "Every target timeline animates the first entrance element");

			AssertKeyFrames(target[0], CenterOfRotationX, Linear(0, 267));
			AssertKeyFrames(target[1], CenterOfRotationY, Linear(1, 267));
			AssertKeyFrames(target[2], CenterOfRotationZ, Linear(0, 267));
			AssertKeyFrames(target[3], RotationX,
				Linear(-50, 267),
				Power(0, 617, 2, EasingMode.EaseOut));
			AssertKeyFrames(target[4], TranslateY,
				Linear(200, 267),
				Exponential(-40, 367, 8, EasingMode.EaseOut),
				Exponential(0, 617, 0.75, EasingMode.EaseInOut));
			AssertKeyFrames(target[5], ScaleX,
				Linear(1.5, 267),
				Power(1, 617, 2, EasingMode.EaseOut));
			AssertKeyFrames(target[6], ScaleY,
				Linear(1.5, 267),
				Power(1, 617, 2, EasingMode.EaseOut));
			AssertKeyFrames(target[7], Opacity,
				Linear(0, 0),
				Linear(0, 267),
				Exponential(1, 434, 6, EasingMode.EaseOut));

			AssertTransformOrigin(page, new Point(0.5, 0.5), timelines[11]);
			AssertHitTestDisabled(page, timelines[12]);

			Assert.IsInstanceOfType<PlaneProjection>(first.Projection);
			Assert.IsInstanceOfType<CompositeTransform>(first.RenderTransform);
			Assert.IsNull(second.Projection);
			Assert.IsNull(outside.Projection);
		}
		finally
		{
			ContinuumNavigationTransitionInfo.SetIsEntranceElement(outside, false);
			ContinuumNavigationTransitionInfo.SetIsEntranceElement(first, false);
			ContinuumNavigationTransitionInfo.SetIsEntranceElement(second, false);
		}
#endif
	}

	[TestMethod]
	public void When_NavigatingTo_EntranceElement_Outside_Page()
	{
#if HAS_UNO
		var page = new Border();
		var outside = new Border();

		try
		{
			ContinuumNavigationTransitionInfo.SetIsEntranceElement(outside, true);

			var timelines = CreateSingleStoryboard(new ContinuumNavigationTransitionInfo(), page, NavigationTrigger.NavigatingTo);

			Assert.HasCount(5, timelines);
			Assert.IsNull(outside.Projection);
		}
		finally
		{
			ContinuumNavigationTransitionInfo.SetIsEntranceElement(outside, false);
		}
#endif
	}

	[TestMethod]
	public void When_NavigatingAway_Without_ExitElement()
	{
#if HAS_UNO
		var page = new Border();
		var timelines = CreateSingleStoryboard(new ContinuumNavigationTransitionInfo(), page, NavigationTrigger.NavigatingAway);

		Assert.HasCount(2, timelines);
		AssertNavigatingAwayPage(page, timelines[0]);
		AssertHitTestDisabled(page, timelines[1]);
		Assert.IsNull(page.RenderTransform);
#endif
	}

	[TestMethod]
	public void When_NavigatingAway_With_ExitElement()
	{
#if HAS_UNO
		var page = new Border();
		// WinUI does not check that the exit element is inside the page.
		var exitElement = new Border();
		var info = new ContinuumNavigationTransitionInfo { ExitElement = exitElement };

		var timelines = CreateSingleStoryboard(info, page, NavigationTrigger.NavigatingAway);

		Assert.HasCount(11, timelines);
		AssertNavigatingAwayPage(page, timelines[0]);

		var target = timelines.Skip(1).Take(8).ToList();
		Assert.IsTrue(target.All(t => t.Target == exitElement), "Every target timeline animates the exit element");

		AssertKeyFrames(target[0], CenterOfRotationX, Linear(0, 0));
		AssertKeyFrames(target[1], CenterOfRotationY, Linear(1, 0));
		AssertKeyFrames(target[2], CenterOfRotationZ, Linear(0, 0));
		AssertKeyFrames(target[3], RotationX,
			Linear(0, 0),
			Exponential(80, 120, 2, EasingMode.EaseIn));
		AssertKeyFrames(target[4], TranslateY,
			Linear(0, 0),
			Exponential(200, 120, 6, EasingMode.EaseIn));
		AssertKeyFrames(target[5], ScaleX,
			Linear(1, 0),
			Exponential(1.5, 120, 6, EasingMode.EaseIn));
		AssertKeyFrames(target[6], ScaleY,
			Linear(1, 0),
			Exponential(1.5, 120, 6, EasingMode.EaseIn));
		AssertKeyFrames(target[7], Opacity,
			Linear(1, 0),
			Linear(1, 0),
			Linear(1, 100),
			Linear(0, 120));

		AssertTransformOrigin(exitElement, new Point(0, 1), timelines[9]);
		AssertHitTestDisabled(page, timelines[10]);

		Assert.IsInstanceOfType<PlaneProjection>(exitElement.Projection);
		Assert.IsInstanceOfType<CompositeTransform>(exitElement.RenderTransform);
		Assert.IsNull(page.RenderTransform);
#endif
	}

	[TestMethod]
	public void When_BackNavigatingTo_Without_ExitElement()
	{
#if HAS_UNO
		var page = new Border();
		var timelines = CreateSingleStoryboard(new ContinuumNavigationTransitionInfo(), page, NavigationTrigger.BackNavigatingTo);

		Assert.HasCount(2, timelines);
		AssertBackNavigatingToPage(page, timelines[0]);
		AssertHitTestDisabled(page, timelines[1]);
#endif
	}

	[TestMethod]
	public void When_BackNavigatingTo_With_ExitElement()
	{
#if HAS_UNO
		var page = new Border();
		var exitElement = new Border();
		var info = new ContinuumNavigationTransitionInfo { ExitElement = exitElement };

		var timelines = CreateSingleStoryboard(info, page, NavigationTrigger.BackNavigatingTo);

		Assert.HasCount(8, timelines);
		AssertBackNavigatingToPage(page, timelines[0]);

		var target = timelines.Skip(1).Take(6).ToList();
		Assert.IsTrue(target.All(t => t.Target == exitElement), "Every target timeline animates the exit element");

		AssertKeyFrames(target[0], CenterOfRotationX, Linear(0, 267));
		AssertKeyFrames(target[1], CenterOfRotationY, Linear(0, 267));
		AssertKeyFrames(target[2], CenterOfRotationZ, Linear(0, 267));
		AssertKeyFrames(target[3], RotationX,
			Linear(-90, 267),
			Exponential(0, 467, 4, EasingMode.EaseOut));
		AssertKeyFrames(target[4], TranslateY,
			Linear(-20, 267),
			Exponential(0, 467, 4, EasingMode.EaseOut));
		AssertKeyFrames(target[5], Opacity,
			Linear(0, 0),
			Linear(0, 266),
			Linear(1, 267));

		// No transform origin is set for this trigger.
		AssertHitTestDisabled(page, timelines[7]);
#endif
	}

	[TestMethod]
	public async Task When_BackNavigatingTo_Ignores_EntranceElement()
	{
#if HAS_UNO
		var (page, first, _) = await LoadPage();

		try
		{
			ContinuumNavigationTransitionInfo.SetIsEntranceElement(first, true);

			var timelines = CreateSingleStoryboard(new ContinuumNavigationTransitionInfo(), page, NavigationTrigger.BackNavigatingTo);

			Assert.HasCount(2, timelines);
			Assert.IsNull(first.Projection);
		}
		finally
		{
			ContinuumNavigationTransitionInfo.SetIsEntranceElement(first, false);
		}
#endif
	}

	[TestMethod]
	public void When_BackNavigatingAway()
	{
#if HAS_UNO
		var page = new Border();
		var exitElement = new Border();
		var info = new ContinuumNavigationTransitionInfo { ExitElement = exitElement };

		var timelines = CreateSingleStoryboard(info, page, NavigationTrigger.BackNavigatingAway);

		// Only the page is animated, even with an exit element.
		Assert.HasCount(3, timelines);
		AssertSame(page, timelines[0]);
		AssertKeyFrames(timelines[0], TranslateY,
			Linear(0, 0),
			Exponential(200, 250, 6, EasingMode.EaseIn));
		AssertSame(page, timelines[1]);
		AssertKeyFrames(timelines[1], Opacity,
			Linear(1, 0),
			Linear(1, 0),
			Linear(1, 240),
			Linear(0, 250));
		AssertHitTestDisabled(page, timelines[2]);

		Assert.IsInstanceOfType<CompositeTransform>(page.RenderTransform);
		Assert.IsNull(exitElement.Projection);
#endif
	}

	[TestMethod]
	public async Task When_IsExitElement_Not_Animated()
	{
#if HAS_UNO
		var (page, first, _) = await LoadPage();

		try
		{
			ContinuumNavigationTransitionInfo.SetIsExitElement(first, true);

			foreach (var trigger in new[] { NavigationTrigger.NavigatingAway, NavigationTrigger.BackNavigatingTo, NavigationTrigger.BackNavigatingAway })
			{
				var timelines = CreateSingleStoryboard(new ContinuumNavigationTransitionInfo(), page, trigger);

				Assert.IsFalse(timelines.Any(t => t.Target == first), $"{trigger} animates an IsExitElement element");
			}

			Assert.IsNull(first.Projection);
			Assert.IsNull(first.RenderTransform);
		}
		finally
		{
			ContinuumNavigationTransitionInfo.SetIsExitElement(first, false);
		}
#endif
	}

	[TestMethod]
	public async Task When_ClearEntranceElements_Keeps_Live_Elements()
	{
#if HAS_UNO
		var (page, first, _) = await LoadPage();

		try
		{
			ContinuumNavigationTransitionInfo.SetIsEntranceElement(first, true);

			// Clearing only prunes dead references.
			ContinuumNavigationTransitionInfo.ClearEntranceElements();
			ContinuumNavigationTransitionInfo.ClearExitElements();

			var timelines = CreateSingleStoryboard(new ContinuumNavigationTransitionInfo(), page, NavigationTrigger.NavigatingTo);

			Assert.HasCount(13, timelines);
			AssertSame(first, timelines[3]);
		}
		finally
		{
			ContinuumNavigationTransitionInfo.SetIsEntranceElement(first, false);
		}
#endif
	}

	[TestMethod]
	public async Task When_NavigatingAway_Storyboard_Runs()
	{
#if HAS_UNO
		var exitElement = new Border { Width = 50, Height = 50 };
		var page = new Border { Width = 100, Height = 100, Child = exitElement };
		TestServices.WindowHelper.WindowContent = page;
		await TestServices.WindowHelper.WaitForLoaded(page);

		var info = new ContinuumNavigationTransitionInfo { ExitElement = exitElement };
		var storyboard = info.CreateStoryboards(page, NavigationTrigger.NavigatingAway).Single();

		storyboard.Begin();
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsFalse(page.IsHitTestVisible);

		// The property paths resolve to the projection and the transform installed on the target.
		storyboard.SkipToFill();
		await TestServices.WindowHelper.WaitForIdle();

		var projection = (PlaneProjection)exitElement.Projection;
		var transform = (CompositeTransform)exitElement.RenderTransform;
		Assert.AreEqual(80, projection.RotationX, 1e-6);
		Assert.AreEqual(0, projection.CenterOfRotationX, 1e-6);
		Assert.AreEqual(1, projection.CenterOfRotationY, 1e-6);
		Assert.AreEqual(200, transform.TranslateY, 1e-6);
		Assert.AreEqual(1.5, transform.ScaleX, 1e-6);
		Assert.AreEqual(1.5, transform.ScaleY, 1e-6);
		Assert.AreEqual(0, exitElement.Opacity, 1e-6);
		Assert.AreEqual(new Point(0, 1), exitElement.RenderTransformOrigin);

		storyboard.Stop();

		Assert.IsTrue(page.IsHitTestVisible);
		Assert.AreEqual(0, projection.RotationX, 1e-6);
		Assert.AreEqual(1, exitElement.Opacity, 1e-6);
		Assert.AreEqual(new Point(0, 0), exitElement.RenderTransformOrigin);
#endif
	}

	[TestMethod]
	public void When_NavigationState()
	{
#if HAS_UNO
		var info = new ContinuumNavigationTransitionInfo();

		Assert.AreEqual("0", info.GetNavigationStateCoreInternal());

		info.SetNavigationStateCoreInternal("42");

		Assert.AreEqual("0", info.GetNavigationStateCoreInternal());
#endif
	}

#if HAS_UNO
	private static async Task<(Border page, Border first, Border second)> LoadPage()
	{
		var first = new Border { Width = 20, Height = 20 };
		var second = new Border { Width = 20, Height = 20 };
		var page = new Border
		{
			Width = 100,
			Height = 100,
			Child = new StackPanel { Children = { first, second } },
		};
		TestServices.WindowHelper.WindowContent = page;
		await TestServices.WindowHelper.WaitForLoaded(page);

		return (page, first, second);
	}

	private static void AssertNavigatingToPage(UIElement page, List<Timeline> timelines)
	{
		AssertSame(page, timelines[0]);
		AssertKeyFrames(timelines[0], ScaleX,
			Circle(0.9, 267, EasingMode.EaseOut),
			Circle(1, 617, EasingMode.EaseOut));
		AssertSame(page, timelines[1]);
		AssertKeyFrames(timelines[1], ScaleY,
			Circle(0.9, 267, EasingMode.EaseOut),
			Circle(1, 617, EasingMode.EaseOut));
		AssertSame(page, timelines[2]);
		AssertKeyFrames(timelines[2], Opacity,
			Linear(0, 0),
			Linear(0, 266),
			Linear(1, 267));
	}

	private static void AssertNavigatingAwayPage(UIElement page, Timeline timeline)
	{
		AssertSame(page, timeline);
		AssertKeyFrames(timeline, Opacity,
			Linear(1, 0),
			Linear(1, 0),
			Linear(1, 120),
			Circle(0, 250, EasingMode.EaseOut));
	}

	private static void AssertBackNavigatingToPage(UIElement page, Timeline timeline)
	{
		AssertSame(page, timeline);
		AssertKeyFrames(timeline, Opacity,
			Linear(0, 0),
			Circle(0, 267, EasingMode.EaseOut),
			Circle(1, 617, EasingMode.EaseOut));
	}

	private static List<Timeline> CreateSingleStoryboard(NavigationTransitionInfo info, UIElement element, NavigationTrigger trigger)
	{
		var storyboards = info.CreateStoryboards(element, trigger);
		Assert.HasCount(1, storyboards);
		return storyboards[0].Children.ToList();
	}

	private static void AssertSame(DependencyObject expected, Timeline timeline)
		=> Assert.AreSame(expected, timeline.Target, $"Unexpected target of the {Storyboard.GetTargetProperty(timeline)} timeline.");

	private static void AssertTransformOrigin(UIElement element, Point origin, Timeline timeline)
	{
		var animation = (ObjectAnimationUsingKeyFrames)timeline;
		AssertSame(element, animation);
		Assert.AreEqual(nameof(UIElement.RenderTransformOrigin), Storyboard.GetTargetProperty(animation));
		Assert.HasCount(1, animation.KeyFrames);
		Assert.AreEqual(TimeSpan.Zero, animation.KeyFrames[0].KeyTime.TimeSpan);
		Assert.AreEqual(origin, animation.KeyFrames[0].Value);
	}

	private static void AssertHitTestDisabled(UIElement element, Timeline timeline)
	{
		var animation = (ObjectAnimationUsingKeyFrames)timeline;
		AssertSame(element, animation);
		Assert.AreEqual(nameof(UIElement.IsHitTestVisible), Storyboard.GetTargetProperty(animation));
		Assert.HasCount(1, animation.KeyFrames);
		Assert.IsInstanceOfType<DiscreteObjectKeyFrame>(animation.KeyFrames[0]);
		Assert.AreEqual(TimeSpan.Zero, animation.KeyFrames[0].KeyTime.TimeSpan);
		Assert.IsFalse((bool)animation.KeyFrames[0].Value);
	}

	private static void AssertKeyFrames(Timeline timeline, string targetProperty, params Action<DoubleKeyFrame>[] expected)
	{
		Assert.AreEqual(targetProperty, Storyboard.GetTargetProperty(timeline));

		var animation = (DoubleAnimationUsingKeyFrames)timeline;
		Assert.HasCount(expected.Length, animation.KeyFrames, $"Key frame count of {targetProperty}");

		for (var i = 0; i < expected.Length; i++)
		{
			expected[i](animation.KeyFrames[i]);
		}
	}

	private static Action<DoubleKeyFrame> Linear(double value, long ms) => frame =>
	{
		Assert.IsInstanceOfType<LinearDoubleKeyFrame>(frame);
		AssertFrame(frame, value, ms);
	};

	private static Action<DoubleKeyFrame> Exponential(double value, long ms, double exponent, EasingMode mode) => frame =>
	{
		AssertFrame(frame, value, ms);
		var ease = (ExponentialEase)((EasingDoubleKeyFrame)frame).EasingFunction;
		Assert.AreEqual(exponent, ease.Exponent);
		Assert.AreEqual(mode, ease.EasingMode);
	};

	private static Action<DoubleKeyFrame> Power(double value, long ms, double power, EasingMode mode) => frame =>
	{
		AssertFrame(frame, value, ms);
		var ease = (PowerEase)((EasingDoubleKeyFrame)frame).EasingFunction;
		Assert.AreEqual(power, ease.Power);
		Assert.AreEqual(mode, ease.EasingMode);
	};

	private static Action<DoubleKeyFrame> Circle(double value, long ms, EasingMode mode) => frame =>
	{
		AssertFrame(frame, value, ms);
		var ease = (CircleEase)((EasingDoubleKeyFrame)frame).EasingFunction;
		Assert.AreEqual(mode, ease.EasingMode);
	};

	private static void AssertFrame(DoubleKeyFrame frame, double value, long ms)
	{
		Assert.AreEqual(value, frame.Value, 1e-9, $"Value of the key frame at {ms}ms");
		Assert.AreEqual(TimeSpan.FromMilliseconds(ms), frame.KeyTime.TimeSpan, $"Key time of the {value} key frame");
	}
#endif
}
