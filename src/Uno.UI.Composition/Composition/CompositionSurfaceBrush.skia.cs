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

		internal override bool RequiresRepaintOnEveryFrame => ((IOnlineBrush)this).IsOnline;

		Vector2? ISizedBrush.Size => Surface switch
		{
			SkiaCompositionSurface { Image: SKImage img } => new(img.Width, img.Height),
			ISkiaSurface { Surface: SKSurface surface } => new(surface.Canvas.DeviceClipBounds.Width / surface.Canvas.TotalMatrix.ScaleX, surface.Canvas.DeviceClipBounds.Height / surface.Canvas.TotalMatrix.ScaleY),
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

		internal override bool CanPaint() => TryGetSkiaCompositionSurface(Surface, out _) || (Surface as ISkiaSurface)?.Surface is not null;

		internal override void UpdatePaint(SKPaint fillPaint, SKRect bounds)
		{
			if (bounds.IsEmpty)
			{
				return;
			}

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

					SKShader imageShader;
					var scaleX = scs.Image.Width / bounds.Width;
					var scaleY = scs.Image.Height / bounds.Height;
					if (scaleX < 3 || scaleY < 3)
					{
						imageShader = SKShader.CreateImage(scs.Image, SKShaderTileMode.Decal, SKShaderTileMode.Decal, new SKSamplingOptions(SKCubicResampler.CatmullRom), matrix.ToSKMatrix());
					}
					else
					{
						// Severe Downsampling : we use our own downsampler instead of Catmull-Rom which is absolutely
						// terrible when downsampling. We can't use SKImageFilters because they would apply
						// after resizing, so we're forced to write the shader manually.
						if (_effect is null)
						{
							_effect = SKRuntimeEffect.CreateShader(ImageDownsamplingShader, out var error);
							if (error is { } e)
							{
								throw new InvalidOperationException(e);
							}
						}

						// var nShaderApplications = Math.Max(sigmaX, sigmaY) / ;

						var uniforms1 = new SKRuntimeEffectUniforms(_effect)
						{
							{ "imageSize", new[] { (float)backgroundArea.Width, (float)backgroundArea.Height } },
							{ "scale", scaleX },
							{ "stepSize", new[] { 1.0f, 0 } }
						};
						var uniforms2 = new SKRuntimeEffectUniforms(_effect)
						{
							{ "imageSize", new[] { (float)backgroundArea.Width, (float)backgroundArea.Height } },
							{ "scale", scaleY },
							{ "stepSize", new[] { 0, 1.0f } }
						};
						var children = new SKRuntimeEffectChildren(_effect)
						{
							["image"] = SKShader.CreateImage(scs.Image, SKShaderTileMode.Decal, SKShaderTileMode.Decal)
						};

						imageShader = SKShader.CreateCompose(_effect.ToShader(uniforms1, children, matrix.ToSKMatrix()), _effect.ToShader(uniforms2, children, matrix.ToSKMatrix()));
					}

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
				}
			}
			else if (Surface is ISkiaSurface skiaSurface)
			{
				skiaSurface.UpdateSurface();

				if (skiaSurface.Surface is not null)
				{
					var matrix = TransformMatrix * Matrix3x2.CreateScale(1 / skiaSurface.Surface.Canvas.TotalMatrix.ScaleX);
					var image = skiaSurface.Surface.Snapshot();

					fillPaint.Shader = SKShader.CreateImage(image, SKShaderTileMode.Decal, SKShaderTileMode.Decal, matrix.ToSKMatrix());
					fillPaint.IsAntialias = true;
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

		// A Lanczor resampling shader for downsampling
		private const string ImageDownsamplingShader =
			"""
			uniform shader image;
			uniform vec2 imageSize;
			uniform float scale;
			uniform vec2 stepSize;

			const float a = 2;
			const float PI = 3.14159265359;
			
			float lanczos(float x, float a) {
				if (abs(x) < 1e-6) {
					return 1;
				}
				float piX = PI * x;
				float pi2X2 = pow(piX, 2);
				return a * sin(piX) * sin(piX / a) / pi2X2;
			}
			
			vec4 main(vec2 texCoords){
				// i = 0
				float totalWeight = 1; // lanczos(0, a)
				vec4 finalColor = image.eval(texCoords) * totalWeight;
				
				// we need this combination of const loop parameters + check-and-break
				// to mimick variable-length loops which are not supported by SKSL
				// since on some backends loops are actually unrolled at compile time.
				for (float i = 1; i < 20; i++) {
					if (i > scale * a) {
						break;
					}
					
					float weight = lanczos(i / scale, a);
					finalColor += (image.eval(texCoords + i * stepSize) + image.eval(texCoords - i * stepSize)) * weight;
					totalWeight += 2 * weight;
				}
				
				return finalColor / totalWeight;
			}
			""";
	}
}
