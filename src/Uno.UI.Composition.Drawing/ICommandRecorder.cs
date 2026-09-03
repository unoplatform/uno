#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// A drawing session that captures the draw calls made on it into a replayable command buffer
/// (<see cref="IRenderRecord"/>) — the universal begin/record/<see cref="Finish"/> shape, not specific to any backend.
/// Recordings nest: recording a sub-tree yields a record whose <see cref="IRenderRecord.Replay"/> composes it into a parent recorder.
/// </summary>
public interface ICommandRecorder : IDrawingSession
{
	/// <summary>Finishes recording and returns the backend's retained representation of what was drawn.</summary>
	IRenderRecord Finish();
}
