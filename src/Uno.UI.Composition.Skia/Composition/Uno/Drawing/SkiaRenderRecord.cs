#nullable enable

using System;
using SkiaSharp;

namespace Uno.UI.Composition.Drawing;

/// <summary>SkiaSharp-backed <see cref="IRenderRecord"/> holding a native <c>SKPicture</c> handle.</summary>
internal sealed class SkiaRenderRecord : IRenderRecord
{
	public SkiaRenderRecord(IntPtr picture) => _picture = picture;

	// The owned native SKPicture handle; IntPtr.Zero once disposed, or if nothing was recorded.
	private IntPtr _picture;

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
		if (_picture != IntPtr.Zero)
		{
			UnoSkiaApi.sk_refcnt_safe_unref(_picture);
			_picture = IntPtr.Zero;
		}
	}
}
