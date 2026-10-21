#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Uno.UI.Composition.Drawing;
using Uno.UI.Runtime.Wayland.Protocols;
using Windows.Foundation;

namespace Uno.UI.Runtime.Wayland.Rendering;

/// <summary>
/// Software graphics context: the backend renders into a persistent CPU buffer, which <see cref="Present"/> copies
/// into a wl_shm buffer the compositor is not holding. All members run on the owner thread.
/// </summary>
internal sealed class WaylandSoftwareGraphicsContext : IWaylandGraphicsContext
{
	// Bounds memory even when a compositor retains old-size buffers across resizes.
	private const int MaximumBuffers = 2;
	// Past this many rects the compositor gets one full-buffer damage instead.
	private const int MaximumDamageRects = 16;

	private readonly WlSurface _surface;
	private readonly WlShm _shm;
	private readonly Action<Action> _postToOwner;
	private readonly WaylandCommitGate _prepareCommit;
	private readonly List<WaylandShmBuffer> _buffers = new();
	private SoftwareRenderTarget? _target;
	private bool _contentsPreserved;
	private bool _frameAcquired;
	private bool _disposed;
	// Accumulated since the last commit, so frames that could not be committed are still reported.
	private readonly List<(int X, int Y, int Width, int Height)> _damage = new();
	private bool _damageWhole = true;
	private bool _damageReported;

	// Buffer release events arrive on the dispatch thread and are posted to the owner thread.
	public WaylandSoftwareGraphicsContext(WlSurface surface, WlShm shm, Action<Action> postToOwner, WaylandCommitGate prepareCommit)
	{
		_surface = surface;
		_shm = shm;
		_postToOwner = postToOwner;
		_prepareCommit = prepareCommit;
	}

	public event EventHandler? BufferAvailable;

	public GraphicsContextKind Kind => GraphicsContextKind.Software;

	// The target outlives presentation, so an unresized frame can repaint only its damage.
	public bool PreservesContents => _contentsPreserved;

	public bool Committed { get; private set; }

	public IRenderTarget AcquireRenderTarget(int width, int height)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		width = Math.Max(1, width);
		height = Math.Max(1, height);
		_contentsPreserved = _target is not null && _target.Width == width && _target.Height == height;
		if (!_contentsPreserved)
		{
			_target?.Free();
			_target = new SoftwareRenderTarget(width, height);
			_damageWhole = true;
		}
		_frameAcquired = true;
		_damageReported = false;
		return _target!;
	}

	public void SetPresentDamage(Rect[]? damage)
	{
		_damageReported = true;
		if (damage is null || _damageWhole || _damage.Count + damage.Length > MaximumDamageRects)
		{
			_damageWhole = true;
			_damage.Clear();
			return;
		}
		foreach (var rect in damage)
		{
			var x = (int)Math.Floor(rect.X);
			var y = (int)Math.Floor(rect.Y);
			_damage.Add((x, y, (int)Math.Ceiling(rect.Right) - x, (int)Math.Ceiling(rect.Bottom) - y));
		}
	}

	public void Present()
	{
		Committed = false;
		if (!_frameAcquired || _target is not { } target || _disposed)
		{
			return;
		}
		_frameAcquired = false;
		if (!_damageReported)
		{
			_damageWhole = true;
		}
		// A frame that cannot be committed stays in the target and is copied by a later present.
		if (GetAvailableBuffer(target.Width, target.Height) is not { } buffer)
		{
			return;
		}
		unsafe
		{
			NativeMemory.Copy((void*)target.Pixels, (void*)buffer.Memory, (nuint)buffer.Length);
		}
		Committed = _prepareCommit(target.Width, target.Height, () =>
		{
			buffer.Busy = true;
			_surface.Attach(buffer.Proxy, 0, 0);
			if (_surface.Version < 4)
			{
				// Surface-local damage would need the buffer scale and viewport; damage_buffer needs neither.
				_surface.Damage(0, 0, int.MaxValue, int.MaxValue);
			}
			else if (_damageWhole)
			{
				_surface.DamageBuffer(0, 0, target.Width, target.Height);
			}
			else
			{
				foreach (var (x, y, width, height) in _damage)
				{
					_surface.DamageBuffer(x, y, width, height);
				}
			}
			_surface.Commit();
			return true;
		});
		if (Committed)
		{
			_damage.Clear();
			_damageWhole = false;
		}
	}

	private WaylandShmBuffer? GetAvailableBuffer(int width, int height)
	{
		WaylandShmBuffer? available = null;
		foreach (var buffer in _buffers.ToArray())
		{
			if (buffer.Width != width || buffer.Height != height)
			{
				buffer.Retired = true;
				if (!buffer.Busy)
				{
					Remove(buffer);
				}
			}
			else if (!buffer.Retired && !buffer.Busy)
			{
				available = buffer;
			}
		}
		if (available is null)
		{
			if (_buffers.Count >= MaximumBuffers)
			{
				return null;
			}
			available = new WaylandShmBuffer(_shm, width, height, buffer => _postToOwner(() => OnRelease(buffer)));
			_buffers.Add(available);
		}
		return available;
	}

	private void OnRelease(WaylandShmBuffer buffer)
	{
		if (!_buffers.Contains(buffer))
		{
			return;
		}
		buffer.Busy = false;
		if (buffer.Retired || _disposed)
		{
			Remove(buffer);
		}
		if (!_disposed)
		{
			BufferAvailable?.Invoke(this, EventArgs.Empty);
		}
	}

	private void Remove(WaylandShmBuffer buffer)
	{
		_buffers.Remove(buffer);
		buffer.Dispose();
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;
		foreach (var buffer in _buffers.ToArray())
		{
			Remove(buffer);
		}
		_target?.Free();
		_target = null;
	}

	private sealed class SoftwareRenderTarget : ISoftwareRenderTarget
	{
		public SoftwareRenderTarget(int width, int height)
		{
			Width = width;
			Height = height;
			unsafe
			{
				Pixels = (nint)NativeMemory.AllocZeroed((nuint)checked(width * 4 * height));
			}
		}

		public nint Pixels { get; private set; }
		public int RowBytes => Width * 4;
		public int Width { get; }
		public int Height { get; }
		public GraphicsColorFormat ColorFormat => GraphicsColorFormat.Bgra8888;

		// The backend disposes the targets it is handed per frame; the context owns the memory.
		public void Dispose() { }

		public unsafe void Free()
		{
			NativeMemory.Free((void*)Pixels);
			Pixels = 0;
		}
	}
}
