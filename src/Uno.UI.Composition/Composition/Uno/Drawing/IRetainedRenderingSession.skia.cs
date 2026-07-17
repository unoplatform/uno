#nullable enable

using Windows.Foundation;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Optional capability an <see cref="IDrawingSession"/> may advertise: it can retain a sequence of draw
/// calls as an opaque <see cref="IRenderData"/> and replay it cheaply. Composition uses this to cache
/// per-visual and collapsed-subtree content; a backend whose session does not implement this interface is
/// simply re-drawn every frame (correct, just uncached). A backend opts in by implementing it.
/// </summary>
/// <remarks>
/// Kept off the core <see cref="IDrawingSession"/> so a third-party backend only has to provide the
/// immediate-mode drawing verbs — retained recording is an implementation choice, not an obligation.
/// </remarks>
internal interface IRetainedRenderingSession
{
	/// <summary>
	/// Begins recording into a nested session whose draw calls are captured as an <see cref="IRenderData"/>
	/// (via <see cref="ICommandRecorder.Finish"/>) that can later be replayed with <see cref="Replay"/>.
	/// </summary>
	ICommandRecorder CreateRecording();

	/// <summary>Replays previously recorded <paramref name="data"/> into this session.</summary>
	void Replay(IRenderData data);
}
