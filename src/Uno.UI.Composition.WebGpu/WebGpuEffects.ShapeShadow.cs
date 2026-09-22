#nullable disable

using System;
using System.Collections.Generic;
using System.Numerics;
using Uno.WebGpu.Native;
using static Uno.WebGpu.Native.WGPU;
using WColor = Windows.UI.Color;

namespace Uno.UI.Composition.WebGpu;

/// <summary>
/// Blurred shadows for layers whose content resolves to a single rounded rect (see TrySilhouette). A drop shadow
/// is the blurred ALPHA of its content, so a wall of identically sized cards casts ONE shadow: the photo, scrim
/// and caption inside a card are behind an already-opaque outline and contribute nothing.
///
/// That collapses the whole sheet pipeline for those layers. Instead of replaying each card into a slot of a
/// shared 2048 sheet and blurring the sheet every frame, the shape is drawn once into a small texture, blurred
/// once, and every card -- this frame and every later one -- samples it.
/// </summary>
internal sealed unsafe partial class WebGpuEffects
{
	internal readonly struct ShapeKey : IEquatable<ShapeKey>
	{
		public readonly int W, H, R, SigmaQ;

		public ShapeKey(float w, float h, float r, float sigma)
		{
			// Quantised to whole device pixels: a card a fraction of a pixel wider casts the same shadow once blurred.
			W = (int)MathF.Round(w); H = (int)MathF.Round(h); R = (int)MathF.Round(r); SigmaQ = (int)MathF.Round(sigma * 4f);
		}

		public bool Equals(ShapeKey o) => W == o.W && H == o.H && R == o.R && SigmaQ == o.SigmaQ;
		public override bool Equals(object o) => o is ShapeKey k && Equals(k);
		public override int GetHashCode() => (W * 397) ^ (H * 31) ^ (R * 7) ^ SigmaQ;
	}

	internal sealed class ShapeShadow
	{
		public IntPtr Tex, View;
		public int TexW, TexH;
		public float Pad;        // device px of blur reach baked around the shape
		public long LastUsed;
	}

	private const int ShapeShadowIdleFrames = 240;

	// A WebGpuFrame -- and so this -- is constructed fresh every frame, so the cache lives on the device, which is
	// what persists. Holding it here would rebuild an empty dictionary each frame and never hit.
	private Dictionary<ShapeKey, ShapeShadow> _shapeShadows => _d.ShapeShadows;
	private List<ShapeKey> _shapeStale => _d.ShapeShadowStale;
	private List<ShapeKey> _shapeQueued => _d.ShapeShadowQueue;


	/// <summary>
	/// The blurred shadow of a rounded rect of this size, baking it on first use. Returns the texture to sample
	/// and, through <paramref name="pad"/>, how far the blur reaches past the shape -- the caller draws the quad
	/// inflated by that much.
	/// </summary>
	internal bool TryShapeShadow(float w, float h, float radius, float sigma, out IntPtr view, out float pad)
	{
		view = IntPtr.Zero; pad = 0f;
		if (WebGpuDevice.NoShapeShadow || w < 1f || h < 1f || sigma <= 0f) { return false; }
		var key = new ShapeKey(w, h, radius, sigma);
		if (_shapeShadows.TryGetValue(key, out var hit))
		{
			hit.LastUsed = _d.FrameSeq;
			view = hit.View; pad = hit.Pad;
				return true;
		}

		// A miss must NOT render here: this runs inside the walk, and opening a pass mid-build recurses through
		// EncodePass/FlushLayerSheets. Queue it and let the caller use the sheet for this one frame; the bake runs
		// at the top of the next frame and every frame after hits.
		if (!_shapeQueued.Contains(key)) { _shapeQueued.Add(key); }
		return false;
	}

