// Per-device recycling of GPU objects whose allocation cost dominates their use: offscreen texture views rented
// for a frame, and vertex/uniform buffers.
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

// Transient GPU-texture pool for the per-frame offscreens (shadow/backdrop/layer/path-coverage surfaces + blur
// temps). BeginFrame marks all entries free; Rent reuses a free entry matching the key or creates one — so a
// steady-state frame allocates nothing. Every renter clears (LoadOp.Clear) before writing, so reuse is safe.
// (These offscreens stay "in use" until the frame's main pass samples them; reuse happens across frames.)
internal sealed unsafe class WebGpuTexturePool : IDisposable
{
	private readonly WebGpuDevice _d;
	private sealed class Entry { public IntPtr Tex; public IntPtr View; public int W, H, Samples; public WGPUTextureFormat Fmt; public WGPUTextureUsage Usage; public bool InUse; public int LastUsed; }
	private readonly System.Collections.Generic.List<Entry> _entries = new();
	// The pool is shared per-device, so an off-loop render (e.g. RenderTargetBitmap) can hit it concurrently
	// with the on-window render loop. Guard mutation/enumeration so a concurrent Add can't invalidate Rent's walk.
	private readonly object _gate = new();
	private int _frameNo;
	// Release entries not rented for this many frames. Without eviction, every window resize strands a whole
	// generation of full-window MSAA colour + depth textures (they no longer match a Rent key) until process exit.
	private const int EvictAfterFrames = 16;

	public WebGpuTexturePool(WebGpuDevice d) => _d = d;

	public void BeginFrame()
	{
		lock (_gate)
		{
			for (int i = _entries.Count - 1; i >= 0; i--)
			{
				var e = _entries[i];
				if (!e.InUse && _frameNo - e.LastUsed > EvictAfterFrames)
				{
					if (e.View != IntPtr.Zero) { wgpuTextureViewRelease(e.View); }
					if (e.Tex != IntPtr.Zero) { wgpuTextureDestroy(e.Tex); }
					_entries.RemoveAt(i);
				}
				else { e.InUse = false; }
			}
			_frameNo++;
		}
	}

	public IntPtr Rent(int w, int h, int samples, WGPUTextureUsage usage, WGPUTextureFormat fmt)
	{
		lock (_gate)
		{
			foreach (var e in _entries)
			{
				if (!e.InUse && e.W == w && e.H == h && e.Samples == samples && e.Fmt == fmt && e.Usage == usage) { e.InUse = true; e.LastUsed = _frameNo; return e.View; }
			}
			var td = new WGPUTextureDescriptor { Size = new WGPUExtent3D { Width = (uint)w, Height = (uint)h, DepthOrArrayLayers = 1 }, Format = fmt, MipLevelCount = 1, SampleCount = (uint)samples, Dimension = WGPUTextureDimension._2D, Usage = usage };
			var tex = wgpuDeviceCreateTexture(_d.Dev, &td);
			var view = wgpuTextureCreateView(tex, null);
			_entries.Add(new Entry { Tex = tex, View = view, W = w, H = h, Samples = samples, Fmt = fmt, Usage = usage, InUse = true, LastUsed = _frameNo });
			return view;
		}
	}

	/// <summary>Marks a rented view free again so it can be re-rented within the SAME frame. Used for the depth/
	/// stencil target, which is written only inside its own (already-ended) render pass and never sampled after —
	/// so one depth texture per size is reused across all of a frame's offscreen passes + the main pass.</summary>
	public void Return(IntPtr view)
	{
		if (view == IntPtr.Zero) { return; }
		lock (_gate) { foreach (var e in _entries) { if (e.View == view) { e.InUse = false; return; } } }
	}

	/// <summary>The backing texture for a rented view, or Zero if unknown. Used to flush an offscreen's resolve so
	/// a later, separately-submitted pass can sample it.</summary>
	public IntPtr TexForView(IntPtr view)
	{
		lock (_gate) { foreach (var e in _entries) { if (e.View == view) { return e.Tex; } } }
		return IntPtr.Zero;
	}

	public void Dispose()
	{
		lock (_gate)
		{
			foreach (var e in _entries)
			{
				if (e.View != IntPtr.Zero) { wgpuTextureViewRelease(e.View); }
				if (e.Tex != IntPtr.Zero) { wgpuTextureDestroy(e.Tex); }
			}
			_entries.Clear();
		}
	}
}


// Transient GPU-buffer pool (vertex + uniform buffers). Like the texture pool: BeginFrame frees all; Rent
// reuses a free buffer of the same usage with enough capacity or creates one, so a steady-state frame allocates
// no buffers. Callers QueueWriteBuffer their data before use.
internal sealed unsafe class WebGpuBufferPool : IDisposable
{
	private readonly WebGpuDevice _d;
	private sealed class Entry { public IntPtr Buf; public int Cap; public WGPUBufferUsage Usage; public bool InUse; }
	private readonly System.Collections.Generic.List<Entry> _entries = new();
	// Shared per-device; guard against concurrent Add invalidating Rent's enumeration (see WebGpuTexturePool).
	private readonly object _gate = new();

	public WebGpuBufferPool(WebGpuDevice d) => _d = d;

	public void BeginFrame() { lock (_gate) { foreach (var e in _entries) { e.InUse = false; } } }

	public void Dispose()
	{
		lock (_gate)
		{
			foreach (var e in _entries) { if (e.Buf != IntPtr.Zero) { wgpuBufferRelease(e.Buf); } }
			_entries.Clear();
		}
	}

	public IntPtr Rent(int byteSize, WGPUBufferUsage usage)
	{
		lock (_gate)
		{
			foreach (var e in _entries)
			{
				if (!e.InUse && e.Usage == usage && e.Cap >= byteSize) { e.InUse = true; return e.Buf; }
			}
			int cap = Math.Max(byteSize, 256);
			var bd = new WGPUBufferDescriptor { Size = (nuint)cap, Usage = usage };
			var buf = wgpuDeviceCreateBuffer(_d.Dev, &bd);
			_entries.Add(new Entry { Buf = buf, Cap = cap, Usage = usage, InUse = true });
			return buf;
		}
	}
}
