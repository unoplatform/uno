#nullable enable

using System;
using Uno.UI.Runtime.Wayland.Protocols;

namespace Uno.UI.Runtime.Wayland.Rendering;

/// <summary>
/// A wl_buffer over its own memfd-backed shared memory, in premultiplied ARGB8888 (BGRA bytes).
/// </summary>
internal sealed class WaylandShmBuffer : IDisposable
{
	private const uint ShmFormatArgb8888 = 0;
	private const uint MfdCloexec = 1;
	private const int ProtReadWrite = 3;
	private const int MapShared = 1;

	private readonly Action<WaylandShmBuffer>? _released;

	public WaylandShmBuffer(WlShm shm, int width, int height, Action<WaylandShmBuffer>? released)
	{
		Width = width;
		Height = height;
		_released = released;
		Stride = checked(width * 4);
		Length = checked(Stride * height);
		var fd = WaylandRenderNative.memfd_create("uno-wayland", MfdCloexec);
		if (fd < 0)
		{
			throw new InvalidOperationException("memfd_create failed.");
		}
		WlBuffer? proxy = null;
		try
		{
			if (WaylandRenderNative.ftruncate(fd, Length) != 0)
			{
				throw new InvalidOperationException("ftruncate failed.");
			}
			Memory = WaylandRenderNative.mmap(IntPtr.Zero, (nuint)Length, ProtReadWrite, MapShared, fd, 0);
			if (Memory == new IntPtr(-1))
			{
				Memory = IntPtr.Zero;
				throw new InvalidOperationException("mmap failed.");
			}
			using var pool = shm.CreatePool(fd, Length);
			proxy = pool.CreateBuffer(0, width, height, Stride, ShmFormatArgb8888);
			Proxy = proxy;
			Proxy.Release += OnRelease;
		}
		catch
		{
			proxy?.Dispose();
			if (Memory != IntPtr.Zero)
			{
				_ = WaylandRenderNative.munmap(Memory, (nuint)Length);
			}
			throw;
		}
		finally
		{
			_ = WaylandRenderNative.close(fd);
		}
	}

	public int Width { get; }

	public int Height { get; }

	public int Stride { get; }

	public int Length { get; }

	public IntPtr Memory { get; }

	public WlBuffer Proxy { get; }

	// Owner thread state: attached and not yet released by the compositor.
	public bool Busy { get; set; }

	// Owner thread state: no longer the current size, destroyed once released.
	public bool Retired { get; set; }

	private void OnRelease(object? sender, EventArgs args) => _released?.Invoke(this);

	public void Dispose()
	{
		Proxy.Release -= OnRelease;
		Proxy.Dispose();
		_ = WaylandRenderNative.munmap(Memory, (nuint)Length);
	}
}
