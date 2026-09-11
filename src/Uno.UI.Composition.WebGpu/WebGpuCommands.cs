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

// One clip entry, reached from the space the clip is expressed in through M. Analytic: a rounded rect in its own
// space, exact under any affine since the shape never leaves the space it was recorded in. Mask: M maps to the
// mask's texels and Rect is its slot in the draw's mask texture (see WebGpuCoverage.ResolveClipMasks).
internal struct ClipEntry
{
	public Matrix3x2 M;     // clip space -> this entry's space (a mask's texel space)
	public Vector4 Rect;    // L,T,R,B in the entry's space; a mask's slot x,y,w,h
	public Vector4 Radii;   // per-corner X radius (TL,TR,BR,BL)
	public Vector4 RadiiY;  // per-corner Y radius (elliptical corners; equals Radii for circular)
	public bool Exclude;    // Difference op: keep the area OUTSIDE rather than inside
	public bool Mask;       // a path mask entry rather than a rounded rect

	// The same entry expressed in a space that reaches this one through `m` (p_here = Transform(p_new, m)).
	public ClipEntry Under(in Matrix3x2 m) => new() { M = m * M, Rect = Rect, Radii = Radii, RadiiY = RadiiY, Exclude = Exclude, Mask = Mask };
}

// One path clip as recorded: the geometry and the matrix that was current, its fill rule, and whether it keeps the
// inside (Intersect) or the outside (Difference). Its mask is baked at draw time (see WebGpuShapeCache). Immutable
// once built, so a composed clip can share it by reference.
internal sealed class PathClip
{
	public IGeometry Geometry;
	public Matrix3x2 M;      // the geometry's space -> the clip's
	public bool EvenOdd;
	public bool Exclude;
	public Vector4 Bbox;     // L,T,R,B in the clip's space
	public Vector2 Offset => new(M.M31, M.M32);

	public PathClip Transformed(in Matrix3x2 m)
	{
		var mm = M * m;
		Geo.Bounds(Geometry, mm, out var min, out var max);
		return new PathClip { Geometry = Geometry, M = mm, EvenOdd = EvenOdd, Exclude = Exclude, Bbox = new Vector4(min.X, min.Y, max.X, max.Y) };
	}
}

internal static class Geo
{
	/// <summary>The geometry's bounds under <paramref name="m"/>, as the box of the mapped corners.</summary>
	public static void Bounds(IGeometry g, in Matrix3x2 m, out Vector2 min, out Vector2 max)
	{
		var b = g.Bounds;
		var p0 = Vector2.Transform(new Vector2((float)b.Left, (float)b.Top), m);
		var p1 = Vector2.Transform(new Vector2((float)b.Right, (float)b.Top), m);
		var p2 = Vector2.Transform(new Vector2((float)b.Right, (float)b.Bottom), m);
		var p3 = Vector2.Transform(new Vector2((float)b.Left, (float)b.Bottom), m);
		min = Vector2.Min(Vector2.Min(p0, p1), Vector2.Min(p2, p3));
		max = Vector2.Max(Vector2.Max(p0, p1), Vector2.Max(p2, p3));
	}
}

internal struct ClipData
{
	public Vector4 Aabb;    // device L,T,R,B scissor
	// Every analytic clip in force, all ANDed per-fragment (clipCov). null/empty = none. Copy-on-write: each push
	// allocates a fresh array so Save/Restore snapshots and sibling commands keep their own reference.
	public ClipEntry[] Entries;
	// Every path clip in force, innermost last; each becomes a mask entry at op build (see ResolveClipMasks), so
	// nesting has no limit. Copy-on-write like Entries.
	public PathClip[] Paths;
	// The op's own shape as a coverage texture (an atlas page or a mask of its own), sampled by the op's vertex uv:
	// the innermost clip. 0 = the geometry carries the shape. Set by the present session, never by the recorder.
	public nint Coverage;
	// The coverage texture is drawn scaled or rotated rather than texel for pixel, so it is sampled filtered.
	public bool CoverageFiltered;
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

	public static void PushEntry(ref ClipData clip, in ClipEntry e)
	{
		int n = clip.Entries?.Length ?? 0;
		var arr = new ClipEntry[n + 1];
		if (n > 0) { System.Array.Copy(clip.Entries, arr, n); }
		arr[n] = e;
		clip.Entries = arr;
	}

