// The coverage atlas: rasterizes a shape's coverage mask once, then places it as a textured quad 1:1 with device
// pixels for as long as nothing about its device-space footprint changes.
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

public sealed unsafe partial class WebGpuPresentSession
{
	/// <summary>
	/// Emits an atlased fill as a tinted quad, or returns false to leave it on the geometry path.
	/// </summary>
	private bool TryAtlasFill(PathFill pf, List<DrawOp> ops, OwnedResources owned, Vector2 scale, bool big = false)
	{
		if (!TryAtlasOp(pf, owned, scale, out var op, big)) { return false; }
		ops.Add(op);
		return true;
	}


	internal static int AtlasTried, AtlasNoKey, AtlasHit, AtlasBaked, AtlasNoRoom, AtlasNoEdges, ScaleBlocked;

	/// <summary>
	/// Keys and places on the op's own coordinate space, with <paramref name="scale"/> giving the extra scale the
	/// GPU applies afterwards — Vector2.One for geometry already in device space, the replay scale for an arena
	/// recording (identity-baked geometry mapped by the xform table). Getting that scale wrong bakes the mask at
	/// the wrong size, which is what broke When_ShapeVisual_ViewBox_Shape_Combinations.
	/// </summary>
	private bool TryAtlasOp(PathFill pf, OwnedResources owned, Vector2 scale, out DrawOp result, bool big = false)
	{
		result = default;
		if (!TryAtlasSlot(pf, owned, scale, out var slot, out var ox, out var oy, big)) { return false; }
		_atlasQuads.Clear();
		AppendAtlasQuad(_atlasQuads, slot, ox, oy, scale, pf.Color);
		result = MakeAtlasOp(pf, slot.Owner, _atlasQuads, owned);
		return true;
	}

	private readonly List<float> _atlasQuads = new();

	/// <summary>
	/// Emits a RUN of consecutive fills sharing an atlas page, a colour and a clip as ONE draw, advancing
	/// <paramref name="i"/> past them. Per-glyph geometry turns a string into N fills, and a draw apiece is far
	/// worse than the single merged run it replaces - the quads all sample one page, so they batch trivially.
	/// </summary>
	private bool TryAtlasBatch(List<WebGpuCommand> cmds, ref int i, OwnedResources owned, Vector2 scale, out DrawOp result)
	{
		result = default;
		if (cmds[i] is not PathFill first) { return false; }
		if (!TryAtlasSlot(first, owned, scale, out var slot0, out var ox0, out var oy0)) { return false; }

		_atlasQuads.Clear();
		AppendAtlasQuad(_atlasQuads, slot0, ox0, oy0, scale, first.Color);
		var j = i + 1;
		while (j < cmds.Count && cmds[j] is PathFill nx
			&& nx.Color.R == first.Color.R && nx.Color.G == first.Color.G
			&& nx.Color.B == first.Color.B && nx.Color.A == first.Color.A
			&& ClipDataEquals(nx.Clip, first.Clip))
		{
			// A fill landing on ANOTHER page cannot share this draw's bind group. It stays baked, so the caller
			// picks it up next and starts a fresh batch on what is by then a cache hit.
			if (!TryAtlasSlot(nx, owned, scale, out var slotN, out var oxN, out var oyN)) { break; }
			if (!ReferenceEquals(slotN.Owner, slot0.Owner)) { break; }
			AppendAtlasQuad(_atlasQuads, slotN, oxN, oyN, scale, first.Color);
			j++;
		}

		result = MakeAtlasOp(first, slot0.Owner, _atlasQuads, owned);

		i = j - 1;
		return true;
	}

