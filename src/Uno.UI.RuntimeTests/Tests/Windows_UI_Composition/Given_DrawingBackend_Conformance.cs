#nullable enable

using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests;
using Uno.UI.RuntimeTests.Helpers;
using Colors = Microsoft.UI.Colors;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

/// <summary>
/// Backend-neutral conformance for the drawing seam: each test renders a deterministic solid-colour scene through
/// the normal XAML → composition → drawing-backend pipeline and asserts pixel colours at known points, so it runs
/// identically on every backend (Skia and WebGPU) under software rendering (lavapipe) — no golden images, no
/// runtime-shader dependence. The goal is parity: the SAME assertions pass whichever backend is active
/// (select WebGPU with UNO_WEBGPU=1). Complements the golden-image tests (Given_GradientBrush) and the effect
/// parity tests (Given_EffectBrush_Parity).
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_DrawingBackend_Conformance
{
	private const byte Tol = 24;

	private static async Task<RawBitmap> Render(FrameworkElement content, int w = 100, int h = 100)
	{
		var host = new Border { Width = w, Height = h, Background = new SolidColorBrush(Colors.White), Child = content };
		await UITestHelper.Load(host);
		await TestServices.WindowHelper.WaitForIdle();
		var bmp = await UITestHelper.ScreenShot(host);
		await bmp.Populate();
		return bmp;
	}

	[TestMethod]
	public async Task When_Solid_Fill()
	{
		var bmp = await Render(new Rectangle { Fill = new SolidColorBrush(Colors.Red) });
		ImageAssert.HasColorAt(bmp, 50, 50, Colors.Red, Tol);
	}

	[TestMethod]
	public async Task When_RoundedRect_Corner_Is_Clipped()
	{
		var bmp = await Render(new Border { CornerRadius = new CornerRadius(40), Background = new SolidColorBrush(Colors.Green) });
		ImageAssert.HasColorAt(bmp, 50, 50, Colors.Green, Tol);   // centre filled
		ImageAssert.HasColorAt(bmp, 3, 3, Colors.White, Tol);      // clipped corner shows the white host
	}

	[TestMethod]
	public async Task When_Path_Fill_Triangle()
	{
		// A triangle covering the bottom edge: centre-bottom filled, top corner empty.
		var path = new Microsoft.UI.Xaml.Shapes.Path
		{
			Fill = new SolidColorBrush(Colors.Blue),
			Data = new PathGeometry
			{
				Figures =
				{
					new PathFigure
					{
						StartPoint = new Point(0, 100),
						IsClosed = true,
						Segments = { new LineSegment { Point = new Point(100, 100) }, new LineSegment { Point = new Point(50, 20) } },
					},
				},
			},
		};
		var bmp = await Render(path);
		ImageAssert.HasColorAt(bmp, 50, 90, Colors.Blue, Tol);   // inside triangle
		ImageAssert.HasColorAt(bmp, 5, 10, Colors.White, Tol);   // outside (top-left)
	}

	[TestMethod]
	public async Task When_Stroke_Outline()
	{
		var bmp = await Render(new Rectangle
		{
			Stroke = new SolidColorBrush(Colors.Black),
			StrokeThickness = 8,
			Fill = null,
			Margin = new Thickness(10),
		});
		ImageAssert.HasColorAt(bmp, 50, 50, Colors.White, Tol);   // hollow centre
		ImageAssert.HasColorAt(bmp, 50, 11, Colors.Black, Tol);   // top stroke edge
	}

	[TestMethod]
	public async Task When_Linear_Gradient()
	{
		var brush = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 0) };
		brush.GradientStops.Add(new GradientStop { Offset = 0, Color = Colors.Red });
		brush.GradientStops.Add(new GradientStop { Offset = 1, Color = Colors.Blue });
		var bmp = await Render(new Rectangle { Fill = brush });
		ImageAssert.HasColorAt(bmp, 4, 50, Colors.Red, 40);
		ImageAssert.HasColorAt(bmp, 96, 50, Colors.Blue, 40);
	}

	[TestMethod]
	public async Task When_Radial_Gradient_Concentric()
	{
		var brush = new RadialGradientBrush { Center = new Point(0.5, 0.5), GradientOrigin = new Point(0.5, 0.5), RadiusX = 0.5, RadiusY = 0.5 };
		brush.GradientStops.Add(new GradientStop { Offset = 0, Color = Colors.Red });
		brush.GradientStops.Add(new GradientStop { Offset = 1, Color = Colors.Blue });
		var bmp = await Render(new Rectangle { Fill = brush });
		ImageAssert.HasColorAt(bmp, 50, 50, Colors.Red, 50);   // centre → inner stop
	}

	[TestMethod]
	public async Task When_Opacity_Blends()
	{
		// 50%-opacity red over the white host ≈ (255,128,128).
		var bmp = await Render(new Rectangle { Fill = new SolidColorBrush(Colors.Red), Opacity = 0.5 });
		ImageAssert.HasColorAt(bmp, 50, 50, Color.FromArgb(255, 255, 128, 128), 30);
	}

	[TestMethod]
	public async Task When_Clip_Restricts_Drawing()
	{
		var rect = new Rectangle
		{
			Fill = new SolidColorBrush(Colors.Purple),
			Clip = new RectangleGeometry { Rect = new Rect(0, 0, 50, 100) },
		};
		var bmp = await Render(rect);
		ImageAssert.HasColorAt(bmp, 25, 50, Colors.Purple, Tol);   // inside clip
		ImageAssert.HasColorAt(bmp, 75, 50, Colors.White, Tol);    // clipped away
	}
}
