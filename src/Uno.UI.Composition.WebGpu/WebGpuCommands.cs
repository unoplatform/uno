// The shapes a recording is made of: one command per drawing call, plus the clip they carry.
#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Uno.WebGpu.Native;
using static Uno.WebGpu.Native.WGPU;
using Uno.UI.Composition.Drawing;
using Uno.Foundation.Logging;
using Windows.Graphics.Effects.Interop;
using Windows.Foundation;
using WColor = Windows.UI.Color;

namespace Uno.UI.Composition.WebGpu;

internal struct RoundClip
{
	public Vector4 Rect;    // device rounded-rect L,T,R,B
	public Vector4 Radii;   // per-corner X radius (TL,TR,BR,BL), device px
	public Vector4 RadiiY;  // per-corner Y radius (elliptical corners; equals Radii for circular)
	public bool Exclude;    // Difference op: keep the area OUTSIDE the rounded rect (PushClipExclude) rather than inside
}

// One arbitrary path clip as the coverage rasterizer consumes it: closed device-space edges (x0,y0,x1,y1 each),
// its fill rule, and whether it keeps the inside (Intersect) or the outside (Difference). Immutable once built, so
// a composed clip can share it by reference.
internal sealed class PathClip
{
	public float[] Edges;
	public bool EvenOdd;
	public bool Exclude;
	public Vector4 Bbox;   // device L,T,R,B of the edges

	public PathClip Transformed(in Matrix3x2 m)
	{
		var e = new float[Edges.Length];
		var bbMin = new Vector2(float.MaxValue); var bbMax = new Vector2(float.MinValue);
		for (int i = 0; i < e.Length; i += 2)
		{
			float x = Edges[i], y = Edges[i + 1];
			var q = new Vector2(x * m.M11 + y * m.M21 + m.M31, x * m.M12 + y * m.M22 + m.M32);
			e[i] = q.X; e[i + 1] = q.Y;
			bbMin = Vector2.Min(bbMin, q); bbMax = Vector2.Max(bbMax, q);
		}
		return new PathClip { Edges = e, EvenOdd = EvenOdd, Exclude = Exclude, Bbox = new Vector4(bbMin.X, bbMin.Y, bbMax.X, bbMax.Y) };
	}
}

internal struct ClipData
{
	public const int MaxRounds = 4;   // nesting depth beyond this drops the outermost (least likely to clip content)
	public Vector4 Aabb;    // device L,T,R,B scissor
							// Nested rounded-rect clips, all ANDed per-fragment (clipCov). null/empty = none. Copy-on-write: each push
							// allocates a fresh array so Save/Restore snapshots and sibling commands keep their own reference.
	public RoundClip[] Rounds;
	// Every path clip in force, innermost last, for the per-fragment coverage mask (see ResolveClipMask). Nests
	// without limit: the mask is the PRODUCT of each path's coverage, so intersecting N shapes is N
	// accumulate+resolve passes into one texture. Copy-on-write like Rounds.
	public PathClip[] Paths;
	public static ClipData None => new() { Aabb = new Vector4(-1e9f, -1e9f, 1e9f, 1e9f), ScissorInert = true };

	// The op's geometry is provably inside Aabb (containment proven at record time), so the scissor is not
	// required for correctness: emit uses the full surface instead, letting the scissor dedup collapse and
	// ClipDataEquals group ops across visuals whose only difference is their (inert) layout-clip AABB. The
	// tight Aabb is KEPT — it still drives per-op culling against the composed present/damage clip.
	public bool ScissorInert;

	// The clip's rect edge rides the op's ClipU (dedicated rect slot): the scissor is then cull-only, so the
	// emit widens it to the full surface and consecutive such ops share one SetScissorRect.
	public bool AabbInClipU;
	// Set by the stamp paths when the scissor MUST stay tight (the ClipU was built from a different-space clip
	// that could not fold the full rect constraint) — blocks the emit's derived-widening fallback.
	public bool ScissorLoadBearing;

