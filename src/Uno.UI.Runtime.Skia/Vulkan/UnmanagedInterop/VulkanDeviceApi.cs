// Based on the Avalonia project (MIT License, Copyright (c) AvaloniaUI OÜ).
// Original source: https://github.com/AvaloniaUI/Avalonia/tree/master/src/Avalonia.Vulkan
using System;
using System.Runtime.InteropServices;
using static Uno.UI.Runtime.Skia.Vulkan.UnmanagedInterop.VulkanProcHelper;
using uint32_t = System.UInt32;
using uint64_t = System.UInt64;
using VkBool32 = System.UInt32;
using VkDeviceSize = System.UInt64;

namespace Uno.UI.Runtime.Skia.Vulkan.UnmanagedInterop;

internal unsafe class VulkanDeviceApi
{
	// Delegate types
	private delegate VkResult PFN_vkCreateFence(VkDevice device, ref VkFenceCreateInfo pCreateInfo, IntPtr pAllocator, out VkFence pFence);
	private delegate void PFN_vkDestroyFence(VkDevice device, VkFence fence, IntPtr pAllocator);
	private delegate VkResult PFN_vkCreateCommandPool(VkDevice device, ref VkCommandPoolCreateInfo pCreateInfo, IntPtr pAllocator, out VkCommandPool pCommandPool);
	private delegate void PFN_vkDestroyCommandPool(VkDevice device, VkCommandPool pool, IntPtr pAllocator);
	private delegate VkResult PFN_vkAllocateCommandBuffers(VkDevice device, ref VkCommandBufferAllocateInfo pAllocateInfo, VkCommandBuffer* pCommandBuffers);
	private delegate void PFN_vkFreeCommandBuffers(VkDevice device, VkCommandPool commandPool, uint32_t commandBufferCount, VkCommandBuffer* pCommandBuffers);
	private delegate VkResult PFN_vkWaitForFences(VkDevice device, uint32_t fenceCount, VkFence* pFences, VkBool32 waitAll, uint64_t timeout);
	private delegate VkResult PFN_vkGetFenceStatus(VkDevice device, VkFence fence);
	private delegate VkResult PFN_vkBeginCommandBuffer(VkCommandBuffer commandBuffer, ref VkCommandBufferBeginInfo pBeginInfo);
	private delegate VkResult PFN_vkEndCommandBuffer(VkCommandBuffer commandBuffer);
	private delegate VkResult PFN_vkCreateSemaphore(VkDevice device, ref VkSemaphoreCreateInfo pCreateInfo, IntPtr pAllocator, out VkSemaphore pSemaphore);
	private delegate void PFN_vkDestroySemaphore(VkDevice device, VkSemaphore semaphore, IntPtr pAllocator);
	private delegate VkResult PFN_vkResetFences(VkDevice device, uint32_t fenceCount, VkFence* pFences);
	private delegate VkResult PFN_vkQueueSubmit(VkQueue queue, uint32_t submitCount, VkSubmitInfo* pSubmits, VkFence fence);
	private delegate VkResult PFN_vkCreateImage(VkDevice device, ref VkImageCreateInfo pCreateInfo, IntPtr pAllocator, out VkImage pImage);
	private delegate void PFN_vkDestroyImage(VkDevice device, VkImage image, IntPtr pAllocator);
	private delegate void PFN_vkGetImageMemoryRequirements(VkDevice device, VkImage image, out VkMemoryRequirements pMemoryRequirements);
	private delegate VkResult PFN_vkAllocateMemory(VkDevice device, ref VkMemoryAllocateInfo pAllocateInfo, IntPtr pAllocator, out VkDeviceMemory pMemory);
	private delegate void PFN_vkFreeMemory(VkDevice device, VkDeviceMemory memory, IntPtr pAllocator);
	private delegate VkResult PFN_vkBindImageMemory(VkDevice device, VkImage image, VkDeviceMemory memory, VkDeviceSize memoryOffset);
	private delegate VkResult PFN_vkCreateImageView(VkDevice device, ref VkImageViewCreateInfo pCreateInfo, IntPtr pAllocator, out VkImageView pView);
	private delegate void PFN_vkDestroyImageView(VkDevice device, VkImageView imageView, IntPtr pAllocator);
	private delegate void PFN_vkCmdPipelineBarrier(VkCommandBuffer commandBuffer, VkPipelineStageFlags srcStageMask, VkPipelineStageFlags dstStageMask, VkDependencyFlags dependencyFlags, uint32_t memoryBarrierCount, IntPtr pMemoryBarriers, uint32_t bufferMemoryBarrierCount, IntPtr pBufferMemoryBarriers, uint32_t imageMemoryBarrierCount, VkImageMemoryBarrier* pImageMemoryBarriers);
	private delegate VkResult PFN_vkCreateSwapchainKHR(VkDevice device, ref VkSwapchainCreateInfoKHR pCreateInfo, IntPtr pAllocator, out VkSwapchainKHR pSwapchain);
	private delegate void PFN_vkDestroySwapchainKHR(VkDevice device, VkSwapchainKHR swapchain, IntPtr pAllocator);
	private delegate VkResult PFN_vkGetSwapchainImagesKHR(VkDevice device, VkSwapchainKHR swapchain, ref uint32_t pSwapchainImageCount, VkImage* pSwapchainImages);
	private delegate VkResult PFN_vkDeviceWaitIdle(VkDevice device);
	private delegate VkResult PFN_vkQueueWaitIdle(VkQueue queue);
	private delegate VkResult PFN_vkAcquireNextImageKHR(VkDevice device, VkSwapchainKHR swapchain, uint64_t timeout, VkSemaphore semaphore, VkFence fence, out uint32_t pImageIndex);
	private delegate void PFN_vkCmdBlitImage(VkCommandBuffer commandBuffer, VkImage srcImage, VkImageLayout srcImageLayout, VkImage dstImage, VkImageLayout dstImageLayout, uint32_t regionCount, VkImageBlit* pRegions, VkFilter filter);
	private delegate VkResult PFN_vkQueuePresentKHR(VkQueue queue, ref VkPresentInfoKHR pPresentInfo);

