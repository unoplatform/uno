#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// A drawing session that captures the draw calls made on it into a replayable command buffer
/// (<see cref="IRenderRecord"/>). This is the universal begin/record/<see cref="Finish"/> command-buffer shape
/// (SKPicture, D3D/Vulkan/WebGPU command buffers), not specific to any backend.
/// </summary>
/// <remarks>
/// Recordings nest naturally: recording a sub-tree yields an <see cref="IRenderRecord"/> whose
/// <see cref="IRenderRecord.Replay"/> composes it into a parent recorder (also an <see cref="IDrawingSession"/>).
/// </remarks>
public interface ICommandRecorder : IDrawingSession
{
	/// <summary>Finishes recording and returns the backend's retained representation of what was drawn.</summary>
	IRenderRecord Finish();
}
