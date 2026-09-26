#nullable enable

using System;
using SkiaSharp;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// SkiaSharp-backed <see cref="ICommandRecorder"/> that draws into an <c>SKPictureRecorder</c>'s canvas
/// and produces a <see cref="SkiaRenderRecord"/> (an <c>SKPicture</c>) on <see cref="Finish"/>.
/// </summary>
internal sealed class SkiaCommandRecorder : SkiaDrawingSession, ICommandRecorder
{
	private readonly SKPictureRecorder _recorder;

	public SkiaCommandRecorder(SKPictureRecorder recorder, SKCanvas recordingCanvas, IDrawingFactory factory)
		: base(recordingCanvas, factory)
		=> _recorder = recorder;

	public IRenderRecord Finish()
	{
		if (RenderRecordingOptions.CaptureFrameData)
		{
			var managed = _recorder.EndRecording();
			ReturnRecorder(_recorder);
			// Match the raw path below, which represents "nothing was recorded" as a zero handle rather than throwing.
			return managed is null ? new SkiaRenderRecord(IntPtr.Zero) : new SkiaRenderRecord(managed);
		}

		var picture = UnoSkiaApi.sk_picture_recorder_end_recording(_recorder.Handle);
		ReturnRecorder(_recorder);
		return new SkiaRenderRecord(picture);
	}
}
