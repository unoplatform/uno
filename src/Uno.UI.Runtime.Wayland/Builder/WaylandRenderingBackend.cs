#nullable enable

namespace Uno.UI.Hosting;

/// <summary>
/// A rendering backend (GPU API) offered by the Wayland host, used with
/// <see cref="WaylandHostBuilder.ForceRenderingBackend"/> and <see cref="WaylandHostBuilder.DisableRenderingBackends"/>.
/// </summary>
public enum WaylandRenderingBackend
{
	/// <summary>
	/// OpenGL ES via EGL.
	/// </summary>
	OpenGLES = 1,

	/// <summary>
	/// CPU-based software rendering into shared-memory buffers. No GPU acceleration.
	/// </summary>
	Software = 2,
}
