#nullable enable

#if UNO_DRAWING_SKIA
using System.Numerics;
using SkiaSharp;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

[TestClass]
[RunsOnUIThread]
public class Given_SkiaDrawingSession
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25089")]
	public void When_SetMatrix_Flattens_Z_Then_TotalMatrix_Round_Trips()
	{
		// AnimatedAcceptVisualSource's outer transform (the CheckBox glyph): a 1.05 scale that zeroes Z (M33 = 0).
		AssertRoundTrips(new Matrix4x4(
			1.05f, 0, 0, 0,
			0, 1.05f, 0, 0,
			0, 0, 0, 0,
			24, 24, 0, 1));
	}

	[TestMethod]
	public void When_SetMatrix_Rotates_In_3D_Then_TotalMatrix_Round_Trips()
		=> AssertRoundTrips(Matrix4x4.CreateRotationX(0.5f) * Matrix4x4.CreateTranslation(10, 20, 30));

	[TestMethod]
	public void When_Canvas_Transformed_Then_TotalMatrix_Matches()
	{
		using var surface = SKSurface.Create(new SKImageInfo(16, 16));
		surface.Canvas.Translate(10, 20);
		surface.Canvas.Scale(2, 3);

		var session = new SkiaDrawingSession(surface.Canvas, DrawingFactory.Current);

		Assert.AreEqual(Matrix4x4.CreateScale(2, 3, 1) * Matrix4x4.CreateTranslation(10, 20, 0), session.TotalMatrix);
	}

	private static void AssertRoundTrips(Matrix4x4 matrix)
	{
		using var surface = SKSurface.Create(new SKImageInfo(16, 16));
		var session = new SkiaDrawingSession(surface.Canvas, DrawingFactory.Current);

		session.SetMatrix(matrix);

		Assert.AreEqual(matrix, session.TotalMatrix);
	}
}
#endif
