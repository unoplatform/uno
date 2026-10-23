// Encoding ops into a render pass: one case per DrawKind, the backdrop's pass-segment split, and the per-frame
// stats dump.
#nullable disable
using System;
using System.Collections.Generic;
using System.Numerics;
using Silk.NET.WebGPU;
using Buffer = Silk.NET.WebGPU.Buffer;

namespace Uno.UI.Composition.WebGpu;

internal sealed unsafe partial class WebGpuFrame
{
	/// <summary>
	/// Encodes one backdrop (the acrylic path): ends the open pass so the target holds the content BEHIND the backdrop,
	/// blurs the affected region, then opens a fresh pass that loads that content back and composites the blurred
	/// backdrop and its tint over the effect region. Ops after this one draw on top in the new pass, so each command is
	/// still encoded exactly once with no prefix re-render.
	/// </summary>
	/// <returns>The newly opened pass, which the caller is responsible for ending.</returns>
	private RenderPassEncoder* EncodeBackdropSegment(BackdropCmd backdrop, ref PassOps pst)
	{
		var target = pst.Target;
		Api.RenderPassEncoderEnd(pst.Pass);
		Api.RenderPassEncoderRelease(pst.Pass);

		// Blur only the element AABB, not the whole framebuffer -- and not a padded version of it either: a
		// backdrop samples the element's OWN backdrop, so reaching outside pulls in whatever sits behind the
		// neighbours. The pyramid samples clamp-to-edge, so the element's own edge pixels extend outward instead.
		var effect = backdrop.Effect;
		var aabb = backdrop.Clip.Aabb;
		float regionX = Math.Clamp(aabb.X, 0f, Target.Width), regionY = Math.Clamp(aabb.Y, 0f, Target.Height);
		float regionW = MathF.Max(1f, MathF.Min(Target.Width, aabb.Z) - regionX);
		float regionH = MathF.Max(1f, MathF.Min(Target.Height, aabb.W) - regionY);
		var blurred = Effects.BlurPyramidRegion(target.View, Target.Width, Target.Height, regionX, regionY, regionW, regionH, effect.SigmaX, effect.SigmaY);

		var color = new RenderPassColorAttachment
		{
			DepthSlice = uint.MaxValue,
			View = target.View,
			LoadOp = LoadOp.Load,
			StoreOp = StoreOp.Store,   // a following segment, or another backdrop, reloads it
		};
		// The reopened segment must carry the same attachments as the pass it continues -- a pipeline is only valid
		// against a matching set -- so the depth buffer comes back, LOADED, and occlusion carries across the split.
		var bda = new RenderPassDepthStencilAttachment
		{
			View = _depthView,
			DepthLoadOp = LoadOp.Load,
			DepthStoreOp = StoreOp.Store,
			StencilLoadOp = LoadOp.Undefined,
			StencilStoreOp = StoreOp.Undefined,
		};
		var desc = new RenderPassDescriptor { ColorAttachmentCount = 1, ColorAttachments = &color, DepthStencilAttachment = _depthView != null ? &bda : null };
		var pass = Api.CommandEncoderBeginRenderPass(Encoder, &desc);

		pst.Pass = pass;
		pst.Enc.Rebind(pass);

		if (TryScissor(aabb, out var sx, out var sy, out var sw, out var sh))
		{
			pst.Enc.Scissor(sx, sy, sw, sh);
			DrawBlurredBackdrop(ref pst, backdrop, blurred, new Vector2(regionX, regionY), new Vector2(regionW, regionH));
			if (effect.Color.A != 0)
			{
				DrawBackdropTint(ref pst, backdrop);
			}
		}

		return pass;
	}

