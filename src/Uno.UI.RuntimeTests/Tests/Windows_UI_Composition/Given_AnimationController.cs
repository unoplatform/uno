using System;
using System.Threading.Tasks;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

[TestClass]
[RunsOnUIThread]
public partial class Given_AnimationController
{
	// LottieGen (WinUIVersion 3.0) creates a single AnimationController and registers every keyframe
	// animation against it via StartAnimation(property, animation, controller), scrubbing them all by
	// setting the controller's Progress. A single-association controller would freeze every animation
	// except the last one registered.
	[TestMethod]
#if !__SKIA__
	[Ignore("AnimationController scrubbing is Skia-only")]
#endif
	public async Task When_Shared_Controller_Scrubs_All_Associated_Animations()
	{
		var border = new Border() { Width = 100, Height = 100 };
		await UITestHelper.Load(border);

		var compositor = ElementCompositionPreview.GetElementVisual(border).Compositor;

		var properties = compositor.CreatePropertySet();
		properties.InsertScalar("A", 0f);
		properties.InsertScalar("B", 0f);

		var linear = compositor.CreateLinearEasingFunction();

		var animationA = compositor.CreateScalarKeyFrameAnimation();
		animationA.Duration = TimeSpan.FromSeconds(1);
		animationA.InsertKeyFrame(0f, 0f, linear);
		animationA.InsertKeyFrame(1f, 10f, linear);

		var animationB = compositor.CreateScalarKeyFrameAnimation();
		animationB.Duration = TimeSpan.FromSeconds(1);
		animationB.InsertKeyFrame(0f, 0f, linear);
		animationB.InsertKeyFrame(1f, 20f, linear);

		var controller = compositor.CreateAnimationController();
		properties.StartAnimation("A", animationA, controller);
		properties.StartAnimation("B", animationB, controller);
		controller.Pause();

		controller.Progress = 0.5f;

		properties.TryGetScalar("A", out var a);
		properties.TryGetScalar("B", out var b);

		// Before the multi-association fix, only "B" (the last registered) would scrub and "A" stayed 0.
		// Both must reach the same progress: A spans 0..10, B spans 0..20, so A == B / 2 at any progress.
		Assert.AreEqual(5f, a, 0.001f);
		Assert.AreEqual(10f, b, 0.001f);
		Assert.AreEqual(b / 2f, a, 0.001f);
	}

	[TestMethod]
#if !__SKIA__
	[Ignore("AnimationController is Skia-only")]
#endif
	public async Task When_PlaybackRate_Is_Set_It_Round_Trips()
	{
		var border = new Border() { Width = 100, Height = 100 };
		await UITestHelper.Load(border);

		var compositor = ElementCompositionPreview.GetElementVisual(border).Compositor;
		var properties = compositor.CreatePropertySet();
		properties.InsertScalar("A", 0f);

		var animation = compositor.CreateScalarKeyFrameAnimation();
		animation.Duration = TimeSpan.FromSeconds(1);
		animation.InsertKeyFrame(1f, 10f);

		var controller = compositor.CreateAnimationController();
		properties.StartAnimation("A", animation, controller);

		Assert.AreEqual(1.0f, controller.PlaybackRate, 0.001f);

		controller.PlaybackRate = 2.0f;
		Assert.AreEqual(2.0f, controller.PlaybackRate, 0.001f);
	}
}
