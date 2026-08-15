#nullable enable

using System;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// The device face of a Vulkan graphics context: the stable device/instance/queue details a backend needs to
/// build its Vulkan rendering state — everything a <c>GRVkBackendContext</c> requires. A backend reads these
/// from the context at <see cref="IGraphicsProvider{TContext}.CreateGraphics"/> (the context <em>is</em> the
/// device); the per-frame color image is a separate <see cref="IVulkanRenderTarget"/> concern. Neutral: only
/// opaque handles, primitives, string arrays and a plain <c>Func</c> cross the seam — no GPU-library type.
/// </summary>
public interface IVulkanDeviceContext : IGraphicsContext
{
	nint Instance { get; }

	nint PhysicalDevice { get; }

	nint Device { get; }

	nint Queue { get; }

	uint GraphicsQueueFamilyIndex { get; }

	/// <summary>The highest Vulkan API version the device was created for (packed VK_MAKE_VERSION).</summary>
	uint MaxApiVersion { get; }

	/// <summary>The instance extensions enabled at creation (required so the backend can declare them).</summary>
	string[] InstanceExtensions { get; }

	/// <summary>The device extensions enabled at creation.</summary>
	string[] DeviceExtensions { get; }

	/// <summary>
	/// Vulkan proc-address loader: (name, instance, device) → address. Resolves a device proc when
	/// <paramref name="device"/> is non-zero, else an instance proc. Mirrors the Vulkan get-proc contract a
	/// backend's GRVk interface assembly needs; neutral (a plain <c>Func</c>, no GPU-library delegate type).
	/// </summary>
	Func<string, nint, nint, nint> GetProcAddress { get; }
}
