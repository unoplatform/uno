#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// A drawing session that captures the draw calls made on it, producing a replayable
/// <see cref="IRenderData"/>. Obtained from <see cref="IDrawingSession.CreateRecording"/>.
/// </summary>
internal interface IRecordingSession : IDrawingSession
{
	/// <summary>Finishes recording and returns the backend's retained representation of what was drawn.</summary>
	IRenderData EndRecording();
}
