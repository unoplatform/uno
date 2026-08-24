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

		internal override bool RequiresRepaintOnEveryFrame => Surface is IPaintableSurface;

		Vector2? ISizedBrush.Size => Surface switch
		{
			CompositionImageSurface { Size: { } sz } => sz,
			IPaintableSurface skiaSurface => skiaSurface.Size,
			ICompositionImageSurfaceProvider { ImageSurface: { Size: { } sz } } => sz,
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

		private static bool TryGetCompositionImageSurface(ICompositionSurface? surface, [NotNullWhen(true)] out CompositionImageSurface? skiaCompositionSurface)
		{
			if (surface is CompositionImageSurface scs)
			{
				skiaCompositionSurface = scs;
				return true;
			}
			else if (surface is ICompositionImageSurfaceProvider scsp && scsp.ImageSurface is CompositionImageSurface scsps)
			{
				skiaCompositionSurface = scsps;
				return true;
			}

			skiaCompositionSurface = null;
			return false;
		}

		internal override bool CanPaint() => TryGetCompositionImageSurface(Surface, out _) || Surface is IPaintableSurface;

		internal override bool TryPaint(IDrawingSession session, float opacity, Rect bounds)
		{
			if (bounds.Width <= 0 || bounds.Height <= 0)
			{
				return true;
			}

			if (Surface is IPaintableSurface paintableSurface)
			{
				session.Save();
				session.ClipRect(bounds, antialias: true);
				paintableSurface.Paint(session, opacity, bounds);
				session.Restore();
				return true;
			}

			if (!TryGetCompositionImageSurface(Surface, out var scs))
			{
				return false;
			}

			// Size comes from the current frame's IImage or a directly-retained texture (SVG) — never dereference
			// Image, which is null for a texture-backed surface.
			if (scs.Size is not { } sourceSize)
			{
				return true;
			}

			var backgroundArea = GetArrangedImageRect(new Size(sourceSize.X, sourceSize.Y), bounds);
			if (backgroundArea.Width <= 0 || backgroundArea.Height <= 0)
			{
				return true;
			}

			// RelativeTransform is applied to the brush's output before it's mapped to the paint area;
			// Transform is applied after. (See the WPF brush-transform docs.)
			var matrix = Matrix3x2.Identity;
			matrix *= Matrix3x2.CreateScale((float)(backgroundArea.Width / sourceSize.X), (float)(backgroundArea.Height / sourceSize.Y));
			matrix *= Matrix3x2.CreateTranslation((float)backgroundArea.Left, (float)backgroundArea.Top);
			matrix *= TransformMatrix;
			matrix *= Matrix3x2.CreateScale((float)bounds.Width, (float)bounds.Height).Inverse();
			matrix *= RelativeTransform;
			matrix *= Matrix3x2.CreateScale((float)bounds.Width, (float)bounds.Height);

			if (scs.GetTexture() is not { } texture)
			{
				return true;
			}

			session.Save();
			session.Concat(new Matrix4x4(matrix));
			if (MonochromeColor is { } color)
			{
				// Recolor the image to a single tint (its coverage kept), with opacity folded into the tint alpha.
				var faded = global::Windows.UI.Color.FromArgb((byte)(color.A * opacity), color.R, color.G, color.B);
				var colorFilter = session.Factory.CreateBlendModeColorFilter(faded, BlendMode.SrcIn);
				session.DrawImage(texture, 0, 0, ImageSampling.Linear, colorFilter, antialias: true);
			}
			else
			{
				session.DrawImage(texture, 0, 0, ImageSampling.Linear, opacity: opacity, antialias: true);
			}
			session.Restore();
			return true;
		}
	}
}
