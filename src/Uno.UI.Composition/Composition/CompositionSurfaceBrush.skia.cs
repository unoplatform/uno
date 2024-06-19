#nullable enable

using System;
using System.Numerics;
using Uno.UI.Composition;
using SkiaSharp;
using Windows.Foundation;
using System.Diagnostics.CodeAnalysis;
using Windows.Graphics.Display;
using Uno.Extensions;

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
				var backgroundArea = GetArrangedImageRect(new Size(scs.Image!.Width, scs.Image.Height), bounds);

				if (backgroundArea.Width <= 0 || backgroundArea.Height <= 0)
				{
					fillPaint.Shader = null;
				}
				else
				{
					// Relevant doc snippet from WPF: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/brush-transformation-overview#differences-between-the-transform-and-relativetransform-properties
					// When you apply a transform to a brush's RelativeTransform property, that transform is applied to the brush before its output is mapped to the painted area. The following list describes the order in which a brush’s contents are processed and transformed.
					//  * Process the brush’s contents. For a GradientBrush, this means determining the gradient area. For a TileBrush, the Viewbox is mapped to the Viewport. This becomes the brush’s output.
					// 	* Project the brush’s output onto the 1 x 1 transformation rectangle.
					// 	* Apply the brush’s RelativeTransform, if it has one.
					// 	* Project the transformed output onto the area to paint.
					// 	* Apply the brush’s Transform, if it has one.
					var matrix = Matrix3x2.Identity;
					matrix *= Matrix3x2.CreateTranslation((float)backgroundArea.Left, (float)backgroundArea.Top);
					matrix *= TransformMatrix;
					matrix *= Matrix3x2.CreateScale(bounds.Width, bounds.Height).Inverse();
					matrix *= RelativeTransform;
					matrix *= Matrix3x2.CreateScale(bounds.Width, bounds.Height);

					// The brush is not tied to a specific window, so we can't get the scaling of a specific XamlRoot and
					// instead we settle for the main window's scaling which should hopefully be the same.
					var scale = DisplayInformation.GetForCurrentViewSafe().LogicalDpi / DisplayInformation.BaseDpi;
					// We scale the backgroundArea with the dpi scaling and then scale with 1 / dpi scaling in the shader,
					// This is conceptually a noop, but it actually affects quality. For example, if you have a hi-res image
					// that should be drawn in a 500x500 rectangle. If the scaling is 2x, it will actually be drawn in a
					// 1000x1000 rectangle. We don't want to resize the image to 500x500 and then stretch it to 1000x1000.
					// Instead, we resize it to 1000x1000 directly and counteract the global 2x scaling done by the
					// renderer by telling the shader to scale by 1/2.
					backgroundArea = backgroundArea with { Width = backgroundArea.Width * scale, Height = backgroundArea.Height * scale };
					matrix *= Matrix3x2.CreateScale(1 / scale);

					// Adding image downscaling in the shader matrix directly is very blurry
					// since the default downsampler in Skia is really low quality (but really fast).
					// We force Lanczos instead.
					// https://github.com/mono/SkiaSharp/issues/520#issuecomment-444973518
#pragma warning disable CS0612 // Type or member is obsolete
					var resizedBitmap = scs.Image.ToSKBitmap().Resize(scs.Image.Info.WithSize((int)backgroundArea.Size.Width, (int)backgroundArea.Size.Height), SKBitmapResizeMethod.Lanczos3.ToFilterQuality());
#pragma warning restore CS0612 // Type or member is obsolete
					var resizedImage = SKImage.FromBitmap(resizedBitmap);

					var imageShader = SKShader.CreateImage(resizedImage, SKShaderTileMode.Decal, SKShaderTileMode.Decal, matrix.ToSKMatrix());

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
				{
					fillPaint.Shader = null;
				}
			}
			else
			{
				fillPaint.Shader = null;
			}
		}

		internal bool UsePaintColorToColorSurface { private get; set; }

		void IOnlineBrush.Paint(in Visual.PaintingSession session, SKRect bounds)
		{
			if (Surface is ISkiaSurface skiaSurface)
				skiaSurface.UpdateSurface(session);
		}
	}
}
