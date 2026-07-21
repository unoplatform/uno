#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// A drawing session that captures the draw calls made on it into a replayable command buffer
/// (<see cref="IRenderData"/>). Obtained from <see cref="IRetainedRenderingSession.CreateRecording"/>.
/// It is itself retained-capable so recordings can nest. This is the universal
/// begin/record/<see cref="Finish"/> command-buffer shape (SKPicture, D3D/Vulkan/WebGPU command buffers), not
/// specific to any backend.
/// </summary>
public interface ICommandRecorder : IDrawingSession, IRetainedRenderingSession
{
	/// <summary>Finishes recording and returns the backend's retained representation of what was drawn.</summary>
	IRenderData Finish();
}
