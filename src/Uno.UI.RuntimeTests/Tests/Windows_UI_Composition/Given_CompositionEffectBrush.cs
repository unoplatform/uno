#if HAS_UNO
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

[TestClass]
[RunsOnUIThread]
public class Given_CompositionEffectBrush
{
#if __SKIA__
	[TestMethod]
	public async Task When_NonBackdrop_GrayscaleEffect_Renders()
	{
		// Regression test: non-backdrop effects must produce visible output.
		// Before the fix, all effects used the backdrop path (SaveLayer with Backdrop filter),
		// which produces nothing when rendered into an SKPicture (no backdrop content to filter).
		var compositor = Compositor.GetSharedCompositor();

		var grayscale = new GrayscaleEffect
		{
			Source = new CompositionEffectSourceParameter("source")
		};

		var factory = compositor.CreateEffectFactory(grayscale);
		var effectBrush = factory.CreateBrush();
		effectBrush.SetSourceParameter("source", compositor.CreateColorBrush(Colors.Red));

		var visual = compositor.CreateShapeVisual();
		visual.Size = new Vector2(100, 100);

		var shape = compositor.CreateSpriteShape();
		shape.Geometry = compositor.CreateRectangleGeometry();
		((CompositionRectangleGeometry)shape.Geometry).Size = new Vector2(100, 100);
		shape.FillBrush = effectBrush;
		visual.Shapes.Add(shape);

		var host = new ContentControl { Width = 100, Height = 100 };
		ElementCompositionPreview.SetElementChildVisual(host, visual);

		await UITestHelper.Load(host);
		await TestServices.WindowHelper.WaitForIdle();

		var screenshot = await UITestHelper.ScreenShot(host);

		// Red (255,0,0) grayscaled should produce approximately equal R,G,B channels.
		// The key assertion: the center should NOT be transparent/empty (the bug produced nothing).
		ImageAssert.DoesNotHaveColorAt(screenshot, 50, 50, Colors.Transparent);

		// Verify grayscale: R, G, B channels should be approximately equal
		var pixel = screenshot.GetPixel(50, 50);
		Assert.IsTrue(pixel.A > 200, $"Expected mostly opaque pixel, got A={pixel.A}");
		Assert.IsTrue(
			System.Math.Abs(pixel.R - pixel.G) < 20 && System.Math.Abs(pixel.G - pixel.B) < 20,
			$"Expected grayscale pixel (R~=G~=B), got ({pixel.R},{pixel.G},{pixel.B})");
	}

	[TestMethod]
	public async Task When_NonBackdrop_InvertEffect_Renders()
	{
		var compositor = Compositor.GetSharedCompositor();

		var invert = new InvertEffect
		{
			Source = new CompositionEffectSourceParameter("source")
		};

		var factory = compositor.CreateEffectFactory(invert);
		var effectBrush = factory.CreateBrush();

		// White (255,255,255) inverted should produce Black (0,0,0)
		effectBrush.SetSourceParameter("source", compositor.CreateColorBrush(Colors.White));

		var visual = compositor.CreateShapeVisual();
		visual.Size = new Vector2(100, 100);

		var shape = compositor.CreateSpriteShape();
		shape.Geometry = compositor.CreateRectangleGeometry();
		((CompositionRectangleGeometry)shape.Geometry).Size = new Vector2(100, 100);
		shape.FillBrush = effectBrush;
		visual.Shapes.Add(shape);

		var host = new ContentControl { Width = 100, Height = 100 };
		ElementCompositionPreview.SetElementChildVisual(host, visual);

		await UITestHelper.Load(host);
		await TestServices.WindowHelper.WaitForIdle();

		var screenshot = await UITestHelper.ScreenShot(host);

		// Inverted white should be black (or very close to it)
		ImageAssert.HasColorAt(screenshot, 50, 50, Colors.Black, tolerance: 15);
	}

	[TestMethod]
	public async Task When_NonBackdrop_ColorSourceEffect_Renders()
	{
		var compositor = Compositor.GetSharedCompositor();

		var colorSource = new ColorSourceEffect
		{
			Color = Color.FromArgb(255, 66, 135, 245)
		};

		var factory = compositor.CreateEffectFactory(colorSource);
		var effectBrush = factory.CreateBrush();

		var visual = compositor.CreateShapeVisual();
		visual.Size = new Vector2(100, 100);

		var shape = compositor.CreateSpriteShape();
		shape.Geometry = compositor.CreateRectangleGeometry();
		((CompositionRectangleGeometry)shape.Geometry).Size = new Vector2(100, 100);
		shape.FillBrush = effectBrush;
		visual.Shapes.Add(shape);

		var host = new ContentControl { Width = 100, Height = 100 };
		ElementCompositionPreview.SetElementChildVisual(host, visual);

		await UITestHelper.Load(host);
		await TestServices.WindowHelper.WaitForIdle();

		var screenshot = await UITestHelper.ScreenShot(host);

		ImageAssert.HasColorAt(screenshot, 50, 50, Color.FromArgb(255, 66, 135, 245), tolerance: 15);
	}

