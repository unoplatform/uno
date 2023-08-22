#nullable enable

using System;
using System.Numerics;
using Uno.UI.Composition;
using SkiaSharp;
using System.Runtime.Intrinsics.Arm;

namespace Windows.UI.Composition
{
	public partial class CompositionSurfaceBrush : CompositionBrush, IOnlineBrush
	{
		bool IOnlineBrush.IsOnline => Surface is ISkiaSurface skiaSurface;

		internal override void UpdatePaint(SKPaint fillPaint, SKRect bounds)
		{
			if (Surface is SkiaCompositionSurface scs)
			{
				var imageShader = SKShader.CreateImage(scs.Image, SKShaderTileMode.Repeat, SKShaderTileMode.Repeat, TransformMatrix.ToSKMatrix());

				fillPaint.Shader = imageShader;

				fillPaint.IsAntialias = true;
			}
			else if (Surface is ISkiaSurface skiaSurface)
			{
				skiaSurface.UpdateSurface();

				if (skiaSurface.Surface is not null)
				{
					fillPaint.Shader = skiaSurface.Surface.Snapshot().ToShader(SKShaderTileMode.Repeat, SKShaderTileMode.Repeat, TransformMatrix.ToSKMatrix());
					fillPaint.IsAntialias = true;
				}
				else
					fillPaint.Shader = null;
			}
			else
			{
				fillPaint.Shader = null;
			}
		}

		void IOnlineBrush.Draw(in DrawingSession session, SKRect bounds)
		{
			if (Surface is ISkiaSurface skiaSurface)
				skiaSurface.UpdateSurface(session);
		}
	}
}