	/// <summary>
	/// Resolves (baking on a miss) the atlas entry for one fill. <paramref name="big"/> admits fills too large for
	/// a shared page as entries with a texture of their own -- the cached form of what used to be a per-frame mask.
	/// </summary>
	private bool TryAtlasSlot(PathFill pf, OwnedResources owned, Vector2 scale, out WebGpuPathAtlas.Slot slot, out float ox, out float oy, bool big = false)
	{
		slot = null; ox = oy = 0;
		if (!_pathAtlas) { return false; }
		AtlasTried++;
		// A cached recording OWNS the entries it bakes and frees them when released. A per-frame op has no such
		// owner, so the ATLAS holds the reference and drops it after the entry goes idle (HoldForCache/SweepCache).
		// Per-frame ops must be able to BAKE, not only hit: restricting them to hits looks safer (nothing owns
		// the entry) but renders identical content crisp through the retained path and tessellated through the
		// command-list fallback.
		bool hitOnly = owned is null;
		if (!CanCoverageBake(pf)) { AtlasNoEdges++; return false; }
		if (!WebGpuPathAtlas.TryKey(pf.GeomKey, pf.GeomMatrix, pf.BbMin, pf.BbMax, scale, out var key, out var w, out var h, out ox, out oy, allowBig: big)) { AtlasNoKey++; return false; }

		if (_d.PathAtlas.RegularPages == 0) { _d.AddPathAtlasPage(); }
		if (_d.PathAtlas.TryGet(key, out slot))
		{
			// An entry baked through the big-fill route is a cached mask, not an atlas quad, whatever its size: it was
			// baked at the transform's axis lengths and its quad rotates it, so the replay guards must compare its
			// scale, not its axis alignment.
			if (big) { FillMaskHits++; } else { AtlasHit++; }
			_d.PathAtlas.NoteUse(slot, _d.FrameSeq);
			// A REUSED entry needs its own reference for this recording. One slot backs a glyph everywhere it
			// appears, so without this the recording that baked it releases the region out from under every other
			// holder and they start sampling whatever took its place.
			if (!hitOnly)
			{
				_d.PathAtlas.Retain(slot);
				(owned.AtlasSlots ??= new()).Add(slot);
			}
		}
		else
		{
			// A per-frame op enters the cache only once its key has held for two frames (see Recurring); until then
			// it takes the per-frame bake, so moving content never accumulates entries.
			if (owned is null && big && !_d.PathAtlas.Recurring(key, _d.FrameSeq)) { return false; }
			if (WebGpuPathAtlas.IsBig(w, h))
			{
				slot = AddStandaloneSlot(key, w, h, ox, oy, _d.ColorFormat);
			}
			else
			{
				slot = _d.PathAtlas.Allocate(key, w, h, ox, oy);
				if (slot is null)
				{
					// Every page is exhausted: open another rather than falling back, which would leave this shape
					// aliased at one sample while its neighbours stayed crisp.
					_d.AddPathAtlasPage();
					slot = _d.PathAtlas.Allocate(key, w, h, ox, oy);
				}
			}
			if (slot is null) { AtlasNoRoom++; return false; }
			if (owned is not null) { (owned.AtlasSlots ??= new()).Add(slot); }
			else { _d.PathAtlas.HoldForCache(slot, _d.FrameSeq); }
			QueueEntryBake(pf, slot, scale);
			if (big) { FillMasksBaked++; } else { AtlasBaked++; }
		}

		return true;
	}

	// A texture of the entry's own size, registered as its own atlas page so it shares the key, reference count and
	// idle sweep of a shelf slot. Format follows what will be rendered into it.
	private WebGpuPathAtlas.Slot AddStandaloneSlot(in WebGpuPathAtlas.Key key, int w, int h, float ox, float oy, WGPUTextureFormat format)
	{
		var td = new WGPUTextureDescriptor
		{
			Size = new WGPUExtent3D { Width = (uint)w, Height = (uint)h, DepthOrArrayLayers = 1 },
			Format = format, MipLevelCount = 1, SampleCount = 1, Dimension = WGPUTextureDimension._2D,
			Usage = WGPUTextureUsage.RenderAttachment | WGPUTextureUsage.TextureBinding,
		};
		var tex = wgpuDeviceCreateTexture(_d.Dev, &td);
		return _d.PathAtlas.AddStandalone(key, w, h, ox, oy, tex, wgpuTextureCreateView(tex, null));
	}

	/// <summary>Appends one entry as 6 solid vertices (pos, colour, coverage uv), placed at the fill's OWN origin.</summary>
	private void AppendAtlasQuad(List<float> dst, WebGpuPathAtlas.Slot slot, float ox, float oy, Vector2 scale, WColor color)
	{
		float cr = color.R / 255f, cg = color.G / 255f, cb = color.B / 255f, ca = color.A / 255f;
		// The quad lives in the op's own space; one device pixel is 1/scale there, so a slot.W-wide mask needs a
		// slot.W/scale-wide quad to land 1:1 after the replay scale is applied on the GPU. Placed at this fill's
		// origin rather than the slot's: on a cache hit the same shape elsewhere draws at its own position, and
		// the subpixel phase is part of the key, so the mask already suits it.
		float x0 = ox - 1f / scale.X, y0 = oy - 1f / scale.Y;
		float x1 = x0 + slot.W / scale.X, y1 = y0 + slot.H / scale.Y;
		float pw = slot.Owner.W, ph = slot.Owner.H;
		float u0 = slot.X / pw, v0 = slot.Y / ph;
		float u1 = (slot.X + slot.W) / pw, v1 = (slot.Y + slot.H) / ph;
		void QV(float x, float y, float uu, float vv) { dst.Add(x); dst.Add(y); dst.Add(cr); dst.Add(cg); dst.Add(cb); dst.Add(ca); dst.Add(uu); dst.Add(vv); }
		QV(x0, y0, u0, v0); QV(x1, y0, u1, v0); QV(x1, y1, u1, v1);
		QV(x0, y0, u0, v0); QV(x1, y1, u1, v1); QV(x0, y1, u0, v1);
	}

