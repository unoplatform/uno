#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// A well-known GPU-API context kind. This is the negotiation currency between the framework's
/// per-kind context providers and a backend's declared preferences. WebGPU is a first-class kind,
/// not a special case.
/// </summary>
public enum GraphicsContextKind
{
	OpenGL,
	OpenGLES,
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

/// <summary>The windowing system a <see cref="INativeWindow"/> belongs to.</summary>
public enum NativeWindowKind
{
	X11,
	Win32,
	Android,
	Metal,
	Wasm,
	Headless,
}

/// <summary>
/// Capabilities a backend needs from whatever context it is given. These are <em>support guarantees</em>
/// on the created context (the provider must select a device that satisfies them, else context creation
/// fails and negotiation falls through) — not resource allocations. The backend still allocates its own
/// depth/stencil/scratch from the context; the requirements only guarantee the device can.
/// </summary>
public readonly struct GraphicsRequirements
{
	/// <summary>Minimum stencil bit-depth the backend needs (e.g. 8 for even-odd/nonzero path fills). 0 = none.</summary>
	public int MinStencilBits { get; init; }

	/// <summary>Whether the backend needs a depth buffer.</summary>
	public bool NeedsDepth { get; init; }

	/// <summary>MSAA sample count for the color target (1 = no multisampling).</summary>
	public int SampleCount { get; init; }

	/// <summary>Preferred color format for the framework-created color target.</summary>
	public GraphicsColorFormat PreferredColor { get; init; }
}
