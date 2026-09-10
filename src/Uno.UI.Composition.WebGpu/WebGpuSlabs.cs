// The persistent, shared buffers. One GPU buffer holds every visual's geometry (or every clip uniform) so a
// frame uploads once and draws from offsets, instead of a buffer and a queue write per visual.
#nullable disable
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Uno.WebGpu.Native;
using static Uno.WebGpu.Native.WGPU;
using Uno.UI.Composition.Drawing;
using Windows.Graphics.Effects.Interop;
using Windows.Foundation;
using WColor = Windows.UI.Color;

namespace Uno.UI.Composition.WebGpu;

internal sealed unsafe class WebGpuSlab
{
	private readonly WebGpuDevice _d;
	private readonly int _stride;                 // floats per vertex
	private readonly WebGpuVertexSlab _alloc = new();
	private readonly List<float> _shadow = new();
	private readonly HashSet<long> _live = new();
	public IntPtr Buf;                             // persistent GPU buffer (Vertex | CopyDst)
	private int _bufVerts;                         // GPU buffer capacity in vertices

	public WebGpuSlab(WebGpuDevice d, int strideFloats) { _d = d; _stride = strideFloats; }

	public void BeginFrame() => _live.Clear();
	public void MarkLive(long id) => _live.Add(id);
	public void EndFrame() => _alloc.RetainOnly(_live);
	public int ByteOffset(long id) => (_alloc.TryGet(id, out var s) ? s.Off : 0) * _stride * sizeof(float);

	// A recording whose LOCAL verts don't change on a move re-derives its slice's CURRENT byte offset each frame
	// (never a cached one — a stale offset into a culled-then-reclaimed slice reads another visual's verts). Returns
	// false if the slice was reclaimed (culled last frame) so the caller re-Puts it; marks it live on a hit so it survives.
	public bool TryByteOffset(long id, out int byteOff)
	{
		if (_alloc.TryGet(id, out var s)) { byteOff = s.Off * _stride * sizeof(float); _live.Add(id); return true; }
		byteOff = 0; return false;
	}

	// Reserve/reuse `id`'s stable slice, and upload ONLY what changed: the whole shadow if the buffer had to grow,
	// otherwise a dirty diff against the CPU shadow — skip the write entirely when byte-identical (static UI), else
	// write only the changed [lo..hi] sub-range. Returns the slice's BYTE offset.
	public int Put(long id, System.Collections.Generic.List<float> verts)
	{
		_live.Add(id);
		int vcount = verts.Count / _stride;
		int voff = _alloc.Ensure(id, vcount, out _);
		int capVerts = _alloc.Capacity;
		int needFloats = capVerts * _stride;
		if (_shadow.Count < needFloats) { System.Runtime.InteropServices.CollectionsMarshal.SetCount(_shadow, needFloats); }
		var dst = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_shadow);
		var src = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(verts);
		int byteOff = voff * _stride * sizeof(float);
		int n = verts.Count;
		var slot = dst.Slice(voff * _stride, n);
		if (Buf == IntPtr.Zero || _bufVerts < capVerts)
		{
			// (Re)allocate the persistent buffer (1.5x headroom already in the allocator) and upload the whole shadow.
			src.CopyTo(slot);
			if (Buf != IntPtr.Zero) { _d.DeferReleaseBuffer(Buf); }
			_bufVerts = capVerts;
			var bd = new WGPUBufferDescriptor { Size = (nuint)(_bufVerts * _stride * sizeof(float)), Usage = WGPUBufferUsage.Vertex | WGPUBufferUsage.CopyDst };
			Buf = wgpuDeviceCreateBuffer(_d.Dev, &bd);
			fixed (float* p = dst) { wgpuQueueWriteBuffer(_d.Q, Buf, 0, (IntPtr)p, (nuint)(needFloats * sizeof(float))); }
			return byteOff;
		}
		// Dirty diff vs the shadow: first/last changed float. Identical → nothing to upload (the common static case).
		int lo = 0; while (lo < n && slot[lo] == src[lo]) { lo++; }
		if (lo == n) { return byteOff; }
		int hi = n - 1; while (hi > lo && slot[hi] == src[hi]) { hi--; }
		int len = hi - lo + 1;
		src.Slice(lo, len).CopyTo(slot.Slice(lo, len));
		fixed (float* p = &dst[voff * _stride + lo]) { wgpuQueueWriteBuffer(_d.Q, Buf, (nuint)(byteOff + lo * sizeof(float)), (IntPtr)p, (nuint)(len * sizeof(float))); }
		return byteOff;
	}
}

