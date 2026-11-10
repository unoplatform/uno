#if __SKIA__
using System.Collections.Generic;
using Microsoft.UI;
using System.Numerics;
using Microsoft.UI.Composition;
using Uno.UI.Composition.Drawing;
using Windows.Foundation;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

// Native elements can't be blended with Skia content, so each frame they are clipped to the area no XAML drawn
// after them paints. Only what a visual actually paints may be cut out; its bounds over-approximate it.
[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
public class Given_Visual_NativeClip
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26278")]
	public void When_Outline_Only_Border_Over_Native_Host_Then_Only_Ring_Is_Cut()
	{
#if __SKIA__
		var compositor = Compositor.GetSharedCompositor();
		var border = compositor.CreateBorderVisual();
		border.Size = new Vector2(200, 200);
		border.BorderThickness = new Uno.UI.Composition.Thickness(4);
		border.BorderBrush = compositor.CreateColorBrush(Colors.Red);

		using var clip = ComputeNativeClip(border);

		Assert.IsTrue(clip.FillContains(new Vector2(100, 100)), "The inside of an outline-only border was cut out of the native element.");
		Assert.IsFalse(clip.FillContains(new Vector2(2, 100)), "The border ring was not cut out of the native element.");
		Assert.IsFalse(clip.FillContains(new Vector2(198, 198)), "The border ring was not cut out of the native element.");
#endif
	}

	[TestMethod]
	public void When_Filled_Border_Over_Native_Host_Then_Whole_Rect_Is_Cut()
	{
#if __SKIA__
		var compositor = Compositor.GetSharedCompositor();
		var border = compositor.CreateBorderVisual();
		border.Size = new Vector2(200, 200);
		border.BorderThickness = new Uno.UI.Composition.Thickness(4);
		border.BorderBrush = compositor.CreateColorBrush(Colors.Red);
		border.BackgroundBrush = compositor.CreateColorBrush(Colors.Blue);

		using var clip = ComputeNativeClip(border);

		Assert.IsFalse(clip.FillContains(new Vector2(100, 100)));
		Assert.IsFalse(clip.FillContains(new Vector2(2, 100)));
#endif
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26278")]
	public void When_Zero_Thickness_Border_Brush_Over_Native_Host_Then_Nothing_Is_Cut()
	{
#if __SKIA__
		var compositor = Compositor.GetSharedCompositor();
		var border = compositor.CreateBorderVisual();
		border.Size = new Vector2(200, 200);
		border.BorderBrush = compositor.CreateColorBrush(Colors.Red);

		using var clip = ComputeNativeClip(border);

		Assert.IsTrue(clip.FillContains(new Vector2(100, 100)), "A border brush with no thickness paints nothing, but was cut out of the native element.");
		Assert.IsTrue(clip.FillContains(new Vector2(1, 1)), "A border brush with no thickness paints nothing, but was cut out of the native element.");
#endif
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26278")]
	public void When_Rounded_Filled_Border_Over_Native_Host_Then_Corners_Are_Kept()
	{
#if __SKIA__
		var compositor = Compositor.GetSharedCompositor();
		var border = compositor.CreateBorderVisual();
		border.Size = new Vector2(200, 200);
		border.CornerRadius = new Uno.UI.Composition.CornerRadius(50);
		border.BackgroundBrush = compositor.CreateColorBrush(Colors.Blue);

		using var clip = ComputeNativeClip(border);

		Assert.IsFalse(clip.FillContains(new Vector2(100, 100)));
		Assert.IsTrue(clip.FillContains(new Vector2(3, 3)), "The corner outside the rounded background was cut out of the native element.");
#endif
	}

	[TestMethod]
	public void When_Border_Subclass_Paints_Own_Content_Over_Native_Host_Then_Whole_Rect_Is_Cut()
	{
#if __SKIA__
		var compositor = Compositor.GetSharedCompositor();
		var border = new SelfPaintingBorderVisual(compositor);
		border.Size = new Vector2(200, 200);
		border.BorderThickness = new Uno.UI.Composition.Thickness(4);
		border.BorderBrush = compositor.CreateColorBrush(Colors.Red);

		using var clip = ComputeNativeClip(border);

		Assert.IsFalse(clip.FillContains(new Vector2(100, 100)), "Content the subclass paints over its whole area was not cut out of the native element.");
#endif
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26278")]
	public void When_Filled_Ellipse_Over_Native_Host_Then_Bounding_Box_Corners_Are_Kept()
	{
#if __SKIA__
		var compositor = Compositor.GetSharedCompositor();
		var ellipse = compositor.CreateEllipseGeometry();
		ellipse.Center = new Vector2(100, 100);
		ellipse.Radius = new Vector2(100, 100);
		var shape = compositor.CreateSpriteShape(ellipse);
		shape.FillBrush = compositor.CreateColorBrush(Colors.Red);
		var visual = compositor.CreateShapeVisual();
		visual.Size = new Vector2(200, 200);
		visual.Shapes.Add(shape);

		using var clip = ComputeNativeClip(visual);

		Assert.IsFalse(clip.FillContains(new Vector2(100, 100)), "The ellipse was not cut out of the native element.");
		Assert.IsTrue(clip.FillContains(new Vector2(5, 5)), "The bounding-box corner outside the ellipse was cut out of the native element.");
#endif
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26278")]
	public void When_Stroked_Ellipse_With_Transparent_Fill_Over_Native_Host_Then_Only_Stroke_Is_Cut()
	{
#if __SKIA__
		var compositor = Compositor.GetSharedCompositor();
		var ellipse = compositor.CreateEllipseGeometry();
		ellipse.Center = new Vector2(100, 100);
		ellipse.Radius = new Vector2(90, 90);
		var shape = compositor.CreateSpriteShape(ellipse);
		shape.FillBrush = compositor.CreateColorBrush(Colors.Transparent);
		shape.StrokeBrush = compositor.CreateColorBrush(Colors.Red);
		shape.StrokeThickness = 4;
		var visual = compositor.CreateShapeVisual();
		visual.Size = new Vector2(200, 200);
		visual.Shapes.Add(shape);

		using var clip = ComputeNativeClip(visual);

		Assert.IsTrue(clip.FillContains(new Vector2(100, 100)), "The transparent fill was cut out of the native element.");
		Assert.IsFalse(clip.FillContains(new Vector2(100, 10)), "The stroke was not cut out of the native element.");
#endif
	}

#if __SKIA__
	// A 200x200 native host with the overlay drawn after it, as a later sibling.
	private static IGeometry ComputeNativeClip(Visual overlay)
	{
		var compositor = Compositor.GetSharedCompositor();
		var root = compositor.CreateContainerVisual();
		root.Size = new Vector2(200, 200);

		var host = compositor.CreateContainerVisual();
		host.Size = new Vector2(200, 200);
		host.SetAsNativeHostVisual(true);
		root.Children.InsertAtTop(host);
		root.Children.InsertAtTop(overlay);

		using var parentClip = GeometryFactory.Current.CreateRectangleGeometry(new Rect(0, 0, 200, 200));
		var seed = GeometryFactory.Current.CreateRectangleGeometry(default);
		var clip = root.GetNativeViewPathAndZOrder(parentClip, seed, new List<Visual>());
		if (!ReferenceEquals(clip, seed))
		{
			seed.Dispose();
		}

		return clip;
	}

	private sealed class SelfPaintingBorderVisual(Compositor compositor) : BorderVisual(compositor)
	{
		internal override bool CanPaint() => true;
	}
#endif
}
