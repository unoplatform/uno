#nullable enable

using System.Numerics;
using Microsoft.UI.Composition;
using SkiaSharp;
using Windows.Foundation;
using Windows.UI;

namespace Uno.UI.Composition.Drawing;

/// <summary>The default <see cref="IDrawingBackend"/>, backed by SkiaSharp.</summary>
internal sealed class SkiaDrawingBackend : IDrawingBackend
{
	public IPathBuilder CreatePathBuilder() => new SkiaPathBuilder();

	public IGeometry CreateRectangleGeometry(Rect rect)
	{
		var builder = new SKPathBuilder();
		builder.AddRect(rect.ToSKRect());
		return new SkiaGeometrySource2D(builder.Detach());
	}

	public IShader CreateLinearGradientShader(
		Vector2 start,
		Vector2 end,
		Color[] colors,
		float[] colorPositions,
		GradientTileMode tileMode,
		Matrix3x2 localMatrix)
	{
		var shader = SKShader.CreateLinearGradient(
			new SKPoint(start.X, start.Y),
			new SKPoint(end.X, end.Y),
			ToSKColors(colors),
			colorPositions,
			ToSK(tileMode),
			localMatrix.ToSKMatrix());

		return new SkiaShader(shader);
	}

	public IShader CreateRadialGradientShader(
		Vector2 center,
		float radius,
		Color[] colors,
		float[] colorPositions,
		GradientTileMode tileMode,
		Matrix3x2 localMatrix)
		=> new SkiaShader(SKShader.CreateRadialGradient(
			new SKPoint(center.X, center.Y),
			radius,
			ToSKColors(colors),
			colorPositions,
			ToSK(tileMode),
			localMatrix.ToSKMatrix()));

	public IShader CreateTwoPointConicalGradientShader(
		Vector2 start,
		float startRadius,
		Vector2 end,
		float endRadius,
		Color[] colors,
		float[] colorPositions,
		GradientTileMode tileMode,
		Matrix3x2 localMatrix)
		=> new SkiaShader(SKShader.CreateTwoPointConicalGradient(
			new SKPoint(start.X, start.Y),
			startRadius,
			new SKPoint(end.X, end.Y),
			endRadius,
			ToSKColors(colors),
			colorPositions,
			ToSK(tileMode),
			localMatrix.ToSKMatrix()));

	public IShader CreateColorShader(Color color) => new SkiaShader(SKShader.CreateColor(color.ToSKColor()));

	public IShader ComposeShaders(IShader outer, IShader inner)
		=> new SkiaShader(SKShader.CreateCompose(((SkiaShader)outer).Shader, ((SkiaShader)inner).Shader));

	private static SKColor[] ToSKColors(Color[] colors)
	{
		var skColors = new SKColor[colors.Length];
		for (var i = 0; i < colors.Length; i++)
		{
			skColors[i] = colors[i].ToSKColor();
		}
		return skColors;
	}

	public IColorFilter? CreateOpacityColorFilter(float opacity)
		=> opacity.ToColorFilter() is { } filter ? new SkiaColorFilter(filter) : null;

	public IColorFilter CreateBlendModeColorFilter(Color color, BlendMode mode)
		=> new SkiaColorFilter(SKColorFilter.CreateBlendMode(color.ToSKColor(), SkiaDrawingSession.ToSKBlendMode(mode)));

	public IMaskFilter CreateBlurMaskFilter(float sigma)
		=> new SkiaMaskFilter(SKMaskFilter.CreateBlur(SKBlurStyle.Normal, sigma));

	private static SKShaderTileMode ToSK(GradientTileMode mode) => mode switch
	{
		GradientTileMode.Repeat => SKShaderTileMode.Repeat,
		GradientTileMode.Mirror => SKShaderTileMode.Mirror,
		_ => SKShaderTileMode.Clamp,
	};
}