	// Loaded delegates
	private readonly PFN_vkCreateFence _createFence;
	private readonly PFN_vkDestroyFence _destroyFence;
	private readonly PFN_vkCreateCommandPool _createCommandPool;
	private readonly PFN_vkDestroyCommandPool _destroyCommandPool;
	private readonly PFN_vkAllocateCommandBuffers _allocateCommandBuffers;
	private readonly PFN_vkFreeCommandBuffers _freeCommandBuffers;
	private readonly PFN_vkWaitForFences _waitForFences;
	private readonly PFN_vkGetFenceStatus _getFenceStatus;
	private readonly PFN_vkBeginCommandBuffer _beginCommandBuffer;
	private readonly PFN_vkEndCommandBuffer _endCommandBuffer;
	private readonly PFN_vkCreateSemaphore _createSemaphore;
	private readonly PFN_vkDestroySemaphore _destroySemaphore;
	private readonly PFN_vkResetFences _resetFences;
	private readonly PFN_vkQueueSubmit _queueSubmit;
	private readonly PFN_vkCreateImage _createImage;
	private readonly PFN_vkDestroyImage _destroyImage;
	private readonly PFN_vkGetImageMemoryRequirements _getImageMemoryRequirements;
	private readonly PFN_vkAllocateMemory _allocateMemory;
	private readonly PFN_vkFreeMemory _freeMemory;
	private readonly PFN_vkBindImageMemory _bindImageMemory;
	private readonly PFN_vkCreateImageView _createImageView;
	private readonly PFN_vkDestroyImageView _destroyImageView;
	private readonly PFN_vkCmdPipelineBarrier _cmdPipelineBarrier;
	private readonly PFN_vkCreateSwapchainKHR _createSwapchainKHR;
	private readonly PFN_vkDestroySwapchainKHR _destroySwapchainKHR;
	private readonly PFN_vkGetSwapchainImagesKHR _getSwapchainImagesKHR;
	private readonly PFN_vkDeviceWaitIdle _deviceWaitIdle;
	private readonly PFN_vkQueueWaitIdle _queueWaitIdle;
	private readonly PFN_vkAcquireNextImageKHR _acquireNextImageKHR;
	private readonly PFN_vkCmdBlitImage _cmdBlitImage;
	private readonly PFN_vkQueuePresentKHR _queuePresentKHR;

