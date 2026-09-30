namespace Uno.UI.Hosting;

/// <summary>
/// A rendering backend (GPU API) offered by the Win32 Skia host, used with
/// <see cref="Win32HostBuilder.ForceRenderingBackend"/> and <see cref="Win32HostBuilder.DisableRenderingBackends"/>.
/// </summary>
public enum Win32RenderingBackend
{
	// 0 is unused: it was the removed Default member, and the values below must keep the meaning an
	// already-compiled caller passes.

	/// <summary>
	/// Vulkan hardware acceleration.
	/// </summary>
	Vulkan = 1,

	/// <summary>
	/// OpenGL via WGL.
	/// </summary>
	OpenGL = 2,

	/// <summary>
	/// CPU-based software rendering. No GPU acceleration.
	/// </summary>
	Software = 3,
}
