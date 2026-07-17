#nullable enable

using System;
using Microsoft.UI.Composition;
using SkiaSharp;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.Helpers;

/// <summary>The default <see cref="IRenderBackend"/>: the established Skia two-phase (SKPicture) lifecycle.</summary>
internal sealed class SkiaRenderBackend : IRenderBackend
{
	public ICommandRecorder BeginFrame() => SkiaDrawingSession.StartRecording();

	public void Present(IRenderData frame, IRenderSurface target, Action<IDrawingSession>? postPresent)
	{
		var canvas = ((SkiaRenderSurface)target).Canvas;
		using (new SKAutoCanvasRestore(canvas, true))
		{
			canvas.Clear(SKColors.Transparent);
			// Draws nothing if we get a present request before the first frame is recorded.
			var session = new SkiaDrawingSession(canvas);
			session.Replay(frame);
			postPresent?.Invoke(session);
		}

		canvas.Flush();
	}
}
