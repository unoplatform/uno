#nullable enable

using SkiaSharp;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// SkiaSharp-backed <see cref="IRecordingSession"/> that draws into an <c>SKPictureRecorder</c>'s canvas
/// and produces a <see cref="SkiaRenderData"/> (an <c>SKPicture</c>) on <see cref="EndRecording"/>.
/// </summary>
internal sealed class SkiaRecordingSession : SkiaDrawingSession, IRecordingSession
{
	private readonly SKPictureRecorder _recorder;

	public SkiaRecordingSession(SKPictureRecorder recorder, SKCanvas recordingCanvas)
		: base(recordingCanvas)
		=> _recorder = recorder;

	public IRenderData EndRecording()
	{
		var picture = UnoSkiaApi.sk_picture_recorder_end_recording(_recorder.Handle);
		ReturnRecorder(_recorder);
		return new SkiaRenderData(picture);
	}
}
