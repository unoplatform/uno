#nullable enable

using System;
using System.IO;
using System.Numerics;
using Microsoft.UI.Composition;
using Windows.Foundation;
using Windows.Graphics.Effects;
using Windows.UI;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// A SkiaSharp-free <see cref="IDrawingBackend"/> factory built on the managed engines (geometry via
/// <see cref="ManagedPathBuilder"/>, decode via <see cref="ManagedImageDecoder"/>). Geometry and images are
/// neutral, so any renderer (WebGPU, a future managed rasterizer) can reuse this as its <see cref="IGraphicsBackend.Drawing"/>.
/// Shaders/effects/offscreen rasterization are still Skia-only and remain unimplemented here — a fully
/// Skia-less setup needs managed equivalents (deferred).
/// </summary>
public sealed class ManagedDrawingBackend : IDrawingBackend
{
	public IPathBuilder CreatePathBuilder() => new ManagedPathBuilder();

	public IPrimitiveGeometryBuilder CreatePrimitiveGeometryBuilder() => new ManagedPathBuilder();

	public IGeometry CreateRectangleGeometry(Rect rect)
	{
		var builder = new ManagedPathBuilder();
		builder.AddRectangle(rect);
		return builder.Build();
	}

	public IImage RenderOffscreen(int pixelWidth, int pixelHeight, Action<IDrawingSession> render)
		=> throw new NotImplementedException("Managed offscreen rasterization is not yet implemented; requires a managed rasterizer.");

	// The managed factory is a CPU-resource factory (geometry/decode); GPU textures are created by the
	// device-bound backend factory (e.g. WebGpuDrawingBackend), not here.
	public IImageTexture CreateImageTexture(IImage image)
		=> throw new NotSupportedException("ManagedDrawingBackend has no GPU device; use a device-bound backend factory to create textures.");

	public IShader CreateLinearGradientShader(Vector2 start, Vector2 end, Color[] colors, float[] colorPositions, GradientTileMode tileMode, Matrix3x2 localMatrix)
		=> throw new NotImplementedException("Managed gradient shaders are not yet implemented.");

	public IShader CreateRadialGradientShader(Vector2 center, Vector2 gradientOrigin, float radiusX, float radiusY, Color[] colors, float[] colorPositions, GradientTileMode tileMode, Matrix3x2 localMatrix)
		=> throw new NotImplementedException("Managed gradient shaders are not yet implemented.");

	public IColorFilter CreateBlendModeColorFilter(Color color, BlendMode mode)
		=> throw new NotImplementedException("Managed color filters are not yet implemented.");

	public IColorFilter CreateColorMatrixColorFilter(float[] matrix)
		=> throw new NotImplementedException("Managed color filters are not yet implemented.");

	public IEffectFilter? CreateEffectFilter(IGraphicsEffect effect, Rect bounds, Func<string, CompositionBrush?> sourceResolver, bool useBackdropBlurClamp, bool isSoftwareRenderer, out bool hasBackdropInput)
		=> throw new NotImplementedException("Managed effect filters are not yet implemented.");

	public IEffectFilter CreateDropShadowFilter(float dx, float dy, float sigmaX, float sigmaY, Color color)
		=> throw new NotImplementedException("Managed drop-shadow filters are not yet implemented.");
}
