#nullable enable

using System.Numerics;
using Uno.UI.Composition;
using Uno.UI.Composition.Drawing;
using SkiaSharp;
using Windows.Foundation;
using System.Diagnostics.CodeAnalysis;
using Uno.Extensions;

namespace Microsoft.UI.Composition
{
	public partial class CompositionSurfaceBrush : CompositionBrush, ISizedBrush
	{
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

		internal override bool TryPaint(IDrawingSession session, float opacity, Rect bounds)
		{
			if (bounds.Width <= 0 || bounds.Height <= 0)
			{
				return true;
			}

			if (Surface is ISkiaSurface skiaSurface)
			{
				session.Save();
				session.ClipRect(bounds, antialias: true);
				skiaSurface.Paint(session, opacity);
				session.Restore();
				return true;
			}

			if (!TryGetSkiaCompositionSurface(Surface, out var scs))
			{
				return false;
			}

			var skBounds = bounds.ToSKRect();
			var backgroundArea = GetArrangedImageRect(new Size(scs.Image!.Width, scs.Image.Height), skBounds);
			if (backgroundArea.Width <= 0 || backgroundArea.Height <= 0)
			{
				return true;
			}

			// See the Paint(SKCanvas) overload for the RelativeTransform/Transform ordering rationale.
			var matrix = Matrix3x2.Identity;
			matrix *= Matrix3x2.CreateScale((float)(backgroundArea.Width / scs.Image!.Width), (float)(backgroundArea.Height / scs.Image.Height));
			matrix *= Matrix3x2.CreateTranslation((float)backgroundArea.Left, (float)backgroundArea.Top);
			matrix *= TransformMatrix;
			matrix *= Matrix3x2.CreateScale(skBounds.Width, skBounds.Height).Inverse();
			matrix *= RelativeTransform;
			matrix *= Matrix3x2.CreateScale(skBounds.Width, skBounds.Height);

			IColorFilter? colorFilter;
			if (MonochromeColor is { } color)
			{
				var faded = global::Windows.UI.Color.FromArgb((byte)(color.Alpha * opacity), color.Red, color.Green, color.Blue);
				colorFilter = DrawingBackend.Current.CreateBlendModeColorFilter(faded, BlendMode.SrcIn);
			}
			else
			{
				colorFilter = DrawingBackend.Current.CreateOpacityColorFilter(opacity);
			}

			session.Save();
			session.Concat(new Matrix4x4(matrix));
			// Opaque paint colour: DrawImage modulates the image alpha by the paint colour's alpha, so a
			// transparent (default) colour would erase the image. RGB is ignored for image draws.
			session.DrawImage(new SkiaImage(scs.Image), 0, 0, ImageSampling.Linear, new PaintParams(global::Windows.UI.Colors.White) { IsAntialias = true, ColorFilter = colorFilter });
			session.Restore();
			return true;
		}
	}
}
