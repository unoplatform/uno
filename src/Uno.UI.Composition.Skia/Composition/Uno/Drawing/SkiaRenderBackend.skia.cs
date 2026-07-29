#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>The default <see cref="IRenderBackend"/>: the established Skia two-phase (SKPicture) lifecycle.</summary>
internal sealed class SkiaRenderBackend : IRenderBackend
{
	public ICommandRecorder BeginFrame() => SkiaDrawingSession.StartRecording();

	public IPresentSession BeginPresent(IRenderTarget target)
		=> new SkiaPresentSession(((SkiaRenderTarget)target).Canvas);
}
