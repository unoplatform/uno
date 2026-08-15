#nullable enable

using System;
using SkiaSharp;

namespace Uno.UI.Composition.Drawing;

/// <summary>SkiaSharp-backed <see cref="IRenderData"/> holding a native <c>SKPicture</c> handle.</summary>
internal sealed class SkiaRenderData : IRenderData
{
	public SkiaRenderData(IntPtr picture) => Picture = picture;

	/// <summary>The native <c>SKPicture</c> handle (may be <see cref="IntPtr.Zero"/> if nothing was recorded).</summary>
	public IntPtr Picture { get; private set; }

	// Backend-bound: an SKPicture only replays onto an SKCanvas, so `into` must be a Skia session (guaranteed by
	// the single-registered-backend invariant). The cast is the backend recognizing its own session type.
	public void Replay(IDrawingSession into)
	{
		if (Picture != IntPtr.Zero && into is SkiaDrawingSession session)
		{
			unsafe
			{
				UnoSkiaApi.sk_canvas_draw_picture(session.Canvas.Handle, Picture, null, IntPtr.Zero);
			}
		}
	}

	public void Dispose()
	{
		if (Picture != IntPtr.Zero)
		{
			UnoSkiaApi.sk_refcnt_safe_unref(Picture);
			Picture = IntPtr.Zero;
		}
	}
}
