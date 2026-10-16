#nullable enable

using System;
using SkiaSharp;

namespace Uno.UI.Composition.Drawing;

/// <summary>SkiaSharp-backed <see cref="IRenderRecord"/> holding a native <c>SKPicture</c> handle.</summary>
internal sealed class SkiaRenderRecord : IRenderRecord
{
	public SkiaRenderRecord(IntPtr picture) => _picture = picture;

	/// <summary>
	/// Keeps the managed picture alive so it can be handed out as frame data. SkiaSharp has no public way to wrap
	/// an existing handle, so the managed object has to be the one EndRecording produced.
	/// </summary>
	public SkiaRenderRecord(SKPicture picture)
	{
		_managed = picture;
		_picture = picture.Handle;
	}

	// The owned native SKPicture handle; IntPtr.Zero once disposed, or if nothing was recorded.
	private IntPtr _picture;
	private SKPicture? _managed;

	public object? FrameData => _managed;

	// Backend-bound: an SKPicture only replays onto an SKCanvas, so `into` must be a Skia session (guaranteed by
	// the single-registered-backend invariant). The cast is the backend recognizing its own session type.
	public void Replay(IDrawingSession into)
	{
		if (_picture != IntPtr.Zero && into is SkiaDrawingSession session)
		{
			unsafe
			{
				UnoSkiaApi.sk_canvas_draw_picture(session.Canvas.Handle, _picture, null, IntPtr.Zero);
			}
		}
	}

	public void Dispose()
	{
		if (_managed is { } managed)
		{
			// The managed wrapper owns the same handle, so releasing it twice would over-unref.
			_managed = null;
			_picture = IntPtr.Zero;
			managed.Dispose();
			return;
		}

		if (_picture != IntPtr.Zero)
		{
			UnoSkiaApi.sk_refcnt_safe_unref(_picture);
			_picture = IntPtr.Zero;
		}
	}
}
