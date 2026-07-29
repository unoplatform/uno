#nullable enable

using System.Numerics;
using System.Runtime.CompilerServices;
using Microsoft.UI.Composition;
using SkiaSharp;
using Windows.Foundation;
using Windows.UI;

namespace Uno.UI.Composition.Drawing;

/// <summary>The default <see cref="IDrawingBackend"/>, backed by SkiaSharp.</summary>
internal sealed class SkiaDrawingBackend : IDrawingBackend
{
	// Self-registers when this assembly loads, so the core never news-up a concrete backend. When the
	// Skia backend becomes a separate assembly, this travels with it and registers on that assembly's load.
	[ModuleInitializer]
	internal static void Register() => DrawingBackend.Register(new SkiaDrawingBackend());

	// Opt-in switch to build geometry through the SkiaSharp-free ManagedGeometry engine instead of SKPath,
	// selected via DrawingBackendOptions.UseManagedGeometry at init.
	private static bool _useManagedGeometry => DrawingBackendOptions.UseManagedGeometry;

	private readonly SkiaFontManager _skiaFontManager = new();

	// Uses the host-provided font resolver if one was set, else the default Skia resolver.
	public IFontManager FontManager => DrawingBackendOptions.FontManager ?? _skiaFontManager;

	public IPathBuilder CreatePathBuilder() => _useManagedGeometry ? new ManagedPathBuilder() : new SkiaPathBuilder();

	public IPrimitiveGeometryBuilder CreatePrimitiveGeometryBuilder() => _useManagedGeometry ? new ManagedPathBuilder() : new SkiaPathBuilder();

	public IGeometry CreateRectangleGeometry(Rect rect)
	{
		if (_useManagedGeometry)
		{
			var managed = new ManagedPathBuilder();
			managed.AddRectangle(rect);
			return managed.Build();
		}

		var builder = new SKPathBuilder();
		builder.AddRect(rect.ToSKRect());
		return new SkiaGeometrySource2D(builder.Detach());
	}

