// The shared uniform buffers: one GPU buffer holds a frame's (or the arena's) clip and gradient uniforms in
// fixed slots, so a frame uploads once per chunk and binds by offset instead of a buffer and a queue write per draw.
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
	private readonly IntPtr _extraTexture, _extraTexture2, _sampler, _extraBuffer;   // bindings 1..4 when set (clip layouts)
	private readonly WGPUBufferUsage _usage;
	private int _next;

	public WebGpuUniformSlab(WebGpuDevice d, int uniformBytes, IntPtr extraTexture = default, WGPUBufferUsage usage = WGPUBufferUsage.Uniform | WGPUBufferUsage.CopyDst, IntPtr extraTexture2 = default, IntPtr sampler = default, IntPtr extraBuffer = default)
	{
		_d = d;
		_usage = usage;
		_extraTexture = extraTexture;
		_extraTexture2 = extraTexture2;
		_sampler = sampler;
		_extraBuffer = extraBuffer;
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
			var e = stackalloc WGPUBindGroupEntry[5];
			e[0] = new WGPUBindGroupEntry { Binding = 0, Buffer = c.Buf, Offset = (nuint)(slot * _slotBytes), Size = (nuint)_uniformBytes };
			int n = 1;
			if (_extraTexture != IntPtr.Zero) { e[n++] = new WGPUBindGroupEntry { Binding = 1, TextureView = _extraTexture }; }
			if (_extraTexture2 != IntPtr.Zero) { e[n++] = new WGPUBindGroupEntry { Binding = 2, TextureView = _extraTexture2 }; }
			if (_sampler != IntPtr.Zero) { e[n++] = new WGPUBindGroupEntry { Binding = 3, Sampler = _sampler }; }
			if (_extraBuffer != IntPtr.Zero) { e[n++] = new WGPUBindGroupEntry { Binding = 4, Buffer = _extraBuffer, Offset = 0, Size = WebGpuPresentSession.ClipEntryBytes }; }
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
	// Owned ClipU slots: chunked uniform buffers with fixed 512-byte slots (uniform bind offsets align to 256), a
	// shadow copy and dirty tracking, so a per-frame restamp is a shadow write flushed once per chunk. A slot whose
	// draw has more than four clips also owns the storage buffer holding the rest.
	private sealed class Chunk
	{
		public IntPtr Buf;
		public float[] Shadow;
		public IntPtr[] More;
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

	public nint Alloc() => Alloc(WebGpuPresentSession.ClipUBytes);

	private nint Alloc(int bytes)
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
				var bd = new WGPUBufferDescriptor { Size = (nuint)(sc.ChunkSlots * sc.SlotBytes), Usage = WGPUBufferUsage.Uniform | WGPUBufferUsage.CopyDst };
				sc.Chunks.Add(new Chunk { Buf = wgpuDeviceCreateBuffer(_d.Dev, &bd), Shadow = new float[sc.ChunkSlots * sc.SlotFloats], More = new IntPtr[sc.ChunkSlots] });
			}
		}
		return ((nint)ci << SlotShift) | (nint)(slot + 1);
	}

	public void Free(nint handle)
	{
		if (handle == 0) { return; }
		var (sc, slot) = Decode(handle);
		var c = sc.Chunks[slot / sc.ChunkSlots];
		var idx = slot % sc.ChunkSlots;
		if (c.More[idx] != IntPtr.Zero) { _d.DeferReleaseBuffer(c.More[idx]); c.More[idx] = IntPtr.Zero; }
		sc.Free.Push(slot);
	}

	public IntPtr MoreOf(nint handle) { var (sc, slot) = Decode(handle); return sc.Chunks[slot / sc.ChunkSlots].More[slot % sc.ChunkSlots]; }
	public void SetMore(nint handle, IntPtr buf) { var (sc, slot) = Decode(handle); sc.Chunks[slot / sc.ChunkSlots].More[slot % sc.ChunkSlots] = buf; }

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
