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
