#nullable enable

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
		var picture = UnoSkiaApi.sk_picture_recorder_end_recording(_recorder.Handle);
		ReturnRecorder(_recorder);
		return new SkiaRenderRecord(picture);
	}
}