	/// <summary>
	/// Draws the blurred backdrop over its region. Luminosity, noise and opacity ride in the image uniform,
	/// so the acrylic recipe costs one textured quad.
	/// </summary>
	private void DrawBlurredBackdrop(ref PassOps pst, BackdropCmd backdrop, TextureView* blurred, Vector2 origin, Vector2 size)
	{
		var uniform = MakeUniform(WebGpuDevice.ImageUniformBytes);
		var fields = stackalloc float[36];
		for (var zi = 0; zi < 36; zi++) { fields[zi] = 0f; }
		var lum = backdrop.Effect.LumColor;
		fields[0] = backdrop.Opacity;
		fields[3] = 1f;
		fields[4] = lum.R / 255f; fields[5] = lum.G / 255f; fields[6] = lum.B / 255f; fields[7] = lum.A / 255f;
		fields[24] = backdrop.Effect.Noise;
		Api.QueueWriteBuffer(_d.Q, uniform, 0, fields, WebGpuDevice.ImageUniformBytes);

		var entries = stackalloc BindGroupEntry[3];
		entries[0] = new BindGroupEntry { Binding = 0, TextureView = blurred };
		entries[1] = new BindGroupEntry { Binding = 1, Sampler = _d.Smp };
		entries[2] = new BindGroupEntry { Binding = 2, Buffer = uniform, Offset = 0, Size = WebGpuDevice.ImageUniformBytes };
		var bgDesc = new BindGroupDescriptor { Layout = _d.ImgBgl, EntryCount = 3, Entries = entries };
		var imageBg = _d.TrackBg(Api.DeviceCreateBindGroup(_d.Dev, &bgDesc));

		var verts = MakeBuffer(TexturedQuad(origin, size));
		var clipBg = MakeClipBg(backdrop.Clip);

		pst.Enc.Pipe(UseDepth ? _d.ImagePipeD : _d.ImagePipe);
		Api.RenderPassEncoderSetBindGroup(pst.Pass, 0, pst.PassBg, 0, null);
		Api.RenderPassEncoderSetBindGroup(pst.Pass, 1, imageBg, 0, null);
		Api.RenderPassEncoderSetBindGroup(pst.Pass, 2, clipBg, 0, null);
		Api.RenderPassEncoderSetBindGroup(pst.Pass, 3, _d.IdentitySiteBg, 0, null);
		Api.RenderPassEncoderSetVertexBuffer(pst.Pass, 0, verts, 0, (nuint)(24 * sizeof(float)));
		pst.Enc.Reset();
		pst.Enc.Draw(6);
	}

	/// <summary>Draws the backdrop's tint colour over the effect region.</summary>
	private void DrawBackdropTint(ref PassOps pst, BackdropCmd backdrop)
	{
		var c = backdrop.Effect.Color;
		float r = c.R / 255f, g = c.G / 255f, b = c.B / 255f, a = c.A / 255f;
		var verts = new VertBuf();
		void Vert(float x, float y)
		{
			verts.Add(x); verts.Add(y); verts.Add(r); verts.Add(g); verts.Add(b); verts.Add(a); verts.Add(0f); verts.Add(0f);
		}
		var aabb = backdrop.Clip.Aabb;
		Vert(aabb.X, aabb.Y); Vert(aabb.Z, aabb.Y); Vert(aabb.Z, aabb.W);
		Vert(aabb.X, aabb.Y); Vert(aabb.Z, aabb.W); Vert(aabb.X, aabb.W);

		var buf = MakeBuffer(verts);
		var clipBg = MakeClipBg(backdrop.Clip);

		pst.Enc.Pipe(UseDepth ? _d.SolidPipeD : _d.SolidPipe);
		Api.RenderPassEncoderSetBindGroup(pst.Pass, 0, pst.PassBg, 0, null);
		Api.RenderPassEncoderSetBindGroup(pst.Pass, 1, clipBg, 0, null);
		Api.RenderPassEncoderSetBindGroup(pst.Pass, 3, _d.IdentitySiteBg, 0, null);
		Api.RenderPassEncoderSetVertexBuffer(pst.Pass, 0, buf, 0, (nuint)(6 * VertexStride.Solid * sizeof(float)));
		pst.Enc.Reset();
		pst.Enc.Draw(6);
	}

