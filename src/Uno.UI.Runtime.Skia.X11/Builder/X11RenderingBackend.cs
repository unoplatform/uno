namespace Uno.UI.Hosting;

/// <summary>
/// A rendering backend (GPU API) offered by the X11 Skia host, used with
/// <see cref="X11HostBuilder.ForceRenderingBackend"/> and <see cref="X11HostBuilder.DisableRenderingBackends"/>.
/// </summary>
public enum X11RenderingBackend
{
	/// <summary>
	/// Vulkan hardware acceleration.
	/// </summary>
	Vulkan,

	/// <summary>
	/// OpenGL via GLX.
	/// </summary>
	OpenGL,

	/// <summary>
	/// OpenGL ES via EGL.
	/// </summary>
	OpenGLES,

	/// <summary>
	/// CPU-based software rendering. No GPU acceleration.
	/// </summary>
	Software,
}
