#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// A drawing session that captures the draw calls made on it into a replayable command buffer
/// (<see cref="IRenderData"/>). This is the universal begin/record/<see cref="Finish"/> command-buffer shape
/// (SKPicture, D3D/Vulkan/WebGPU command buffers), not specific to any backend.
/// </summary>
/// <remarks>
/// A backend may also implement <see cref="IRetainedRenderingSession"/> on its recorder for efficient native
/// nesting (composition obtains that capability through <c>RetainedRenderingSession.For</c>, which supplies a
/// command-list fallback when the recorder does not) — so it is not a required base of this interface.
/// </remarks>
public interface ICommandRecorder : IDrawingSession
{
	/// <summary>Finishes recording and returns the backend's retained representation of what was drawn.</summary>
	IRenderData Finish();
}
