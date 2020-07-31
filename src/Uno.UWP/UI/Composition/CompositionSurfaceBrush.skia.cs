#nullable enable

using System;
using System.Numerics;
using SkiaSharp;

namespace Windows.UI.Composition
{
	public partial class CompositionSurfaceBrush : CompositionBrush
	{
		internal override void UpdatePaint(SKPaint fillPaint)
		{
			if (Surface is SkiaCompositionSurface scs)
			{
				var imageShader = SKShader.CreateImage(scs.Image, SKShaderTileMode.Repeat, SKShaderTileMode.Repeat, TransformMatrix.ToSKMatrix());
				var opacity = 255 * Compositor.CurrentOpacity;
				var filteredImageShader = SKShader.CreateColorFilter(imageShader, SKColorFilter.CreateBlendMode(new SKColor(0xFF, 0xFF, 0xFF, (byte)opacity), SKBlendMode.Modulate));

				fillPaint.Shader = filteredImageShader;

				fillPaint.IsAntialias = true;
			}
			else
			{
				fillPaint.Shader = null;
			}
		}
	}
}
