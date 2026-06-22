using System;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

[TestClass]
[RunsOnUIThread]
public partial class Given_KeyFrameAnimation
{
	// UIElement.StartAnimation used to throw NotSupportedException for anything other than an
	// ExpressionAnimation. TeachingTip.StartExpandToOpen drives Vector3 keyframe animations through
	// that API, so opening a tip threw mid-render once CompositionScopedBatch became implemented —
	// fatal on macOS where the exception escapes the native draw callback.

	[TestMethod]
#if !__SKIA__
	[Ignore("UIElement.StartAnimation is only implemented on Skia")]
#endif
	public async Task When_Element_StartAnimation_With_KeyFrameAnimation()
	{
		var border = new Border()
		{
			Width = 100,
			Height = 100,
		};

		await UITestHelper.Load(border);

		var compositor = ElementCompositionPreview.GetElementVisual(border).Compositor;

		var scaleAnimation = compositor.CreateVector3KeyFrameAnimation();
		scaleAnimation.InsertKeyFrame(1.0f, new Vector3(1.0f, 1.0f, 1.0f));
		scaleAnimation.Target = "Scale";

		try
		{
			// Used to throw NotSupportedException on Skia.
			border.StartAnimation(scaleAnimation);
			border.StopAnimation(scaleAnimation);
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
#if !__SKIA__
	[Ignore("UIElement.StartAnimation is only implemented on Skia")]
#endif
	public async Task When_Element_StartAnimation_With_KeyFrameAnimation_Translation()
	{
		var border = new Border()
		{
			Width = 100,
			Height = 100,
		};

		await UITestHelper.Load(border);

		var compositor = ElementCompositionPreview.GetElementVisual(border).Compositor;

		var translationAnimation = compositor.CreateVector3KeyFrameAnimation();
		translationAnimation.InsertKeyFrame(1.0f, new Vector3(10.0f, 20.0f, 0.0f));
		translationAnimation.Target = "Translation";

		try
		{
			// Exercises the Translation-enabling branch with a keyframe animation.
			border.StartAnimation(translationAnimation);
			border.StopAnimation(translationAnimation);
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
#if !__SKIA__
	[Ignore("UIElement.StartAnimation is only implemented on Skia")]
#endif
	public async Task When_Element_StartAnimation_With_Implicit_Start_KeyFrame()
	{
		// Mirrors TeachingTip's elevation animation: a single keyframe at progress 1.0 and no explicit
		// keyframe at 0, so Start() must read the current Translation as the implicit start value.
		var border = new Border()
		{
			Width = 100,
			Height = 100,
		};

		await UITestHelper.Load(border);

		var compositor = ElementCompositionPreview.GetElementVisual(border).Compositor;

		var elevationAnimation = compositor.CreateVector3KeyFrameAnimation();
		elevationAnimation.InsertExpressionKeyFrame(1.0f, "Vector3(0, 0, 8)");
		elevationAnimation.Target = "Translation";

		try
		{
			border.StartAnimation(elevationAnimation);
			border.StopAnimation(elevationAnimation);
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
#if !__SKIA__
	[Ignore("KeyFrameAnimation evaluation is Skia-only")]
#endif
	public async Task When_KeyFrameAnimation_Has_No_Value_KeyFrames()
	{
		var border = new Border()
		{
			Width = 100,
			Height = 100,
		};

		await UITestHelper.Load(border);

		var visual = ElementCompositionPreview.GetElementVisual(border);

		// Vector3 animations discard expression keyframes, so this leaves no value keyframes —
		// mirroring TeachingTip's elevation animation, whose render tick threw
		// "Sequence contains no elements".
		var animation = visual.Compositor.CreateVector3KeyFrameAnimation();
		animation.InsertExpressionKeyFrame(1.0f, "Vector3(0, 0, 8)");
		animation.Target = "Offset";
		// Non-zero duration so the tick actually reaches the interpolation path (a zero duration
		// short-circuits to the final value before it).
		animation.Duration = TimeSpan.FromSeconds(1);

		visual.StartAnimation("Offset", animation);
		try
		{
#if __SKIA__
			// What the compositor does every render tick; threw before the fix.
			_ = animation.Evaluate();
#endif
		}
		finally
		{
			visual.StopAnimation("Offset");
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
#if !__SKIA__
	[Ignore("KeyFrameAnimation evaluation is Skia-only")]
#endif
	public async Task When_Vector3_Expression_KeyFrame_Is_Evaluated()
	{
		var border = new Border()
		{
			Width = 100,
			Height = 100,
		};

		await UITestHelper.Load(border);

		var visual = ElementCompositionPreview.GetElementVisual(border);

		var animation = visual.Compositor.CreateVector3KeyFrameAnimation();
		animation.SetScalarParameter("W", 4000f);
		animation.SetScalarParameter("H", 1000f);
		// Mirrors TeachingTip's expand keyframe: scalar parameters + Min + Vector3 constructor.
		animation.InsertExpressionKeyFrame(1.0f, "Vector3(Min(0.01, 20.0 / W), Min(0.01, 20.0 / H), 1.0)");
		animation.Target = "Scale";
		animation.Duration = TimeSpan.FromSeconds(1);

		visual.StartAnimation("Scale", animation);
		try
		{
#if __SKIA__
			var value = (Vector3)animation.Evaluate(1.0f);
			Assert.AreEqual(0.005f, value.X, 0.0001f);
			Assert.AreEqual(0.01f, value.Y, 0.0001f);
			Assert.AreEqual(1.0f, value.Z, 0.0001f);
#endif
		}
		finally
		{
			visual.StopAnimation("Scale");
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
#if !__SKIA__
	[Ignore("KeyFrameAnimation evaluation is Skia-only")]
#endif
	public async Task When_Vector4_Expression_KeyFrame_Is_Evaluated()
	{
		var border = new Border()
		{
			Width = 100,
			Height = 100,
		};

		await UITestHelper.Load(border);

		var compositor = ElementCompositionPreview.GetElementVisual(border).Compositor;
		var properties = compositor.CreatePropertySet();
		properties.InsertVector4("Foo", Vector4.Zero);

		var animation = compositor.CreateVector4KeyFrameAnimation();
		animation.SetScalarParameter("A", 2f);
		animation.InsertExpressionKeyFrame(1.0f, "Vector4(A, A + 1, Max(A, 5), 0)");
		animation.Target = "Foo";
		animation.Duration = TimeSpan.FromSeconds(1);

		properties.StartAnimation("Foo", animation);
		try
		{
#if __SKIA__
			var value = (Vector4)animation.Evaluate(1.0f);
			Assert.AreEqual(new Vector4(2, 3, 5, 0), value);
#endif
		}
		finally
		{
			properties.StopAnimation("Foo");
		}
	}

	[TestMethod]
#if !__SKIA__
	[Ignore("KeyFrameAnimation evaluation is Skia-only")]
#endif
	public async Task When_Boolean_Expression_KeyFrame_Is_Evaluated()
	{
		var border = new Border()
		{
			Width = 100,
			Height = 100,
		};

		await UITestHelper.Load(border);

		var compositor = ElementCompositionPreview.GetElementVisual(border).Compositor;
		var properties = compositor.CreatePropertySet();
		properties.InsertBoolean("Foo", false);

		var animation = compositor.CreateBooleanKeyFrameAnimation();
		animation.SetScalarParameter("A", 5f);
		animation.InsertExpressionKeyFrame(1.0f, "A > 3");
		animation.Target = "Foo";
		animation.Duration = TimeSpan.FromSeconds(1);

		properties.StartAnimation("Foo", animation);
		try
		{
#if __SKIA__
			var value = (bool)animation.Evaluate(1.0f);
			Assert.IsTrue(value);
#endif
		}
		finally
		{
			properties.StopAnimation("Foo");
		}
	}

	[TestMethod]
#if !__SKIA__
	[Ignore("KeyFrameAnimation evaluation is Skia-only")]
#endif
	public async Task When_Vector3_Expression_KeyFrame_References_This_Target()
	{
		var border = new Border()
		{
			Width = 100,
			Height = 100,
		};

		await UITestHelper.Load(border);

		var visual = ElementCompositionPreview.GetElementVisual(border);
		ElementCompositionPreview.SetIsTranslationEnabled(border, true);

		var animation = visual.Compositor.CreateVector3KeyFrameAnimation();
		animation.SetScalarParameter("contentElevation", 8f);
		// Exactly TeachingTip's elevation keyframe: 'this.Target' resolves to the animated visual,
		// preserving its current X/Y translation while animating Z to contentElevation.
		animation.InsertExpressionKeyFrame(1.0f, "Vector3(this.Target.Translation.X, this.Target.Translation.Y, contentElevation)");
		animation.Target = "Translation";
		animation.Duration = TimeSpan.FromSeconds(1);

		visual.StartAnimation("Translation", animation);
		try
		{
#if __SKIA__
			var value = (Vector3)animation.Evaluate(1.0f);
			Assert.AreEqual(new Vector3(0, 0, 8), value);
#endif
		}
		finally
		{
			visual.StopAnimation("Translation");
			TestServices.WindowHelper.WindowContent = null;
		}
	}
}
