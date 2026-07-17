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

	public void Present(IRenderData frame, IRenderSurface target)
	{
		var canvas = ((SkiaRenderSurface)target).Canvas;
		using (new SKAutoCanvasRestore(canvas, true))
		{
			canvas.Clear(SKColors.Transparent);
			// Draws nothing if we get a present request before the first frame is recorded.
			new SkiaDrawingSession(canvas).Replay(frame);
		}

		canvas.Flush();
	}
}
