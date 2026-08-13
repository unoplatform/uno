#nullable enable

using System;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Effects;
using Windows.UI;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// A drawing backend that mints geometry through the SkiaSharp-free <see cref="ManagedPathBuilder"/> while
/// delegating everything device-resident (textures, shaders, effects, offscreen rasterization) to the Skia
/// backend. The Skia renderer rasterizes the resulting <c>ManagedGeometry</c> via its neutral bridge. This is a
/// concrete example of registering a custom path implementor: <c>DrawingFactory.Register(new
/// SkiaManagedGeometryDrawingFactory())</c> — geometry comes from the managed engine, pixels from Skia.
/// </summary>
public sealed class SkiaManagedGeometryDrawingFactory : IDrawingFactory
{
	private readonly IDrawingFactory _skia = new SkiaDrawingFactory();

	public IPathBuilder CreatePathBuilder() => new ManagedPathBuilder();

	public IPrimitiveGeometryBuilder CreatePrimitiveGeometryBuilder() => new ManagedPathBuilder();

	public IImageTexture RenderOffscreen(int pixelWidth, int pixelHeight, Action<IDrawingSession> render)
		=> _skia.RenderOffscreen(pixelWidth, pixelHeight, render);

	public Task<IImage> SnapshotAsync(IImageTexture texture) => _skia.SnapshotAsync(texture);

	public IImageTexture CreateImageTexture(IImage image) => _skia.CreateImageTexture(image);

	public IShader CreateLinearGradientShader(Vector2 start, Vector2 end, Color[] colors, float[] colorPositions, GradientTileMode tileMode, Matrix3x2 localMatrix)
		=> _skia.CreateLinearGradientShader(start, end, colors, colorPositions, tileMode, localMatrix);

	public IShader CreateRadialGradientShader(Vector2 center, Vector2 gradientOrigin, float radiusX, float radiusY, Color[] colors, float[] colorPositions, GradientTileMode tileMode, Matrix3x2 localMatrix)
		=> _skia.CreateRadialGradientShader(center, gradientOrigin, radiusX, radiusY, colors, colorPositions, tileMode, localMatrix);

	public IColorFilter CreateBlendModeColorFilter(Color color, BlendMode mode) => _skia.CreateBlendModeColorFilter(color, mode);

	public IColorFilter CreateColorMatrixColorFilter(float[] matrix) => _skia.CreateColorMatrixColorFilter(matrix);

	public IEffectFilter? CreateEffectFilter(IGraphicsEffect effect, Rect bounds, Func<string, IEffectSource?> sourceResolver, bool useBackdropBlurClamp, bool isSoftwareRenderer, out bool hasBackdropInput)
		=> _skia.CreateEffectFilter(effect, bounds, sourceResolver, useBackdropBlurClamp, isSoftwareRenderer, out hasBackdropInput);

	public IEffectFilter CreateDropShadowFilter(float dx, float dy, float sigmaX, float sigmaY, Color color)
		=> _skia.CreateDropShadowFilter(dx, dy, sigmaX, sigmaY, color);
}
