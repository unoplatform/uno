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
	public void When_Entrance_NavigatingTo()
	{
#if HAS_UNO
		var element = new Border();
		var timelines = CreateSingleStoryboard(new EntranceNavigationTransitionInfo(), element, NavigationTrigger.NavigatingTo);

		Assert.HasCount(2, timelines);
		AssertKeyFrames(timelines[0], "Y",
			Discrete(140, 0),
			Discrete(140, 150),
			Spline(0, 450, SlideInCP1, SlideInCP2));
		AssertKeyFrames(timelines[1], nameof(UIElement.Opacity),
			Discrete(0, 0),
			Discrete(1, 150));
		AssertSame(element, timelines[1]);
#endif
	}

	[TestMethod]
	public void When_Entrance_NavigatingAway()
	{
#if HAS_UNO
		var element = new Border();
		var timelines = CreateSingleStoryboard(new EntranceNavigationTransitionInfo(), element, NavigationTrigger.NavigatingAway);

		Assert.HasCount(1, timelines);
		AssertKeyFrames(timelines[0], nameof(UIElement.Opacity),
			Discrete(1, 0),
			Spline(0, 150, SlideOutCP1, SlideOutCP2));
		Assert.IsNull(element.RenderTransform);
#endif
	}

	[TestMethod]
	public void When_Entrance_BackNavigatingAway()
	{
#if HAS_UNO
		var element = new Border();
		var timelines = CreateSingleStoryboard(new EntranceNavigationTransitionInfo(), element, NavigationTrigger.BackNavigatingAway);

		Assert.HasCount(2, timelines);
		AssertKeyFrames(timelines[0], "Y",
			Discrete(0, 0),
			Spline(140, 150, SlideOutCP1, SlideOutCP2));
		AssertKeyFrames(timelines[1], nameof(UIElement.Opacity),
			Discrete(1, 0),
			Discrete(0, 150));
#endif
	}

	[TestMethod]
	public void When_Entrance_BackNavigatingTo()
	{
#if HAS_UNO
		var element = new Border();
		var timelines = CreateSingleStoryboard(new EntranceNavigationTransitionInfo(), element, NavigationTrigger.BackNavigatingTo);

		Assert.HasCount(1, timelines);
		AssertKeyFrames(timelines[0], nameof(UIElement.Opacity),
			Discrete(0, 0),
			Discrete(0, 150),
			Spline(1, 450, SlideInCP1, SlideInCP2));
#endif
	}

	[TestMethod]
	public void When_Entrance_TargetElement_Outside_Page()
	{
#if HAS_UNO
		// WinUI returns the last live target element even when no target is inside the page.
		var page = new Border();
		var outside = new Border();

		try
		{
			EntranceNavigationTransitionInfo.SetIsTargetElement(outside, true);

			var timelines = CreateSingleStoryboard(new EntranceNavigationTransitionInfo(), page, NavigationTrigger.NavigatingAway);

			AssertSame(outside, timelines[0]);
		}
		finally
		{
			EntranceNavigationTransitionInfo.SetIsTargetElement(outside, false);
		}
#endif
	}

	[TestMethod]
	public void When_Entrance_TargetElement_Is_Page()
	{
#if HAS_UNO
		// An element is not its own ancestor, so the page itself never counts as a target inside the page.
		var page = new Border();
		var outside = new Border();

		try
		{
			EntranceNavigationTransitionInfo.SetIsTargetElement(page, true);
			EntranceNavigationTransitionInfo.SetIsTargetElement(outside, true);

			var timelines = CreateSingleStoryboard(new EntranceNavigationTransitionInfo(), page, NavigationTrigger.NavigatingAway);

			AssertSame(outside, timelines[0]);
		}
		finally
		{
			EntranceNavigationTransitionInfo.SetIsTargetElement(page, false);
			EntranceNavigationTransitionInfo.SetIsTargetElement(outside, false);
		}
#endif
	}

	[TestMethod]
	public async Task When_Entrance_TargetElement_Inside_Page()
	{
#if HAS_UNO
		var inner = new Border { Width = 50, Height = 50 };
		var page = new Border { Width = 100, Height = 100, Child = inner };
		var outside = new Border();
		TestServices.WindowHelper.WindowContent = page;
		await TestServices.WindowHelper.WaitForLoaded(page);

		try
		{
			EntranceNavigationTransitionInfo.SetIsTargetElement(inner, true);
			EntranceNavigationTransitionInfo.SetIsTargetElement(outside, true);

			var timelines = CreateSingleStoryboard(new EntranceNavigationTransitionInfo(), page, NavigationTrigger.NavigatingAway);

			AssertSame(inner, timelines[0]);
		}
		finally
		{
			EntranceNavigationTransitionInfo.SetIsTargetElement(inner, false);
			EntranceNavigationTransitionInfo.SetIsTargetElement(outside, false);
		}
#endif
	}

	[TestMethod]
	[DataRow(0 /* NavigatingAway */, 1.0, 1.04, 100L, 1.0, 0.0, 100L, false)]
	[DataRow(1 /* NavigatingTo */, 0.94, 1.0, 783L, 0.0, 1.0, 333L, false)]
	[DataRow(2 /* BackNavigatingAway */, 1.0, 0.96, 100L, 1.0, 0.0, 100L, false)]
	[DataRow(3 /* BackNavigatingTo */, 1.06, 1.0, 333L, 0.0, 1.0, 333L, true)]
	public void When_DrillIn(int trigger, double scaleFrom, double scaleTo, long scaleDuration, double opacityFrom, double opacityTo, long opacityDuration, bool backScaleCurve)
	{
#if HAS_UNO
		var element = new Border();
		var timelines = CreateSingleStoryboard(new DrillInNavigationTransitionInfo(), element, (NavigationTrigger)trigger);

		var scaleCP1 = backScaleCurve ? new Point(0.12, 0.0) : new Point(0.1, 0.9);
		var scaleCP2 = backScaleCurve ? new Point(0.0, 1.0) : new Point(0.2, 1.0);
		var opacityCP1 = new Point(0.17, 0.17);
		var opacityCP2 = new Point(0.0, 1.0);

		Assert.HasCount(4, timelines);

		AssertSame(element, timelines[0]);
		Assert.AreEqual(nameof(UIElement.RenderTransformOrigin), Storyboard.GetTargetProperty(timelines[0]));

		AssertKeyFrames(timelines[1], nameof(ScaleTransform.ScaleX),
			Spline(scaleFrom, 0, scaleCP1, scaleCP2),
			Spline(scaleTo, scaleDuration, scaleCP1, scaleCP2));
		AssertKeyFrames(timelines[2], nameof(ScaleTransform.ScaleY),
			Spline(scaleFrom, 0, scaleCP1, scaleCP2),
			Spline(scaleTo, scaleDuration, scaleCP1, scaleCP2));
		AssertSame(element, timelines[3]);
		AssertKeyFrames(timelines[3], nameof(UIElement.Opacity),
			Spline(opacityFrom, 0, opacityCP1, opacityCP2),
			Spline(opacityTo, opacityDuration, opacityCP1, opacityCP2));

		// The origin is animated, not set.
		Assert.AreEqual(new Point(0, 0), element.RenderTransformOrigin);
#endif
	}

	[TestMethod]
	public async Task When_DrillIn_TransformOrigin_Restored_On_Stop()
	{
#if HAS_UNO
		var element = new Border { Width = 100, Height = 100 };
		TestServices.WindowHelper.WindowContent = element;
		await TestServices.WindowHelper.WaitForLoaded(element);

		var storyboard = new DrillInNavigationTransitionInfo().CreateStoryboards(element, NavigationTrigger.NavigatingTo).Single();
		storyboard.Begin();
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(new Point(0.5, 0.5), element.RenderTransformOrigin);

		storyboard.Stop();

		Assert.AreEqual(new Point(0, 0), element.RenderTransformOrigin);
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

	[TestMethod]
	public void When_Suppress()
	{
#if HAS_UNO
		// The transition uses an empty storyboard.
		Assert.IsEmpty(new SuppressNavigationTransitionInfo().CreateStoryboards(new Border(), NavigationTrigger.NavigatingTo));
#endif
	}

	[TestMethod]
	public void When_NavigationState()
	{
#if HAS_UNO
		NavigationTransitionInfo[] infos =
		[
			new SlideNavigationTransitionInfo(),
			new EntranceNavigationTransitionInfo(),
			new DrillInNavigationTransitionInfo(),
			new SuppressNavigationTransitionInfo(),
		];

		foreach (var info in infos)
		{
			Assert.AreEqual("0", info.GetNavigationStateCoreInternal(), info.GetType().Name);

			info.SetNavigationStateCoreInternal("42");

			Assert.AreEqual("0", info.GetNavigationStateCoreInternal(), info.GetType().Name);
		}
#endif
	}

	[TestMethod]
	public void When_NavigationState_Base()
	{
#if HAS_UNO
		// The base NavigationTransitionInfo implementation neither stores nor returns a state.
		var info = new CustomTransitionInfo();

		info.SetNavigationStateCoreInternal("42");

		Assert.IsNull(info.GetNavigationStateCoreInternal());
#endif
	}

	private const string RotationPath = "(UIElement.Projection).(PlaneProjection.RotationY)";
	private const string CenterXPath = "(UIElement.Projection).(PlaneProjection.CenterOfRotationX)";
	private const string CenterZPath = "(UIElement.Projection).(PlaneProjection.CenterOfRotationZ)";

	[TestMethod]
	public void When_Common_NavigatingTo()
	{
#if HAS_UNO
		var element = new Border();
		var timelines = CreateSingleStoryboard(new CommonNavigationTransitionInfo(), element, NavigationTrigger.NavigatingTo);

		Assert.HasCount(5, timelines);
		AssertPageTurnstile(element, timelines,
			[Linear(-80, 0), Linear(-80, 128), Easing(0, 556, exponent: 6, EasingMode.EaseOut)],
			[Linear(0, 0), Linear(0, 128), Linear(1, 129)]);
		AssertHitTestDisabled(element, timelines[4]);
#endif
	}

	[TestMethod]
	public void When_Common_NavigatingAway()
	{
#if HAS_UNO
		var element = new Border();
		var timelines = CreateSingleStoryboard(new CommonNavigationTransitionInfo(), element, NavigationTrigger.NavigatingAway);

		Assert.HasCount(5, timelines);
		AssertPageTurnstile(element, timelines,
			[Linear(0, 0), Easing(50, 128, exponent: 6, EasingMode.EaseIn)],
			[Linear(1, 0), Linear(1, 128), Linear(0, 129)]);
		AssertHitTestDisabled(element, timelines[4]);
#endif
	}

	[TestMethod]
	public void When_Common_BackNavigatingTo()
	{
#if HAS_UNO
		var element = new Border();
		var timelines = CreateSingleStoryboard(new CommonNavigationTransitionInfo(), element, NavigationTrigger.BackNavigatingTo);

		Assert.HasCount(5, timelines);
		AssertPageTurnstile(element, timelines,
			[Linear(50, 0), Linear(50, 128), Easing(0, 556, exponent: 6, EasingMode.EaseOut)],
			[Linear(0, 0), Linear(0, 128), Linear(1, 129)]);
		AssertHitTestDisabled(element, timelines[4]);
#endif
	}

	[TestMethod]
	public void When_Common_BackNavigatingAway()
	{
#if HAS_UNO
		var element = new Border();
		var timelines = CreateSingleStoryboard(new CommonNavigationTransitionInfo(), element, NavigationTrigger.BackNavigatingAway);

		Assert.HasCount(5, timelines);
		AssertPageTurnstile(element, timelines,
			[Linear(0, 0), Easing(-80, 128, exponent: 6, EasingMode.EaseIn)],
			[Linear(1, 0), Linear(1, 128), Linear(0, 129)]);
		AssertHitTestDisabled(element, timelines[4]);
#endif
	}

	[TestMethod]
	public void When_Common_Staggering_Without_Targets()
	{
#if HAS_UNO
		// Without a visible stagger target the page turnstiles.
		var element = new Border();
		var info = new CommonNavigationTransitionInfo { IsStaggeringEnabled = true };
		var timelines = CreateSingleStoryboard(info, element, NavigationTrigger.NavigatingTo);

		Assert.HasCount(5, timelines);
		AssertPageTurnstile(element, timelines,
			[Linear(-80, 0), Linear(-80, 128), Easing(0, 556, exponent: 6, EasingMode.EaseOut)],
			[Linear(0, 0), Linear(0, 128), Linear(1, 129)]);
#endif
	}

	[TestMethod]
	public async Task When_Common_Staggering_NavigatingTo()
	{
#if HAS_UNO
		await RunStaggerTest(NavigationTrigger.NavigatingTo, (page, timelines, targets) =>
		{
			AssertStaggerPageOpacity(page, timelines, Linear(0, 0), Linear(0, 128), Linear(1, 129));
			AssertStaggerTarget(targets[0], timelines, 1,
				[Linear(-80, 0), Linear(-80, 64), Easing(0, 278, exponent: 6, EasingMode.EaseOut)],
				[Linear(0, 0), Linear(0, 64), Linear(1, 65)]);
			AssertStaggerTarget(targets[1], timelines, 5,
				[Linear(-80, 0), Linear(-80, 97), Easing(0, 311, exponent: 6, EasingMode.EaseOut)],
				[Linear(0, 0), Linear(0, 97), Linear(1, 98)]);
		});
#endif
	}

	[TestMethod]
	public async Task When_Common_Staggering_NavigatingAway()
	{
#if HAS_UNO
		// WinUI passes the unset page easing here, so the rotation is linear.
		await RunStaggerTest(NavigationTrigger.NavigatingAway, (page, timelines, targets) =>
		{
			AssertStaggerPageOpacity(page, timelines, Linear(1, 0), Linear(1, 128), Linear(0, 129));
			AssertStaggerTarget(targets[0], timelines, 1,
				[Linear(0, 0), Linear(50, 64)],
				[Linear(1, 0), Linear(1, 64), Linear(0, 65)]);
			AssertStaggerTarget(targets[1], timelines, 5,
				[Linear(0, 33), Linear(50, 97)],
				[Linear(1, 33), Linear(1, 97), Linear(0, 98)]);
		});
#endif
	}

	[TestMethod]
	public async Task When_Common_Staggering_BackNavigatingTo()
	{
#if HAS_UNO
		// WinUI passes the unset page easing here, so the rotation is linear.
		await RunStaggerTest(NavigationTrigger.BackNavigatingTo, (page, timelines, targets) =>
		{
			AssertStaggerPageOpacity(page, timelines, Linear(0, 0), Linear(0, 128), Linear(1, 129));
			AssertStaggerTarget(targets[0], timelines, 1,
				[Linear(50, 0), Linear(50, 64), Linear(0, 278)],
				[Linear(0, 0), Linear(0, 64), Linear(1, 65)]);
			AssertStaggerTarget(targets[1], timelines, 5,
				[Linear(50, 0), Linear(50, 97), Linear(0, 311)],
				[Linear(0, 0), Linear(0, 97), Linear(1, 98)]);
		});
#endif
	}

	[TestMethod]
	public async Task When_Common_Staggering_BackNavigatingAway()
	{
#if HAS_UNO
		await RunStaggerTest(NavigationTrigger.BackNavigatingAway, (page, timelines, targets) =>
		{
			AssertStaggerPageOpacity(page, timelines, Linear(1, 0), Linear(1, 128), Linear(0, 129));
			AssertStaggerTarget(targets[0], timelines, 1,
				[Linear(0, 0), Easing(-80, 64, exponent: 6, EasingMode.EaseIn)],
				[Linear(1, 0), Linear(1, 64), Linear(0, 65)]);
			AssertStaggerTarget(targets[1], timelines, 5,
				[Linear(0, 33), Easing(-80, 97, exponent: 6, EasingMode.EaseIn)],
				[Linear(1, 33), Linear(1, 97), Linear(0, 98)]);
		});
#endif
	}

	[TestMethod]
	public async Task When_Common_Turnstile_Applied_To_Projection()
	{
#if HAS_UNO
		var element = new Border { Width = 100, Height = 100 };
		TestServices.WindowHelper.WindowContent = element;
		await TestServices.WindowHelper.WaitForLoaded(element);

		var storyboard = new CommonNavigationTransitionInfo().CreateStoryboards(element, NavigationTrigger.NavigatingAway).Single();
		var completed = false;
		storyboard.Completed += (_, _) => completed = true;
		storyboard.Begin();
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsFalse(element.IsHitTestVisible);

		await TestServices.WindowHelper.WaitFor(() => completed);

		var projection = (PlaneProjection)element.Projection;
		Assert.AreEqual(50, projection.RotationY, 1e-9);
		Assert.AreEqual(-0.1, projection.CenterOfRotationX, 1e-9);
		Assert.AreEqual(-100, projection.CenterOfRotationZ, 1e-9);
		Assert.AreEqual(0, element.Opacity, 1e-9);

		storyboard.Stop();

		Assert.IsTrue(element.IsHitTestVisible);
#endif
	}

	[TestMethod]
	public void When_Common_NavigationState()
	{
#if HAS_UNO
		var info = new CommonNavigationTransitionInfo();

		Assert.AreEqual("0", info.GetNavigationStateCoreInternal());

		info.IsStaggeringEnabled = true;
		Assert.AreEqual("1", info.GetNavigationStateCoreInternal());

		var restored = new CommonNavigationTransitionInfo();
		restored.SetNavigationStateCoreInternal("1");
		Assert.IsTrue(restored.IsStaggeringEnabled);

		restored.SetNavigationStateCoreInternal("42");
		Assert.IsFalse(restored.IsStaggeringEnabled);
		Assert.AreEqual("0", restored.GetNavigationStateCoreInternal());
#endif
	}

	private class CustomTransitionInfo : NavigationTransitionInfo
	{
	}

#if HAS_UNO
	private static async Task RunStaggerTest(NavigationTrigger trigger, Action<UIElement, List<Timeline>, Border[]> assert)
	{
		// Two visible descendants, plus a descendant outside the page bounds and an intersecting non-descendant.
		var first = new Border { Width = 50, Height = 50 };
		var second = new Border { Width = 50, Height = 50 };
		var offPage = new Border { Width = 50, Height = 50 };
		var sibling = new Border { Width = 50, Height = 50, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
		Canvas.SetLeft(second, 60);
		Canvas.SetLeft(offPage, 300);

		var page = new Canvas { Width = 200, Height = 200, Children = { first, second, offPage } };
		var root = new Grid { Width = 400, Height = 200, Children = { page, sibling } };

		TestServices.WindowHelper.WindowContent = root;
		await TestServices.WindowHelper.WaitForLoaded(root);

		Border[] registered = [first, offPage, sibling, second];

		try
		{
			foreach (var element in registered)
			{
				CommonNavigationTransitionInfo.SetIsStaggerElement(element, true);
			}

			var info = new CommonNavigationTransitionInfo { IsStaggeringEnabled = true };
			var timelines = CreateSingleStoryboard(info, page, trigger);

			Assert.HasCount(10, timelines);
			assert(page, timelines, [first, second]);
			AssertHitTestDisabled(page, timelines[9]);

			Assert.IsNull(page.Projection);
			Assert.IsNull(offPage.Projection);
			Assert.IsNull(sibling.Projection);
		}
		finally
		{
			foreach (var element in registered)
			{
				CommonNavigationTransitionInfo.SetIsStaggerElement(element, false);
			}
		}
	}

	private static void AssertPageTurnstile(UIElement element, List<Timeline> timelines, Action<DoubleKeyFrame>[] rotation, Action<DoubleKeyFrame>[] opacity)
	{
		Assert.IsInstanceOfType<PlaneProjection>(element.Projection);

		for (var i = 0; i < 4; i++)
		{
			AssertSame(element, timelines[i]);
		}

		AssertKeyFrames(timelines[0], RotationPath, rotation);
		AssertKeyFrames(timelines[1], CenterXPath, Linear(-0.1, 0));
		AssertKeyFrames(timelines[2], CenterZPath, Linear(-100, 0));
		AssertKeyFrames(timelines[3], nameof(UIElement.Opacity), opacity);
	}

	private static void AssertStaggerPageOpacity(UIElement page, List<Timeline> timelines, params Action<DoubleKeyFrame>[] opacity)
	{
		AssertSame(page, timelines[0]);
		AssertKeyFrames(timelines[0], nameof(UIElement.Opacity), opacity);
	}

	private static void AssertStaggerTarget(UIElement target, List<Timeline> timelines, int index, Action<DoubleKeyFrame>[] rotation, Action<DoubleKeyFrame>[] opacity)
	{
		Assert.IsInstanceOfType<PlaneProjection>(target.Projection);

		for (var i = index; i < index + 4; i++)
		{
			AssertSame(target, timelines[i]);
		}

		AssertKeyFrames(timelines[index], RotationPath, rotation);
		AssertKeyFrames(timelines[index + 1], CenterXPath, Linear(-0.1, 0));
		AssertKeyFrames(timelines[index + 2], CenterZPath, Linear(-100, 0));
		AssertKeyFrames(timelines[index + 3], nameof(UIElement.Opacity), opacity);
	}

	private static Action<DoubleKeyFrame> Linear(double value, long ms) => frame =>
	{
		Assert.IsInstanceOfType<LinearDoubleKeyFrame>(frame);
		AssertFrame(frame, value, ms);
	};
#endif

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
