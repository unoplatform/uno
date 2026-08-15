#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// A Vulkan color target: the <c>VkImage</c> the backend renders into, described neutrally as the fields a
/// <c>GRVkImageInfo</c> needs. Pure surface — the Vulkan device details live on the context
/// (<see cref="IVulkanDeviceContext"/>), not here. The Skia backend wraps this image as its surface via a
/// <c>GRContext</c>-Vulkan built from the device context; the host's <see cref="ISwapChain.Present"/> then
/// blits it to the swapchain. Mirrors <see cref="IGLRenderTarget"/> / <see cref="IMetalRenderTarget"/>. All
/// handles are opaque 64-bit Vulkan handles.
/// </summary>
public interface IVulkanRenderTarget : IRenderTarget
{
	ulong Image { get; }

	ulong Memory { get; }

	ulong MemorySize { get; }

	uint Format { get; }

	uint ImageTiling { get; }

	uint ImageLayout { get; }

	uint ImageUsageFlags { get; }

	uint SampleCount { get; }

	uint LevelCount { get; }

	uint CurrentQueueFamily { get; }

	bool Protected { get; }
}
