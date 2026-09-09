// Coverage bakes are batched per target texture: every mask a frame bakes into one texture (an atlas page, a
// standalone entry, the per-frame sheet holding masks nothing owns) goes through ONE accumulate pass and ONE resolve
// pass, so a frame that bakes thousands of small shapes costs a pass pair per page, not per shape.
#nullable disable
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using Uno.WebGpu.Native;
using static Uno.WebGpu.Native.WGPU;

namespace Uno.UI.Composition.WebGpu;

public sealed unsafe partial class WebGpuPresentSession
{
	private const int SheetSize = 2048;

	private sealed class BakeBatch
	{
		public IntPtr Target;                          // the texture the resolve writes; consumers bind it right away
		public int TargetW, TargetH;
		public bool Load;                              // an atlas page keeps its other entries; a fresh texture is cleared
		public int MinX = int.MaxValue, MinY = int.MaxValue, MaxX, MaxY;   // union of the slots: the accumulator's extent
		public readonly List<float> Edges = new();     // x0,y0,x1,y1 per edge, in target pixels
		public readonly List<float> Ext = new();       // per edge: the right bound of its slot, so its quad stops there
		public readonly List<float> Slots = new();     // per slot: x, y, w, h, evenOdd, exclude
		public int Count;
		public int CursorX, ShelfY, ShelfH;            // shelf packing; frame sheets only
	}

	private readonly List<BakeBatch> _pendingBakes = new();
	private readonly Dictionary<IntPtr, BakeBatch> _bakeByTarget = new();
	private BakeBatch _sheet;   // the frame sheet still taking slots

	// The batch writing into a texture this frame, opened on first use. Atlas pages Load: the resolve touches only
	// the new slots and everything else on the page must survive.
	private BakeBatch BatchFor(IntPtr target, int w, int h, bool load)
	{
		if (!_bakeByTarget.TryGetValue(target, out var b))
		{
			b = new BakeBatch { Target = target, TargetW = w, TargetH = h, Load = load };
			_bakeByTarget[target] = b;
			_pendingBakes.Add(b);
		}
		return b;
	}

	// Reserves a w x h slot on the frame's sheet, opening another when it is full. False when the mask is larger than
	// a sheet; such a mask bakes into a texture of its own.
	private bool TryReserveSheetSlot(int w, int h, out BakeBatch sheet, out int x, out int y)
	{
		sheet = null; x = y = 0;
		if (w > SheetSize || h > SheetSize) { return false; }
		var cur = _sheet;
		if (cur is not null)
		{
			if (cur.CursorX + w > SheetSize) { cur.ShelfY += cur.ShelfH; cur.ShelfH = 0; cur.CursorX = 0; }
			if (cur.ShelfY + h > SheetSize) { cur = null; }
		}
		if (cur is null)
		{
			var view = _d.Pool.Rent(SheetSize, SheetSize, 1, WGPUTextureUsage.RenderAttachment | WGPUTextureUsage.TextureBinding, _d.ColorFormat);
			cur = _sheet = BatchFor(view, SheetSize, SheetSize, load: false);
		}
		sheet = cur; x = cur.CursorX; y = cur.ShelfY;
		cur.CursorX += w;
		if (h > cur.ShelfH) { cur.ShelfH = h; }
		SheetSlotsBaked++;
		return true;
	}

	// Queues one outline into a slot of the batch. Edges are in the fill's space and map to target pixels by
	// (e - origin) * scale + 1 (the one-pixel skirt) + the slot corner.
	private void AddBake(BakeBatch b, int x, int y, int w, int h, float[] edges, Vector2 origin, Vector2 scale, bool evenOdd, bool exclude)
	{
		float right = x + w;
		for (var i = 0; i < edges.Length; i += 4)
		{
			b.Edges.Add((edges[i] - origin.X) * scale.X + 1f + x); b.Edges.Add((edges[i + 1] - origin.Y) * scale.Y + 1f + y);
			b.Edges.Add((edges[i + 2] - origin.X) * scale.X + 1f + x); b.Edges.Add((edges[i + 3] - origin.Y) * scale.Y + 1f + y);
			b.Ext.Add(right);
		}
		b.Slots.Add(x); b.Slots.Add(y); b.Slots.Add(w); b.Slots.Add(h); b.Slots.Add(evenOdd ? 1f : 0f); b.Slots.Add(exclude ? 1f : 0f);
		b.Count++;
		if (x < b.MinX) { b.MinX = x; }
		if (y < b.MinY) { b.MinY = y; }
		if (x + w > b.MaxX) { b.MaxX = x + w; }
		if (y + h > b.MaxY) { b.MaxY = y + h; }
	}

