#nullable enable

using System;
using System.Numerics;
using SkiaSharp;

namespace Microsoft.UI.Composition
{
	public partial class CompositionSurfaceBrush : CompositionBrush
	{
		internal override void UpdatePaint(SKPaint fillPaint, SKRect bounds)
		{
			if (Surface is SkiaCompositionSurface scs)
			{
				var imageShader = SKShader.CreateImage(scs.Image, SKShaderTileMode.Repeat, SKShaderTileMode.Repeat, TransformMatrix.ToSKMatrix());

				fillPaint.Shader = imageShader;

				fillPaint.IsAntialias = true;
			}
			else
			{
				fillPaint.Shader = null;
			}
		}
	}
}