	public static PathClip[] PushPath(PathClip[] existing, PathClip pc)
	{
		int n = existing?.Length ?? 0;
		var arr = new PathClip[n + 1];
		if (n > 0) { System.Array.Copy(existing, arr, n); }
		arr[n] = pc;
		return arr;
	}

}

/// <summary>Which command a <see cref="WebGpuCommand"/> is, so the walk dispatches on a field rather than on a
/// chain of type tests: a list of thousands of recordings would otherwise test every earlier kind for each one.</summary>
internal enum CmdKind : byte { Rect, RoundedRect, Path, Image, Gradient, Shadow, Layer, Backdrop, ReplayRef }

// Draw commands share one ordered stream so cross-type z-order (rect over path over image) is preserved.
internal abstract class WebGpuCommand
{
	public readonly CmdKind Kind;
	public ClipData Clip;

	protected WebGpuCommand(CmdKind kind) => Kind = kind;
}

internal sealed class RectCommand : WebGpuCommand
{
	public RectCommand() : base(CmdKind.Rect) { }

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
	public RoundedRectCmd() : base(CmdKind.RoundedRect) { }

	public Vector2 P0, P1, P2, P3;   // device-space corners: TL, TR, BR, BL (matches RectCommand order)
	public Vector2 Half;             // local half-size
	public Vector4 Radii;            // local per-corner
	public WColor Color; public float Opacity = 1f;
	public Vector2 InnerHalf = new(-1f, -1f);
	public Vector2 InnerCenter;
	public Vector4 InnerRadii;
}

// A path, filled (Stroke == 0) or stroked Stroke wide in its own units, as recorded: the geometry and the matrix that
// was current, so its rasterisation can wait for the density it is drawn at (see WebGpuShapeCache).
internal sealed class PathCmd : WebGpuCommand
{
	public PathCmd() : base(CmdKind.Path) { }

	public IGeometry Geometry;
	public Matrix3x2 M;            // the geometry's space -> the recording's
	public float Stroke;
	public WColor Color;
	public bool EvenOdd;
	public Vector2 BbMin, BbMax;   // in the recording's space
	public Vector2 Offset => new(M.M31, M.M32);
}

internal sealed unsafe class ImageCmd : WebGpuCommand
{
	public ImageCmd() : base(CmdKind.Image) { }

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
	public GradientCmd() : base(CmdKind.Gradient) { }

	public Vector2 P0, P1, P2, P3;   // device-space quad
	public float[] Uniform;          // packed Grad struct (WebGpuDevice.GradientUniformBytes / 4 floats)
}

// A drop shadow: the silhouette's coverage is baked offscreen at draw time, gaussian-blurred (SigmaX/Y), then
// composited tinted by Color.
internal sealed class ShadowCmd : WebGpuCommand
{
	public ShadowCmd() : base(CmdKind.Shadow) { }

	public IGeometry Geometry;     // the silhouette, with the matrix that was current: baked at draw time
	public Matrix3x2 M;
	public Vector2 BbMin, BbMax;   // in the recording's space
	public bool EvenOdd;
	public WColor Color;
	public float SigmaX, SigmaY;
	public bool Additive;
	public Vector2 Offset => new(M.M31, M.M32);
}

// A SaveLayer group: its Commands are rendered into a full-size offscreen surface, then composited onto the
// parent with CompositeMode (0 = SrcOver, 1 = DstIn mask) and an optional color matrix (SaveLayer(IColorFilter)).
internal sealed class LayerCmd : WebGpuCommand
{
	public LayerCmd() : base(CmdKind.Layer) { }

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
	public BackdropCmd() : base(CmdKind.Backdrop) { }

	public WebGpuEffectFilter Effect;
	public float Opacity;
}

// A replayed child recording under the matrix and clip current where it was replayed. Captures BOTH the recording
// (WebGpuRenderRecord, which owns its arena entry — the persistent retained state) and its immutable command-list
// reference. The list is captured directly so a frame survives the recording's Dispose (which only nulls Commands
// and defers the arena entry's GPU free to the render thread); the frame presents on the render thread while the
// main thread may Dispose the recording.
internal sealed class ReplayRefCmd : WebGpuCommand
{
	public ReplayRefCmd() : base(CmdKind.ReplayRef) { }

	public WebGpuRenderRecord Data;
	public System.Collections.Generic.List<WebGpuCommand> Commands;
	public System.Numerics.Matrix4x4 Transform;
}