	[TestMethod]
	public async Task When_NonBackdrop_SaturationEffect_Renders()
	{
		var compositor = Compositor.GetSharedCompositor();

		// Saturation=0 should fully desaturate (same as grayscale)
		var saturation = new SaturationEffect
		{
			Source = new CompositionEffectSourceParameter("source"),
			Saturation = 0.0f
		};

		var factory = compositor.CreateEffectFactory(saturation);
		var effectBrush = factory.CreateBrush();
		effectBrush.SetSourceParameter("source", compositor.CreateColorBrush(Colors.Red));

		var visual = compositor.CreateShapeVisual();
		visual.Size = new Vector2(100, 100);

		var shape = compositor.CreateSpriteShape();
		shape.Geometry = compositor.CreateRectangleGeometry();
		((CompositionRectangleGeometry)shape.Geometry).Size = new Vector2(100, 100);
		shape.FillBrush = effectBrush;
		visual.Shapes.Add(shape);

		var host = new ContentControl { Width = 100, Height = 100 };
		ElementCompositionPreview.SetElementChildVisual(host, visual);

		await UITestHelper.Load(host);
		await TestServices.WindowHelper.WaitForIdle();

		var screenshot = await UITestHelper.ScreenShot(host);

		// Desaturated red should be a gray - NOT transparent
		ImageAssert.DoesNotHaveColorAt(screenshot, 50, 50, Colors.Transparent);

		var pixel = screenshot.GetPixel(50, 50);
		Assert.IsTrue(pixel.A > 200, $"Expected mostly opaque pixel, got A={pixel.A}");
		Assert.IsTrue(
			System.Math.Abs(pixel.R - pixel.G) < 20 && System.Math.Abs(pixel.G - pixel.B) < 20,
			$"Expected desaturated pixel (R~=G~=B), got ({pixel.R},{pixel.G},{pixel.B})");
	}

	[TestMethod]
	public async Task When_BackdropEffect_HasBackdropBrushInput_IsTrue()
	{
		var compositor = Compositor.GetSharedCompositor();

		var blur = new GaussianBlurEffect
		{
			Source = new CompositionEffectSourceParameter("backdrop"),
			BlurAmount = 5.0f
		};

		var factory = compositor.CreateEffectFactory(blur);
		var effectBrush = factory.CreateBrush();
		effectBrush.SetSourceParameter("backdrop", compositor.CreateBackdropBrush());

		// Force filter generation by painting once
		var visual = compositor.CreateShapeVisual();
		visual.Size = new Vector2(100, 100);

		var shape = compositor.CreateSpriteShape();
		shape.Geometry = compositor.CreateRectangleGeometry();
		((CompositionRectangleGeometry)shape.Geometry).Size = new Vector2(100, 100);
		shape.FillBrush = effectBrush;
		visual.Shapes.Add(shape);

		var host = new ContentControl { Width = 100, Height = 100 };
		ElementCompositionPreview.SetElementChildVisual(host, visual);

		await UITestHelper.Load(host);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(effectBrush.HasBackdropBrushInput, "Backdrop effects should set HasBackdropBrushInput=true");
	}

	[TestMethod]
	public async Task When_NonBackdrop_HasBackdropBrushInput_IsFalse()
	{
		var compositor = Compositor.GetSharedCompositor();

		var grayscale = new GrayscaleEffect
		{
			Source = new CompositionEffectSourceParameter("source")
		};

		var factory = compositor.CreateEffectFactory(grayscale);
		var effectBrush = factory.CreateBrush();
		effectBrush.SetSourceParameter("source", compositor.CreateColorBrush(Colors.Red));

		var visual = compositor.CreateShapeVisual();
		visual.Size = new Vector2(100, 100);

		var shape = compositor.CreateSpriteShape();
		shape.Geometry = compositor.CreateRectangleGeometry();
		((CompositionRectangleGeometry)shape.Geometry).Size = new Vector2(100, 100);
		shape.FillBrush = effectBrush;
		visual.Shapes.Add(shape);

		var host = new ContentControl { Width = 100, Height = 100 };
		ElementCompositionPreview.SetElementChildVisual(host, visual);

		await UITestHelper.Load(host);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsFalse(effectBrush.HasBackdropBrushInput, "Non-backdrop effects should set HasBackdropBrushInput=false");
	}
#endif
}
#endif
