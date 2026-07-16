#nullable enable

using System;

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
internal interface IRenderBackend
{
	/// <summary>Phase 1: begins a frame and returns the session the render cycle records the visual tree into.</summary>
	IRecordingSession BeginFrame();

	/// <summary>
	/// Phase 2: presents a previously recorded <paramref name="frame"/> onto <paramref name="target"/>.
	/// <paramref name="postPresent"/>, if provided, draws overlay content (e.g. diagnostics) onto the same
	/// surface via the session the backend used to present.
	/// </summary>
	void Present(IRenderData frame, IRenderSurface target, Action<IDrawingSession>? postPresent);
}
