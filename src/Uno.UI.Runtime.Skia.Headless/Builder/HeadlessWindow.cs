#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI.Runtime.Skia.Headless;

namespace Uno.UI.Runtime.Skia;

/// <summary>
/// A handle to a single headless (offscreen) window, handed to the caller once when the window is
/// created. Subscribe to <see cref="NewFrameReady"/> to be told when there is new content, and call
/// <see cref="RenderIntoAsync"/> whenever you want to render the current content into a buffer you
/// own — the two are independent.
/// </summary>
public sealed class HeadlessWindow
{
	private readonly HeadlessRenderer _renderer;

	internal HeadlessWindow(HeadlessRenderer renderer, int width, int height)
	{
		_renderer = renderer;
		Width = width;
		Height = height;
	}

	/// <summary>Width of the window's surface, in raw pixels.</summary>
	public int Width { get; }

	/// <summary>Height of the window's surface, in raw pixels.</summary>
	public int Height { get; }

	/// <summary>
	/// Raised (on the invalidation thread) when the window has new content ready to draw. This is only
	/// a signal — it neither renders nor requires you to render; call <see cref="RenderIntoAsync"/>
	/// when and if you want the pixels.
	/// </summary>
	public event EventHandler? NewFrameReady;

	/// <summary>
	/// Requests that the window's current content be rendered into the caller-supplied buffer. Can be
	/// called at any time, from any thread, independently of <see cref="NewFrameReady"/>. The host
	/// schedules the render on its render thread; the returned task completes once the buffer has been
	/// filled. The host writes to <paramref name="buffer"/> until then, so keep it valid until the task
	/// completes — after that it is never touched again and may be temporary or owned by anything
	/// (a window, image, texture, …).
	/// </summary>
	/// <param name="buffer">Destination buffer, at least <c><paramref name="rowBytes"/> × Height</c> bytes.</param>
	/// <param name="rowBytes">Bytes per pixel row (stride) of <paramref name="buffer"/>.</param>
	/// <param name="pixelFormat">The pixel format to render.</param>
	/// <param name="cancellationToken">Cancels the pending render request.</param>
	public Task RenderIntoAsync(IntPtr buffer, int rowBytes, HeadlessPixelFormat pixelFormat = HeadlessPixelFormat.Bgra8888, CancellationToken cancellationToken = default)
		=> _renderer.RenderIntoAsync(buffer, rowBytes, pixelFormat, cancellationToken);

	internal void RaiseNewFrameReady() => NewFrameReady?.Invoke(this, EventArgs.Empty);
}
