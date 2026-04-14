namespace Uno.UI.Hosting;

/// <summary>
/// Specifies the rendering backend for the X11 Skia host.
/// </summary>
public enum X11RenderingBackend
{
	/// <summary>
	/// Platform default: try OpenGL, fall back to software.
	/// </summary>
	Default,

	/// <summary>
	/// Vulkan hardware acceleration. Falls back to OpenGL or software if unavailable.
	/// </summary>
	Vulkan,

	/// <summary>
	/// OpenGL via GLX. Falls back to software if unavailable.
	/// </summary>
	OpenGL,

	/// <summary>
	/// OpenGL ES via EGL. Falls back to software if unavailable.
	/// </summary>
	OpenGLES,

	/// <summary>
	/// CPU-based software rendering. No GPU acceleration.
	/// </summary>
	Software,
}