	/// <summary>Bakes the shapes queued last frame. Called before the frame's own passes, so opening one here is
	/// safe -- which it is not from inside the walk.</summary>
	internal void FlushShapeBakes()
	{
		if (_shapeQueued.Count == 0) { return; }
		foreach (var key in _shapeQueued)
		{
			if (_shapeShadows.ContainsKey(key)) { continue; }
			float sigma = key.SigmaQ / 4f;
			float reach = MathF.Ceiling(3f * sigma) + 2f;
			// Match the sheet path's resolution exactly: it renders a shadow layer at 1/dn and blurs with sigma/dn,
			// which is a different pyramid depth from blurring at 1:1 with the full sigma -- and a different blur.
			// The shape is scaled rather than the basis, since RenderInto has no basis scale.
			int dn = ShadowDownsampleFor(sigma);
			float bsig = sigma / dn;
			float w = key.W / (float)dn, h = key.H / (float)dn, r = key.R / (float)dn, pad = reach / dn;
			int tw = (int)MathF.Ceiling(w + 2f * pad), th = (int)MathF.Ceiling(h + 2f * pad);
			if (tw < 1 || th < 1 || tw > 2048 || th > 2048) { continue; }

			var surface = new WebGpuRenderSurface(_d, tw, th, _d.Pool);
			var cmd = new RoundedRectCmd
			{
				Color = WColor.FromArgb(255, 255, 255, 255),
				Opacity = 1f,
				Half = new Vector2(w * 0.5f, h * 0.5f),
				Radii = new Vector4(r),
				InnerHalf = new Vector2(-1f, -1f),
				Clip = ClipData.None,
			};
			float x0 = pad, y0 = pad, x1 = pad + w, y1 = pad + h;
			cmd.P0 = new Vector2(x0, y0); cmd.P1 = new Vector2(x1, y0); cmd.P2 = new Vector2(x1, y1); cmd.P3 = new Vector2(x0, y1);
			_scratchCmds.Clear();
			_scratchCmds.Add(cmd);
			_f.RenderInto(_scratchCmds, Matrix3x2.Identity, ClipData.None, surface, null, false, 0f, 0f, tw, th);

			int levels = BlurLevels(bsig);
			int bw = Math.Max(1, tw >> levels), bh = Math.Max(1, th >> levels);
			var td = new WGPUTextureDescriptor
			{
				Size = new WGPUExtent3D { Width = (uint)bw, Height = (uint)bh, DepthOrArrayLayers = 1 },
				Format = WebGpuDevice.DefaultColorFormat,
				MipLevelCount = 1,
				SampleCount = 1,
				Dimension = WGPUTextureDimension._2D,
				Usage = WGPUTextureUsage.TextureBinding | WGPUTextureUsage.RenderAttachment,
			};
			var tex = wgpuDeviceCreateTexture(_d.Dev, &td);
			var texView = wgpuTextureCreateView(tex, null);
			BlurPyramidRegion(surface.View, tw, th, 0f, 0f, tw, th, bsig, bsig, texView);
			_f.LayerSurfaces.Add(surface);

			_shapeShadows[key] = new ShapeShadow { Tex = tex, View = texView, TexW = bw, TexH = bh, Pad = reach, LastUsed = _d.FrameSeq };
		}
		_shapeQueued.Clear();
	}

	private readonly List<WebGpuCommand> _scratchCmds = new();

	// Mirrors WebGpuFrame.ShadowDownsample: a shadow is only ever read blurred, so it is rendered smaller and the
	// pyramid shortened by the same amount.
	private static int ShadowDownsampleFor(float sigma) => 1 << Math.Clamp(BlurLevels(sigma) - 1, 0, 2);

	/// <summary>Drops shapes nothing has drawn in a while.</summary>
	internal void SweepShapeShadows()
	{
		if (_shapeShadows.Count == 0 || (_d.FrameSeq & 127) != 0) { return; }
		foreach (var kv in _shapeShadows)
		{
			if (kv.Value.LastUsed < _d.FrameSeq - ShapeShadowIdleFrames) { _shapeStale.Add(kv.Key); }
		}
		foreach (var k in _shapeStale)
		{
			if (_shapeShadows.Remove(k, out var e)) { _d.DeferTextureRelease(e.View, e.Tex); }
		}
		_shapeStale.Clear();
	}
}
