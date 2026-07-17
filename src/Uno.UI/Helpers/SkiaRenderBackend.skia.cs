#nullable enable

using Uno.UI.Composition.Drawing;

namespace Uno.UI.Helpers;

/// <summary>The default <see cref="IRenderBackend"/>: the established Skia two-phase (SKPicture) lifecycle.</summary>
internal sealed class SkiaRenderBackend : IRenderBackend
{
	public ICommandRecorder BeginFrame() => SkiaDrawingSession.StartRecording();

	public IPresentSession BeginPresent(IRenderSurface target)
		=> new SkiaPresentSession(((SkiaRenderSurface)target).Canvas);
}
