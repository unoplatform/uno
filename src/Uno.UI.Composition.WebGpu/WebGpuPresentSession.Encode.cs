// Encoding ops into a render pass: one case per DrawKind, the backdrop's pass-segment split, and the per-frame
// stats dump.
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
	/// Encodes ops [<paramref name="start"/>, <paramref name="end"/>) into the pass, applying each op's scissor and
	/// path-clip mask as it goes. Split out of RenderInto so the command walk and the encode loop can each be read
	/// on their own.
	/// </summary>
	/// <summary>
	/// Encodes one backdrop (the acrylic path): ends the open pass so its MSAA resolves into the target view - the
	/// content BEHIND the backdrop - blurs the affected region, then opens a fresh pass that loads that content back
	/// and composites the blurred backdrop and its tint over the effect region. Ops after this one draw on top in the
	/// new pass, so each command is still encoded exactly once with no prefix re-render.
	/// </summary>
	/// <returns>The newly opened pass, which the caller is responsible for ending.</returns>
	private IntPtr EncodeBackdropSegment(BackdropCmd backdrop, ref PassOps pst)
	{
		var target = pst.Target;
		wgpuRenderPassEncoderEnd(pst.Pass);

		// Blur only the element AABB padded by the blur's reach, not the whole framebuffer.
		var effect = backdrop.Effect;
		float pad = MathF.Max(effect.SigmaX, effect.SigmaY) + 8f;
		var aabb = backdrop.Clip.Aabb;
		float regionX = MathF.Max(0f, aabb.X - pad), regionY = MathF.Max(0f, aabb.Y - pad);
		float regionW = MathF.Max(1f, MathF.Min(_s.Width, aabb.Z + pad) - regionX);
		float regionH = MathF.Max(1f, MathF.Min(_s.Height, aabb.W + pad) - regionY);
		var blurred = BlurPyramidRegion(target.View, _s.Width, _s.Height, regionX, regionY, regionW, regionH, effect.SigmaX, effect.SigmaY);

		var color = new WGPURenderPassColorAttachment
		{
			DepthSlice = uint.MaxValue,
			View = target.MsaaColorView,
			ResolveTarget = _d.MsaaSamples > 1 ? target.View : IntPtr.Zero,
			LoadOp = WGPULoadOp.Load,
			StoreOp = WGPUStoreOp.Store,   // a following segment, or another backdrop, reloads it
		};
		var depthStencil = new WGPURenderPassDepthStencilAttachment
		{
			View = target.DepthView,
			DepthLoadOp = WGPULoadOp.Clear,
			DepthStoreOp = WGPUStoreOp.Discard,
			DepthClearValue = 0f,
			StencilLoadOp = WGPULoadOp.Clear,
			StencilStoreOp = WGPUStoreOp.Discard,
			StencilClearValue = 0,
		};
		var desc = new WGPURenderPassDescriptor { ColorAttachmentCount = 1, ColorAttachments = &color, DepthStencilAttachment = &depthStencil };
		var pass = wgpuCommandEncoderBeginRenderPass(_frameEncoder, &desc);

		pst.Pass = pass;
		pst.Enc.Rebind(pass);
		pst.ClipFan = null; pst.ClipAabb = default;   // fresh pass: the depth mask went with the old one

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
	/// Draws the blurred backdrop over its region. Luminosity, noise and opacity ride in the 112-byte image uniform,
	/// so the acrylic recipe costs one textured quad.
	/// </summary>
	private void DrawBlurredBackdrop(ref PassOps pst, BackdropCmd backdrop, IntPtr blurred, Vector2 origin, Vector2 size)
	{
		var uniform = MakeUniform(112);
		var fields = stackalloc float[28];
		var lum = backdrop.Effect.LumColor;
		fields[0] = backdrop.Opacity;
		fields[3] = 1f;
		fields[4] = lum.R / 255f; fields[5] = lum.G / 255f; fields[6] = lum.B / 255f; fields[7] = lum.A / 255f;
		fields[24] = backdrop.Effect.Noise;
		wgpuQueueWriteBuffer(_d.Q, uniform, 0, (IntPtr)fields, 112);

		var entries = stackalloc WGPUBindGroupEntry[3];
		entries[0] = new WGPUBindGroupEntry { Binding = 0, TextureView = blurred };
		entries[1] = new WGPUBindGroupEntry { Binding = 1, Sampler = _d.Smp };
		entries[2] = new WGPUBindGroupEntry { Binding = 2, Buffer = uniform, Offset = 0, Size = 112 };
		var bgDesc = new WGPUBindGroupDescriptor { Layout = _d.ImgBgl, EntryCount = 3, Entries = entries };
		var imageBg = _d.TrackBg(wgpuDeviceCreateBindGroup(_d.Dev, &bgDesc));

		var verts = MakeBuffer(TexturedQuad(origin, size));
		var clipBg = MakeClipBg(_d.ImageClipBgl, backdrop.Clip);

		pst.Enc.Pipe(_d.ImagePipe);
		wgpuRenderPassEncoderSetBindGroup(pst.Pass, 0, (IntPtr)imageBg, 0, (uint*)null);
		wgpuRenderPassEncoderSetBindGroup(pst.Pass, 1, (IntPtr)clipBg, 0, (uint*)null);
		wgpuRenderPassEncoderSetVertexBuffer(pst.Pass, 0, (IntPtr)verts, 0, (nuint)(24 * sizeof(float)));
		pst.Enc.Reset();
		pst.Enc.Draw(6);
	}

	/// <summary>Draws the backdrop's tint colour over the effect region.</summary>
	private void DrawBackdropTint(ref PassOps pst, BackdropCmd backdrop)
	{
		var c = backdrop.Effect.Color;
		float r = c.R / 255f, g = c.G / 255f, b = c.B / 255f, a = c.A / 255f;
		var verts = new System.Collections.Generic.List<float>(36);
		void Vert(float x, float y)
		{
			var n = Ndc(new Vector2(x, y));
			verts.Add(n.X); verts.Add(n.Y); verts.Add(r); verts.Add(g); verts.Add(b); verts.Add(a);
		}

		var aabb = backdrop.Clip.Aabb;
		Vert(aabb.X, aabb.Y); Vert(aabb.Z, aabb.Y); Vert(aabb.Z, aabb.W);
		Vert(aabb.X, aabb.Y); Vert(aabb.Z, aabb.W); Vert(aabb.X, aabb.W);

		var buf = MakeBuffer(verts);
		var clipBg = MakeClipBg(_d.SolidClipBgl, backdrop.Clip);

		pst.Enc.Pipe(_d.SolidPipe);
		wgpuRenderPassEncoderSetBindGroup(pst.Pass, 0, (IntPtr)clipBg, 0, (uint*)null);
		wgpuRenderPassEncoderSetVertexBuffer(pst.Pass, 0, (IntPtr)buf, 0, (nuint)(36 * sizeof(float)));
		pst.Enc.Reset();
		pst.Enc.Draw(6);
	}

	private void EncodeOps(int start, int end, ref PassOps pst)
	{
		var pass = pst.Pass;
		var target = pst.Target;
		var ops = pst.Ops;
		var backdrops = pst.Backdrops;
		var solidBuf = pst.SolidBuf; var solidBufBytes = pst.SolidBufBytes;
		var rrectBuf = pst.RrectBuf;
		var gradBuf = pst.GradBuf; var gradBufBytes = pst.GradBufBytes;
		var quadBuf = pst.QuadBuf; var quadBufBytes = pst.QuadBufBytes;
		var pathBuf = pst.PathBuf; var pathBufBytes = pst.PathBufBytes;
		var xformBg = pst.XformBg;

		for (int oi = start; oi < end; oi++)
		{
			var (kind, b0, u0, b1, flag, clip, clipBg) = ops[oi];
			pst.Iters++;
			if (_emitStats && clip.PathFan is not null) { pst.FanOps++; }
			if (_emitStats && (kind == DrawKind.TablePath || (kind is DrawKind.Image or DrawKind.Gradient or DrawKind.TilingFan && flag))) { pst.SharedOps++; }
			if (_emitStats && kind == DrawKind.TilingFan) { pst.Tiled++; }
			// Fragment area the stencil-then-cover path actually rasterises: the cover quad spans the whole
			// bbox even when the shape is a 2px stroke outline, so this is where the waste shows up.
			if (_emitStats && kind is DrawKind.Path or DrawKind.TablePath)
			{
				var cb = clip.Aabb;
				var cw = Math.Min(cb.Z, _s.Width) - Math.Max(cb.X, 0);
				var chh = Math.Min(cb.W, _s.Height) - Math.Max(cb.Y, 0);
				if (cw > 0 && chh > 0) { pst.CoverMpx += cw * chh / 1e6; }
			}
			if (!ReferenceEquals(clip.PathFan, pst.ClipFan))
			{
				ApplyDepthClip(pass, pst.ClipFan, pst.ClipAabb, clip);
				pst.Enc.Reset();
				pst.ClipFan = clip.PathFan; pst.ClipAabb = clip.Aabb;
				pst.Enc.Reset();   // the clip setup changed pipeline + scissor state
				pst.ClipChanges++;
			}
			if (!TryScissor(clip.Aabb, out var sx, out var sy, out var sw, out var sh)) { continue; }
			// A widenable op's tight AABB is cull-only (checked above); the applied scissor is the full
			// surface, so consecutive such ops dedup to a single SetScissorRect.
			if (ScissorWidenable(clip)) { sx = 0; sy = 0; sw = (int)_s.Width; sh = (int)_s.Height; }
			if (!pst.Enc.Recording)
			{
				pst.Enc.Scissor(sx, sy, sw, sh);
				pst.Scissors++;
			}
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
							if (nx.kind != DrawKind.Solid || nx.b0 != VertexSource.PassBuffer || nx.clipBg != clipBg
								|| !ReferenceEquals(nx.clip.PathFan, clip.PathFan) || nx.clip.Aabb != clip.Aabb) { break; }
							count += nx.u0; oi++;
						}
						pst.Enc.Pipe(_d.SolidPipe);
						pst.Enc.Bg(0, (IntPtr)clipBg);
						pst.Enc.Vb(solidBuf, (nuint)(startVert * 6 * sizeof(float)), (nuint)(count * 6 * sizeof(float)));
						pst.Enc.Draw(count);
						break;
					}
				case DrawKind.Solid when b0 == VertexSource.Slab:
					{
						// Coalesce a byte-contiguous run sharing this clip and bind group.
						int byteOff = (int)b1; uint count = u0;
						while (oi + 1 < end)
						{
							var nx = ops[oi + 1];
							if (nx.kind != DrawKind.Solid || nx.b0 != VertexSource.Slab || nx.clipBg != clipBg || !ReferenceEquals(nx.clip.PathFan, clip.PathFan)
								|| nx.clip.Aabb != clip.Aabb || (int)nx.b1 != byteOff + (int)(count * 6 * sizeof(float))) { break; }
							count += nx.u0; oi++;
						}
						pst.Enc.Pipe(_d.SolidPipe);
						pst.Enc.Bg(0, (IntPtr)clipBg);
						pst.Enc.Vb(_d.SolidSlab.Buf, (nuint)byteOff, (nuint)(count * 6 * sizeof(float)));
						pst.Enc.Draw(count);
						break;
					}
				case DrawKind.Solid when b0 == VertexSource.TableSlab:
					{
						// Resident SOLID TABLE SLAB (b1 = absolute byte offset, stride 7 = pos+col+slot). Group 0 = the
						// transform table (each vertex's slot positions it), group 1 = ClipU. Coalesce byte-contiguous
						// same-clip runs ACROSS recordings — each vertex still carries its own slot, so one draw is correct.
						int byteOff = (int)b1; uint count = u0;
						while (oi + 1 < end)
						{
							var nx = ops[oi + 1];
							if (nx.kind != DrawKind.Solid || nx.b0 != VertexSource.TableSlab || nx.clipBg != clipBg || !ReferenceEquals(nx.clip.PathFan, clip.PathFan)
								|| nx.clip.Aabb != clip.Aabb || (int)nx.b1 != byteOff + (int)(count * 7 * sizeof(float))) { break; }
							count += nx.u0; oi++;
						}
						pst.Enc.Pipe(_d.SolidTablePipe);
						pst.Enc.Bg(0, (IntPtr)xformBg);
						pst.Enc.Bg(1, (IntPtr)clipBg);
						pst.Enc.Vb(_d.SolidTableSlab.Buf, (nuint)byteOff, (nuint)(count * 7 * sizeof(float)));
						pst.Enc.Draw(count);
						break;
					}
				case DrawKind.Solid:
					// b0 = vertex buffer (private/immediate or a resident frame-solid buffer); b1 = byte offset into it.
					pst.Enc.Pipe(_d.SolidPipe);
					pst.Enc.Bg(0, (IntPtr)clipBg);
					if (b0 == solidBuf)
					{
						// Whole shared buffer bound once (dedups across the run); the op's slice is a vertex offset.
						pst.Enc.Vb((IntPtr)b0, 0, solidBufBytes);
						pst.Enc.Draw(u0, (uint)(b1 / (6 * sizeof(float))));
					}
					else
					{
						pst.Enc.Vb((IntPtr)b0, (nuint)b1, (nuint)(u0 * 6 * sizeof(float)));
						pst.Enc.Draw(u0);   // u0 = 6 * (coalesced) rect count
					}
					break;
				case DrawKind.Path:
					// Path fill via the transform table: fan verts = device pos + slot index (stride 3); cover verts =
					// device pos + colour + slot index (stride 7). Group 0 = storage table (positions the verts);
					// group 1 (cover) = ClipU (analytic clip coverage). Table entries were written during op-build.
					pst.Enc.Pipe(flag ? _d.StencilTableEO : _d.StencilTableNZ);
					pst.Enc.Bg(0, (IntPtr)xformBg);
					pst.Enc.Vb((IntPtr)b0, 0, (nuint)(u0 * 3 * sizeof(float)));
					pst.Enc.Draw(u0);
					pst.Enc.Pipe(_d.CoverTablePipe);
					pst.Enc.Bg(0, (IntPtr)xformBg);
					pst.Enc.Bg(1, (IntPtr)clipBg);
					pst.Enc.Vb((IntPtr)b1, 0, (nuint)(42 * sizeof(float)));
					pst.Enc.Draw(6);
					break;
				case DrawKind.TilingFan:
					// Single-pass fill of a tiling fan (see PathFill.FanTiles). Uses the stencil-independent
					// cover pipeline: there is no stencil pass here, so the masked one would discard everything.
					pst.Enc.Pipe(_d.CoverTableDirectPipe);
					pst.Enc.Bg(0, (IntPtr)xformBg);
					pst.Enc.Bg(1, (IntPtr)clipBg);
					if (flag)
					{
						pst.Enc.Vb((IntPtr)pathBuf, 0, pathBufBytes);
						pst.Enc.Draw(u0, (uint)(b0 / (7 * sizeof(float))));
					}
					else
					{
						pst.Enc.Vb((IntPtr)b0, 0, (nuint)(u0 * 7 * sizeof(float)));
						pst.Enc.Draw(u0);
					}
					break;
				case DrawKind.TablePath:
					// Shared-buffer path fill: same as DrawKind.Path, but b0/b1 are byte offsets into pathBuf, so the
					// vertex buffer is bound once for the whole pass instead of twice per fill.
					pst.Enc.Pipe(flag ? _d.StencilTableEO : _d.StencilTableNZ);
					pst.Enc.Bg(0, (IntPtr)xformBg);
					pst.Enc.Vb((IntPtr)pathBuf, 0, pathBufBytes);
					pst.Enc.Draw(u0, (uint)(b0 / (3 * sizeof(float))));
					pst.Enc.Pipe(_d.CoverTablePipe);
					pst.Enc.Bg(0, (IntPtr)xformBg);
					pst.Enc.Bg(1, (IntPtr)clipBg);
					pst.Enc.Vb((IntPtr)pathBuf, 0, pathBufBytes);
					pst.Enc.Draw(6, (uint)(b1 / (7 * sizeof(float))));
					break;
				case DrawKind.Image:
					pst.Enc.Pipe(_d.ImagePipe);
					pst.Enc.Bg(0, (IntPtr)b0);
					pst.Enc.Bg(1, (IntPtr)clipBg);
					if (flag)
					{
						pst.Enc.Vb((IntPtr)quadBuf, 0, quadBufBytes);
						pst.Enc.Draw(6, (uint)(b1 / (4 * sizeof(float))));
					}
					else
					{
						var atlasVerts = u0 == 0 ? 6u : u0;
						pst.Enc.Vb((IntPtr)b1, 0, (nuint)(atlasVerts * 4 * sizeof(float)));
						pst.Enc.Draw(atlasVerts);
					}
					break;
				case DrawKind.Gradient:
					{
						var gn = u0 == 0 ? 6u : u0;   // 6 = quad, else the clip-tightened n-gon
						pst.Enc.Pipe(_d.GradientPipe);
						pst.Enc.Bg(0, (IntPtr)b0);
						pst.Enc.Bg(1, (IntPtr)clipBg);
						if (flag)
						{
							pst.Enc.Vb((IntPtr)gradBuf, 0, gradBufBytes);
							pst.Enc.Draw(gn, (uint)(b1 / (2 * sizeof(float))));
						}
						else
						{
							pst.Enc.Vb((IntPtr)b1, 0, (nuint)(gn * 2 * sizeof(float)));
							pst.Enc.Draw(gn);
						}
						break;
					}
				case DrawKind.CompositeLayer:
					wgpuRenderPassEncoderSetPipeline(pass, u0 == 1 ? _d.CompositeDstIn : _d.CompositeSrcOver);
					pst.Enc.Reset();   // set directly, so the dedup cache no longer reflects the encoder
					pst.Enc.Bg(0, (IntPtr)b0);
					wgpuRenderPassEncoderDraw(pass, 3, 1, 0, 0);
					break;
				case DrawKind.BackdropSegment:
					pass = EncodeBackdropSegment(backdrops[(int)b1], ref pst);
					break;
				case DrawKind.RoundedRect when b0 == VertexSource.PassBuffer:
					{
						// Shared rrect buffer (b1=start vert, u0=6). COALESCE the run of following rrect ops sharing this
						// clip bind group + clip: their 22-float verts are contiguous, so the run draws in ONE call.
						int startVert = (int)b1; uint count = u0;
						while (oi + 1 < end)
						{
							var nx = ops[oi + 1];
							if (nx.kind != DrawKind.RoundedRect || nx.b0 != VertexSource.PassBuffer || nx.clipBg != clipBg
								|| !ReferenceEquals(nx.clip.PathFan, clip.PathFan) || nx.clip.Aabb != clip.Aabb) { break; }
							count += nx.u0; oi++;
						}
						pst.Enc.Pipe(_d.RrPipe);
						pst.Enc.Bg(0, (IntPtr)clipBg);
						pst.Enc.Vb(rrectBuf, (nuint)(startVert * 22 * sizeof(float)), (nuint)(count * 22 * sizeof(float)));
						pst.Enc.Draw(count);
						break;
					}
				case DrawKind.RoundedRect when b0 == VertexSource.Slab:
					{
						// Resident RRECT SLAB (b1 = absolute byte offset). Coalesce byte-contiguous same-clip runs.
						int byteOff = (int)b1; uint count = u0;
						while (oi + 1 < end)
						{
							var nx = ops[oi + 1];
							if (nx.kind != DrawKind.RoundedRect || nx.b0 != VertexSource.Slab || nx.clipBg != clipBg || !ReferenceEquals(nx.clip.PathFan, clip.PathFan)
								|| nx.clip.Aabb != clip.Aabb || (int)nx.b1 != byteOff + (int)(count * 22 * sizeof(float))) { break; }
							count += nx.u0; oi++;
						}
						pst.Enc.Pipe(_d.RrPipe);
						pst.Enc.Bg(0, (IntPtr)clipBg);
						pst.Enc.Vb(_d.RrectSlab.Buf, (nuint)byteOff, (nuint)(count * 22 * sizeof(float)));
						pst.Enc.Draw(count);
						break;
					}
				case DrawKind.RoundedRect when b0 == VertexSource.TableSlab:
					{
						// Resident RRECT TABLE SLAB (b1 = absolute byte offset, stride 23). Group 0 = the transform table
						// (per-vertex slot positions the local corners), group 1 = ClipU. Coalesce byte-contiguous same-clip runs.
						int byteOff = (int)b1; uint count = u0;
						while (oi + 1 < end)
						{
							var nx = ops[oi + 1];
							if (nx.kind != DrawKind.RoundedRect || nx.b0 != VertexSource.TableSlab || nx.clipBg != clipBg || !ReferenceEquals(nx.clip.PathFan, clip.PathFan)
								|| nx.clip.Aabb != clip.Aabb || (int)nx.b1 != byteOff + (int)(count * 23 * sizeof(float))) { break; }
							count += nx.u0; oi++;
						}
						pst.Enc.Pipe(_d.RrTablePipe);
						pst.Enc.Bg(0, (IntPtr)xformBg);
						pst.Enc.Bg(1, (IntPtr)clipBg);
						pst.Enc.Vb(_d.RrectTableSlab.Buf, (nuint)byteOff, (nuint)(count * 23 * sizeof(float)));
						pst.Enc.Draw(count);
						break;
					}
				case DrawKind.RoundedRect:
					// b0 = vertex buffer (resident frame-solid or legacy per-op); b1 = byte offset; u0 = vertex count.
					pst.Enc.Pipe(_d.RrPipe);
					pst.Enc.Bg(0, (IntPtr)clipBg);
					pst.Enc.Vb((IntPtr)b0, (nuint)b1, (nuint)(u0 * 22 * sizeof(float)));
					pst.Enc.Draw(u0);
					break;
			}
		}
	}

	/// <summary>
	/// Dumps the frame's encode counters (<c>UNO_WEBGPU_STATS=1</c>, every 60th frame) and clears them, grouped by
	/// the question each set answers: how much got drawn, what the encoder had to change between draws, how much of
	/// the scene came from reused recordings, and why the reuse and admission paths turned work away.
	/// </summary>
	private void WriteFrameStats(int opCount, ref PassOps pst, int bundleReplay, int bundleWrite)
	{
		var line = new System.Text.StringBuilder(512);
		line.Append($"[webgpu-stats] {_s.Width}x{_s.Height}:");

		// Drawn
		line.Append($" ops={opCount} emitted={pst.Iters} sharedOps={pst.SharedOps} tiled={pst.Tiled}");
		line.Append($" coverMpx={pst.CoverMpx:F1} strips={WgStrokeStats.Strips} tilesCmd={WgStrokeStats.TilesCmd}");

		// Changed between draws
		line.Append($" scissorChanges={pst.Scissors} clipChanges={pst.ClipChanges} fanOps={pst.FanOps}");
		line.Append($" bundle=r{bundleReplay}+w{bundleWrite} clipUp={_d.ClipSlab.LastFlushBytes / 1024}KB");

		// Reused
		line.Append($" replays=c{WebGpuCommandRecorder.StatCacheableReplays}+i{WebGpuCommandRecorder.StatInlineReplays}");
		line.Append($" inlineCmds={WebGpuCommandRecorder.StatInlineCmds}");
		line.Append($" block=ref{WebGpuCommandRecorder.StatBlockRef}/layer{WebGpuCommandRecorder.StatBlockLayer}");
		line.Append($"/shadow{WebGpuCommandRecorder.StatBlockShadow}/other{WebGpuCommandRecorder.StatBlockOther}");
		line.Append($"/empty{WebGpuCommandRecorder.StatBlockEmpty}");

		// Rebuilt anyway, and why
		line.Append($" tableRebuilds={_statTableRebuilds} arenaRebuilds={_statArenaRebuilds} stamps={_statStamps}");
		line.Append($" cachedRebuilds={_statCachedRebuilds}(miss{_statCrMiss}/move{_statCrMove}");
		line.Append($"/flip{_statCrPathFlip}/size{_statCrSize}/clip{_statCrClip})");

		// Turned away, and why
		line.Append($" fanTry=t{StatFanTried}/ok{StatFanStripped}/big{StatFanTooBig}");
		line.Append($"/concave{StatFanConcave}/nocover{StatFanNotCovering}");
		line.Append($" atlas=try{AtlasTried}/key-no{AtlasNoKey}/hit{AtlasHit}/baked{AtlasBaked}");
		line.Append($"/full{AtlasNoRoom}/ring{AtlasNoRing}/scaleblk{ScaleBlocked}/big{WebGpuPathAtlas.RejBig}");
		line.Append($"/pages{_d.PathAtlas.Pages.Count}");

		System.Console.WriteLine(line.ToString());

		WebGpuCommandRecorder.StatCacheableReplays = WebGpuCommandRecorder.StatInlineReplays = WebGpuCommandRecorder.StatInlineCmds = 0;
		StatFanTried = StatFanStripped = StatFanTooBig = StatFanConcave = StatFanNotCovering = 0;
		_statTableRebuilds = _statStamps = _statArenaRebuilds = _statCachedRebuilds = 0;
		_statCrMiss = _statCrMove = _statCrPathFlip = _statCrSize = _statCrClip = 0;
	}
}
