#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Entry point for a pluggable 2D drawing backend. The default implementation is backed by SkiaSharp
/// (<see cref="SkiaDrawingFactory"/>); a host or experiment can supply an alternative via
/// <see cref="DrawingFactory.Register"/>.
/// </summary>
/// <remarks>
/// This is the device-bound resource half of the abstraction: it manufactures the stateful handles that cross the
/// backend boundary and need the GPU/pixel device — images, shaders and effect filters. Transient draw configuration
/// (paint) is passed inline on the drawing-session verbs instead of being manufactured here. The backend-independent
/// seams live separately: geometry (<see cref="GeometryFactory"/>), image decoding (<see cref="ImageDecoder"/>) and
/// font resolution (<see cref="FontProvider"/>). The render backend consumes the neutral <see cref="IGeometry"/>
/// those produce, runtime-checking for the concrete types it knows to take a fast path.
/// </remarks>
public interface IDrawingFactory
{
	/// <summary>
	/// Renders <paramref name="render"/> into a fresh transparent offscreen target of the given pixel size and
	/// returns it as a backend-resident <see cref="IImageTexture"/> — the same currency the draw verbs consume
	/// (<see cref="IDrawingSession.DrawImage"/>), so the result is sampled directly with no CPU round-trip. The
	/// caller owns the returned texture and disposes it. To read the pixels back to the CPU (e.g.
	/// RenderTargetBitmap), use <see cref="SnapshotAsync"/> — the only genuinely-async operation in the seam,
	/// because a GPU→CPU read cannot block the browser's single JS thread.
	/// </summary>
	IImageTexture RenderOffscreen(int pixelWidth, int pixelHeight, Action<IDrawingSession> render);

	/// <summary>
	/// Reads <paramref name="texture"/>'s pixels back to a neutral CPU <see cref="IImage"/> (BGRA8888 premultiplied).
	/// Async because on a GPU backend the readback completes only when control yields to the event loop (on WASM a
	/// synchronous poll would hang). The texture must have been produced by this factory; a foreign texture throws.
	/// </summary>
	Task<IImage> SnapshotAsync(IImageTexture texture);

	/// <summary>
	/// Uploads a neutral <see cref="IImage"/>'s pixels into a backend-specific GPU texture (see
	/// <see cref="IImageTexture"/>). Done once; the caller owns and disposes the result. This is the "store"
	/// half of images — decoding (neutral pixels) is separate, and lives in <see cref="IImageDecoder"/>.
	/// </summary>
	IImageTexture CreateImageTexture(IImage image);

	/// <summary>Creates a linear-gradient shader in the current coordinate space.</summary>
	IShader CreateLinearGradientShader(
		Vector2 start,
		Vector2 end,
		Color[] colors,
		float[] colorPositions,
		GradientTileMode tileMode,
		Matrix3x2 localMatrix);

	/// <summary>
	/// Creates a radial-gradient shader with the full WinUI parameters — per-axis radius and a possibly-offset
	/// <paramref name="gradientOrigin"/>. The backend internalizes any focal/anisotropy technique.
	/// </summary>
	IShader CreateRadialGradientShader(
		Vector2 center,
		Vector2 gradientOrigin,
		float radiusX,
		float radiusY,
		Color[] colors,
		float[] colorPositions,
		GradientTileMode tileMode,
		Matrix3x2 localMatrix);

	/// <summary>Creates a color filter that blends <paramref name="color"/> onto the source using <paramref name="mode"/>.</summary>
	IColorFilter CreateBlendModeColorFilter(Color color, BlendMode mode);

	/// <summary>Creates a color filter from a 4x5 row-major color matrix (as used by grayscale/alpha-mask effects).</summary>
	IColorFilter CreateColorMatrixColorFilter(float[] matrix);

	/// <summary>
	/// Fuses a neutral <see cref="EffectNode"/> tree (produced by Uno's parser — brush inputs already rasterized to
	/// <see cref="TextureInput"/>, the backdrop left as <see cref="SourceInput"/>) into an opaque backend
	/// <see cref="IEffectFilter"/>. The backend combines the whole tree into one native filter (e.g. a single
	/// <c>SKImageFilter</c> DAG) so non-separable blends over the backdrop stay one fused operation. Returns null when
	/// the backend can't realize the tree as a filter (the caller then falls back to the recipe path).
	/// </summary>
	/// <param name="tree">The neutral effect tree to fuse.</param>
	/// <param name="bounds">The bounds the effect is generated for (clamps the backdrop; places rasterized inputs).</param>
	IEffectFilter? CreateEffectFilter(EffectNode tree, Rect bounds);

	/// <summary>
	/// Creates a drop-shadow filter (offset + blur + color) used to derive a shadow from arbitrary rendered
	/// content via <see cref="IDrawingSession.SaveLayer(IEffectFilter)"/>.
	/// </summary>
	IEffectFilter CreateDropShadowFilter(float dx, float dy, float sigmaX, float sigmaY, Color color);
}
