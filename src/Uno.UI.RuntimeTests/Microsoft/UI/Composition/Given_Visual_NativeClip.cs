using System.Threading.Tasks;

#if __SKIA__
using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Composition.Drawing;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using Windows.UI;
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
	[DataRow(false)]
	[DataRow(true)]
	public void When_Outline_Only_Border_Over_Native_Host_Then_Only_Ring_Is_Cut(bool transparentBackground)
	{
#if __SKIA__
		var compositor = Compositor.GetSharedCompositor();
		var border = CreateOutline(compositor);
		if (transparentBackground)
		{
			border.BackgroundBrush = compositor.CreateColorBrush(Colors.Transparent);
		}

		using var clip = ComputeNativeClip(CreateHostRoot(border));

		Assert.IsTrue(clip.FillContains(new Vector2(100, 100)), "The inside of an outline-only border was cut out of the native element.");
		Assert.IsFalse(clip.FillContains(new Vector2(2, 100)), "The border ring was not cut out of the native element.");
		Assert.IsFalse(clip.FillContains(new Vector2(198, 198)), "The border ring was not cut out of the native element.");
#endif
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26278")]
	public void When_Rounded_Outline_Over_Native_Host_Then_Only_Ring_Is_Cut()
	{
#if __SKIA__
		var compositor = Compositor.GetSharedCompositor();
		var border = CreateOutline(compositor);
		border.CornerRadius = new Uno.UI.Composition.CornerRadius(50);

		using var clip = ComputeNativeClip(CreateHostRoot(border));

		Assert.IsTrue(clip.FillContains(new Vector2(100, 100)), "The inside of a rounded outline was cut out of the native element.");
		Assert.IsTrue(clip.FillContains(new Vector2(3, 3)), "The corner outside a rounded outline was cut out of the native element.");
		Assert.IsFalse(clip.FillContains(new Vector2(2, 100)), "The border ring was not cut out of the native element.");
#endif
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26278")]
	public void When_Outline_Is_Offset_Over_Native_Host_Then_Ring_Is_Cut_Where_Drawn()
	{
#if __SKIA__
		var compositor = Compositor.GetSharedCompositor();
		var border = CreateOutline(compositor, size: 100);
		border.Offset = new Vector3(50, 50, 0);

		using var clip = ComputeNativeClip(CreateHostRoot(border));

		Assert.IsTrue(clip.FillContains(new Vector2(100, 100)), "The inside of the offset outline was cut out of the native element.");
		Assert.IsTrue(clip.FillContains(new Vector2(10, 10)), "Area outside the offset outline was cut out of the native element.");
		Assert.IsFalse(clip.FillContains(new Vector2(52, 100)), "The offset ring was not cut out of the native element.");
#endif
	}

	[TestMethod]
	public void When_Hairline_Outline_At_Fractional_Offset_Then_Antialiased_Pixels_Are_Cut()
	{
#if __SKIA__
		var compositor = Compositor.GetSharedCompositor();
		var border = CreateOutline(compositor, size: 100);
		border.BorderThickness = new Uno.UI.Composition.Thickness(1);
		border.Offset = new Vector3(50.5f, 50.5f, 0);

		using var clip = ComputeNativeClip(CreateHostRoot(border));

		// The ring spans 50.5..51.5 and antialiasing reaches past its inner edge. (Past its outer edge, the cut stops at
		// the visual's own clip, as it did when the whole bounds were cut.)
		Assert.IsFalse(clip.FillContains(new Vector2(52, 100)), "A pixel the ring partly paints was left to the native element.");
		Assert.IsTrue(clip.FillContains(new Vector2(100, 100)));
#endif
	}

	[TestMethod]
	public void When_Outline_Is_Drawn_Before_Native_Host_Then_Nothing_Is_Cut()
	{
#if __SKIA__
		var compositor = Compositor.GetSharedCompositor();
		var border = CreateOutline(compositor);
		border.BackgroundBrush = compositor.CreateColorBrush(Colors.Blue);
		var root = compositor.CreateContainerVisual();
		root.Size = new Vector2(200, 200);
		root.Children.InsertAtTop(border);
		root.Children.InsertAtTop(CreateHost(compositor));

		using var clip = ComputeNativeClip(root);

		Assert.IsTrue(clip.FillContains(new Vector2(100, 100)));
		Assert.IsTrue(clip.FillContains(new Vector2(2, 100)));
#endif
	}

	[TestMethod]
	public void When_Outline_Is_Queried_Twice_Then_Ring_Is_Still_Cut()
	{
#if __SKIA__
		var compositor = Compositor.GetSharedCompositor();
		var border = CreateOutline(compositor);
		var root = CreateHostRoot(border);

		ComputeNativeClip(root).Dispose();
		using var clip = ComputeNativeClip(root);

		// The ring is the path the border paints with: lending it to the clip must not release it.
		Assert.IsFalse(clip.FillContains(new Vector2(2, 100)));
		Assert.IsTrue(border.HitTest(new Point(2, 100)));
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

		using var clip = ComputeNativeClip(CreateHostRoot(border));

		Assert.IsTrue(clip.FillContains(new Vector2(100, 100)), "A border brush with no thickness paints nothing, but was cut out of the native element.");
		Assert.IsTrue(clip.FillContains(new Vector2(1, 1)), "A border brush with no thickness paints nothing, but was cut out of the native element.");
#endif
	}

	[TestMethod]
	public void When_Filled_Border_Over_Native_Host_Then_Whole_Rect_Is_Cut()
	{
#if __SKIA__
		var compositor = Compositor.GetSharedCompositor();
		var border = CreateOutline(compositor);
		border.BackgroundBrush = compositor.CreateColorBrush(Colors.Blue);

		using var clip = ComputeNativeClip(CreateHostRoot(border));

		Assert.IsFalse(clip.FillContains(new Vector2(100, 100)));
		Assert.IsFalse(clip.FillContains(new Vector2(2, 100)));
#endif
	}

	[TestMethod]
	public void When_Outline_Background_Fades_Out_Over_Native_Host_Then_Whole_Rect_Is_Cut()
	{
#if __SKIA__
		var compositor = Compositor.GetSharedCompositor();
		var border = CreateOutline(compositor);
		border.BackgroundBrush = compositor.CreateColorBrush(Colors.Transparent);
		compositor.RegisterBackgroundTransition(border, Colors.Blue, Colors.Transparent, TimeSpan.FromSeconds(10));
		try
		{
			using var clip = ComputeNativeClip(CreateHostRoot(border));

			Assert.IsFalse(clip.FillContains(new Vector2(100, 100)), "The background still fading out was not cut out of the native element.");
		}
		finally
		{
			compositor.DeactivateBackgroundTransition(border);
		}
#endif
	}

	[TestMethod]
	public void When_Outline_Casts_Shadow_Over_Native_Host_Then_Whole_Rect_Is_Cut()
	{
#if __SKIA__
		var compositor = Compositor.GetSharedCompositor();
		var border = CreateOutline(compositor);
		border.ShadowState = new Uno.UI.Composition.Composition.ShadowState(0, 4, 4, 4, Colors.Black);

		using var clip = ComputeNativeClip(CreateHostRoot(border));

		Assert.IsFalse(clip.FillContains(new Vector2(100, 100)), "The shadow inside the ring was not cut out of the native element.");
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

		using var clip = ComputeNativeClip(CreateHostRoot(border));

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

		using var clip = ComputeNativeClip(CreateHostRoot(border));

		Assert.IsFalse(clip.FillContains(new Vector2(100, 100)), "Content the subclass paints over its whole area was not cut out of the native element.");
#endif
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26278")]
	public async Task When_Xaml_Outline_Border_Over_Native_Host_Then_Only_Ring_Is_Cut()
	{
#if __SKIA__
		var host = new Border { Width = 200, Height = 200 };
		var outline = new Border
		{
			Width = 200,
			Height = 200,
			BorderBrush = new SolidColorBrush(Colors.Red),
			BorderThickness = new Microsoft.UI.Xaml.Thickness(4),
			IsHitTestVisible = false,
		};
		var grid = new Grid { Width = 200, Height = 200 };
		grid.Children.Add(host);
		grid.Children.Add(outline);
		await UITestHelper.Load(grid);

		host.Visual.SetAsNativeHostVisual(true);
		try
		{
			var root = grid.Visual;
			using var clip = ComputeNativeClip(root, new Rect(-10000, -10000, 20000, 20000));

			Assert.IsTrue(clip.FillContains(Vector2.Transform(new Vector2(100, 100), root.TotalMatrix)), "The inside of an outline-only XAML Border was cut out of the native element.");
			Assert.IsFalse(clip.FillContains(Vector2.Transform(new Vector2(2, 100), root.TotalMatrix)), "The XAML Border's ring was not cut out of the native element.");
		}
		finally
		{
			host.Visual.SetAsNativeHostVisual(null);
		}
#else
		await Task.CompletedTask;
#endif
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26278")]
	public void When_Filled_Ellipse_Over_Native_Host_Then_Bounding_Box_Corners_Are_Kept()
	{
#if __SKIA__
		var compositor = Compositor.GetSharedCompositor();
		var visual = CreateEllipseVisual(compositor, center: 100, radius: 100, fill: Colors.Red);

		using var clip = ComputeNativeClip(CreateHostRoot(visual));

		Assert.IsFalse(clip.FillContains(new Vector2(100, 100)), "The ellipse was not cut out of the native element.");
		Assert.IsTrue(clip.FillContains(new Vector2(5, 5)), "The bounding-box corner outside the ellipse was cut out of the native element.");
#endif
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26278")]
	public void When_Ellipse_Is_Scaled_By_ViewBox_Over_Native_Host_Then_Scaled_Ellipse_Is_Cut()
	{
#if __SKIA__
		var compositor = Compositor.GetSharedCompositor();
		var visual = CreateEllipseVisual(compositor, center: 50, radius: 50, fill: Colors.Red);
		visual.ViewBox = compositor.CreateViewBox();
		visual.ViewBox.Size = new Vector2(100, 100);

		using var clip = ComputeNativeClip(CreateHostRoot(visual));

		Assert.IsFalse(clip.FillContains(new Vector2(150, 100)), "The ellipse scaled up by the ViewBox was not cut out of the native element.");
		Assert.IsTrue(clip.FillContains(new Vector2(5, 5)), "The bounding-box corner outside the scaled ellipse was cut out of the native element.");
#endif
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26278")]
	public void When_Stroked_Ellipse_With_Transparent_Fill_Over_Native_Host_Then_Only_Stroke_Is_Cut()
	{
#if __SKIA__
		var compositor = Compositor.GetSharedCompositor();
		var visual = CreateEllipseVisual(compositor, center: 100, radius: 90, fill: Colors.Transparent);
		var shape = (CompositionSpriteShape)visual.Shapes[0];
		shape.StrokeBrush = compositor.CreateColorBrush(Colors.Red);
		shape.StrokeThickness = 4;

		using var clip = ComputeNativeClip(CreateHostRoot(visual));

		Assert.IsTrue(clip.FillContains(new Vector2(100, 100)), "The transparent fill was cut out of the native element.");
		Assert.IsFalse(clip.FillContains(new Vector2(100, 10)), "The stroke was not cut out of the native element.");
#endif
	}

	[TestMethod]
	public void When_Too_Many_Shapes_Over_Native_Host_Then_Whole_Rect_Is_Cut()
	{
#if __SKIA__
		var compositor = Compositor.GetSharedCompositor();
		var visual = compositor.CreateShapeVisual();
		visual.Size = new Vector2(200, 200);
		for (var i = 0; i < 17; i++)
		{
			var ellipse = compositor.CreateEllipseGeometry();
			ellipse.Center = new Vector2(10 + i, 10);
			ellipse.Radius = new Vector2(5, 5);
			var shape = compositor.CreateSpriteShape(ellipse);
			shape.FillBrush = compositor.CreateColorBrush(Colors.Red);
			visual.Shapes.Add(shape);
		}

		using var clip = ComputeNativeClip(CreateHostRoot(visual));

		Assert.IsFalse(clip.FillContains(new Vector2(100, 100)), "Shapes too many to bound should cut their whole clip out of the native element.");
#endif
	}

#if __SKIA__
	private static BorderVisual CreateOutline(Compositor compositor, float size = 200)
	{
		var border = compositor.CreateBorderVisual();
		border.Size = new Vector2(size, size);
		border.BorderThickness = new Uno.UI.Composition.Thickness(4);
		border.BorderBrush = compositor.CreateColorBrush(Colors.Red);
		return border;
	}

	private static ShapeVisual CreateEllipseVisual(Compositor compositor, float center, float radius, Color fill)
	{
		var ellipse = compositor.CreateEllipseGeometry();
		ellipse.Center = new Vector2(center, center);
		ellipse.Radius = new Vector2(radius, radius);
		var shape = compositor.CreateSpriteShape(ellipse);
		shape.FillBrush = compositor.CreateColorBrush(fill);
		var visual = compositor.CreateShapeVisual();
		visual.Size = new Vector2(200, 200);
		visual.Shapes.Add(shape);
		return visual;
	}

	private static ContainerVisual CreateHost(Compositor compositor)
	{
		var host = compositor.CreateContainerVisual();
		host.Size = new Vector2(200, 200);
		host.SetAsNativeHostVisual(true);
		return host;
	}

	// A 200x200 native host with the overlay drawn after it, as a later sibling.
	private static ContainerVisual CreateHostRoot(Visual overlay)
	{
		var compositor = Compositor.GetSharedCompositor();
		var root = compositor.CreateContainerVisual();
		root.Size = new Vector2(200, 200);
		root.Children.InsertAtTop(CreateHost(compositor));
		root.Children.InsertAtTop(overlay);
		return root;
	}

	private static IGeometry ComputeNativeClip(Visual root, Rect? frame = null)
	{
		using var parentClip = GeometryFactory.Current.CreateRectangleGeometry(frame ?? new Rect(0, 0, 200, 200));
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
