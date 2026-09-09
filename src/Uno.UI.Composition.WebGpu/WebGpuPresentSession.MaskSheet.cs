// The per-frame mask sheet: every coverage mask a frame needs only for itself (a fill the tessellator refused, a
// path clip on a per-frame op) is shelf-packed into one texture and baked with a single accumulate pass and a single
// resolve pass, instead of a texture and two passes apiece.
#nullable disable
using System;
using System.Collections.Generic;
using System.Numerics;
using Uno.WebGpu.Native;
using static Uno.WebGpu.Native.WGPU;

namespace Uno.UI.Composition.WebGpu;

public sealed unsafe partial class WebGpuPresentSession
{
	private const int SheetSize = 2048;

	private sealed class MaskSheet
	{
		public IntPtr View;             // the baked coverage, pooled for the frame; consumers bind it right away
		public int CursorX, ShelfY, ShelfH;
		public readonly List<float> Edges = new();    // x0,y0,x1,y1 per edge, in sheet pixels
		public readonly List<float> Ext = new();      // per edge: the right bound of its slot, so its quad stops there
		public readonly List<float> Resolve = new();  // per slot: 6 verts of (ndc.xy, texel.xy, evenOdd, exclude)
		public int Slots;
	}

	private readonly List<MaskSheet> _pendingSheets = new();

	// Reserves a w x h slot in the frame's current sheet, opening another when it is full. False when the mask is
	// larger than a sheet; such a mask bakes on its own.
	private bool TryReserveSheetSlot(int w, int h, out MaskSheet sheet, out int x, out int y)
	{
		sheet = null; x = y = 0;
		if (w > SheetSize || h > SheetSize) { return false; }
		var cur = _pendingSheets.Count > 0 ? _pendingSheets[^1] : null;
		if (cur is not null)
		{
			if (cur.CursorX + w > SheetSize) { cur.ShelfY += cur.ShelfH; cur.ShelfH = 0; cur.CursorX = 0; }
			if (cur.ShelfY + h > SheetSize) { cur = null; }
		}
		if (cur is null)
		{
			cur = new MaskSheet { View = _d.Pool.Rent(SheetSize, SheetSize, 1, WGPUTextureUsage.RenderAttachment | WGPUTextureUsage.TextureBinding, _d.ColorFormat) };
			_pendingSheets.Add(cur);
		}
		sheet = cur; x = cur.CursorX; y = cur.ShelfY;
		cur.CursorX += w;
		if (h > cur.ShelfH) { cur.ShelfH = h; }
		cur.Slots++;
		return true;
	}

	// Queues one outline into its slot: edges are in the fill's space, mapped to sheet pixels by (e - origin) * scale
	// + 1 (the one-pixel skirt) + the slot corner, exactly as a standalone bake places them.
	private void AddSheetFill(MaskSheet sheet, int x, int y, int w, int h, float[] edges, Vector2 origin, Vector2 scale, bool evenOdd, bool exclude)
	{
		float right = x + w;
		for (var i = 0; i < edges.Length; i += 4)
		{
			sheet.Edges.Add((edges[i] - origin.X) * scale.X + 1f + x); sheet.Edges.Add((edges[i + 1] - origin.Y) * scale.Y + 1f + y);
			sheet.Edges.Add((edges[i + 2] - origin.X) * scale.X + 1f + x); sheet.Edges.Add((edges[i + 3] - origin.Y) * scale.Y + 1f + y);
			sheet.Ext.Add(right);
		}
		float eo = evenOdd ? 1f : 0f, ex = exclude ? 1f : 0f;
		float x0 = x, y0 = y, x1 = x + w, y1 = y + h;
		void V(float px, float py) { sheet.Resolve.Add(px / SheetSize * 2f - 1f); sheet.Resolve.Add(1f - py / SheetSize * 2f); sheet.Resolve.Add(px); sheet.Resolve.Add(py); sheet.Resolve.Add(eo); sheet.Resolve.Add(ex); }
		V(x0, y0); V(x1, y0); V(x1, y1);
		V(x0, y0); V(x1, y1); V(x0, y1);
	}