	// Append a rounded clip, copy-on-write, capped at MaxRounds (drops the oldest/outermost on overflow).
	// TODO: the cap is not expressible in the seam — IDrawingSession.ClipRoundRect nests without limit and the Skia
	// backend honours that exactly. It fails silently where Skia would not: the outermost round keeps its rect
	// extent through Aabb but loses its corner rounding, and an excluded one loses its constraint entirely
	// (exclusions never tighten Aabb). The budget spans the whole nesting chain, since ClipCompose pushes a child
	// recording's rounds onto the parent's. Overflow rounds could go on Paths instead, which has no cap.
	public static PathClip[] PushPath(PathClip[] existing, PathClip pc)
	{
		int n = existing?.Length ?? 0;
		var arr = new PathClip[n + 1];
		if (n > 0) { System.Array.Copy(existing, arr, n); }
		arr[n] = pc;
		return arr;
	}

	public static RoundClip[] Push(RoundClip[] existing, in RoundClip rc)
	{
		int n = existing?.Length ?? 0;
		if (n < MaxRounds)
		{
			var arr = new RoundClip[n + 1];
			if (n > 0) { System.Array.Copy(existing, arr, n); }
			arr[n] = rc;
			return arr;
		}
		var capped = new RoundClip[MaxRounds];
		System.Array.Copy(existing, 1, capped, 0, MaxRounds - 1);
		capped[MaxRounds - 1] = rc;
		return capped;
	}
}

// Draw commands share one ordered stream so cross-type z-order (rect over path over image) is preserved.
internal abstract class WebGpuCommand
{
	public ClipData Clip;
}

internal sealed class RectCommand : WebGpuCommand
{
	public WColor Color;
	public Vector2 P0, P1, P2, P3;
}

// An analytic rounded rectangle / border ring: one SDF quad instead of a tessellated path.
// The SDF is evaluated in LOCAL centred space (Half/Radii are local, transform-independent), so it's correct under
// ANY affine transform (rotation/scale/skew) — the four device corners P0..P3 only position the quad. A positive
// InnerHalf makes it a BORDER RING (outer minus an inner rounded rect at InnerCenter); InnerHalf<0 = solid fill.
// Radii = (TopLeft, TopRight, BottomRight, BottomLeft).
internal sealed class RoundedRectCmd : WebGpuCommand
{
	public Vector2 P0, P1, P2, P3;   // device-space corners: TL, TR, BR, BL (matches RectCommand order)
	public Vector2 Half;             // local half-size
	public Vector4 Radii;            // local per-corner
	public WColor Color; public float Opacity = 1f;
	public Vector2 InnerHalf = new(-1f, -1f);
	public Vector2 InnerCenter;
	public Vector4 InnerRadii;
}

internal static class WgStrokeStats
{
	public static int Strips;
}

internal sealed class PathFill : WebGpuCommand
{
	public float[] FanDevice;
	public Vector2 BbMin, BbMax;
	public WColor Color;
	public bool EvenOdd;
	/// <summary>The fan tiles the shape without overlap, so it can be filled directly — no stencil-then-cover.</summary>
	public bool FanTiles;

	/// <summary>
	/// Per-vertex AA coverage (one per FanDevice point), multiplied into alpha so the shape antialiases itself
	/// instead of relying on a multisampled attachment. Null when the fill has no ring.
	/// </summary>
	public float[] FanCoverage;

	/// <summary>
	/// The flattened outline as device-space edges (x0,y0,x1,y1 per edge), for the signed-area coverage
	/// rasterizer. Independent of the triangulation: coverage needs the boundary, not the interior, which is
	/// why it is available for shapes the tessellator refuses. Null when the contours could not be captured.
	/// </summary>
	public float[] Edges;
	/// <summary>Source geometry + transform, so an atlas entry can be keyed by shape and scale.</summary>
	public object Geometry;
	public Matrix4x4 GeomMatrix;

