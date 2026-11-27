// Per-device recycling of GPU objects whose allocation cost dominates their use: offscreen texture views rented
// for a frame, and vertex/uniform buffers.
#nullable disable
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Silk.NET.WebGPU;
using Buffer = Silk.NET.WebGPU.Buffer;
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
	private WebGPU Api => _d.Api;
	private sealed class Entry { public Texture* Tex; public TextureView* View; public int W, H, Samples; public TextureFormat Fmt; public TextureUsage Usage; public bool InUse; public int LastUsed; }
	private readonly System.Collections.Generic.List<Entry> _entries = new();
	// The pool is shared per-device, so an off-loop render (e.g. RenderTargetBitmap) can hit it concurrently
	// with the on-window render loop. Guard mutation/enumeration so a concurrent Add can't invalidate Rent's walk.
	private readonly object _gate = new();
	private int _frameNo;
	// Release entries not rented for this many frames. Without eviction, every window resize strands a whole
	// generation of full-window MSAA colour + depth textures (they no longer match a Rent key) until process exit.
	// Measured: holding idle entries longer costs more than the churn it saves, so the horizon stays short.
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
					if (e.View != null) { _d.Groups.NoteDestroyed((nint)e.View); Api.TextureViewRelease(e.View); }
					// Destroy frees the allocation now; Release drops the handle, which wgpu would otherwise
					// keep registered (and its memory attributed) for the life of the device.
					if (e.Tex != null) { Api.TextureDestroy(e.Tex); Api.TextureRelease(e.Tex); }
					_entries.RemoveAt(i);
				}
				else { e.InUse = false; }
			}

			_frameNo++;
		}
	}

	public TextureView* Rent(int w, int h, int samples, TextureUsage usage, TextureFormat fmt)
	{
		lock (_gate)
		{
			foreach (var e in _entries)
			{
				if (!e.InUse && e.W == w && e.H == h && e.Samples == samples && e.Fmt == fmt && e.Usage == usage) { e.InUse = true; e.LastUsed = _frameNo; return e.View; }
			}
			var td = new TextureDescriptor { Size = new Extent3D { Width = (uint)w, Height = (uint)h, DepthOrArrayLayers = 1 }, Format = fmt, MipLevelCount = 1, SampleCount = (uint)samples, Dimension = TextureDimension.Dimension2D, Usage = usage };
			var tex = Api.DeviceCreateTexture(_d.Dev, &td);
			var view = Api.TextureCreateView(tex, null);
			_entries.Add(new Entry { Tex = tex, View = view, W = w, H = h, Samples = samples, Fmt = fmt, Usage = usage, InUse = true, LastUsed = _frameNo });
			return view;
		}
	}

	/// <summary>Marks a rented view free again so it can be re-rented within the SAME frame: for a target that is
	/// written only inside its own (already-ended) render pass and never sampled after, such as a coverage
	/// accumulator or an MSAA colour that has resolved.</summary>
	public void Return(TextureView* view)
	{
		if (view == null) { return; }
		lock (_gate) { foreach (var e in _entries) { if (e.View == view) { e.InUse = false; return; } } }
	}

	/// <summary>The backing texture for a rented view, or Zero if unknown. Used to flush an offscreen's resolve so
	/// a later, separately-submitted pass can sample it.</summary>
	public Texture* TexForView(TextureView* view)
	{
		lock (_gate) { foreach (var e in _entries) { if (e.View == view) { return e.Tex; } } }
		return null;
	}

	public void Dispose()
	{
		lock (_gate)
		{
			foreach (var e in _entries)
			{
				if (e.View != null) { Api.TextureViewRelease(e.View); }
				if (e.Tex != null) { Api.TextureDestroy(e.Tex); Api.TextureRelease(e.Tex); }
			}
			_entries.Clear();
		}
	}
}


// The device's one buffer store. A rental is either for the frame (back at the next BeginFrame) or held by an owner
// (back when the owner calls Return, which a cached recording does through the device's deferred release). Capacities
// come in size classes (see SizeClass), so a returned buffer serves the next request of its class whoever made it.
internal sealed unsafe class WebGpuBufferPool : IDisposable
{
	private readonly WebGpuDevice _d;
	private WebGPU Api => _d.Api;
	private sealed class Entry { public Buffer* Buf; public int Cap; public BufferUsage Usage; public int LastUsed; }
	private readonly Dictionary<(BufferUsage Usage, int Cap), List<Entry>> _free = new();
	private readonly List<Entry> _frame = new();
	private readonly Dictionary<nint, Entry> _held = new();
	// Shared per-device; an off-loop render (RenderTargetBitmap) can rent concurrently with the render loop.
	private readonly object _gate = new();
	private int _frameNo;
	// Without eviction the pool keeps the session's high-water mark: one heavy frame (a flyout's clip and stamp
	// buffers, thousands of them) would be held until process exit.
	private const int EvictAfterFrames = 16;
	private const int MinCap = 256;

	public WebGpuBufferPool(WebGpuDevice d) => _d = d;

	public void BeginFrame()
	{
		lock (_gate)
		{
			foreach (var e in _frame) { e.LastUsed = _frameNo; Push(e); }
			_frame.Clear();
			foreach (var list in _free.Values)
			{
				// Each list is in the order its entries came back, so the idle ones are a prefix.
				var idle = 0;
				while (idle < list.Count && _frameNo - list[idle].LastUsed > EvictAfterFrames) { idle++; }
				for (var i = 0; i < idle; i++)
				{
					_d.Groups.NoteDestroyed((nint)list[i].Buf);
					Api.BufferDestroy(list[i].Buf);
					Api.BufferRelease(list[i].Buf);
				}
				list.RemoveRange(0, idle);
			}
			_frameNo++;
		}
	}

