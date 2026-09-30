namespace Uno.UI.Hosting;

/// <summary>
/// A rendering backend (GPU API) offered by the X11 Skia host, used with
/// <see cref="X11HostBuilder.ForceRenderingBackend"/> and <see cref="X11HostBuilder.DisableRenderingBackends"/>.
/// </summary>
public enum X11RenderingBackend
{
	// 0 is unused: it was the removed Default member, and the values below must keep the meaning an
	// already-compiled caller passes.

	/// <summary>
	/// Vulkan hardware acceleration.
	/// </summary>
	Vulkan = 1,

	/// <summary>
	/// OpenGL via GLX.
	/// </summary>
	OpenGL = 2,

	/// <summary>
	/// OpenGL ES via EGL.
	/// </summary>
	OpenGLES = 3,

	/// <summary>
	/// CPU-based software rendering. No GPU acceleration.
	/// </summary>
	Software = 4,
}
