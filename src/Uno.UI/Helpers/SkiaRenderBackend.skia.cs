#nullable enable

using System;
using Microsoft.UI.Composition;
using SkiaSharp;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.Helpers;

/// <summary>The default <see cref="IRenderBackend"/>: the established Skia two-phase (SKPicture) lifecycle.</summary>
internal sealed class SkiaRenderBackend : IRenderBackend
{
	public IRecordingSession BeginFrame()
		=> SkiaDrawingSession.StartRecording(Visual.InfiniteClipRect);

	public void Present(IRenderData frame, SKCanvas target, Action<SKCanvas>? postPresent)
	{
		using (new SKAutoCanvasRestore(target, true))
		{
			target.Clear(SKColors.Transparent);
			// Draws nothing if we get a present request before the first frame is recorded.
			new SkiaDrawingSession(target).Replay(frame);
			postPresent?.Invoke(target);
		}

		target.Flush();
	}
}
