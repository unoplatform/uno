#nullable enable

using System;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// The device face of a Vulkan graphics context: the device/instance/queue details a backend needs to build its
/// Vulkan rendering state. The per-frame color image is a separate <see cref="IVulkanRenderTarget"/> concern;
/// only neutral handles, primitives, string arrays and a plain <c>Func</c> cross the seam.
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
	/// <paramref name="device"/> is non-zero, else an instance proc.
	/// </summary>
	Func<string, nint, nint, nint> GetProcAddress { get; }
}
