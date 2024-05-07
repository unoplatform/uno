#nullable enable

using System;
using System.Numerics;
using Uno.UI.Composition;
using SkiaSharp;
using Windows.Foundation;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.UI.Composition
{
	public partial class CompositionSurfaceBrush : CompositionBrush, IOnlineBrush, ISizedBrush
	{
		bool IOnlineBrush.IsOnline => Surface is ISkiaSurface;

		bool ISizedBrush.IsSized => true;

		Vector2? ISizedBrush.Size
		{
			get => Surface switch
			{
				SkiaCompositionSurface { Image: SKImage img } => new(img.Width, img.Height),
				ISkiaSurface { Surface: SKSurface surface } => new(surface.Canvas.DeviceClipBounds.Width, surface.Canvas.DeviceClipBounds.Height),
				ISkiaCompositionSurfaceProvider { SkiaCompositionSurface: { Image: SKImage img } } => new(img.Width, img.Height),
				_ => null
			};
		}

		private Rect GetArrangedImageRect(Size sourceSize, SKRect targetRect)
		{
			var size = GetArrangedImageSize(sourceSize, targetRect.Size.ToSize());

			var point = new Point(targetRect.Left, targetRect.Top);
			point.X += (targetRect.Width - size.Width) * HorizontalAlignmentRatio;
			point.Y += (targetRect.Height - size.Height) * VerticalAlignmentRatio;
			return new Rect(point, size);
		}

		private Size GetArrangedImageSize(Size sourceSize, Size targetSize)
		{
			var sourceAspectRatio = sourceSize.AspectRatio();
			var targetAspectRatio = targetSize.AspectRatio();
			switch (Stretch)
			{
				default:
				case CompositionStretch.None:
					return sourceSize;
				case CompositionStretch.Fill:
					return targetSize;
				case CompositionStretch.Uniform:
					return targetAspectRatio > sourceAspectRatio
						? new Size(sourceSize.Width * targetSize.Height / sourceSize.Height, targetSize.Height)
						: new Size(targetSize.Width, sourceSize.Height * targetSize.Width / sourceSize.Width);
				case CompositionStretch.UniformToFill:
					return targetAspectRatio < sourceAspectRatio
						? new Size(sourceSize.Width * targetSize.Height / sourceSize.Height, targetSize.Height)
						: new Size(targetSize.Width, sourceSize.Height * targetSize.Width / sourceSize.Width);
			}
		}

		private static bool TryGetSkiaCompositionSurface(ICompositionSurface? surface, [NotNullWhen(true)] out SkiaCompositionSurface? skiaCompositionSurface)
		{
			if (surface is SkiaCompositionSurface scs)
			{
				skiaCompositionSurface = scs;
				return true;
			}
			else if (surface is ISkiaCompositionSurfaceProvider scsp && scsp.SkiaCompositionSurface is SkiaCompositionSurface scsps)
			{
				skiaCompositionSurface = scsps;
				return true;
			}

			skiaCompositionSurface = null;
			return false;
		}

		internal override void UpdatePaint(SKPaint fillPaint, SKRect bounds)
		{
			if (TryGetSkiaCompositionSurface(Surface, out var scs))
			{
				var sourceImageSize = new Size(scs.Image!.Width, scs.Image.Height);
				var backgroundArea = GetArrangedImageRect(sourceImageSize, bounds);
				var matrix = Matrix3x2.CreateScale((float)(backgroundArea.Width / sourceImageSize.Width), (float)(backgroundArea.Height / sourceImageSize.Height));
				matrix *= Matrix3x2.CreateTranslation((float)backgroundArea.Left, (float)backgroundArea.Top);
				matrix *= TransformMatrix;

				var imageShader = SKShader.CreateImage(scs.Image, SKShaderTileMode.Decal, SKShaderTileMode.Decal, matrix.ToSKMatrix());

				if (UsePaintColorToColorSurface)
				{
					// use the set color instead of the image pixel values. This is what happens on WinUI.
					var blendedShader = SKShader.CreateColorFilter(imageShader, SKColorFilter.CreateBlendMode(fillPaint.Color, SKBlendMode.SrcIn));

					fillPaint.Shader = blendedShader;
				}
				else
				{
					fillPaint.Shader = imageShader;
				}

				fillPaint.IsAntialias = true;
				fillPaint.FilterQuality = SKFilterQuality.High;
			}
			else if (Surface is ISkiaSurface skiaSurface)
			{
				skiaSurface.UpdateSurface();

				if (skiaSurface.Surface is not null)
				{
					fillPaint.Shader = skiaSurface.Surface.Snapshot().ToShader(SKShaderTileMode.Repeat, SKShaderTileMode.Repeat, TransformMatrix.ToSKMatrix());
					fillPaint.IsAntialias = true;
					fillPaint.FilterQuality = SKFilterQuality.High;
					fillPaint.IsAutohinted = true;
					fillPaint.IsDither = true;
				}
				else
					fillPaint.Shader = null;
			}
			else
			{
				fillPaint.Shader = null;
			}
		}

		internal bool UsePaintColorToColorSurface { private get; set; }

		void IOnlineBrush.Draw(in DrawingSession session, SKRect bounds)
		{
			if (Surface is ISkiaSurface skiaSurface)
				skiaSurface.UpdateSurface(session);
		}
	}
}
