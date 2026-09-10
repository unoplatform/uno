#if __SKIA__
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation.Metadata;
using Windows.UI;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

[TestClass]
[RunsOnUIThread]
public class Given_CompositionGeometricClip
{
	[TestMethod]
	public async Task When_Nested_Path_Clips()
	{
		if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap, Uno.UI"))
		{
			Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
		}

		// Two path clips, one on the container and one on the sprite inside it: the red must survive only in their
		// intersection, the wedge between the two diagonals, and neither triangle's own remainder may leak.
		var host = new Border { Width = 200, Height = 200, Background = new SolidColorBrush(Colors.White) };
		await UITestHelper.Load(host);

		var compositor = ElementCompositionPreview.GetElementVisual(host).Compositor;
		var container = compositor.CreateContainerVisual();
		container.Size = new Vector2(200, 200);
		container.Clip = compositor.CreateGeometricClip(compositor.CreatePathGeometry(new CompositionPath(Triangle(new Vector2(0, 0), new Vector2(200, 0), new Vector2(0, 200)))));

		var sprite = compositor.CreateSpriteVisual();
		sprite.Size = new Vector2(200, 200);
		sprite.Brush = compositor.CreateColorBrush(Colors.Red);
		sprite.Clip = compositor.CreateGeometricClip(compositor.CreatePathGeometry(new CompositionPath(Triangle(new Vector2(0, 0), new Vector2(200, 0), new Vector2(200, 200)))));
		container.Children.InsertAtTop(sprite);
		ElementCompositionPreview.SetElementChildVisual(host, container);

		await TestServices.WindowHelper.WaitForIdle();
		var screenshot = await UITestHelper.ScreenShot(host);

		ImageAssert.HasColorAt(screenshot, 100, 20, Colors.Red, tolerance: 10);
		ImageAssert.HasColorAt(screenshot, 100, 80, Colors.Red, tolerance: 10);
		ImageAssert.HasColorAt(screenshot, 30, 100, Colors.White, tolerance: 10);
		ImageAssert.HasColorAt(screenshot, 170, 100, Colors.White, tolerance: 10);
		ImageAssert.HasColorAt(screenshot, 100, 150, Colors.White, tolerance: 10);
	}

	[TestMethod]
	public async Task When_Six_Nested_Rectangle_Clips()
	{
		if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap, Uno.UI"))
		{
			Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
		}

		// Six nested rectangle clips, each the tightest somewhere: four strips, a circle (a rounded rect whose radius is
		// half its side) and a fifth strip inside it. Every one of them must cut the red, however many a draw carries.
		var host = new Border { Width = 200, Height = 200, Background = new SolidColorBrush(Colors.White) };
		await UITestHelper.Load(host);

		var compositor = ElementCompositionPreview.GetElementVisual(host).Compositor;
		var clips = new[]
		{
			compositor.CreateRectangleClip(30, 0, 200, 200),
			compositor.CreateRectangleClip(0, 30, 200, 200),
			compositor.CreateRectangleClip(0, 0, 170, 200),
			compositor.CreateRectangleClip(0, 0, 200, 170),
			compositor.CreateRectangleClip(20, 20, 180, 180),
			compositor.CreateRectangleClip(40, 0, 200, 200),
		};
		clips[4].TopLeftRadius = clips[4].TopRightRadius = clips[4].BottomRightRadius = clips[4].BottomLeftRadius = new Vector2(80, 80);

		ContainerVisual outer = null, inner = null;
		foreach (var clip in clips)
		{
			var container = compositor.CreateContainerVisual();
			container.Size = new Vector2(200, 200);
			container.Clip = clip;
			if (inner is null) { outer = container; } else { inner.Children.InsertAtTop(container); }
			inner = container;
		}
		var sprite = compositor.CreateSpriteVisual();
		sprite.Size = new Vector2(200, 200);
		sprite.Brush = compositor.CreateColorBrush(Colors.Red);
		inner.Children.InsertAtTop(sprite);
		ElementCompositionPreview.SetElementChildVisual(host, outer);

		await TestServices.WindowHelper.WaitForIdle();
		var screenshot = await UITestHelper.ScreenShot(host);

		ImageAssert.HasColorAt(screenshot, 100, 100, Colors.Red, tolerance: 10);
		ImageAssert.HasColorAt(screenshot, 45, 100, Colors.Red, tolerance: 10);
		ImageAssert.HasColorAt(screenshot, 100, 45, Colors.Red, tolerance: 10);
		ImageAssert.HasColorAt(screenshot, 150, 150, Colors.Red, tolerance: 10);
		ImageAssert.HasColorAt(screenshot, 25, 100, Colors.White, tolerance: 10);   // strip 1
		ImageAssert.HasColorAt(screenshot, 100, 25, Colors.White, tolerance: 10);   // strip 2
		ImageAssert.HasColorAt(screenshot, 175, 100, Colors.White, tolerance: 10);  // strip 3
		ImageAssert.HasColorAt(screenshot, 100, 175, Colors.White, tolerance: 10);  // strip 4
		ImageAssert.HasColorAt(screenshot, 160, 160, Colors.White, tolerance: 10);  // the circle
		ImageAssert.HasColorAt(screenshot, 35, 100, Colors.White, tolerance: 10);   // strip 6
	}

	private static CanvasGeometry Triangle(Vector2 a, Vector2 b, Vector2 c)
	{
		using var builder = new CanvasPathBuilder(null);
		builder.BeginFigure(a);
		builder.AddLine(b);
		builder.AddLine(c);
		builder.EndFigure(CanvasFigureLoop.Closed);
		return CanvasGeometry.CreatePath(builder);
	}
}
#endif
