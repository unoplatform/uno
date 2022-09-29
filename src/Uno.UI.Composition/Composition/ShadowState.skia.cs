using SkiaSharp;
using Windows.UI;

namespace Uno.UI.Composition.Composition;

/// <summary>
/// Captures the state of drop shadow for a visual.
/// </summary>
internal class ShadowState
{
	private SKPaint? _paint;

	public ShadowState(float dx, float dy, float sigmaX, float sigmaY, Color color)
	{
		Dx = dx;
		Dy = dy;
		SigmaX = sigmaX;
		SigmaY = sigmaY;
		Color = color;
	}

	public float Dx { get; }

	public float Dy { get; }

	public float SigmaX { get; }

	public float SigmaY { get; }

	public Color Color { get; }

	public SKPaint Paint =>
		_paint ??= new SKPaint()
		{
			ImageFilter = SKImageFilter.CreateDropShadow(Dx, Dy, SigmaX, SigmaY, Color)
		};
}