	// One atlas entry's bake, on the placement AppendAtlasQuad draws with: the fill's own origin, shifted a pixel so
	// an edge sitting exactly on the bbox boundary still has the pixel it partly covers.
	private void QueueEntryBake(PathFill pf, WebGpuPathAtlas.Slot slot, Vector2 scale)
		=> AddBake(BatchFor(slot.Owner.View, slot.Owner.W, slot.Owner.H, load: true), slot.X, slot.Y, slot.W, slot.H, pf.Edges, new Vector2(slot.OriginX, slot.OriginY), scale, pf.EvenOdd, false);

	// Bakes every pending batch: one accumulate pass over all its edges into a scratch accumulator covering the
	// union of its slots, one resolve pass writing the slots into the target. Runs before a render pass begins, so
	// the passes that sample the targets are encoded after them.
	private void FlushPendingBakes()
	{
		if (_pendingBakes.Count == 0) { FlushPendingBlurs(); return; }
		foreach (var b in _pendingBakes)
		{
			// Accumulator dims rounded up so the pool sees a few sizes per target, not one per frame.
			int ax = b.MinX, ay = b.MinY;
			int aw = Math.Min(b.TargetW - ax, (b.MaxX - ax + 63) & ~63), ah = Math.Min(b.TargetH - ay, (b.MaxY - ay + 63) & ~63);
			var edges = CollectionsMarshal.AsSpan(b.Edges);
			for (var i = 0; i < edges.Length; i += 2) { edges[i] -= ax; edges[i + 1] -= ay; }
			var ext = CollectionsMarshal.AsSpan(b.Ext);
			for (var i = 0; i < ext.Length; i++) { ext[i] -= ax; }

			var edgeBytes = edges.Length * sizeof(float);
			var extBytes = ext.Length * sizeof(float);
			var edgeBuf = _d.BufferPool.Rent(edgeBytes, WGPUBufferUsage.Storage | WGPUBufferUsage.CopyDst);
			var extBuf = _d.BufferPool.Rent(extBytes, WGPUBufferUsage.Storage | WGPUBufferUsage.CopyDst);
			var sizeBuf = _d.BufferPool.Rent(16, WGPUBufferUsage.Uniform | WGPUBufferUsage.CopyDst);
			fixed (float* p = edges) { wgpuQueueWriteBuffer(_d.Q, edgeBuf, 0, (IntPtr)p, (nuint)edgeBytes); }
			fixed (float* p = ext) { wgpuQueueWriteBuffer(_d.Q, extBuf, 0, (IntPtr)p, (nuint)extBytes); }
			var size = stackalloc float[4] { aw, ah, 0f, 0f };
			wgpuQueueWriteBuffer(_d.Q, sizeBuf, 0, (IntPtr)size, 16);

			var accView = _d.Pool.Rent(aw, ah, 1, WGPUTextureUsage.RenderAttachment | WGPUTextureUsage.TextureBinding, WebGpuDevice.CoverageFormat);
			var ae = stackalloc WGPUBindGroupEntry[3];
			ae[0] = new WGPUBindGroupEntry { Binding = 0, Buffer = edgeBuf, Offset = 0, Size = (nuint)edgeBytes };
			ae[1] = new WGPUBindGroupEntry { Binding = 1, Buffer = extBuf, Offset = 0, Size = (nuint)extBytes };
			ae[2] = new WGPUBindGroupEntry { Binding = 2, Buffer = sizeBuf, Offset = 0, Size = 16 };
			var abgd = new WGPUBindGroupDescriptor { Layout = _d.CoverageSheetBgl, EntryCount = 3, Entries = ae };
			var accumBg = _d.TrackBg(wgpuDeviceCreateBindGroup(_d.Dev, &abgd));
			var acc = new WGPURenderPassColorAttachment { DepthSlice = uint.MaxValue, View = accView, LoadOp = WGPULoadOp.Clear, StoreOp = WGPUStoreOp.Store, ClearValue = default };
			var adesc = new WGPURenderPassDescriptor { ColorAttachmentCount = 1, ColorAttachments = &acc };
			var apass = wgpuCommandEncoderBeginRenderPass(_frameEncoder, &adesc);
			wgpuRenderPassEncoderSetPipeline(apass, _d.CoverageSheetPipe);
			wgpuRenderPassEncoderSetBindGroup(apass, 0, (IntPtr)accumBg, 0, (uint*)null);
			wgpuRenderPassEncoderDraw(apass, (uint)(ext.Length * 6), 1, 0, 0);
			wgpuRenderPassEncoderEnd(apass);

			// Per slot, 6 verts of (ndc in the target, texel in the accumulator, evenOdd, exclude).
			var verts = new float[b.Count * 36];
			var slots = CollectionsMarshal.AsSpan(b.Slots);
			var vi = 0;
			void V(float px, float py, float eo, float ex)
			{
				verts[vi++] = px / b.TargetW * 2f - 1f; verts[vi++] = 1f - py / b.TargetH * 2f;
				verts[vi++] = px - ax; verts[vi++] = py - ay; verts[vi++] = eo; verts[vi++] = ex;
			}
			for (var i = 0; i < slots.Length; i += 6)
			{
				float x0 = slots[i], y0 = slots[i + 1], x1 = x0 + slots[i + 2], y1 = y0 + slots[i + 3], eo = slots[i + 4], ex = slots[i + 5];
				V(x0, y0, eo, ex); V(x1, y0, eo, ex); V(x1, y1, eo, ex);
				V(x0, y0, eo, ex); V(x1, y1, eo, ex); V(x0, y1, eo, ex);
			}
			var re = new WGPUBindGroupEntry { Binding = 0, TextureView = accView };
			var rbgd = new WGPUBindGroupDescriptor { Layout = _d.CoverageResolveSheetBgl, EntryCount = 1, Entries = &re };
			var resolveBg = _d.TrackBg(wgpuDeviceCreateBindGroup(_d.Dev, &rbgd));
			var quads = MakeBuffer(verts);
			var rca = new WGPURenderPassColorAttachment { DepthSlice = uint.MaxValue, View = b.Target, LoadOp = b.Load ? WGPULoadOp.Load : WGPULoadOp.Clear, StoreOp = WGPUStoreOp.Store, ClearValue = default };
			var rdesc = new WGPURenderPassDescriptor { ColorAttachmentCount = 1, ColorAttachments = &rca };
			var rpass = wgpuCommandEncoderBeginRenderPass(_frameEncoder, &rdesc);
			wgpuRenderPassEncoderSetPipeline(rpass, _d.CoverageResolveSheetPipe);
			wgpuRenderPassEncoderSetBindGroup(rpass, 0, (IntPtr)resolveBg, 0, (uint*)null);
			wgpuRenderPassEncoderSetVertexBuffer(rpass, 0, quads, 0, (nuint)(verts.Length * sizeof(float)));
			wgpuRenderPassEncoderDraw(rpass, (uint)(b.Count * 6), 1, 0, 0);
			wgpuRenderPassEncoderEnd(rpass);
			// The accumulator was consumed by the resolve just encoded; re-rentable within the frame.
			_d.Pool.Return(accView);
			BakeBatches++;
		}
		_pendingBakes.Clear();
		_bakeByTarget.Clear();
		_sheet = null;
		FlushPendingBlurs();
	}

	internal static int SheetSlotsBaked, BakeBatches;
}