// One chunked uniform buffer backing every OWNED (restampable) ClipU. A stamp is a stable 512-byte slice —
// chunk buffers never move, so the op's clip bind group survives for the slot's lifetime — written into a CPU
// shadow; a frame's restamps flush as ONE queue write per dirty chunk range instead of one wgpuQueueWriteBuffer
// per op (a scrolling table restamps thousands of ClipUs per frame; the per-call/per-copy overhead dominated
// opsBuild and submit). Slot handles are 1-based nints so 0 keeps meaning "none" at the call sites.
// Per-frame uniform slab: 256-aligned slots in a shared buffer, each with a bind group created once and reused
// for the life of the slab. A frame's gradient uniforms then upload in ONE queue write per chunk instead of one
// per gradient — a native call costs far more than the bytes it carries. Slots are handed out sequentially and
// recycled every frame (Reset), so the used range is always a contiguous prefix.
internal sealed unsafe class WebGpuUniformSlab : IDisposable
{
	private const int ChunkSlots = 256;

	private sealed class Chunk
	{
		public IntPtr Buf;
		public float[] Shadow;
		public IntPtr[] Bgs;
	}

	private readonly WebGpuDevice _d;
	private readonly List<Chunk> _chunks = new();
	private readonly int _uniformBytes, _slotBytes, _slotFloats, _uniformFloats;
	// A constant texture bound at binding 1 of every slot's group (the clip layouts carry the path-clip mask there;
	// slab-rented clips have none and bind the placeholder). Zero for layouts with only the uniform.
	private readonly IntPtr _extraTexture, _extraTexture2, _sampler;   // bindings 1..3 when set (clip layouts)
	private readonly WGPUBufferUsage _usage;
	private int _next;

	public WebGpuUniformSlab(WebGpuDevice d, int uniformBytes, IntPtr extraTexture = default, WGPUBufferUsage usage = WGPUBufferUsage.Uniform | WGPUBufferUsage.CopyDst, IntPtr extraTexture2 = default, IntPtr sampler = default)
	{
		_d = d;
		_usage = usage;
		_extraTexture = extraTexture;
		_extraTexture2 = extraTexture2;
		_sampler = sampler;
		_uniformBytes = uniformBytes;
		_uniformFloats = uniformBytes / sizeof(float);
		_slotBytes = (uniformBytes + 255) / 256 * 256;   // uniform bind offsets must be 256-aligned
		_slotFloats = _slotBytes / sizeof(float);
	}

	public void Reset() => _next = 0;

	/// <summary>Copies `data` into the next slot and returns that slot's (persistent) bind group.</summary>
	public IntPtr Rent(IntPtr layout, float[] data)
	{
		var idx = _next++;
		var ci = idx / ChunkSlots;
		while (_chunks.Count <= ci)
		{
			var bd = new WGPUBufferDescriptor { Size = (nuint)(ChunkSlots * _slotBytes), Usage = _usage };
			_chunks.Add(new Chunk { Buf = wgpuDeviceCreateBuffer(_d.Dev, &bd), Shadow = new float[ChunkSlots * _slotFloats], Bgs = new IntPtr[ChunkSlots] });
		}
		var c = _chunks[ci];
		var slot = idx % ChunkSlots;
		Array.Copy(data, 0, c.Shadow, slot * _slotFloats, Math.Min(data.Length, _uniformFloats));
		if (c.Bgs[slot] == IntPtr.Zero)
		{
			var e = stackalloc WGPUBindGroupEntry[4];
			e[0] = new WGPUBindGroupEntry { Binding = 0, Buffer = c.Buf, Offset = (nuint)(slot * _slotBytes), Size = (nuint)_uniformBytes };
			int n = 1;
			if (_extraTexture != IntPtr.Zero) { e[n++] = new WGPUBindGroupEntry { Binding = 1, TextureView = _extraTexture }; }
			if (_extraTexture2 != IntPtr.Zero) { e[n++] = new WGPUBindGroupEntry { Binding = 2, TextureView = _extraTexture2 }; }
			if (_sampler != IntPtr.Zero) { e[n++] = new WGPUBindGroupEntry { Binding = 3, Sampler = _sampler }; }
			var bgd = new WGPUBindGroupDescriptor { Layout = layout, EntryCount = (nuint)n, Entries = e };
			c.Bgs[slot] = wgpuDeviceCreateBindGroup(_d.Dev, &bgd);
		}
		return c.Bgs[slot];
	}

