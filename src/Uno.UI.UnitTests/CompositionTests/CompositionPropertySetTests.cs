using System;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.Interactions;
using Microsoft.UI.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using Uno.UI.Composition;

namespace Uno.UI.Tests.CompositionTests;

[TestClass]
public class CompositionPropertySetTests
{
	[TestMethod]
	public void When_Referenced_PropertySet_Changes_Expression_Is_Reevaluated()
	{
		var compositor = Compositor.GetSharedCompositor();
		var source = compositor.CreatePropertySet();
		source.InsertScalar("Progress", 0.25f);

		var target = compositor.CreateSpriteVisual();
		target.Opacity = 0.0f;

		var animation = compositor.CreateExpressionAnimation("source.Progress");
		animation.SetReferenceParameter("source", source);
		target.StartAnimation(nameof(Visual.Opacity), animation);

		Assert.AreEqual(0.25f, target.Opacity);

		source.InsertScalar("Progress", 0.75f);

		Assert.AreEqual(0.75f, target.Opacity);
	}

	[TestMethod]
	public void When_Visual_PropertySet_Changes_Visual_Reference_Expression_Is_Reevaluated()
	{
		var compositor = Compositor.GetSharedCompositor();
		var source = compositor.CreateSpriteVisual();
		source.Properties.InsertScalar("Progress", 0.2f);

		var target = compositor.CreateSpriteVisual();
		target.Opacity = 0.0f;

		var animation = compositor.CreateExpressionAnimation("source.Progress");
		animation.SetReferenceParameter("source", source);
		target.StartAnimation(nameof(Visual.Opacity), animation);

		Assert.AreEqual(0.2f, target.Opacity);

		source.Properties.InsertScalar("Progress", 0.9f);

		Assert.AreEqual(0.9f, target.Opacity);
	}

	[TestMethod]
	public void When_AnimationController_Progress_Changes_Expression_Sees_New_Value()
	{
		var compositor = Compositor.GetSharedCompositor();
		var source = compositor.CreateSpriteVisual();
		var animation = compositor.CreateScalarKeyFrameAnimation();
		animation.Duration = TimeSpan.FromSeconds(1);
		animation.InsertKeyFrame(0, 0);
		animation.InsertKeyFrame(1, 1);
		source.StartAnimation(nameof(Visual.Opacity), animation);

		var controller = source.TryGetAnimationController(nameof(Visual.Opacity));
		Assert.IsNotNull(controller);
		controller.Pause();

		var target = compositor.CreateSpriteVisual();
		var expression = compositor.CreateExpressionAnimation("controller.Progress");
		expression.SetReferenceParameter("controller", controller);
		target.StartAnimation(nameof(Visual.Opacity), expression);

		controller.Progress = 0.75f;

		Assert.AreEqual(0.75f, target.Opacity);
	}

	[TestMethod]
	public void When_AnimationController_Is_Paused_Compositor_Is_Not_Animating()
	{
		var compositor = Compositor.GetSharedCompositor();
		var target = compositor.CreateSpriteVisual();
		target.CompositionTarget = new TestCompositionTarget();
		var animation = compositor.CreateScalarKeyFrameAnimation();
		animation.InsertKeyFrame(1.0f, 1.0f);
		animation.Duration = TimeSpan.FromSeconds(1);

		target.StartAnimation(nameof(Visual.Opacity), animation);
		var controller = target.TryGetAnimationController(nameof(Visual.Opacity));

		Assert.IsNotNull(controller);
		Assert.IsTrue(compositor.IsAnimating);

		controller.Pause();
		Assert.IsFalse(compositor.IsAnimating);

		controller.Resume();
		Assert.IsTrue(compositor.IsAnimating);

		target.StopAnimation(nameof(Visual.Opacity));
		Assert.IsFalse(compositor.IsAnimating);
	}

	private sealed class TestCompositionTarget : ICompositionTarget
	{
		public double RasterizationScale => 1;

		public event EventHandler RasterizationScaleChanged
		{
			add { }
			remove { }
		}

		public void AddDamage(SKRect bounds)
		{
		}

		public void AddDamage(SKPath region)
		{
		}

		public void RequestNewFrame()
		{
		}

		public void TryRedirectForManipulation(PointerPoint pointerPoint, InteractionTracker tracker)
		{
		}
	}
}
