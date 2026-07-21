#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// A rendering backend, as a passive participant in Uno's backend-agnostic two-phase render cycle
/// (the cycle itself — scheduling, vsync, threading — stays in <c>CompositionTarget</c>):
/// <list type="number">
/// <item>Phase 1 (UI thread): <see cref="BeginFrame"/> returns the <see cref="ICommandRecorder"/> that the
/// cycle walks the visual tree into (the walk lives in <c>Visual.skia.cs</c>, not here); the cycle then
/// calls <see cref="ICommandRecorder.Finish"/> to obtain the opaque <see cref="IRenderData"/> frame.</item>
/// <item>Phase 2 (on a vsync/present signal): <see cref="BeginPresent"/> composes a recorded frame onto the target.</item>
/// </list>
/// </summary>
public interface IRenderBackend
{
	/// <summary>Phase 1: begins a frame and returns the session the render cycle records the visual tree into.</summary>
	ICommandRecorder BeginFrame();

	/// <summary>
	/// Phase 2: begins composing onto <paramref name="target"/>. The cycle replays the recorded frame and
	/// draws any overlay into the returned session, then disposes it to finalize (present) the composition.
	/// </summary>
	IPresentSession BeginPresent(IRenderSurface target);
}
