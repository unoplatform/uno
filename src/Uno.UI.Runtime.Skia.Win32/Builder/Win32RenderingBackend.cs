namespace Uno.UI.Hosting;

/// <summary>
/// A rendering backend (GPU API) offered by the Win32 Skia host, used with
/// <see cref="Win32HostBuilder.ForceRenderingBackend"/> and <see cref="Win32HostBuilder.DisableRenderingBackends"/>.
/// </summary>
public enum Win32RenderingBackend
{
	/// <summary>
	/// Vulkan hardware acceleration.
	/// </summary>
	Vulkan,

	/// <summary>
	/// OpenGL via WGL.
	/// </summary>
	OpenGL,

	/// <summary>
	/// CPU-based software rendering. No GPU acceleration.
	/// </summary>
	Software,
}
