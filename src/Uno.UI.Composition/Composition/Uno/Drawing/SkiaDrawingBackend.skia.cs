#nullable enable

using System.Numerics;
using Microsoft.UI.Composition;
using SkiaSharp;
using Windows.UI;

namespace Uno.UI.Composition.Drawing;

/// <summary>The default <see cref="IDrawingBackend"/>, backed by SkiaSharp.</summary>
internal sealed class SkiaDrawingBackend : IDrawingBackend
{
	public IPathBuilder CreatePathBuilder() => new SkiaPathBuilder();

	public IShader CreateLinearGradientShader(
		Vector2 start,
		Vector2 end,
		Color[] colors,
		float[] colorPositions,
		GradientTileMode tileMode,
		Matrix3x2 localMatrix)
	{
		var skColors = new SKColor[colors.Length];
		for (var i = 0; i < colors.Length; i++)
		{
			skColors[i] = colors[i].ToSKColor();
		}

		var shader = SKShader.CreateLinearGradient(
			new SKPoint(start.X, start.Y),
			new SKPoint(end.X, end.Y),
			skColors,
			colorPositions,
			ToSK(tileMode),
			localMatrix.ToSKMatrix());

		return new SkiaShader(shader);
	}

	public IColorFilter? CreateOpacityColorFilter(float opacity)
		=> opacity.ToColorFilter() is { } filter ? new SkiaColorFilter(filter) : null;

	private static SKShaderTileMode ToSK(GradientTileMode mode) => mode switch
	{
		GradientTileMode.Repeat => SKShaderTileMode.Repeat,
		GradientTileMode.Mirror => SKShaderTileMode.Mirror,
		_ => SKShaderTileMode.Clamp,
	};
}
