#nullable enable

using System;
using System.Numerics;
using Microsoft.UI.Composition;
using Windows.Foundation;
using Windows.Graphics.Effects;
using Windows.UI;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Entry point for a pluggable 2D drawing backend. The default implementation is backed by SkiaSharp
/// (<see cref="SkiaDrawingBackend"/>); a host or experiment can supply an alternative via
/// <see cref="DrawingBackend.Register"/>.
/// </summary>
/// <remarks>
/// This is the resource-factory half of the abstraction: it manufactures the stateful handles
/// (geometry today; images, typefaces and shaders later) that cross the backend boundary. Transient draw
/// configuration (paint) is passed inline on the drawing-session verbs instead of being manufactured here.
/// </remarks>
internal interface IDrawingBackend
{
	/// <summary>Creates a builder used to construct an <see cref="IGeometry"/>.</summary>
	IPathBuilder CreatePathBuilder();

	/// <summary>Creates a rectangular geometry.</summary>
	IGeometry CreateRectangleGeometry(Rect rect);

	/// <summary>
	/// Renders <paramref name="render"/> into a fresh transparent offscreen image of the given pixel size
	/// and returns it (e.g. to rasterize a brush before nine-slicing it).
	/// </summary>
	IImage RenderOffscreen(int pixelWidth, int pixelHeight, Action<IDrawingSession> render);

	/// <summary>Creates a linear-gradient shader in the current coordinate space.</summary>
	IShader CreateLinearGradientShader(
		Vector2 start,
		Vector2 end,
		Color[] colors,
		float[] colorPositions,
		GradientTileMode tileMode,
		Matrix3x2 localMatrix);

	/// <summary>Creates a radial-gradient shader in the current coordinate space.</summary>
	IShader CreateRadialGradientShader(
		Vector2 center,
		float radius,
		Color[] colors,
		float[] colorPositions,
		GradientTileMode tileMode,
		Matrix3x2 localMatrix);

	/// <summary>Creates a two-point conical (radial) gradient shader between two circles.</summary>
	IShader CreateTwoPointConicalGradientShader(
		Vector2 start,
		float startRadius,
		Vector2 end,
		float endRadius,
		Color[] colors,
		float[] colorPositions,
		GradientTileMode tileMode,
		Matrix3x2 localMatrix);

	/// <summary>Creates a shader that paints a single solid color everywhere.</summary>
	IShader CreateColorShader(Color color);

	/// <summary>Composes two shaders, drawing <paramref name="inner"/> over <paramref name="outer"/>.</summary>
	IShader ComposeShaders(IShader outer, IShader inner);

	/// <summary>Creates a color filter that multiplies alpha by <paramref name="opacity"/>, or null when it would be a no-op.</summary>
	IColorFilter? CreateOpacityColorFilter(float opacity);

	/// <summary>Creates a color filter that blends <paramref name="color"/> onto the source using <paramref name="mode"/>.</summary>
	IColorFilter CreateBlendModeColorFilter(Color color, BlendMode mode);

	/// <summary>Creates a color filter from a 4x5 row-major color matrix (as used by grayscale/alpha-mask effects).</summary>
	IColorFilter CreateColorMatrixColorFilter(float[] matrix);

	/// <summary>Creates a normal (Gaussian) blur mask filter with the given standard deviation.</summary>
	IMaskFilter CreateBlurMaskFilter(float sigma);

	/// <summary>
	/// Realizes a neutral <see cref="IGraphicsEffect"/> graph into an opaque backend effect. Mirrors the
	/// public <c>CompositionEffectBrush</c> graph rather than any backend-specific representation.
	/// </summary>
	/// <param name="effect">The root of the effect graph to realize.</param>
	/// <param name="bounds">The bounds the effect is generated for.</param>
	/// <param name="sourceResolver">Maps an effect source-parameter name to its bound input brush, or null.</param>
	/// <param name="useBackdropBlurClamp">Clamps backdrop blurs to the element's area (prevents edge bleeding).</param>
	/// <param name="isSoftwareRenderer">Whether the compositor is currently using a software renderer.</param>
	/// <param name="hasBackdropInput">Set to true when the graph references a backdrop brush.</param>
	/// <returns>The realized effect, or null when the graph resolves to nothing renderable.</returns>
	IEffectFilter? CreateEffectFilter(
		IGraphicsEffect effect,
		Rect bounds,
		Func<string, CompositionBrush?> sourceResolver,
		bool useBackdropBlurClamp,
		bool isSoftwareRenderer,
		out bool hasBackdropInput);

	/// <summary>
	/// Creates a drop-shadow filter (offset + blur + color) used to derive a shadow from arbitrary rendered
	/// content via <see cref="IDrawingSession.SaveLayer(IEffectFilter)"/>.
	/// </summary>
	IEffectFilter CreateDropShadowFilter(float dx, float dy, float sigmaX, float sigmaY, Color color);
}
