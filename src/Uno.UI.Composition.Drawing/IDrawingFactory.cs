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
/// Entry point for a pluggable 2D drawing backend, supplied via <see cref="DrawingFactory.Register"/>. The
/// framework registers a default backend when a host sets none; this interface names no specific one.
/// </summary>
/// <remarks>
/// This is the device-bound resource half of the abstraction: it manufactures the stateful handles that cross the
/// backend boundary and need the GPU/pixel device — images, shaders and effect filters. Transient draw configuration
/// (paint) is passed inline on the drawing-session verbs instead of being manufactured here. The backend-independent
/// seams live separately: geometry (<see cref="GeometryFactory"/>), image decoding (<see cref="ImageEncoderDecoder"/>) and
/// font resolution (<see cref="FontProvider"/>). The render backend consumes the neutral <see cref="IGeometry"/>
/// those produce, runtime-checking for the concrete types it knows to take a fast path.
/// </remarks>
public interface IDrawingFactory
{
	/// <summary>
	/// Renders <paramref name="render"/> into a fresh transparent offscreen target of the given pixel size and
	/// returns it as a backend-resident <see cref="ITexture"/> — the same currency the draw verbs consume
	/// (<see cref="IDrawingSession.DrawImage"/>), so the result is sampled directly with no CPU round-trip. The
	/// caller owns the returned texture and disposes it. To read the pixels back to the CPU (e.g.
	/// RenderTargetBitmap), use <see cref="SnapshotAsync"/> — the only genuinely-async operation in the seam,
	/// because a GPU→CPU read cannot block the browser's single JS thread.
	/// </summary>
	ITexture RenderOffscreen(int pixelWidth, int pixelHeight, Action<IDrawingSession> render);

	/// <summary>
	/// Reads <paramref name="texture"/>'s pixels back to a neutral CPU <see cref="IImage"/> (BGRA8888 premultiplied).
	/// Async because on a GPU backend the readback completes only when control yields to the event loop (on WASM a
	/// synchronous poll would hang). The texture must have been produced by this factory; a foreign texture throws.
	/// </summary>
	Task<IImage> SnapshotAsync(ITexture texture);

	/// <summary>
	/// Uploads a neutral <see cref="IImage"/>'s pixels into a backend-specific GPU texture (see
	/// <see cref="ITexture"/>). Done once; the caller owns and disposes the result. This is the "store"
	/// half of images — decoding (neutral pixels) is separate, and lives in <see cref="IImageEncoderDecoder"/>.
	/// </summary>
	ITexture CreateTexture(IImage image);

	/// <summary>
	/// Uploads raw BGRA8888-premultiplied pixels (tightly packed, <paramref name="pixelWidth"/> × <paramref name="pixelHeight"/>
	/// × 4 bytes) into a backend-specific GPU texture. The pixels-in-hand sibling of <see cref="CreateTexture(IImage)"/>,
	/// for a caller that already holds bytes (e.g. an add-in that rasterized to its own surface) and shouldn't detour
	/// through the codec's <see cref="IImageEncoderDecoder.CreateImage"/>. The caller owns and disposes the result.
	/// </summary>
	ITexture CreateTexture(int pixelWidth, int pixelHeight, ReadOnlySpan<byte> bgraPremul);

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

	/// <summary>
	/// Begins a recording — the session the render cycle records the visual tree (or a subtree) into;
	/// <see cref="ICommandRecorder.Finish"/> yields the opaque <see cref="IRenderRecord"/>. The root frame is the
	/// first call. One factory for both the frame and nested recordings (was <c>IRenderer.BeginFrame</c> and the
	/// per-session <c>CreateRecording</c>).
	/// </summary>
	ICommandRecorder CreateRecording();
}

/// <summary>
/// The present half of a backend, typed to the render-target kind it composes onto. A backend implements one
/// instantiation per kind it serves (Skia: <see cref="IGLRenderTarget"/> / <see cref="IMetalRenderTarget"/> /
/// <see cref="ISoftwareRenderTarget"/>; WebGPU: <see cref="IWebGpuRenderTarget"/>). The target arrives already
/// typed — the backend never casts or type-switches a neutral <see cref="IRenderTarget"/>. The framework does
/// the neutral→typed narrowing (a single Uno-side cast) and gates negotiation on which instantiations a backend
/// implements, so a backend can't win a kind it can't present.
/// </summary>
public interface IDrawingFactory<in TTarget> : IDrawingFactory where TTarget : IRenderTarget
{
	/// <summary>
	/// Phase 2: begins composing onto <paramref name="target"/>. The cycle replays a recorded frame
	/// (<see cref="IRenderRecord.Replay"/>) and draws any overlay into the returned session, then disposes it to present.
	/// </summary>
	IPresentSession BeginPresent(TTarget target);
}
