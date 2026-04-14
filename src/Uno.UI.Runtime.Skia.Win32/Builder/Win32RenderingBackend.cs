namespace Uno.UI.Hosting;

/// <summary>
/// Specifies the rendering backend for the Win32 Skia host.
/// </summary>
public enum Win32RenderingBackend
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
	/// OpenGL via WGL. Falls back to software if unavailable.
	/// </summary>
	OpenGL,

	/// <summary>
	/// CPU-based software rendering. No GPU acceleration.
	/// </summary>
	Software,
}