	/// <summary>Uploads the used prefix — call before any submit whose commands read these uniforms. Re-uploading
	/// a range an earlier submit already consumed is harmless: queue writes are ordered against submits.</summary>
	public void Flush()
	{
		var used = _next;
		for (int ci = 0; ci < _chunks.Count && used > 0; ci++)
		{
			var n = Math.Min(used, ChunkSlots);
			var c = _chunks[ci];
			fixed (float* p = c.Shadow) { wgpuQueueWriteBuffer(_d.Q, c.Buf, 0, (IntPtr)p, (nuint)(n * _slotBytes)); }
			used -= n;
		}
	}

	public void Dispose()
	{
		foreach (var c in _chunks)
		{
			foreach (var bg in c.Bgs) { if (bg != IntPtr.Zero) { wgpuBindGroupRelease(bg); } }
			if (c.Buf != IntPtr.Zero) { wgpuBufferRelease(c.Buf); }
		}
		_chunks.Clear();
	}
}

internal sealed unsafe class WebGpuClipSlab : IDisposable
{
	// A ClipU is its 96-byte header plus one 96-byte entry per clip, so slots come in size classes: each
	// class is its own chunked buffer with fixed slots (storage bind offsets align to 256), a shadow copy and dirty
	// tracking, and a handle names (class, slot). A slot is rewritten in place only within its class.
	private sealed class Chunk
	{
		public IntPtr Buf;
		public float[] Shadow;
		public int DirtyMin = int.MaxValue;
		public int DirtyMax = -1;
	}

	private sealed class SizeClass
	{
		public int SlotBytes, SlotFloats, ChunkSlots;
		public readonly List<Chunk> Chunks = new();
		public readonly Stack<int> Free = new();
		public int Next;
	}

	private readonly WebGpuDevice _d;
	private readonly List<SizeClass> _classes = new();
	private readonly Dictionary<int, int> _classByBytes = new();
	private const int SlotShift = 24;   // handle = (class << 24) | (slot + 1); 0 = none

	public WebGpuClipSlab(WebGpuDevice d) => _d = d;

	private SizeClass ClassFor(int bytes)
	{
		var slotBytes = (bytes + 255) / 256 * 256;
		if (!_classByBytes.TryGetValue(slotBytes, out var ci))
		{
			ci = _classes.Count;
			_classes.Add(new SizeClass { SlotBytes = slotBytes, SlotFloats = slotBytes / sizeof(float), ChunkSlots = Math.Max(64, (1 << 20) / slotBytes) });
			_classByBytes[slotBytes] = ci;
		}
		return _classes[ci];
	}

	public nint Alloc(int bytes)
	{
		var sc = ClassFor(bytes);
		var ci = _classByBytes[sc.SlotBytes];
		int slot;
		if (sc.Free.Count > 0) { slot = sc.Free.Pop(); }
		else
		{
			slot = sc.Next++;
			if (slot / sc.ChunkSlots >= sc.Chunks.Count)
			{
				var bd = new WGPUBufferDescriptor { Size = (nuint)(sc.ChunkSlots * sc.SlotBytes), Usage = WGPUBufferUsage.Storage | WGPUBufferUsage.CopyDst };
				sc.Chunks.Add(new Chunk { Buf = wgpuDeviceCreateBuffer(_d.Dev, &bd), Shadow = new float[sc.ChunkSlots * sc.SlotFloats] });
			}
		}
		return ((nint)ci << SlotShift) | (nint)(slot + 1);
	}

	public void Free(nint handle)
	{
		if (handle == 0) { return; }
		var sc = _classes[(int)(handle >> SlotShift)];
		sc.Free.Push((int)(handle & ((1 << SlotShift) - 1)) - 1);
	}

	private (SizeClass sc, int slot) Decode(nint handle) => (_classes[(int)(handle >> SlotShift)], (int)(handle & ((1 << SlotShift) - 1)) - 1);

	public IntPtr BufferOf(nint handle) { var (sc, slot) = Decode(handle); return sc.Chunks[slot / sc.ChunkSlots].Buf; }
	public uint OffsetOf(nint handle) { var (sc, slot) = Decode(handle); return (uint)(slot % sc.ChunkSlots * sc.SlotBytes); }
	public int SlotBytesOf(nint handle) => Decode(handle).sc.SlotBytes;

	public void Write(nint handle, float[] clipU, int floats)
	{
		var (sc, slot) = Decode(handle);
		var c = sc.Chunks[slot / sc.ChunkSlots];
		var idx = slot % sc.ChunkSlots;
		Array.Copy(clipU, 0, c.Shadow, idx * sc.SlotFloats, Math.Min(floats, sc.SlotFloats));
		if (idx < c.DirtyMin) { c.DirtyMin = idx; }
		if (idx > c.DirtyMax) { c.DirtyMax = idx; }
	}