	public VulkanDeviceApi(IVulkanDevice device)
	{
		IntPtr GetAddr(string name)
		{
			var addr = device.Instance.GetDeviceProcAddress(device.Handle, name);
			if (addr != IntPtr.Zero)
				return addr;
			return device.Instance.GetInstanceProcAddress(device.Instance.Handle, name);
		}

		_createFence = LoadFunc<PFN_vkCreateFence>(GetAddr, "vkCreateFence")!;
		_destroyFence = LoadFunc<PFN_vkDestroyFence>(GetAddr, "vkDestroyFence")!;
		_createCommandPool = LoadFunc<PFN_vkCreateCommandPool>(GetAddr, "vkCreateCommandPool")!;
		_destroyCommandPool = LoadFunc<PFN_vkDestroyCommandPool>(GetAddr, "vkDestroyCommandPool")!;
		_allocateCommandBuffers = LoadFunc<PFN_vkAllocateCommandBuffers>(GetAddr, "vkAllocateCommandBuffers")!;
		_freeCommandBuffers = LoadFunc<PFN_vkFreeCommandBuffers>(GetAddr, "vkFreeCommandBuffers")!;
		_waitForFences = LoadFunc<PFN_vkWaitForFences>(GetAddr, "vkWaitForFences")!;
		_getFenceStatus = LoadFunc<PFN_vkGetFenceStatus>(GetAddr, "vkGetFenceStatus")!;
		_beginCommandBuffer = LoadFunc<PFN_vkBeginCommandBuffer>(GetAddr, "vkBeginCommandBuffer")!;
		_endCommandBuffer = LoadFunc<PFN_vkEndCommandBuffer>(GetAddr, "vkEndCommandBuffer")!;
		_createSemaphore = LoadFunc<PFN_vkCreateSemaphore>(GetAddr, "vkCreateSemaphore")!;
		_destroySemaphore = LoadFunc<PFN_vkDestroySemaphore>(GetAddr, "vkDestroySemaphore")!;
		_resetFences = LoadFunc<PFN_vkResetFences>(GetAddr, "vkResetFences")!;
		_queueSubmit = LoadFunc<PFN_vkQueueSubmit>(GetAddr, "vkQueueSubmit")!;
		_createImage = LoadFunc<PFN_vkCreateImage>(GetAddr, "vkCreateImage")!;
		_destroyImage = LoadFunc<PFN_vkDestroyImage>(GetAddr, "vkDestroyImage")!;
		_getImageMemoryRequirements = LoadFunc<PFN_vkGetImageMemoryRequirements>(GetAddr, "vkGetImageMemoryRequirements")!;
		_allocateMemory = LoadFunc<PFN_vkAllocateMemory>(GetAddr, "vkAllocateMemory")!;
		_freeMemory = LoadFunc<PFN_vkFreeMemory>(GetAddr, "vkFreeMemory")!;
		_bindImageMemory = LoadFunc<PFN_vkBindImageMemory>(GetAddr, "vkBindImageMemory")!;
		_createImageView = LoadFunc<PFN_vkCreateImageView>(GetAddr, "vkCreateImageView")!;
		_destroyImageView = LoadFunc<PFN_vkDestroyImageView>(GetAddr, "vkDestroyImageView")!;
		_cmdPipelineBarrier = LoadFunc<PFN_vkCmdPipelineBarrier>(GetAddr, "vkCmdPipelineBarrier")!;
		_createSwapchainKHR = LoadFunc<PFN_vkCreateSwapchainKHR>(GetAddr, "vkCreateSwapchainKHR")!;
		_destroySwapchainKHR = LoadFunc<PFN_vkDestroySwapchainKHR>(GetAddr, "vkDestroySwapchainKHR")!;
		_getSwapchainImagesKHR = LoadFunc<PFN_vkGetSwapchainImagesKHR>(GetAddr, "vkGetSwapchainImagesKHR")!;
		_deviceWaitIdle = LoadFunc<PFN_vkDeviceWaitIdle>(GetAddr, "vkDeviceWaitIdle")!;
		_queueWaitIdle = LoadFunc<PFN_vkQueueWaitIdle>(GetAddr, "vkQueueWaitIdle")!;
		_acquireNextImageKHR = LoadFunc<PFN_vkAcquireNextImageKHR>(GetAddr, "vkAcquireNextImageKHR")!;
		_cmdBlitImage = LoadFunc<PFN_vkCmdBlitImage>(GetAddr, "vkCmdBlitImage")!;
		_queuePresentKHR = LoadFunc<PFN_vkQueuePresentKHR>(GetAddr, "vkQueuePresentKHR")!;
	}

