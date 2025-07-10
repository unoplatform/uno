#nullable enable

using System.Numerics;
using Uno.UI.Composition;
using SkiaSharp;
using Windows.Foundation;
using System.Diagnostics.CodeAnalysis;
using Uno.Extensions;

namespace Microsoft.UI.Composition
{
	public partial class CompositionSurfaceBrush : CompositionBrush, ISizedBrush
	{
		private static readonly SKPaint _tempPaint = new();
		private SKColor? _monochromeColor;

		internal SKColor? MonochromeColor
		{
			get => _monochromeColor;
			set => SetObjectProperty(ref _monochromeColor, value);
		}

		internal override bool RequiresRepaintOnEveryFrame => Surface is ISkiaSurface;

		Vector2? ISizedBrush.Size => Surface switch
		{
			SkiaCompositionSurface { Image: SKImage img } => new(img.Width, img.Height),
			ISkiaSurface skiaSurface => skiaSurface.Size,
			ISkiaCompositionSurfaceProvider { SkiaCompositionSurface: { Image: SKImage img } } => new(img.Width, img.Height),
			_ => null
		};

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

		internal override bool CanPaint() => TryGetSkiaCompositionSurface(Surface, out _) || Surface is ISkiaSurface;

		internal override void Paint(SKCanvas canvas, float opacity, SKRect bounds)
		{
			if (bounds.IsEmpty)
			{
				return;
			}

			if (Surface is ISkiaSurface skiaSurface)
			{
				canvas.ClipRect(bounds, antialias: true);
				skiaSurface.Paint(canvas, opacity);
				canvas.Restore();
			}
			else if (TryGetSkiaCompositionSurface(Surface, out var scs))
			{
				var backgroundArea = GetArrangedImageRect(new Size(scs.Image!.Width, scs.Image.Height), bounds);

				if (backgroundArea.Width <= 0 || backgroundArea.Height <= 0)
				{
					return;
				}

				// Relevant doc snippet from WPF: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/brush-transformation-overview#differences-between-the-transform-and-relativetransform-properties
				// When you apply a transform to a brush's RelativeTransform property, that transform is applied to the brush before its output is mapped to the painted area. The following list describes the order in which a brush’s contents are processed and transformed.
				//  * Process the brush’s contents. For a GradientBrush, this means determining the gradient area. For a TileBrush, the Viewbox is mapped to the Viewport. This becomes the brush’s output.
				// 	* Project the brush’s output onto the 1 x 1 transformation rectangle.
				// 	* Apply the brush’s RelativeTransform, if it has one.
				// 	* Project the transformed output onto the area to paint.
				// 	* Apply the brush’s Transform, if it has one.
				var matrix = Matrix3x2.Identity;
				matrix *= Matrix3x2.CreateScale((float)(backgroundArea.Width / scs.Image!.Width),
					(float)(backgroundArea.Height / scs.Image.Height));
				matrix *= Matrix3x2.CreateTranslation((float)backgroundArea.Left, (float)backgroundArea.Top);
				matrix *= TransformMatrix;
				matrix *= Matrix3x2.CreateScale(bounds.Width, bounds.Height).Inverse();
				matrix *= RelativeTransform;
				matrix *= Matrix3x2.CreateScale(bounds.Width, bounds.Height);

				_tempPaint.Reset();
				_tempPaint.IsAntialias = true;
				if (MonochromeColor is { } color)
				{
					_tempPaint.ColorFilter = SKColorFilter.CreateBlendMode(color.WithAlpha((byte)(color.Alpha * opacity)), SKBlendMode.SrcIn);
				}
				else
				{
					_tempPaint.ColorFilter = opacity.ToColorFilter();
				}

				canvas.Save();
				canvas.Concat(matrix.ToSKMatrix());
				// Ideally, we would use a CatmullRom sampler when upscaling (i.e. bounds.Size > scs.Image.Size) and
				// a Lanczos sampler when downscaling. However, profiling shows that CatmullRom chokes when the
				// drawing are (i.e. bounds) is large and the improvement over linear sampling is almost imperceptible.
				// For downsampling, Lanczos is slightly better than linear filtering with mipmapping but chokes when
				// the downscaling ratio is too big. Linear filtering with mipmapping is mostly okay but in the most
				// extreme cases with tons of images it's quite a bit slower than a linear filter without improving
				// the output that much.
				canvas.DrawImage(scs.Image, 0, 0, new SKSamplingOptions(SKFilterMode.Linear), _tempPaint);
				canvas.Restore();
			}
		}
	}
}
