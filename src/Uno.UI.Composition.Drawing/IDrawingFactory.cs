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
/// It is the device-bound resource half of the abstraction: it manufactures the GPU/pixel-device-backed handles
/// (images, shaders, effect filters). Backend-independent seams live separately: geometry
/// (<see cref="GeometryFactory"/>), image decoding (<see cref="ImageEncoderDecoder"/>), font resolution (<see cref="FontProvider"/>).
/// </summary>
public interface IDrawingFactory
{
	/// <summary>
	/// Renders <paramref name="render"/> into a fresh transparent offscreen target of the given pixel size and
	/// returns it as a backend-resident <see cref="ITexture"/>, sampled directly with no CPU round-trip. The caller
	/// owns and disposes it. To read the pixels back to the CPU (e.g. RenderTargetBitmap), use <see cref="SnapshotAsync"/>.
	/// </summary>
	ITexture RenderOffscreen(int pixelWidth, int pixelHeight, Action<IDrawingSession> render);

	/// <summary>
	/// Reads <paramref name="texture"/>'s pixels back to a neutral CPU <see cref="IImage"/> (BGRA8888 premultiplied).
	/// Async because a GPU readback completes only when control yields to the event loop. The texture must have been
	/// produced by this factory; a foreign texture throws.
	/// </summary>
	Task<IImage> SnapshotAsync(ITexture texture);

	/// <summary>
	/// Uploads a neutral <see cref="IImage"/>'s pixels into a backend-specific GPU <see cref="ITexture"/>; the caller
	/// owns and disposes the result. Decoding (producing neutral pixels) is separate, in <see cref="IImageEncoderDecoder"/>.
	/// </summary>
	ITexture CreateTexture(IImage image);

	/// <summary>
	/// Uploads raw BGRA8888-premultiplied pixels (tightly packed, <paramref name="pixelWidth"/> × <paramref name="pixelHeight"/>
	/// × 4 bytes) into a backend-specific GPU texture. The pixels-in-hand sibling of <see cref="CreateTexture(IImage)"/>
	/// for a caller that already holds bytes. The caller owns and disposes the result.
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
	/// Fuses a neutral <see cref="EffectNode"/> tree into one opaque backend <see cref="IEffectFilter"/> so
	/// non-separable blends over the backdrop stay a single fused operation. Returns null when the backend can't
	/// realize the tree as a filter (the caller then falls back to the recipe path).
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
	/// <see cref="ICommandRecorder.Finish"/> yields the opaque <see cref="IRenderRecord"/>. Serves both the root
	/// frame (the first call) and nested recordings.
	/// </summary>
	ICommandRecorder CreateRecording();
}

/// <summary>
/// The present half of a backend, typed to the render-target kind it composes onto — a backend implements one
/// instantiation per kind it serves. The target arrives already typed (the framework does the neutral→typed
/// narrowing), so the backend never casts or type-switches a neutral <see cref="IRenderTarget"/> and can't win a
/// kind it can't present.
/// </summary>
public interface IDrawingFactory<in TTarget> : IDrawingFactory where TTarget : IRenderTarget
{
	/// <summary>
	/// Begins composing onto <paramref name="target"/>. The cycle replays a recorded frame
	/// (<see cref="IRenderRecord.Replay"/>) and draws any overlay into the returned session, then disposes it to present.
	/// </summary>
	IPresentSession BeginPresent(TTarget target);
}
