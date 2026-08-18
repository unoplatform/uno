#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// A Vulkan color target: the <c>VkImage</c> (with the fields a <c>GRVkImageInfo</c> needs) the backend renders
/// into, as opaque 64-bit handles. The Vulkan device details live on <see cref="IVulkanDeviceContext"/>, not here.
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