	/// <summary>Bytes uploaded by the last <see cref="Flush"/>, for UNO_WEBGPU_STATS.</summary>
	public long LastFlushBytes;

	/// <summary>One queue write per dirty chunk range — call before any submit whose commands read clips.</summary>
	public void Flush()
	{
		LastFlushBytes = 0;
		foreach (var sc in _classes)
		{
			foreach (var c in sc.Chunks)
			{
				if (c.DirtyMax < 0) { continue; }
				int lo = c.DirtyMin * sc.SlotFloats, len = (c.DirtyMax + 1 - c.DirtyMin) * sc.SlotFloats;
				LastFlushBytes += len * sizeof(float);
				fixed (float* p = &c.Shadow[lo]) { wgpuQueueWriteBuffer(_d.Q, c.Buf, (nuint)(lo * sizeof(float)), (IntPtr)p, (nuint)(len * sizeof(float))); }
				c.DirtyMin = int.MaxValue;
				c.DirtyMax = -1;
			}
		}
	}

	public void Dispose()
	{
		foreach (var sc in _classes)
		{
			foreach (var c in sc.Chunks)
			{
				if (c.Buf != IntPtr.Zero) { wgpuBufferRelease(c.Buf); }
			}
			sc.Chunks.Clear();
		}
		_classes.Clear();
		_classByBytes.Clear();
	}
}

// --- Device-bound factory ---

/// <summary>A wgpu texture uploaded once from a neutral <see cref="IImage"/>'s pixels. Owned/disposed by the framework.</summary>

// Per-visual STABLE slice allocator over a persistent per-kind vertex buffer (
// WebGpuVertexSlab). Each visual (keyed by its recording's command-list identity) gets a fixed offset+capacity in
// a shared GPU buffer, so a content change rewrites its slice IN PLACE (stable offset → dirty only that byte
// range) and geometry is RESIDENT across frames (no re-upload for a static visual). Holds CPU metadata only.
internal sealed class WebGpuVertexSlab
{
	internal struct Slice { public int Off; public int Cap; public int Len; }   // in vertices (caller's stride)
	private readonly Dictionary<long, Slice> _map = new();
	private readonly List<(int off, int cap)> _free = new();
	private int _cap;                       // high-water = the buffer length (in vertices) to size to
	private readonly List<long> _toFree = new();

	internal int Capacity => _cap;
	internal void Reset() { _map.Clear(); _free.Clear(); _cap = 0; }
	internal bool TryGet(long id, out Slice s) => _map.TryGetValue(id, out s);

	// Reserve `verts` vertices for `id`, reusing its slot when it still fits (stable offset), else best-fit/grow.
	// Returns the VERTEX offset. `grew` is set when the high-water advanced (the GPU buffer must be (re)allocated).
	internal int Ensure(long id, int verts, out bool grew)
	{
		grew = false;
		if (_map.TryGetValue(id, out var s))
		{
			if (s.Cap >= verts) { s.Len = verts; _map[id] = s; return s.Off; }
			_free.Add((s.Off, s.Cap));   // outgrew → reclaim, reallocate below
		}
		int want = verts + (verts >> 1);   // 1.5x slack so small growth doesn't realloc next frame
		int bestI = -1, bestCap = int.MaxValue;
		for (int i = 0; i < _free.Count; i++) { if (_free[i].cap >= verts && _free[i].cap < bestCap) { bestI = i; bestCap = _free[i].cap; } }
		int off, capAlloc;
		if (bestI >= 0) { off = _free[bestI].off; capAlloc = _free[bestI].cap; _free.RemoveAt(bestI); }
		else { off = _cap; capAlloc = want; _cap += want; grew = true; }
		_map[id] = new Slice { Off = off, Cap = capAlloc, Len = verts };
		return off;
	}

	internal void Free(long id) { if (_map.TryGetValue(id, out var s)) { _free.Add((s.Off, s.Cap)); _map.Remove(id); } }

	// Free slices of visuals not present this frame (returns their capacity to the free list). `live` = this frame's ids.
	internal void RetainOnly(HashSet<long> live)
	{
		_toFree.Clear();
		foreach (var id in _map.Keys) { if (!live.Contains(id)) { _toFree.Add(id); } }
		foreach (var id in _toFree) { Free(id); }
	}
}
