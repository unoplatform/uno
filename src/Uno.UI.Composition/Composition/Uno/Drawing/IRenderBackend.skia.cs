#nullable enable

using System;
using SkiaSharp;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// A rendering backend, as a passive participant in Uno's backend-agnostic two-phase render cycle
/// (the cycle itself — scheduling, vsync, threading — stays in <c>CompositionTarget</c>):
/// <list type="number">
/// <item>Phase 1 (UI thread): <see cref="BeginFrame"/> returns the <see cref="IRecordingSession"/> that the
/// cycle walks the visual tree into (the walk lives in <c>Visual.skia.cs</c>, not here); the cycle then
/// calls <see cref="IRecordingSession.EndRecording"/> to obtain the opaque <see cref="IRenderData"/> frame.</item>
/// <item>Phase 2 (on a vsync/present signal): <see cref="Present"/> draws a recorded frame onto the target.</item>
/// </list>
/// </summary>
/// <remarks>
/// The present target is still a SkiaSharp <see cref="SKCanvas"/> here — the host swapchain surface hasn't
/// been neutralized yet; that (and the per-platform host contract) is follow-up work.
/// </remarks>
internal interface IRenderBackend
{
	/// <summary>Phase 1: begins a frame and returns the session the render cycle records the visual tree into.</summary>
	IRecordingSession BeginFrame();

	/// <summary>Phase 2: presents a previously recorded <paramref name="frame"/> onto <paramref name="target"/>.</summary>
	void Present(IRenderData frame, SKCanvas target, Action<SKCanvas>? postPresent);
}
