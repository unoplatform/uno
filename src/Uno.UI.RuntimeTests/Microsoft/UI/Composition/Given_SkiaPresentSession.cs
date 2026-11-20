#nullable enable

#if UNO_DRAWING_SKIA
using SkiaSharp;
using Uno.UI.Composition.Drawing;
using Rect = Windows.Foundation.Rect;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

[TestClass]
[RunsOnUIThread]
public class Given_SkiaPresentSession
{
	// The retained pixels a partial repaint must either fully overwrite or leave untouched, never blend with.
	private static readonly SKColor Retained = SKColors.Red;
	private static readonly SKColor Repainted = SKColors.Blue;

	[TestMethod]
	public void When_Damage_Is_Fractional_Then_Edge_Pixels_Are_Fully_Repainted()
	{
		using var bitmap = Repaint(new Rect(10.5, 10.5, 9, 9));

		// The partially covered edge pixels belong to the damage, snapped outwards.
		Assert.AreEqual(Repainted, bitmap.GetPixel(10, 10));
		Assert.AreEqual(Repainted, bitmap.GetPixel(19, 19));
		Assert.AreEqual(Repainted, bitmap.GetPixel(15, 15));
		Assert.AreEqual(Retained, bitmap.GetPixel(9, 9));
		Assert.AreEqual(Retained, bitmap.GetPixel(20, 20));
	}

	[TestMethod]
	public void When_Several_Fractional_Damage_Rects_Then_Edges_Are_Not_Blended()
	{
		using var bitmap = Repaint(new Rect(2.25, 2.25, 4.5, 4.5), new Rect(20.75, 3.5, 5, 5));

		Assert.AreEqual(Repainted, bitmap.GetPixel(2, 2));
		Assert.AreEqual(Repainted, bitmap.GetPixel(6, 6));
		Assert.AreEqual(Repainted, bitmap.GetPixel(20, 3));
		Assert.AreEqual(Repainted, bitmap.GetPixel(25, 8));
		Assert.AreEqual(Retained, bitmap.GetPixel(7, 7));
		Assert.AreEqual(Retained, bitmap.GetPixel(13, 5));
		Assert.AreEqual(Retained, bitmap.GetPixel(26, 9));
	}

	private static SKBitmap Repaint(params Rect[] damage)
	{
		var bitmap = new SKBitmap(new SKImageInfo(32, 32, SKColorType.Rgba8888, SKAlphaType.Premul));
		using var canvas = new SKCanvas(bitmap);
		canvas.Clear(Retained);

		using var factory = new SkiaDrawingFactory();
		using (var session = new SkiaPresentSession(canvas, factory))
		{
			session.ClipToDamage(damage);
			canvas.Clear(Repainted);
		}

		return bitmap;
	}
}
#endif
