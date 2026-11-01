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

// Expected values come from ThemeTransitions.cpp / NavigateTransitionHelper.h (winui3/release/2.5.1).
[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
public class Given_NavigationTransitionInfo
{
	private static readonly Point SlideInCP1 = new(0.1, 0.9);
	private static readonly Point SlideInCP2 = new(0.2, 1.0);
	private static readonly Point SlideOutCP1 = new(0.7, 0.0);
	private static readonly Point SlideOutCP2 = new(1.0, 0.5);

	[TestMethod]
	public void When_Slide_FromBottom_NavigatingTo()
	{
#if HAS_UNO
		var element = new Border();
		var timelines = CreateSingleStoryboard(new SlideNavigationTransitionInfo(), element, NavigationTrigger.NavigatingTo);

		Assert.HasCount(3, timelines);
		AssertKeyFrames(timelines[0], "Y",
			Discrete(200, 0),
			Discrete(200, 250),
			Easing(0, 600, exponent: 6, EasingMode.EaseOut));
		AssertTarget<TranslateTransform>(timelines[0]);
		AssertSame(element, timelines[1]);
		AssertKeyFrames(timelines[1], nameof(UIElement.Opacity),
			Discrete(0, 0),
			Discrete(0, 240),
			Discrete(1, 250));
		AssertHitTestDisabled(element, timelines[2]);
#endif
	}

	[TestMethod]
	public void When_Slide_FromBottom_NavigatingAway()
	{
#if HAS_UNO
		var element = new Border();
		var timelines = CreateSingleStoryboard(new SlideNavigationTransitionInfo(), element, NavigationTrigger.NavigatingAway);

		Assert.HasCount(2, timelines);
		AssertSame(element, timelines[0]);
		AssertKeyFrames(timelines[0], nameof(UIElement.Opacity),
			Discrete(1, 0),
			Discrete(1, 240),
			Discrete(0, 250));
		AssertHitTestDisabled(element, timelines[1]);
#endif
	}

	[TestMethod]
	public void When_Slide_FromBottom_BackNavigatingTo()
	{
#if HAS_UNO
		var element = new Border();
		var timelines = CreateSingleStoryboard(new SlideNavigationTransitionInfo(), element, NavigationTrigger.BackNavigatingTo);

		Assert.HasCount(2, timelines);
		AssertKeyFrames(timelines[0], nameof(UIElement.Opacity),
			Discrete(0, 0),
			Discrete(0, 240),
			Discrete(1, 250));
		AssertHitTestDisabled(element, timelines[1]);
#endif
	}

	[TestMethod]
	public void When_Slide_FromBottom_BackNavigatingAway()
	{
#if HAS_UNO
		var element = new Border();
		var timelines = CreateSingleStoryboard(new SlideNavigationTransitionInfo(), element, NavigationTrigger.BackNavigatingAway);

		Assert.HasCount(3, timelines);
		AssertKeyFrames(timelines[0], "Y",
			Discrete(0, 0),
			Easing(200, 600, exponent: 6, EasingMode.EaseIn));
		AssertKeyFrames(timelines[1], nameof(UIElement.Opacity),
			Discrete(1, 0),
			Discrete(1, 240),
			Discrete(0, 250));
		AssertHitTestDisabled(element, timelines[2]);
#endif
	}

	[TestMethod]
	public void When_Slide_FromLeft_NavigatingTo()
	{
#if HAS_UNO
		var element = new Border();
		var info = new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromLeft };
		var timelines = CreateSingleStoryboard(info, element, NavigationTrigger.NavigatingTo);

		Assert.HasCount(3, timelines);
		AssertKeyFrames(timelines[0], "X",
			Discrete(-200, 150),
			Spline(0, 450, SlideInCP1, SlideInCP2));
		AssertKeyFrames(timelines[1], nameof(UIElement.Opacity),
			Discrete(0, 0),
			Discrete(1, 150));
		AssertHitTestDisabled(element, timelines[2]);
#endif
	}

	[TestMethod]
	public void When_Slide_FromRight_NavigatingAway()
	{
#if HAS_UNO
		var element = new Border();
		var info = new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight };
		var timelines = CreateSingleStoryboard(info, element, NavigationTrigger.NavigatingAway);

		Assert.HasCount(3, timelines);
		AssertKeyFrames(timelines[0], "X",
			Discrete(0, 0),
			Spline(-150, 150, SlideOutCP1, SlideOutCP2));
		AssertKeyFrames(timelines[1], nameof(UIElement.Opacity),
			Discrete(1, 0),
			Discrete(0, 150));
		AssertHitTestDisabled(element, timelines[2]);
#endif
	}

	[TestMethod]
	public async Task When_Slide_HitTest_Disabled_While_Running()
	{
#if HAS_UNO
		var element = new Border { Width = 100, Height = 100 };
		TestServices.WindowHelper.WindowContent = element;
		await TestServices.WindowHelper.WaitForLoaded(element);

		var storyboard = new SlideNavigationTransitionInfo().CreateStoryboards(element, NavigationTrigger.NavigatingTo).Single();
		storyboard.Begin();
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsFalse(element.IsHitTestVisible);

		storyboard.Stop();

		Assert.IsTrue(element.IsHitTestVisible);
#endif
	}

#if HAS_UNO
	private static List<Timeline> CreateSingleStoryboard(NavigationTransitionInfo info, UIElement element, NavigationTrigger trigger)
	{
		var storyboards = info.CreateStoryboards(element, trigger);
		Assert.HasCount(1, storyboards);
		return storyboards[0].Children.ToList();
	}

	private static void AssertSame(DependencyObject expected, Timeline timeline)
		=> Assert.AreSame(expected, timeline.Target, $"Unexpected target of the {Storyboard.GetTargetProperty(timeline)} timeline.");

	private static void AssertTarget<T>(Timeline timeline)
		=> Assert.IsInstanceOfType<T>(timeline.Target);

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

	private static Action<DoubleKeyFrame> Discrete(double value, long ms) => frame =>
	{
		Assert.IsInstanceOfType<DiscreteDoubleKeyFrame>(frame);
		AssertFrame(frame, value, ms);
	};

	private static Action<DoubleKeyFrame> Easing(double value, long ms, double exponent, EasingMode mode) => frame =>
	{
		var easing = (EasingDoubleKeyFrame)frame;
		AssertFrame(frame, value, ms);
		var ease = (ExponentialEase)easing.EasingFunction;
		Assert.AreEqual(exponent, ease.Exponent);
		Assert.AreEqual(mode, ease.EasingMode);
	};

	private static Action<DoubleKeyFrame> Spline(double value, long ms, Point cp1, Point cp2) => frame =>
	{
		var spline = (SplineDoubleKeyFrame)frame;
		AssertFrame(frame, value, ms);
		Assert.AreEqual(cp1.X, spline.KeySpline.ControlPoint1.X, 1e-6);
		Assert.AreEqual(cp1.Y, spline.KeySpline.ControlPoint1.Y, 1e-6);
		Assert.AreEqual(cp2.X, spline.KeySpline.ControlPoint2.X, 1e-6);
		Assert.AreEqual(cp2.Y, spline.KeySpline.ControlPoint2.Y, 1e-6);
	};

	private static void AssertFrame(DoubleKeyFrame frame, double value, long ms)
	{
		Assert.AreEqual(value, frame.Value, 1e-9, $"Value of the key frame at {ms}ms");
		Assert.AreEqual(TimeSpan.FromMilliseconds(ms), frame.KeyTime.TimeSpan, $"Key time of the {value} key frame");
	}
#endif
}