	// Public methods
	public VkResult CreateFence(VkDevice device, ref VkFenceCreateInfo pCreateInfo, IntPtr pAllocator, out VkFence pFence)
		=> _createFence(device, ref pCreateInfo, pAllocator, out pFence);

	public void DestroyFence(VkDevice device, VkFence fence, IntPtr pAllocator)
		=> _destroyFence(device, fence, pAllocator);

	public VkResult CreateCommandPool(VkDevice device, ref VkCommandPoolCreateInfo pCreateInfo, IntPtr pAllocator, out VkCommandPool pCommandPool)
		=> _createCommandPool(device, ref pCreateInfo, pAllocator, out pCommandPool);

	public void DestroyCommandPool(VkDevice device, VkCommandPool pool, IntPtr pAllocator)
		=> _destroyCommandPool(device, pool, pAllocator);

	public VkResult AllocateCommandBuffers(VkDevice device, ref VkCommandBufferAllocateInfo pAllocateInfo, VkCommandBuffer* pCommandBuffers)
		=> _allocateCommandBuffers(device, ref pAllocateInfo, pCommandBuffers);

	public void FreeCommandBuffers(VkDevice device, VkCommandPool commandPool, uint32_t commandBufferCount, VkCommandBuffer* pCommandBuffers)
		=> _freeCommandBuffers(device, commandPool, commandBufferCount, pCommandBuffers);

	public VkResult WaitForFences(VkDevice device, uint32_t fenceCount, VkFence* pFences, VkBool32 waitAll, uint64_t timeout)
		=> _waitForFences(device, fenceCount, pFences, waitAll, timeout);

	public VkResult GetFenceStatus(VkDevice device, VkFence fence)
		=> _getFenceStatus(device, fence);

	public VkResult BeginCommandBuffer(VkCommandBuffer commandBuffer, ref VkCommandBufferBeginInfo pBeginInfo)
		=> _beginCommandBuffer(commandBuffer, ref pBeginInfo);

	public VkResult EndCommandBuffer(VkCommandBuffer commandBuffer)
		=> _endCommandBuffer(commandBuffer);

	public VkResult CreateSemaphore(VkDevice device, ref VkSemaphoreCreateInfo pCreateInfo, IntPtr pAllocator, out VkSemaphore pSemaphore)
		=> _createSemaphore(device, ref pCreateInfo, pAllocator, out pSemaphore);

	public void DestroySemaphore(VkDevice device, VkSemaphore semaphore, IntPtr pAllocator)
		=> _destroySemaphore(device, semaphore, pAllocator);

	public VkResult ResetFences(VkDevice device, uint32_t fenceCount, VkFence* pFences)
		=> _resetFences(device, fenceCount, pFences);

	public VkResult QueueSubmit(VkQueue queue, uint32_t submitCount, VkSubmitInfo* pSubmits, VkFence fence)
		=> _queueSubmit(queue, submitCount, pSubmits, fence);

	public VkResult CreateImage(VkDevice device, ref VkImageCreateInfo pCreateInfo, IntPtr pAllocator, out VkImage pImage)
		=> _createImage(device, ref pCreateInfo, pAllocator, out pImage);