	/// <summary>
	/// Encodes ops [<paramref name="start"/>, <paramref name="end"/>) into the pass, applying each op's scissor as it
	/// goes. Consecutive ops that differ only in a contiguous vertex range merge into one draw.
	/// </summary>
	private void EncodeOps(int start, int end, ref PassOps pst)
	{
		var ops = pst.Ops;
		for (int oi = start; oi < end; oi++)
		{
			var op = ops[oi];
			pst.Iters++;
			if (op.Kind == DrawKind.BackdropSegment)
			{
				pst.Pass = EncodeBackdropSegment(pst.Backdrops[(int)op.Count], ref pst);
				continue;
			}
			// An op placed by a site scissors to that site's box: its own would have to be rewritten every time
			// the recording moved, which is the cost this avoids.
			var scissorBox = op.SiteSlot != 0 ? SiteScissor(op.SiteSlot) : op.Clip.Aabb;
			// A split op draws only the band no later opaque rect covers, so that band wins over any widening.
			var split = op.CullScissor.Z > op.CullScissor.X;
			if (split) { scissorBox = Meet(scissorBox, op.CullScissor); }
			if (!TryScissor(scissorBox, out var sx, out var sy, out var sw, out var sh)) { continue; }
			// A widenable op's tight AABB is cull-only (checked above); the applied scissor is the full
			// surface, so consecutive such ops dedup to a single SetScissorRect.
			if (!split && op.SiteSlot == 0 && ScissorWidenable(op.Clip)) { sx = 0; sy = 0; sw = (int)(BasisW / BasisScale); sh = (int)(BasisH / BasisScale); }
			// On the layer sheet every draw stays inside its layer's slot, whatever its clip says; under damage it
			// stays inside the repainted region.
			if (_bound.X > float.MinValue)
			{
				if (!TryScissor(_bound, out var bx, out var by, out var bw, out var bh)) { continue; }
				int x1 = Math.Min(sx + sw, bx + bw), y1 = Math.Min(sy + sh, by + bh);
				sx = Math.Max(sx, bx); sy = Math.Max(sy, by); sw = x1 - sx; sh = y1 - sy;
				if (sw <= 0 || sh <= 0) { continue; }
			}
			pst.Enc.Scissor(sx, sy, sw, sh);
			pst.Scissors++;
			if (_emitStats && op.SharesBuffer && op.Kind is DrawKind.Image or DrawKind.Gradient) { pst.SharedOps++; }

			// The one merge rule: same draw state, and the next range starts where this one ends.
			uint count = op.Count;
			while (oi + 1 < end)
			{
				var nx = ops[oi + 1];
				if (nx.Kind != op.Kind || nx.Verts != op.Verts || nx.Group1 != op.Group1 || nx.ClipBg != op.ClipBg || nx.SiteBg != op.SiteBg
					|| nx.Clip.Aabb != op.Clip.Aabb || nx.CullScissor != op.CullScissor || nx.FirstVertex != op.FirstVertex + count) { break; }
				count += nx.Count; oi++;
			}

			var pipe = op.Kind switch
			{
				DrawKind.Solid => UseDepth ? _d.SolidPipeD : _d.SolidPipe,
				DrawKind.RoundedRect => UseDepth ? _d.RrPipeD : _d.RrPipe,
				DrawKind.Image => UseDepth ? _d.ImagePipeD : _d.ImagePipe,
				DrawKind.Mask => UseDepth ? _d.ImageDstInPipeD : _d.ImageDstInPipe,
				_ => UseDepth ? _d.GradientPipeD : _d.GradientPipe,
			};
			var stride = op.Kind switch
			{
				DrawKind.Solid => VertexStride.Solid,
				DrawKind.RoundedRect => VertexStride.RoundedRect,
				_ => VertexStride.Quad,
			};
			Buffer* buf;
			nuint bytes;
			if (op.SharesBuffer) { buf = SharedBuffer(op.Kind, ref pst, out bytes); }
			else { buf = (Buffer*)op.Verts; bytes = (nuint)((op.FirstVertex + count) * stride * sizeof(float)); }
			pst.Enc.Pipe(pipe);
			pst.Enc.Bg(0, pst.PassBg);
			if (op.Group1 != null) { pst.Enc.Bg(1, op.Group1); pst.Enc.Bg(2, op.ClipBg); }
			else { pst.Enc.Bg(1, op.ClipBg); }
			pst.Enc.Bg(3, op.SiteBg != null ? op.SiteBg : _d.IdentitySiteBg);
			pst.Enc.Vb(buf, 0, bytes);
			pst.Enc.Draw(count, op.FirstVertex);
		}
	}