	public IImage RenderOffscreen(int pixelWidth, int pixelHeight, System.Action<IDrawingSession> render)
	{
		var info = new SKImageInfo(pixelWidth, pixelHeight, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
		using var surface = SKSurface.Create(info);
		surface.Canvas.Clear(SKColors.Transparent);
		render(new SkiaDrawingSession(surface.Canvas));
		// Snapshot detaches from the surface (copy-on-write), so the returned image outlives it.
		return new SkiaImage(surface.Snapshot());
	}

	public bool TryDecodeImage(System.IO.Stream stream, int? targetWidth, int? targetHeight, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out IImageFrames? frames)
	{
		if (ManagedImageDecoder.Enabled)
		{
			// Try the SkiaSharp-free decoder first; buffer the bytes so we can still fall back to the codec.
			var bytes = ReadAllBytes(stream);
			if (ManagedImageDecoder.TryDecode(bytes, targetWidth, targetHeight, out var decoded))
			{
				frames = ToImageFrames(decoded);
				return true;
			}

			stream = new System.IO.MemoryStream(bytes, writable: false);
		}

		if (SkiaImageDecoder.TryDecode(stream, targetWidth, targetHeight, out var skiaFrames))
		{
			frames = skiaFrames;
			return true;
		}

		frames = null;
		return false;
	}

	private static byte[] ReadAllBytes(System.IO.Stream stream)
	{
		if (stream is System.IO.MemoryStream ms)
		{
			return ms.ToArray();
		}

		using var buffer = new System.IO.MemoryStream();
		stream.CopyTo(buffer);
		return buffer.ToArray();
	}

	private static SkiaImageFrames ToImageFrames(DecodedImage decoded)
	{
		var info = new SKImageInfo(decoded.Width, decoded.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
		var images = new SKImage[decoded.Frames.Length];
		for (var i = 0; i < images.Length; i++)
		{
			images[i] = SKImage.FromPixelCopy(info, decoded.Frames[i]);
		}

		return new SkiaImageFrames(images, decoded.DurationsMs);
	}

	public IImageFrames CreateImageFrame(int pixelWidth, int pixelHeight, System.ReadOnlySpan<byte> bgraPremul)
	{
		var info = new SKImageInfo(pixelWidth, pixelHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
		return SkiaImageFrames.FromImage(SKImage.FromPixelCopy(info, bgraPremul));
	}

	public IImageFrames CreateImageFrames(IImage image) => SkiaImageFrames.FromImage(((SkiaImage)image).Image);

	public IImageTexture CreateImageTexture(IImage image)
	{
		var info = new SKImageInfo(image.PixelWidth, image.PixelHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
		var pixels = new byte[image.PixelWidth * image.PixelHeight * 4];
		image.CopyPixels(pixels);
		return new SkiaImageTexture(SKImage.FromPixelCopy(info, pixels));
	}

	public IShader CreateLinearGradientShader(
		Vector2 start,
		Vector2 end,
		Color[] colors,
		float[] colorPositions,
		GradientTileMode tileMode,
		Matrix3x2 localMatrix)
	{
		var shader = SKShader.CreateLinearGradient(
			new SKPoint(start.X, start.Y),
			new SKPoint(end.X, end.Y),
			ToSKColors(colors),
			colorPositions,
			ToSK(tileMode),
			localMatrix.ToSKMatrix());

		return new SkiaShader(shader);
	}

	public IShader CreateRadialGradientShader(
		Vector2 center,
		Vector2 gradientOrigin,
		float radiusX,
		float radiusY,
		Color[] colors,
		float[] colorPositions,
		GradientTileMode tileMode,
		Matrix3x2 localMatrix)
	{
		// SkiaSharp radial gradients take a single radius, so squash the larger axis onto the smaller with a
		// scale-down matrix and use one radius.
		ComputeRadiusAndScale(center, radiusX, radiusY, out var radius, out var squash);

		if (radius <= 0)
		{
			// Radius 0: match the last gradient color everywhere.
			return new SkiaShader(SKShader.CreateColor(LastColor(colors).ToSKColor()));
		}

		// The scale-down matrix is applied before the brush transform (SKMatrix.PreConcat), which in the
		// row-vector convention is `squash * localMatrix`.
		var totalMatrix = squash * localMatrix;
		var skTotal = totalMatrix.ToSKMatrix();
		var skTile = ToSK(tileMode);

		if (center == gradientOrigin)
		{
			return new SkiaShader(SKShader.CreateRadialGradient(
				new SKPoint(center.X, center.Y), radius, ToSKColors(colors), colorPositions, skTile, skTotal));
		}

		// Offset origin: SkiaSharp has no focal radial gradient, so approximate with a two-point conical
		// gradient (reversed stops) composed over the last color, which fills the region the conical leaves
		// uncovered.
		var reversedColors = new SKColor[colors.Length];
		for (var i = 0; i < colors.Length; i++)
		{
			reversedColors[i] = colors[colors.Length - 1 - i].ToSKColor();
		}

		var reversedPositions = new float[colorPositions.Length];
		for (var i = 0; i < colorPositions.Length; i++)
		{
			var p = colorPositions[i];
			reversedPositions[i] = (p > 0 && p < 1) ? System.Math.Abs(1 - p) : p;
		}

		Matrix3x2.Invert(totalMatrix, out var inverse);
		var origin = Vector2.Transform(gradientOrigin, inverse);

		var conical = SKShader.CreateTwoPointConicalGradient(
			new SKPoint(center.X, center.Y), radius, new SKPoint(origin.X, origin.Y), 0,
			reversedColors, reversedPositions, skTile, skTotal);
		var fallback = SKShader.CreateColor(LastColor(colors).ToSKColor());
		return new SkiaShader(SKShader.CreateCompose(fallback, conical));
	}

	private static Color LastColor(Color[] colors) => colors.Length > 0 ? colors[^1] : Colors.Transparent;

	// SkiaSharp doesn't allow explicit RadiusX/RadiusY on a radial gradient, so we build a scale-down
	// transform that squashes the larger axis onto the smaller and use a single radius.
	private static void ComputeRadiusAndScale(Vector2 center, float radiusX, float radiusY, out float radius, out Matrix3x2 matrix)
	{
		matrix = Matrix3x2.Identity;
		if (radiusX == 0 || radiusY == 0)
		{
			// Handle this specific case as zero division would cause us troubles.
			radius = 0;
			return;
		}

		if (radiusX >= radiusY)
		{
			// radiusX is larger, use it and scale down radiusY.
			radius = radiusX;
			var scaleDownRatio = radiusY / radiusX;
			matrix = new Matrix3x2(1, 0, 0, scaleDownRatio, 0, center.Y - scaleDownRatio * center.Y);
		}
		else
		{
			// radiusY is larger, use it and scale down radiusX.
			radius = radiusY;
			var scaleDownRatio = radiusX / radiusY;
			matrix = new Matrix3x2(scaleDownRatio, 0, 0, 1, center.X - scaleDownRatio * center.X, 0);
		}
	}

	private static SKColor[] ToSKColors(Color[] colors)
	{
		var skColors = new SKColor[colors.Length];
		for (var i = 0; i < colors.Length; i++)
		{
			skColors[i] = colors[i].ToSKColor();
		}
		return skColors;
	}

	public IColorFilter CreateBlendModeColorFilter(Color color, BlendMode mode)
		=> new SkiaColorFilter(SKColorFilter.CreateBlendMode(color.ToSKColor(), SkiaDrawingSession.ToSKBlendMode(mode)));

	public IColorFilter CreateColorMatrixColorFilter(float[] matrix)
		=> new SkiaColorFilter(SKColorFilter.CreateColorMatrix(matrix));

	public IEffectFilter? CreateEffectFilter(
		global::Windows.Graphics.Effects.IGraphicsEffect effect,
		Rect bounds,
		System.Func<string, CompositionBrush?> sourceResolver,
		bool useBackdropBlurClamp,
		bool isSoftwareRenderer,
		out bool hasBackdropInput)
	{
		var factory = new SkiaEffectFactory(sourceResolver, useBackdropBlurClamp, isSoftwareRenderer);
		var filter = factory.GenerateEffectFilter(effect, bounds.ToSKRect());
		hasBackdropInput = factory.HasBackdropBrushInput;
		return filter is null ? null : new SkiaEffectFilter(filter);
	}

	public IEffectFilter CreateDropShadowFilter(float dx, float dy, float sigmaX, float sigmaY, Color color)
		=> new SkiaEffectFilter(SKImageFilter.CreateOffset(dx, dy, SKImageFilter.CreateCompose(
			SKImageFilter.CreateBlur(sigmaX, sigmaY),
			SKImageFilter.CreateColorFilter(SKColorFilter.CreateBlendMode(color.ToSKColor(), SKBlendMode.Modulate)))));

	private static SKShaderTileMode ToSK(GradientTileMode mode) => mode switch
	{
		GradientTileMode.Repeat => SKShaderTileMode.Repeat,
		GradientTileMode.Mirror => SKShaderTileMode.Mirror,
		_ => SKShaderTileMode.Clamp,
	};
}
