#if __SKIA__
#nullable enable

using System.Numerics;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Private.Infrastructure;
using SkiaSharp;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

[TestClass]
[RunsOnUIThread]
public class Given_SkiaDrawingSession
{
	// The 2D transform LottieGen emits (e.g. the CheckBox AnimatedIcon visual source): a 3x2 matrix widened
	// with M33 = 0, which a 3x3 SKMatrix cannot represent.
	private static readonly Matrix4x4 FlatLottieTransform = new(
		1.05f, 0, 0, 0,
		0, 1.05f, 0, 0,
		0, 0, 0, 0,
		24, 24, 0, 1);

	[TestMethod]
	public void When_SetMatrix_Then_TotalMatrix_Round_Trips_Full_4x4()
	{
		using var surface = SKSurface.Create(new SKImageInfo(100, 100, SKColorType.Bgra8888, SKAlphaType.Premul));
		var session = new SkiaDrawingSession(surface.Canvas, new SkiaDrawingFactory());

		session.SetMatrix(FlatLottieTransform);

		Assert.AreEqual(FlatLottieTransform, session.TotalMatrix);
	}

	[TestMethod]
	public void When_Ancestor_TransformMatrix_Has_Zero_M33_Then_Identity_Child_Renders()
	{
		var compositor = TestServices.WindowHelper.XamlRoot.Compositor;

		var root = compositor.CreateContainerVisual();
		root.Size = new Vector2(100, 100);

		var flattened = compositor.CreateContainerVisual();
		flattened.Size = new Vector2(40, 40);
		flattened.TransformMatrix = FlatLottieTransform;
		root.Children.InsertAtTop(flattened);

		// Identity local matrix: its session inherits the ancestor's matrix rather than setting its own.
		var child = compositor.CreateSpriteVisual();
		child.Size = new Vector2(40, 40);
		child.Brush = compositor.CreateColorBrush(Colors.Red);
		flattened.Children.InsertAtTop(child);

		using var surface = SKSurface.Create(new SKImageInfo(100, 100, SKColorType.Bgra8888, SKAlphaType.Premul));
		surface.Canvas.Clear(SKColors.White);

		root.RenderRootVisual(new SkiaDrawingSession(surface.Canvas, new SkiaDrawingFactory()), Vector2.Zero);

		using var image = surface.Snapshot();
		using var bitmap = SKBitmap.FromImage(image);
		Assert.AreEqual(SKColors.Red, bitmap.GetPixel(40, 40));
		Assert.AreEqual(SKColors.White, bitmap.GetPixel(10, 10));
	}
}
#endif
