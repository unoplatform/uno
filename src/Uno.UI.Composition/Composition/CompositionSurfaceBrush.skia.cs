#nullable enable

using System.Numerics;
using Uno.UI.Composition;
using Uno.UI.Composition.Drawing;
using Windows.Foundation;
using System.Diagnostics.CodeAnalysis;
using Uno.Extensions;

namespace Microsoft.UI.Composition
{
	public partial class CompositionSurfaceBrush : CompositionBrush, ISizedBrush
	{
		private global::Windows.UI.Color? _monochromeColor;

		internal global::Windows.UI.Color? MonochromeColor
		{
			get => _monochromeColor;
			set => SetObjectProperty(ref _monochromeColor, value);
		}

		internal override bool RequiresRepaintOnEveryFrame => Surface is ISkiaSurface;

		Vector2? ISizedBrush.Size => Surface switch
		{
			SkiaCompositionSurface { Image: { } img } => new(img.PixelWidth, img.PixelHeight),
			ISkiaSurface skiaSurface => skiaSurface.Size,
			ISkiaCompositionSurfaceProvider { SkiaCompositionSurface: { Image: { } img } } => new(img.PixelWidth, img.PixelHeight),
			_ => null
		};

		private Rect GetArrangedImageRect(Size sourceSize, Rect targetRect)
		{
			var size = GetArrangedImageSize(sourceSize, new Size(targetRect.Width, targetRect.Height));

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

			var backgroundArea = GetArrangedImageRect(new Size(scs.Image!.PixelWidth, scs.Image.PixelHeight), bounds);
			if (backgroundArea.Width <= 0 || backgroundArea.Height <= 0)
			{
				return true;
			}

			// RelativeTransform is applied to the brush's output before it's mapped to the paint area;
			// Transform is applied after. (See the WPF brush-transform docs.)
			var matrix = Matrix3x2.Identity;
			matrix *= Matrix3x2.CreateScale((float)(backgroundArea.Width / scs.Image!.PixelWidth), (float)(backgroundArea.Height / scs.Image.PixelHeight));
			matrix *= Matrix3x2.CreateTranslation((float)backgroundArea.Left, (float)backgroundArea.Top);
			matrix *= TransformMatrix;
			matrix *= Matrix3x2.CreateScale((float)bounds.Width, (float)bounds.Height).Inverse();
			matrix *= RelativeTransform;
			matrix *= Matrix3x2.CreateScale((float)bounds.Width, (float)bounds.Height);

			IColorFilter? colorFilter;
			if (MonochromeColor is { } color)
			{
				var faded = global::Windows.UI.Color.FromArgb((byte)(color.A * opacity), color.R, color.G, color.B);
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
			session.DrawImage(scs.Image, 0, 0, ImageSampling.Linear, new PaintParams(global::Windows.UI.Colors.White) { IsAntialias = true, ColorFilter = colorFilter });
			session.Restore();
			return true;
		}
	}
}
