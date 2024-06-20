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
		private static SKRuntimeEffect? _effect;
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
					matrix *= Matrix3x2.CreateScale((float)(backgroundArea.Width / scs.Image!.Width), (float)(backgroundArea.Height / scs.Image.Height));
					matrix *= Matrix3x2.CreateTranslation((float)backgroundArea.Left, (float)backgroundArea.Top);
					matrix *= TransformMatrix;
					matrix *= Matrix3x2.CreateScale(bounds.Width, bounds.Height).Inverse();
					matrix *= RelativeTransform;
					matrix *= Matrix3x2.CreateScale(bounds.Width, bounds.Height);

					// The image rescaling (i.e resampling) algorithm in the shader directly is really low quality (but really fast).
					// SkiaSharp 3 introduces new SKSamplingOptions, but for now there is no alternative to writing
					// the shader logic by hand.
					// When we move to SkiaSharp3, remove this
					// ******************************************************************************
					if (_effect is null)
					{
						_effect = SKRuntimeEffect.Create(ImageResamplingShader, out var error);
						if (error is { } e)
						{
							throw new InvalidOperationException(e);
						}
					}

					var uniforms = new SKRuntimeEffectUniforms(_effect)
					{
						{ "imageSize", new[] { (float)backgroundArea.Width, (float)backgroundArea.Height } }
					};
					var children = new SKRuntimeEffectChildren(_effect)
					{
						["image"] = SKShader.CreateImage(scs.Image, SKShaderTileMode.Decal, SKShaderTileMode.Decal)
					};

					var imageShader = _effect.ToShader(false, uniforms, children, matrix.ToSKMatrix());
					// ******************************************************************************
					// and replace with this one line (and maybe add mipmapping
					// var imageShader = SKShader.CreateImage(scs.Image, SKShaderTileMode.Decal, SKShaderTileMode.Decal, new SKSamplingOptions(SKCubicResampler.Mitchell), matrix.ToSKMatrix());

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

		// Handwritten bicubic resampling adapted from https://stackoverflow.com/a/42179924
		private const string ImageResamplingShader = @"
uniform shader image;
uniform vec2 imageSize;

vec4 cubic(float v) {
    vec4 n = vec4(1.0, 2.0, 3.0, 4.0) - v;
    vec4 s = n * n * n;
    float x = s.x;
    float y = s.y - 4.0 * s.x;
    float z = s.z - 4.0 * s.y + 6.0 * s.x;
    float w = 6.0 - x - y - z;
    return vec4(x, y, z, w) * (1.0/6.0);
}

vec4 main(vec2 texCoords){
	vec2 texSize = imageSize; // SkSL coords to GLSL coords
	texCoords = texCoords / texSize;
	vec2 invTexSize = 1.0 / texSize;

	texCoords = texCoords * texSize - 0.5;

	vec2 fxy = fract(texCoords);
	texCoords -= fxy;

	vec4 xcubic = cubic(fxy.x);
	vec4 ycubic = cubic(fxy.y);

	vec4 c = texCoords.xxyy + vec2(-0.5, +1.5).xyxy;

	vec4 s = vec4(xcubic.xz + xcubic.yw, ycubic.xz + ycubic.yw);
	vec4 offset = c + vec4 (xcubic.yw, ycubic.yw) / s;

	offset *= invTexSize.xxyy;
	offset *= texSize.xxyy; // GLSL coords to SkSL coords

	vec4 sample0 = sample(image, offset.xz);
	vec4 sample1 = sample(image, offset.yz);
	vec4 sample2 = sample(image, offset.xw);
	vec4 sample3 = sample(image, offset.yw);

	float sx = s.x / (s.x + s.y);
	float sy = s.z / (s.z + s.w);

	return mix(mix(sample3, sample2, sx), mix(sample1, sample0, sx), sy);
}
";
	}
}
