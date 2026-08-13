#nullable enable

using Windows.Foundation;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Optional capability an <see cref="IDrawingSession"/> may advertise: it can retain a sequence of draw
/// calls as an opaque <see cref="IRenderData"/> and replay it cheaply (e.g. an SKPicture or a GPU command
/// buffer). Composition uses this to cache per-visual and collapsed-subtree content.
/// </summary>
/// <remarks>
/// Kept off the core <see cref="IDrawingSession"/> so a third-party backend only has to provide the
/// immediate-mode drawing verbs — implementing this is an efficiency choice, not an obligation. Composition
/// never touches a session directly: it goes through <c>RetainedRenderingSession.For</c>, which returns this
/// native capability when present and otherwise a command-list fallback that records and replays the neutral
/// verbs on top of any session. Retention is therefore always available, so composition has no uncached path.
/// </remarks>
public interface IRetainedRenderingSession
{
	/// <summary>
	/// Begins recording into a nested session whose draw calls are captured as an <see cref="IRenderData"/>
	/// (via <see cref="ICommandRecorder.Finish"/>) that can later be replayed with <see cref="Replay"/>.
	/// </summary>
	ICommandRecorder CreateRecording();

	/// <summary>Replays previously recorded <paramref name="data"/> into this session.</summary>
	void Replay(IRenderData data);
}
