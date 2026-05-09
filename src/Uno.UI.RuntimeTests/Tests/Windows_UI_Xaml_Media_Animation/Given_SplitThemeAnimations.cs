using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media_Animation;

[TestClass]
[RunsOnUIThread]
public class Given_SplitThemeAnimations
{
	[TestMethod]
	public async Task When_SplitOpenThemeAnimation_FaceplateOpacity()
	{
		// SplitOpenThemeAnimation animates the closed (faceplate) target's opacity from 1.0 to 0.5 over s_OpacityChangeDuration.
		var openedTarget = new Border { Width = 100, Height = 100 };
		var closedTarget = new Border { Width = 100, Height = 100, Opacity = 1.0 };

		var anim = new SplitOpenThemeAnimation
		{
			OpenedTarget = openedTarget,
			ClosedTarget = closedTarget,
			OpenedLength = 100,
			OffsetFromCenter = 0,
		};

		var stack = new StackPanel
		{
			Children = { openedTarget, closedTarget },
		};
		await UITestHelper.Load(stack, x => x.IsLoaded);

		var sb = new Storyboard { Children = { anim } };
		sb.Begin();

		// Wait long enough to cover the animation duration.
		await Task.Delay(300);
		await WindowHelper.WaitForIdle();

		// Faceplate should now be at ~0.5 opacity.
		Assert.IsTrue(
			closedTarget.Opacity < 0.6,
			$"Expected ClosedTarget opacity to fade to ~0.5, but was {closedTarget.Opacity:F2}");
	}

	[TestMethod]
	public async Task When_SplitCloseThemeAnimation_FaceplateOpacity()
	{
		// SplitCloseThemeAnimation animates the closed (faceplate) target's opacity from 0.0 back to 1.0 in the last s_OpacityChangeDuration ms.
		var openedTarget = new Border { Width = 100, Height = 100 };
		var closedTarget = new Border { Width = 100, Height = 100, Opacity = 1.0 };

		var anim = new SplitCloseThemeAnimation
		{
			OpenedTarget = openedTarget,
			ClosedTarget = closedTarget,
			OpenedLength = 100,
			OffsetFromCenter = 0,
		};

		var stack = new StackPanel
		{
			Children = { openedTarget, closedTarget },
		};
		await UITestHelper.Load(stack, x => x.IsLoaded);

		var sb = new Storyboard { Children = { anim } };
		sb.Begin();

		// During the first part of the animation the faceplate opacity is held at 0.
		await Task.Delay(40);
		await WindowHelper.WaitForIdle();
		Assert.IsTrue(
			closedTarget.Opacity < 0.5,
			$"Expected ClosedTarget opacity to be near 0 during the close animation's leading hold, but was {closedTarget.Opacity:F2}");

		// And by the end it should be back to fully visible.
		await Task.Delay(300);
		await WindowHelper.WaitForIdle();
		Assert.IsTrue(
			closedTarget.Opacity > 0.9,
			$"Expected ClosedTarget opacity to recover to ~1.0 by the end of the close animation, but was {closedTarget.Opacity:F2}");
	}

	[TestMethod]
	public async Task When_SplitCloseThemeAnimation_BackgroundFadesOut()
	{
		// SplitCloseThemeAnimation animates the opened (background) target's opacity from 1.0 to 0.0 in the last s_OpacityChangeDuration ms.
		var openedTarget = new Border { Width = 100, Height = 100, Opacity = 1.0 };

		var anim = new SplitCloseThemeAnimation
		{
			OpenedTarget = openedTarget,
			OpenedLength = 100,
			OffsetFromCenter = 0,
		};

		var stack = new StackPanel
		{
			Children = { openedTarget },
		};
		await UITestHelper.Load(stack, x => x.IsLoaded);

		var sb = new Storyboard { Children = { anim } };
		sb.Begin();

		// Wait until the close animation has finished.
		await Task.Delay(300);
		await WindowHelper.WaitForIdle();

		Assert.IsTrue(
			openedTarget.Opacity < 0.1,
			$"Expected OpenedTarget opacity to fade to near 0 by the end of the close animation, but was {openedTarget.Opacity:F2}");
	}

	[TestMethod]
	public async Task When_TransitionTarget_Opacity_Animates_Element_Opacity()
	{
		// Sanity check: animating (UIElement.TransitionTarget).Opacity flows through to UIElement.Opacity,
		// which is what the SplitOpen/Close animations rely on.
		var target = new Border { Width = 100, Height = 100, Opacity = 1.0 };

		// Faulting in the TransitionTarget instance up front so the animation path resolves.
		_ = target.TransitionTarget;

		var anim = new DoubleAnimation
		{
			To = 0.25,
			Duration = new Duration(TimeSpan.FromMilliseconds(50)),
			EnableDependentAnimation = true,
		};
		Storyboard.SetTarget(anim, target);
		Storyboard.SetTargetProperty(anim, "(UIElement.TransitionTarget).Opacity");

		var sb = new Storyboard { Children = { anim } };
		await UITestHelper.Load(target, x => x.IsLoaded);
		sb.Begin();

		await Task.Delay(150);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(0.25, target.Opacity, 0.05, "Expected Opacity to be animated through the TransitionTarget proxy.");
	}
}
