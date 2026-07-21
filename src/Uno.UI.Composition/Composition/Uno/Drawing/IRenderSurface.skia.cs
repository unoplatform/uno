#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Opaque, backend-defined presentation target that a recorded frame is presented onto via
/// <see cref="IRenderBackend.Present"/>. The host provides it (wrapping its swapchain surface) and
/// composition passes it through without inspecting it; the Skia backend wraps an <c>SKCanvas</c>,
/// another backend may wrap a texture, a device context, or any surface it owns.
/// </summary>
public interface IRenderSurface
{
}