	// The pass's shared vertex buffer for a kind, bound whole so a run of its ops binds it once.
	private static Buffer* SharedBuffer(DrawKind kind, ref PassOps pst, out nuint bytes)
	{
		switch (kind)
		{
			case DrawKind.Solid: bytes = pst.SolidBufBytes; return pst.SolidBuf;
			case DrawKind.RoundedRect: bytes = pst.RrectBufBytes; return pst.RrectBuf;
			case DrawKind.Gradient: bytes = pst.GradBufBytes; return pst.GradBuf;
			default: bytes = pst.QuadBufBytes; return pst.QuadBuf;
		}
	}

	/// <summary>
	/// Dumps the frame's encode counters (<c>UNO_WEBGPU_STATS=1</c>, every 60th frame) and clears them: how much got
	/// drawn, what the encoder had to change between draws, how the arena served replayed recordings, and what the
	/// atlas and the sheets did.
	/// </summary>
	private void WriteFrameStats(int opCount, ref PassOps pst)
	{
		var line = new System.Text.StringBuilder(512);
		line.Append($"[webgpu-stats] {Target.Width}x{Target.Height}:");
		line.Append($" ops={opCount} emitted={pst.Iters} sharedOps={pst.SharedOps}");
		line.Append($" scissorChanges={pst.Scissors} clipUp={_d.ClipSlab.LastFlushBytes / 1024}KB");
		line.Append($" arena={StatArenaHits} rebuilds={_statArenaRebuilds}(miss{_statArMiss}/masks{_statArMasks}) stamps={_statStamps} pool={StatPoolHits}/{StatPoolAdds} walked={StatWalkedRecords} walkPaths={StatWalkPaths} culled={StatCulled}/{StatSplit} clipCulled={StatClipCulled} ringBands={StatRingBands} prepass={StatPrepass} layers=shadow{StatLayerShadow}/matrix{StatLayerMatrix}/mask{StatLayerMask}/plain{StatLayerPlain}");
		line.Append($" fan=refused{WebGpuShapeCache.StatFanRefused}/points{WebGpuShapeCache.StatTessPoints}/tri{WebGpuShapeCache.StatTessTri}/area{WebGpuShapeCache.StatTessArea}/fold{WebGpuShapeCache.StatTessFold}");
		line.Append($" atlas=try{WebGpuCoverage.AtlasTried}/key-no{WebGpuCoverage.AtlasNoKey}/hit{WebGpuCoverage.AtlasHit}/baked{WebGpuCoverage.AtlasBaked} clipMasks={WebGpuCoverage.ClipMasksBaked} fillMasks={WebGpuCoverage.FillMaskHits}/{WebGpuCoverage.FillMasksBaked}/nocache{WebGpuCoverage.FillMaskUncached} sheet={WebGpuCoverage.SheetSlotsBaked} shadowSheet={WebGpuEffects.ShadowSlotsBaked} bakes={WebGpuCoverage.BakeBatches} layerSheet={WebGpuEffects.LayerSheetSlots}/{WebGpuEffects.LayerSheetPasses}");
		line.Append($"/full{WebGpuCoverage.AtlasNoRoom}/noedges{WebGpuCoverage.AtlasNoEdges}/scaleblk{WebGpuCoverage.ScaleBlocked}/big{WebGpuPathAtlas.RejBig}");
		line.Append($"/pages{_d.PathAtlas.Pages.Count}");
		System.Console.WriteLine(line.ToString());
		StatArenaHits = _statArenaRebuilds = _statArMiss = _statArMasks = _statStamps = StatWalkedRecords = StatWalkPaths = StatCulled = StatClipCulled = StatSplit = StatPrepass = StatLayerShadow = StatLayerMatrix = StatLayerMask = StatLayerPlain = 0;
		StatPoolHits = StatPoolAdds = 0;
	}
}
