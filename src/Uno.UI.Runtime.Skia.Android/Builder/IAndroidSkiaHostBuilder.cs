#nullable enable

namespace Uno.UI.Hosting;

/// <summary>
/// Android-specific options for the Uno Platform host.
/// </summary>
public interface IAndroidSkiaHostBuilder
{
	/// <summary>
	/// Enables or disables the Vulkan render view. When enabled (the default) and the device
	/// reports Vulkan support, rendering uses Vulkan; otherwise it falls back to the canvas
	/// render view, whose acceleration is controlled by <see cref="UseOpenGL"/>.
	/// </summary>
	/// <remarks>
	/// When called, this takes precedence over <see cref="FeatureConfiguration.Rendering.UseVulkanOnSkiaAndroid"/>;
	/// if it is never called, any value already set on that flag is preserved.
	/// It is deliberately independent from <see cref="UseOpenGL"/>: the Vulkan path can fail at
	/// runtime, so both values remain meaningful for a single configuration.
	/// </remarks>
	IAndroidSkiaHostBuilder UseVulkan(bool enabled = true);

	/// <summary>
	/// Enables or disables OpenGL ES acceleration of the canvas render view — the view used
	/// whenever the Vulkan path is disabled or unavailable. When disabled, that view renders
	/// in software.
	/// </summary>
	/// <remarks>
	/// When called, this takes precedence over <see cref="FeatureConfiguration.Rendering.UseOpenGLOnSkiaAndroid"/>;
	/// if it is never called, any value already set on that flag is preserved.
	/// </remarks>
	IAndroidSkiaHostBuilder UseOpenGL(bool enabled = true);
}