	/// <summary>A buffer for this frame only.</summary>
	public Buffer* Rent(int byteSize, BufferUsage usage)
	{
		lock (_gate)
		{
			var e = Take(byteSize, usage);
			_frame.Add(e);
			return e.Buf;
		}
	}

	/// <summary>A buffer kept until <see cref="Return"/>.</summary>
	public Buffer* RentHeld(int byteSize, BufferUsage usage)
	{
		lock (_gate)
		{
			var e = Take(byteSize, usage);
			_held[(nint)e.Buf] = e;
			return e.Buf;
		}
	}

	/// <summary>Takes back a held buffer; false when the pool did not hand it out, so the caller releases it.</summary>
	public bool Return(nint buf)
	{
		lock (_gate)
		{
			if (!_held.Remove(buf, out var e)) { return false; }
			e.LastUsed = _frameNo;
			Push(e);
			return true;
		}
	}

	// Powers of two up to 64 KB; above, four classes per doubling, so a held buffer (kept for a recording's lifetime)
	// over-allocates by at most a quarter instead of almost double.
	private const int PowerOfTwoClassesUpTo = 64 * 1024;

	internal static int SizeClass(int byteSize)
	{
		var pow = (int)BitOperations.RoundUpToPowerOf2((uint)Math.Max(1, byteSize));
		if (pow <= PowerOfTwoClassesUpTo) { return Math.Max(MinCap, pow); }
		var step = pow / 8;
		return (byteSize + step - 1) / step * step;
	}

	private Entry Take(int byteSize, BufferUsage usage)
	{
		var cap = SizeClass(byteSize);
		if (_free.TryGetValue((usage, cap), out var list) && list.Count > 0)
		{
			var e = list[^1];
			list.RemoveAt(list.Count - 1);
			return e;
		}
		var bd = new BufferDescriptor { Size = (nuint)cap, Usage = usage };
		return new Entry { Buf = Api.DeviceCreateBuffer(_d.Dev, &bd), Cap = cap, Usage = usage };
	}

	private void Push(Entry e)
	{
		if (!_free.TryGetValue((e.Usage, e.Cap), out var list)) { _free[(e.Usage, e.Cap)] = list = new(); }
		list.Add(e);
	}

	public void Dispose()
	{
		lock (_gate)
		{
			foreach (var list in _free.Values) { foreach (var e in list) { Api.BufferRelease(e.Buf); } }
			foreach (var e in _frame) { Api.BufferRelease(e.Buf); }
			foreach (var e in _held.Values) { Api.BufferRelease(e.Buf); }
			_free.Clear();
			_frame.Clear();
			_held.Clear();
		}
	}
}

/// <summary>
/// Recycles the dedicated colour textures <see cref="WebGpuDrawingFactory.RenderOffscreen"/> hands out. An effect
/// brush rasterizes each of its sources on every paint, so without recycling every paint churns a fresh GPU texture
/// through the allocator — which commits blocks it never gives back, even though the live count stays flat.
/// A texture comes back only at a frame boundary, so an in-flight recording can never be handed someone else's.
/// </summary>
internal sealed unsafe class WebGpuOffscreenPool : IDisposable
{
	private sealed class Entry { public Texture* Tex; public TextureView* View; public int W, H; public int LastUsed; }
	private readonly WebGpuDevice _d;
	private readonly WebGPU _api;
	private readonly System.Collections.Generic.List<Entry> _free = new();
	private readonly object _gate = new();
	private int _frameNo;
	// Long enough that a brush repainting every few frames always hits, short enough that a size that stops
	// being asked for is given back rather than held for the process's life.
	private const int EvictAfterFrames = 240;
	private const int MaxEntries = 48;

	public WebGpuOffscreenPool(WebGpuDevice d) { _d = d; _api = d.Api; }

	public void BeginFrame()
	{
		lock (_gate)
		{
			for (var i = _free.Count - 1; i >= 0; i--)
			{
				if (_frameNo - _free[i].LastUsed <= EvictAfterFrames) { continue; }
				Destroy(_free[i]);
				_free.RemoveAt(i);
			}

			_frameNo++;
		}
	}

	public bool TryRent(int w, int h, out Texture* tex, out TextureView* view)
	{
		lock (_gate)
		{
			for (var i = 0; i < _free.Count; i++)
			{
				if (_free[i].W != w || _free[i].H != h) { continue; }
				tex = _free[i].Tex; view = _free[i].View;
				_free.RemoveAt(i);
				return true;
			}
		}

		tex = null; view = null;
		return false;
	}

	public void Return(Texture* tex, TextureView* view, int w, int h)
	{
		var entry = new Entry { Tex = tex, View = view, W = w, H = h };
		lock (_gate)
		{
			if (_free.Count >= MaxEntries) { Destroy(entry); return; }
			entry.LastUsed = _frameNo;
			_free.Add(entry);
		}
	}

	private void Destroy(Entry e)
	{
		if (e.View != null) { _d.Groups?.NoteDestroyed((nint)e.View); _api.TextureViewRelease(e.View); }
		if (e.Tex != null) { _api.TextureDestroy(e.Tex); _api.TextureRelease(e.Tex); }
	}

	public void Dispose()
	{
		lock (_gate)
		{
			foreach (var e in _free) { Destroy(e); }
			_free.Clear();
		}
	}
}
