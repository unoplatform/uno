#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// A well-known GPU-API context kind — the negotiation currency between the framework's per-kind context
/// providers and a backend's declared preferences.
/// </summary>
public enum GraphicsContextKind
{
	OpenGL,
	OpenGLES,
	WebGL,
	Vulkan,
	Metal,
	WebGpu,
	Software,
}

/// <summary>Neutral color format for a render target / swapchain.</summary>
public enum GraphicsColorFormat
{
	Bgra8888,
	Rgba8888,
}

