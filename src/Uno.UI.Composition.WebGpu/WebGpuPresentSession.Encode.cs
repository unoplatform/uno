// Encoding ops into a render pass: one case per DrawKind, the backdrop's pass-segment split, and the per-frame
// stats dump.
#nullable disable
using System;
using System.Collections.Generic;
using System.Numerics;
using Uno.WebGpu.Native;
using static Uno.WebGpu.Native.WGPU;

namespace Uno.UI.Composition.WebGpu;

public sealed unsafe partial class WebGpuPresentSession
{
	/// <summary>
	/// Encodes one backdrop (the acrylic path): ends the open pass so the target holds the content BEHIND the backdrop,
	/// blurs the affected region, then opens a fresh pass that loads that content back and composites the blurred
	/// backdrop and its tint over the effect region. Ops after this one draw on top in the new pass, so each command is
	/// still encoded exactly once with no prefix re-render.
	/// </summary>
	/// <returns>The newly opened pass, which the caller is responsible for ending.</returns>
	private IntPtr EncodeBackdropSegment(BackdropCmd backdrop, ref PassOps pst)
	{
		var target = pst.Target;
		wgpuRenderPassEncoderEnd(pst.Pass);

		// Blur only the element AABB, not the whole framebuffer -- and not a padded version of it either: a
		// backdrop samples the element's OWN backdrop, so reaching outside pulls in whatever sits behind the
		// neighbours. The pyramid samples clamp-to-edge, so the element's own edge pixels extend outward instead.
		var effect = backdrop.Effect;
		var aabb = backdrop.Clip.Aabb;
		float regionX = Math.Clamp(aabb.X, 0f, _s.Width), regionY = Math.Clamp(aabb.Y, 0f, _s.Height);
		float regionW = MathF.Max(1f, MathF.Min(_s.Width, aabb.Z) - regionX);
		float regionH = MathF.Max(1f, MathF.Min(_s.Height, aabb.W) - regionY);
		var blurred = BlurPyramidRegion(target.View, _s.Width, _s.Height, regionX, regionY, regionW, regionH, effect.SigmaX, effect.SigmaY);

		var color = new WGPURenderPassColorAttachment
		{
			DepthSlice = uint.MaxValue,
			View = target.View,
			LoadOp = WGPULoadOp.Load,
			StoreOp = WGPUStoreOp.Store,   // a following segment, or another backdrop, reloads it
		};
		var desc = new WGPURenderPassDescriptor { ColorAttachmentCount = 1, ColorAttachments = &color };
		var pass = wgpuCommandEncoderBeginRenderPass(_frameEncoder, &desc);

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
	private void DrawBlurredBackdrop(ref PassOps pst, BackdropCmd backdrop, IntPtr blurred, Vector2 origin, Vector2 size)
	{
		var uniform = MakeUniform(WebGpuDevice.ImageUniformBytes);
		var fields = stackalloc float[36];
		for (var zi = 0; zi < 36; zi++) { fields[zi] = 0f; }
		var lum = backdrop.Effect.LumColor;
		fields[0] = backdrop.Opacity;
		fields[3] = 1f;
		fields[4] = lum.R / 255f; fields[5] = lum.G / 255f; fields[6] = lum.B / 255f; fields[7] = lum.A / 255f;
		fields[24] = backdrop.Effect.Noise;
		wgpuQueueWriteBuffer(_d.Q, uniform, 0, (IntPtr)fields, WebGpuDevice.ImageUniformBytes);

		var entries = stackalloc WGPUBindGroupEntry[3];
		entries[0] = new WGPUBindGroupEntry { Binding = 0, TextureView = blurred };
		entries[1] = new WGPUBindGroupEntry { Binding = 1, Sampler = _d.Smp };
		entries[2] = new WGPUBindGroupEntry { Binding = 2, Buffer = uniform, Offset = 0, Size = WebGpuDevice.ImageUniformBytes };
		var bgDesc = new WGPUBindGroupDescriptor { Layout = _d.ImgBgl, EntryCount = 3, Entries = entries };
		var imageBg = _d.TrackBg(wgpuDeviceCreateBindGroup(_d.Dev, &bgDesc));

		var verts = MakeBuffer(TexturedQuad(origin, size));
		var clipBg = MakeClipBg(backdrop.Clip);

		pst.Enc.Pipe(_d.ImagePipe);
		wgpuRenderPassEncoderSetBindGroup(pst.Pass, 0, pst.PassBg, 0, (uint*)null);
		wgpuRenderPassEncoderSetBindGroup(pst.Pass, 1, (IntPtr)imageBg, 0, (uint*)null);
		wgpuRenderPassEncoderSetBindGroup(pst.Pass, 2, (IntPtr)clipBg, 0, (uint*)null);
		wgpuRenderPassEncoderSetVertexBuffer(pst.Pass, 0, (IntPtr)verts, 0, (nuint)(24 * sizeof(float)));
		pst.Enc.Reset();
		pst.Enc.Draw(6);
	}

	/// <summary>Draws the backdrop's tint colour over the effect region.</summary>
	private void DrawBackdropTint(ref PassOps pst, BackdropCmd backdrop)
	{
		var c = backdrop.Effect.Color;
		float r = c.R / 255f, g = c.G / 255f, b = c.B / 255f, a = c.A / 255f;
		var verts = new List<float>(48);
		void Vert(float x, float y)
		{
			verts.Add(x); verts.Add(y); verts.Add(r); verts.Add(g); verts.Add(b); verts.Add(a); verts.Add(0f); verts.Add(0f);
		}
		var aabb = backdrop.Clip.Aabb;
		Vert(aabb.X, aabb.Y); Vert(aabb.Z, aabb.Y); Vert(aabb.Z, aabb.W);
		Vert(aabb.X, aabb.Y); Vert(aabb.Z, aabb.W); Vert(aabb.X, aabb.W);

		var buf = MakeBuffer(verts);
		var clipBg = MakeClipBg(backdrop.Clip);

		pst.Enc.Pipe(_d.SolidPipe);
		wgpuRenderPassEncoderSetBindGroup(pst.Pass, 0, pst.PassBg, 0, (uint*)null);
		wgpuRenderPassEncoderSetBindGroup(pst.Pass, 1, (IntPtr)clipBg, 0, (uint*)null);
		wgpuRenderPassEncoderSetVertexBuffer(pst.Pass, 0, (IntPtr)buf, 0, (nuint)(6 * VertexStride.Solid * sizeof(float)));
		pst.Enc.Reset();
		pst.Enc.Draw(6);
	}

	/// <summary>
	/// Encodes ops [<paramref name="start"/>, <paramref name="end"/>) into the pass, applying each op's scissor as it
	/// goes. Runs of same-clip solids or rounded rects in the pass buffers collapse into one draw each.
	/// </summary>
	private void EncodeOps(int start, int end, ref PassOps pst)
	{
		var pass = pst.Pass;
		var ops = pst.Ops;
		var backdrops = pst.Backdrops;
		var solidBuf = pst.SolidBuf; var solidBufBytes = pst.SolidBufBytes;
		var rrectBuf = pst.RrectBuf;
		var gradBuf = pst.GradBuf; var gradBufBytes = pst.GradBufBytes;
		var quadBuf = pst.QuadBuf; var quadBufBytes = pst.QuadBufBytes;

		for (int oi = start; oi < end; oi++)
		{
			var (kind, b0, u0, b1, flag, clip, clipBg) = ops[oi];
			pst.Iters++;
			if (_emitStats && kind is DrawKind.Image or DrawKind.Gradient && flag) { pst.SharedOps++; }
			if (!TryScissor(clip.Aabb, out var sx, out var sy, out var sw, out var sh)) { continue; }
			// A widenable op's tight AABB is cull-only (checked above); the applied scissor is the full
			// surface, so consecutive such ops dedup to a single SetScissorRect.
			if (ScissorWidenable(clip)) { sx = 0; sy = 0; sw = (int)BasisW; sh = (int)BasisH; }
			// On the layer sheet every draw stays inside its layer's slot, whatever its clip says.
			if (_bound.X > float.MinValue)
			{
				if (!TryScissor(_bound, out var bx, out var by, out var bw, out var bh)) { continue; }
				int x1 = Math.Min(sx + sw, bx + bw), y1 = Math.Min(sy + sh, by + bh);
				sx = Math.Max(sx, bx); sy = Math.Max(sy, by); sw = x1 - sx; sh = y1 - sy;
				if (sw <= 0 || sh <= 0) { continue; }
			}
			pst.Enc.Scissor(sx, sy, sw, sh);
			pst.Scissors++;
			switch (kind)
			{
				case DrawKind.Solid when b0 == VertexSource.PassBuffer:
					{
						// Coalesce the maximal following run that shares this clip and bind group: their verts are
						// contiguous in the shared buffer by construction, so the whole run draws in ONE call.
						int startVert = (int)b1; uint count = u0;
						while (oi + 1 < end)
						{
							var nx = ops[oi + 1];
							if (nx.kind != DrawKind.Solid || nx.b0 != VertexSource.PassBuffer || nx.clipBg != clipBg || nx.clip.Aabb != clip.Aabb) { break; }
							count += nx.u0; oi++;
						}
						pst.Enc.Pipe(_d.SolidPipe);
						pst.Enc.Bg(0, pst.PassBg);
						pst.Enc.Bg(1, (IntPtr)clipBg);
						pst.Enc.Vb(solidBuf, (nuint)(startVert * VertexStride.Solid * sizeof(float)), (nuint)(count * VertexStride.Solid * sizeof(float)));
						pst.Enc.Draw(count);
						break;
					}
				case DrawKind.Solid:
					// b0 = the op's own vertex buffer; b1 = byte offset into it; u0 = vertex count.
					pst.Enc.Pipe(_d.SolidPipe);
					pst.Enc.Bg(0, pst.PassBg);
					pst.Enc.Bg(1, (IntPtr)clipBg);
					pst.Enc.Vb((IntPtr)b0, (nuint)b1, (nuint)(u0 * VertexStride.Solid * sizeof(float)));
					pst.Enc.Draw(u0);
					break;
				case DrawKind.Image:
				case DrawKind.Mask:
					pst.Enc.Pipe(kind == DrawKind.Mask ? _d.ImageDstInPipe : _d.ImagePipe);
					pst.Enc.Bg(0, pst.PassBg);
					pst.Enc.Bg(1, (IntPtr)b0);
					pst.Enc.Bg(2, (IntPtr)clipBg);
					if (flag)
					{
						pst.Enc.Vb((IntPtr)quadBuf, 0, quadBufBytes);
						pst.Enc.Draw(6, (uint)(b1 / (4 * sizeof(float))));
					}
					else
					{
						var verts = u0 == 0 ? 6u : u0;
						pst.Enc.Vb((IntPtr)b1, 0, (nuint)(verts * 4 * sizeof(float)));
						pst.Enc.Draw(verts);
					}
					break;
				case DrawKind.Gradient:
					{
						var gn = u0 == 0 ? 6u : u0;   // 6 = quad, else the clip-tightened n-gon
						pst.Enc.Pipe(_d.GradientPipe);
						pst.Enc.Bg(0, pst.PassBg);
						pst.Enc.Bg(1, (IntPtr)b0);
						pst.Enc.Bg(2, (IntPtr)clipBg);
						if (flag)
						{
							pst.Enc.Vb((IntPtr)gradBuf, 0, gradBufBytes);
							pst.Enc.Draw(gn, (uint)(b1 / (4 * sizeof(float))));
						}
						else
						{
							pst.Enc.Vb((IntPtr)b1, 0, (nuint)(gn * 4 * sizeof(float)));
							pst.Enc.Draw(gn);
						}
						break;
					}
				case DrawKind.BackdropSegment:
					pass = EncodeBackdropSegment(backdrops[(int)b1], ref pst);
					break;
				case DrawKind.RoundedRect when b0 == VertexSource.PassBuffer:
					{
						// Shared rrect buffer (b1 = start vert, u0 = 6): the run of following rrect ops sharing this clip
						// bind group and clip is contiguous, so it draws in ONE call.
						int startVert = (int)b1; uint count = u0;
						while (oi + 1 < end)
						{
							var nx = ops[oi + 1];
							if (nx.kind != DrawKind.RoundedRect || nx.b0 != VertexSource.PassBuffer || nx.clipBg != clipBg || nx.clip.Aabb != clip.Aabb) { break; }
							count += nx.u0; oi++;
						}
						pst.Enc.Pipe(_d.RrPipe);
						pst.Enc.Bg(0, pst.PassBg);
						pst.Enc.Bg(1, (IntPtr)clipBg);
						pst.Enc.Vb(rrectBuf, (nuint)(startVert * RrectStride * sizeof(float)), (nuint)(count * RrectStride * sizeof(float)));
						pst.Enc.Draw(count);
						break;
					}
				case DrawKind.RoundedRect:
					// b0 = the op's own vertex buffer; b1 = byte offset; u0 = vertex count.
					pst.Enc.Pipe(_d.RrPipe);
					pst.Enc.Bg(0, pst.PassBg);
					pst.Enc.Bg(1, (IntPtr)clipBg);
					pst.Enc.Vb((IntPtr)b0, (nuint)b1, (nuint)(u0 * RrectStride * sizeof(float)));
					pst.Enc.Draw(u0);
					break;
			}
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
		line.Append($"[webgpu-stats] {_s.Width}x{_s.Height}:");
		line.Append($" ops={opCount} emitted={pst.Iters} sharedOps={pst.SharedOps}");
		line.Append($" scissorChanges={pst.Scissors} clipUp={_d.ClipSlab.LastFlushBytes / 1024}KB");
		line.Append($" arena={StatArenaHits} rebuilds={_statArenaRebuilds}(miss{_statArMiss}/masks{_statArMasks}) stamps={_statStamps}");
		line.Append($" fan=refused{WebGpuShapeCache.StatFanRefused}/points{WebGpuShapeCache.StatTessPoints}/tri{WebGpuShapeCache.StatTessTri}/area{WebGpuShapeCache.StatTessArea}/fold{WebGpuShapeCache.StatTessFold}");
		line.Append($" atlas=try{AtlasTried}/key-no{AtlasNoKey}/hit{AtlasHit}/baked{AtlasBaked} clipMasks={ClipMasksBaked} fillMasks={FillMasksBaked} sheet={SheetSlotsBaked} shadowSheet={ShadowSlotsBaked} bakes={BakeBatches} layerSheet={LayerSheetSlots}/{LayerSheetPasses}");
		line.Append($"/full{AtlasNoRoom}/noedges{AtlasNoEdges}/scaleblk{ScaleBlocked}/big{WebGpuPathAtlas.RejBig}");
		line.Append($"/pages{_d.PathAtlas.Pages.Count}");
		System.Console.WriteLine(line.ToString());
		StatArenaHits = _statArenaRebuilds = _statArMiss = _statArMasks = _statStamps = 0;
	}
}
