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
}