	public void DestroyImage(VkDevice device, VkImage image, IntPtr pAllocator)
		=> _destroyImage(device, image, pAllocator);

	public void GetImageMemoryRequirements(VkDevice device, VkImage image, out VkMemoryRequirements pMemoryRequirements)
		=> _getImageMemoryRequirements(device, image, out pMemoryRequirements);

	public VkResult AllocateMemory(VkDevice device, ref VkMemoryAllocateInfo pAllocateInfo, IntPtr pAllocator, out VkDeviceMemory pMemory)
		=> _allocateMemory(device, ref pAllocateInfo, pAllocator, out pMemory);

	public void FreeMemory(VkDevice device, VkDeviceMemory memory, IntPtr pAllocator)
		=> _freeMemory(device, memory, pAllocator);

	public VkResult BindImageMemory(VkDevice device, VkImage image, VkDeviceMemory memory, VkDeviceSize memoryOffset)
		=> _bindImageMemory(device, image, memory, memoryOffset);

	public VkResult CreateImageView(VkDevice device, ref VkImageViewCreateInfo pCreateInfo, IntPtr pAllocator, out VkImageView pView)
		=> _createImageView(device, ref pCreateInfo, pAllocator, out pView);

	public void DestroyImageView(VkDevice device, VkImageView imageView, IntPtr pAllocator)
		=> _destroyImageView(device, imageView, pAllocator);

	public void CmdPipelineBarrier(VkCommandBuffer commandBuffer, VkPipelineStageFlags srcStageMask, VkPipelineStageFlags dstStageMask, VkDependencyFlags dependencyFlags, uint32_t memoryBarrierCount, IntPtr pMemoryBarriers, uint32_t bufferMemoryBarrierCount, IntPtr pBufferMemoryBarriers, uint32_t imageMemoryBarrierCount, VkImageMemoryBarrier* pImageMemoryBarriers)
		=> _cmdPipelineBarrier(commandBuffer, srcStageMask, dstStageMask, dependencyFlags, memoryBarrierCount, pMemoryBarriers, bufferMemoryBarrierCount, pBufferMemoryBarriers, imageMemoryBarrierCount, pImageMemoryBarriers);

	public VkResult CreateSwapchainKHR(VkDevice device, ref VkSwapchainCreateInfoKHR pCreateInfo, IntPtr pAllocator, out VkSwapchainKHR pSwapchain)
		=> _createSwapchainKHR(device, ref pCreateInfo, pAllocator, out pSwapchain);

	public void DestroySwapchainKHR(VkDevice device, VkSwapchainKHR swapchain, IntPtr pAllocator)
		=> _destroySwapchainKHR(device, swapchain, pAllocator);

	public VkResult GetSwapchainImagesKHR(VkDevice device, VkSwapchainKHR swapchain, ref uint32_t pSwapchainImageCount, VkImage* pSwapchainImages)
		=> _getSwapchainImagesKHR(device, swapchain, ref pSwapchainImageCount, pSwapchainImages);

	public VkResult DeviceWaitIdle(VkDevice device)
		=> _deviceWaitIdle(device);

	public VkResult QueueWaitIdle(VkQueue queue)
		=> _queueWaitIdle(queue);

	public VkResult AcquireNextImageKHR(VkDevice device, VkSwapchainKHR swapchain, uint64_t timeout, VkSemaphore semaphore, VkFence fence, out uint32_t pImageIndex)
		=> _acquireNextImageKHR(device, swapchain, timeout, semaphore, fence, out pImageIndex);

	public void CmdBlitImage(VkCommandBuffer commandBuffer, VkImage srcImage, VkImageLayout srcImageLayout, VkImage dstImage, VkImageLayout dstImageLayout, uint32_t regionCount, VkImageBlit* pRegions, VkFilter filter)
		=> _cmdBlitImage(commandBuffer, srcImage, srcImageLayout, dstImage, dstImageLayout, regionCount, pRegions, filter);

	public VkResult QueuePresentKHR(VkQueue queue, ref VkPresentInfoKHR pPresentInfo)
		=> _queuePresentKHR(queue, ref pPresentInfo);
}