	// Bakes every pending sheet: one accumulate pass over all its edges, one resolve pass over all its slots. Runs
	// before a render pass begins, so the passes that sample the sheets are encoded after them.
	private void FlushMaskSheets()
	{
		if (_pendingSheets.Count == 0) { return; }
		var sizeBuf = _d.BufferPool.Rent(16, WGPUBufferUsage.Uniform | WGPUBufferUsage.CopyDst);
		var size = stackalloc float[4] { SheetSize, SheetSize, 0f, 0f };
		wgpuQueueWriteBuffer(_d.Q, sizeBuf, 0, (IntPtr)size, 16);

		foreach (var sheet in _pendingSheets)
		{
			var edgeBytes = sheet.Edges.Count * sizeof(float);
			var edgeBuf = _d.BufferPool.Rent(edgeBytes, WGPUBufferUsage.Storage | WGPUBufferUsage.CopyDst);
			var extBuf = _d.BufferPool.Rent(sheet.Ext.Count * sizeof(float), WGPUBufferUsage.Storage | WGPUBufferUsage.CopyDst);
			fixed (float* p = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(sheet.Edges)) { wgpuQueueWriteBuffer(_d.Q, edgeBuf, 0, (IntPtr)p, (nuint)edgeBytes); }
			fixed (float* p = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(sheet.Ext)) { wgpuQueueWriteBuffer(_d.Q, extBuf, 0, (IntPtr)p, (nuint)(sheet.Ext.Count * sizeof(float))); }

			var accView = _d.Pool.Rent(SheetSize, SheetSize, 1, WGPUTextureUsage.RenderAttachment | WGPUTextureUsage.TextureBinding, WebGpuDevice.CoverageFormat);
			var ae = stackalloc WGPUBindGroupEntry[3];
			ae[0] = new WGPUBindGroupEntry { Binding = 0, Buffer = edgeBuf, Offset = 0, Size = (nuint)edgeBytes };
			ae[1] = new WGPUBindGroupEntry { Binding = 1, Buffer = extBuf, Offset = 0, Size = (nuint)(sheet.Ext.Count * sizeof(float)) };
			ae[2] = new WGPUBindGroupEntry { Binding = 2, Buffer = sizeBuf, Offset = 0, Size = 16 };
			var abgd = new WGPUBindGroupDescriptor { Layout = _d.CoverageSheetBgl, EntryCount = 3, Entries = ae };
			var accumBg = _d.TrackBg(wgpuDeviceCreateBindGroup(_d.Dev, &abgd));
			var acc = new WGPURenderPassColorAttachment { DepthSlice = uint.MaxValue, View = accView, LoadOp = WGPULoadOp.Clear, StoreOp = WGPUStoreOp.Store, ClearValue = default };
			var adesc = new WGPURenderPassDescriptor { ColorAttachmentCount = 1, ColorAttachments = &acc };
			var apass = wgpuCommandEncoderBeginRenderPass(_frameEncoder, &adesc);
			wgpuRenderPassEncoderSetPipeline(apass, _d.CoverageSheetPipe);
			wgpuRenderPassEncoderSetBindGroup(apass, 0, (IntPtr)accumBg, 0, (uint*)null);
			wgpuRenderPassEncoderDraw(apass, (uint)(sheet.Ext.Count * 6), 1, 0, 0);
			wgpuRenderPassEncoderEnd(apass);

			var re = new WGPUBindGroupEntry { Binding = 0, TextureView = accView };
			var rbgd = new WGPUBindGroupDescriptor { Layout = _d.CoverageResolveSheetBgl, EntryCount = 1, Entries = &re };
			var resolveBg = _d.TrackBg(wgpuDeviceCreateBindGroup(_d.Dev, &rbgd));
			var quads = MakeBuffer(sheet.Resolve);
			var rca = new WGPURenderPassColorAttachment { DepthSlice = uint.MaxValue, View = sheet.View, LoadOp = WGPULoadOp.Clear, StoreOp = WGPUStoreOp.Store, ClearValue = default };
			var rdesc = new WGPURenderPassDescriptor { ColorAttachmentCount = 1, ColorAttachments = &rca };
			var rpass = wgpuCommandEncoderBeginRenderPass(_frameEncoder, &rdesc);
			wgpuRenderPassEncoderSetPipeline(rpass, _d.CoverageResolveSheetPipe);
			wgpuRenderPassEncoderSetBindGroup(rpass, 0, (IntPtr)resolveBg, 0, (uint*)null);
			wgpuRenderPassEncoderSetVertexBuffer(rpass, 0, quads, 0, (nuint)(sheet.Resolve.Count * sizeof(float)));
			wgpuRenderPassEncoderDraw(rpass, (uint)(sheet.Slots * 6), 1, 0, 0);
			wgpuRenderPassEncoderEnd(rpass);
			// The accumulator was consumed by the resolve just encoded; re-rentable within the frame.
			_d.Pool.Return(accView);
			SheetSlotsBaked += sheet.Slots;
		}
		_pendingSheets.Clear();
	}

	internal static int SheetSlotsBaked;
}