	/// <summary>Six solid vertices (pos, colour, coverage uv over <paramref name="uv"/> = u0,v0,u1,v1) covering the rect at <paramref name="origin"/>.</summary>
	private static float[] CoverageQuad(Vector2 origin, Vector2 size, WColor color, Vector4 uv)
	{
		float cr = color.R / 255f, cg = color.G / 255f, cb = color.B / 255f, ca = color.A / 255f;
		var q = new float[6 * VertexStride.Solid];
		int i = 0;
		void V(float x, float y, float u, float v) { q[i++] = x; q[i++] = y; q[i++] = cr; q[i++] = cg; q[i++] = cb; q[i++] = ca; q[i++] = u; q[i++] = v; }
		float x0 = origin.X, y0 = origin.Y, x1 = origin.X + size.X, y1 = origin.Y + size.Y;
		V(x0, y0, uv.X, uv.Y); V(x1, y0, uv.Z, uv.Y); V(x1, y1, uv.Z, uv.W);
		V(x0, y0, uv.X, uv.Y); V(x1, y1, uv.Z, uv.W); V(x0, y1, uv.X, uv.W);
		return q;
	}

	/// <summary>The op's clip with its own coverage texture as the innermost entry.</summary>
	private static ClipData WithCoverage(ClipData clip, IntPtr view)
	{
		clip.Coverage = (nint)view;
		return clip;
	}

	/// <summary>
	/// Six vertices (pos.xy in pixels, uv.xy) for an axis-aligned textured quad covering the device rect at
	/// <paramref name="origin"/>, sampling the texture rect <paramref name="uv"/> (x0, y0, x1, y1).
	/// </summary>
	private float[] TexturedQuad(Vector2 origin, Vector2 size, Vector4 uv)
	{
		var q = new float[24];
		void V(int i, Vector2 pos, float u, float v)
		{
			q[i] = pos.X; q[i + 1] = pos.Y; q[i + 2] = u; q[i + 3] = v;
		}

		var tr = origin + new Vector2(size.X, 0);
		var br = origin + size;
		var bl = origin + new Vector2(0, size.Y);
		V(0, origin, uv.X, uv.Y); V(4, tr, uv.Z, uv.Y); V(8, br, uv.Z, uv.W);
		V(12, origin, uv.X, uv.Y); V(16, br, uv.Z, uv.W); V(20, bl, uv.X, uv.W);
		return q;
	}

	private float[] TexturedQuad(Vector2 origin, Vector2 size) => TexturedQuad(origin, size, new Vector4(0f, 0f, 1f, 1f));

	/// <summary>
	/// Bind group for a SrcIn-tinted image draw: the texture carries coverage in its alpha and the colour comes
	/// from <paramref name="tint"/> (see ImageWgsl op.y). Pass <paramref name="owned"/> for a cached recording so
	/// the uniform is persistent - a per-frame one would be recycled and replay in another element's colour.
	/// </summary>
	private IntPtr TintedImageBg(IntPtr view, WColor tint, OwnedResources owned = null)
	{
		var ubuf = Ubuf(WebGpuDevice.ImageUniformBytes, owned);
		var u = stackalloc float[36];
		for (var zi = 0; zi < 36; zi++) { u[zi] = 0f; }
		u[0] = 1f; u[1] = 1f;
		u[4] = tint.R / 255f; u[5] = tint.G / 255f; u[6] = tint.B / 255f; u[7] = tint.A / 255f;
		// Whole uniform, so a recycled buffer cannot leave the edge-AA flag set: the mask carries the coverage.
		wgpuQueueWriteBuffer(_d.Q, ubuf, 0, (IntPtr)u, WebGpuDevice.ImageUniformBytes);
		var e = stackalloc WGPUBindGroupEntry[3];
		e[0] = new WGPUBindGroupEntry { Binding = 0, TextureView = view };
		e[1] = new WGPUBindGroupEntry { Binding = 1, Sampler = _d.Smp };
		e[2] = new WGPUBindGroupEntry { Binding = 2, Buffer = ubuf, Offset = 0, Size = WebGpuDevice.ImageUniformBytes };
		var bgd = new WGPUBindGroupDescriptor { Layout = _d.ImgBgl, EntryCount = 3, Entries = e };
		return Bg(ref bgd, owned);
	}

	/// <summary>One solid draw for a batch of quads sharing a page, a colour and a clip; the page is their coverage.</summary>
	private DrawOp MakeAtlasOp(PathFill pf, WebGpuPathAtlas.Page page, List<float> quads, OwnedResources owned)
	{
		var clip = WithCoverage(pf.Clip, page.View);
		return new DrawOp(DrawKind.Solid, (nint)Vbuf(quads, owned), (uint)(quads.Count / VertexStride.Solid), 0, false, clip, (nint)MakeClipBg(clip, owned));
	}
}
