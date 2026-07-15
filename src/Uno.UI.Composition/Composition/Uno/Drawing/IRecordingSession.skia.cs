#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// A drawing session that captures the draw calls made on it, producing a replayable
/// <see cref="IRenderData"/>. Obtained from <see cref="IRetainedRenderingSession.CreateRecording"/>.
/// It is itself retained-capable so recordings can nest.
/// </summary>
internal interface IRecordingSession : IDrawingSession, IRetainedRenderingSession
{
	/// <summary>Finishes recording and returns the backend's retained representation of what was drawn.</summary>
	IRenderData EndRecording();
}