	// The stencil fan the GPU consumes: FanDevice with the transform-table slot interleaved as a third float.
	// Recordings are cached, so FanDevice never changes — rebuilding this element by element every frame is pure
	// waste, and a giant glyph flattens to thousands of points. Keyed by the slot it was built for.

	// The transformed copy this command produced for a given replay transform. Inline replay runs every frame and
	// is otherwise a full transform + allocation of the whole fan each time.
	private PathFill _replayed;
	private Matrix4x4 _replayedM;

	public PathFill ReplayedAt(in Matrix4x4 m) => _replayed is not null && _replayedM == m ? _replayed : null;

	public void StoreReplayed(in Matrix4x4 m, PathFill value)
	{
		_replayed = value;
		_replayedM = m;
	}

}

internal sealed unsafe class ImageCmd : WebGpuCommand
{
	public Vector2 P0, P1, P2, P3;
	public IntPtr View;   // the pre-uploaded WebGpuTexture view (no per-frame upload)
	public int W, H;
	public float Opacity;
	public float U0, V0, U1 = 1f, V1 = 1f;   // source UV sub-rect (whole texture by default)
	public EdgeExtend ExtendX, ExtendY;      // sampler address modes; UVs run past 1 for a tiled fill
	public int TintMode;        // 0 = none, 1 = SrcIn blend-mode tint
	public Vector4 Tint;        // straight-alpha tint color (0..1) for TintMode 1
	public float[] ColorMatrix; // null, or 20-float (4x5) effect colour matrix applied in the image shader
}

internal sealed class GradientCmd : WebGpuCommand
{
	public Vector2 P0, P1, P2, P3;   // device-space quad
	public float[] Uniform;          // packed Grad struct (WebGpuDevice.GradientUniformBytes / 4 floats)
}

// A drop shadow: the silhouette (flattened, device space) is filled into an offscreen coverage texture,
// separably gaussian-blurred (SigmaX/Y), then composited tinted by Color. Same fan/bbox form as PathFill.
internal sealed class ShadowCmd : WebGpuCommand
{
	public float[] Edges;   // the silhouette as device-space edges (x0,y0,x1,y1 each), for the coverage bake
	public Vector2 BbMin, BbMax;
	public bool EvenOdd;
	public WColor Color;
	public float SigmaX, SigmaY;
	public bool Additive;
}

// A SaveLayer group: its Commands are rendered into a full-size offscreen surface, then composited onto the
// parent with CompositeMode (0 = SrcOver, 1 = DstIn mask) and an optional color matrix (SaveLayer(IColorFilter)).
internal sealed class LayerCmd : WebGpuCommand
{
	public List<WebGpuCommand> Commands;
	public int CompositeMode;   // 0 = SrcOver, 1 = DstIn
	public float[] ColorMatrix; // null, or 20-float (4x5) color matrix applied at composite
	public WebGpuEffectFilter ShadowEffect; // SaveLayer(IEffectFilter): a drop shadow derived from the content
}

// DrawEffectBackdrop (acrylic): the content drawn BEFORE this in the frame is captured, gaussian-blurred by
// Effect's sigma, drawn clipped to the effect region, then tinted by Effect.Color. Effect-graph realization is
// simplified to blur + tint (the dominant acrylic visual), not the full IGraphicsEffect DAG.
internal sealed class BackdropCmd : WebGpuCommand
{
	public WebGpuEffectFilter Effect;
	public float Opacity;
}

// A deferred replay of a cacheable child recording under a transform+clip. Captures BOTH the recording
// (WebGpuRenderRecord, which owns its compiled GPU draw-list — the persistent retained state) and its immutable
// command-list reference. The list is captured directly so a build survives the recording's Dispose (which only
// nulls Commands + defers the compiled state's GPU free to the render thread); the frame presents on the render
// thread while the main thread may Dispose the recording.
internal sealed class ReplayRefCmd : WebGpuCommand
{
	public WebGpuRenderRecord Data;
	public System.Collections.Generic.List<WebGpuCommand> Commands;
	public System.Numerics.Matrix4x4 Transform;
}
