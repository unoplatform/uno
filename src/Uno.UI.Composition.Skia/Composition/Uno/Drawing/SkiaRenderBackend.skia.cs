#nullable enable

using System;

namespace Uno.UI.Composition.Drawing;

/// <summary>The default <see cref="IRenderer"/>: the established Skia two-phase (SKPicture) lifecycle.</summary>
internal sealed class SkiaRenderer : IRenderer
{
	public ICommandRecorder BeginFrame() => SkiaDrawingSession.StartRecording();

	// Wraps whatever target the active context handed over. A host that already owns an SKCanvas passes a
	// SkiaRenderTarget directly; a neutral context (Uno's context factory) hands a kind-specific target that
	// the Skia backend wraps here — the CPU-framebuffer case turns an ISoftwareRenderTarget into an SKSurface.
	public IPresentSession BeginPresent(IRenderTarget target)
		=> target switch
		{
			SkiaRenderTarget skia => new SkiaPresentSession(skia.Canvas),
			ISoftwareRenderTarget software => SkiaPresentSession.ForSoftware(software),
			_ => throw new NotSupportedException($"The Skia backend cannot present onto a render target of type {target.GetType().Name}."),
		};
}
