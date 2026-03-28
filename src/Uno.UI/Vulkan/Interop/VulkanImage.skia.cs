#nullable enable
// Based on the Avalonia project (MIT License, Copyright (c) AvaloniaUI OÜ).
// Original source: https://github.com/AvaloniaUI/Avalonia/tree/master/src/Avalonia.Vulkan
using System;
using SkiaSharp;
using Uno.UI.Runtime.Skia.Vulkan;
using Uno.UI.Runtime.Skia.Vulkan.UnmanagedInterop;

namespace Uno.UI.Runtime.Skia.Vulkan.Interop;

internal class VulkanImageBase : IDisposable
{
	private IVulkanPlatformGraphicsContext _context;

	private VkAccessFlags _currentAccessFlags;
	public VkImageUsageFlags UsageFlags { get; }
	private VkImageView _imageView;
	private VkDeviceMemory _imageMemory;
	private VkImage _handle;
	private VulkanCommandBufferPool _commandBufferPool;
	internal VkImage Handle => _handle;
	internal VkFormat Format { get; }
	internal VkImageAspectFlags AspectFlags { get; private set; }
	public uint MipLevels { get; private set; }
	public SKSizeI Size { get; }
	public ulong MemorySize { get; private set; }
	public VkImageLayout CurrentLayout { get; protected set; }
	public VkDeviceMemory MemoryHandle => _imageMemory;

	public VkImageTiling Tiling => VkImageTiling.VK_IMAGE_TILING_OPTIMAL;

	public VulkanImageInfo ImageInfo => new()
	{
		Handle = Handle.Handle,
		PixelSize = Size,
		Format = (uint)Format,
		MemoryHandle = MemoryHandle.Handle,
		MemorySize = MemorySize,
		ViewHandle = _imageView.Handle,
		UsageFlags = (uint)UsageFlags,
		Layout = (uint)CurrentLayout,
		Tiling = (uint)Tiling,
		LevelCount = MipLevels,
		SampleCount = 1,
		IsProtected = false
	};

	public struct MemoryImportInfo
	{
		public IntPtr Next = IntPtr.Zero;
		public ulong MemorySize = 0;
		public ulong MemoryOffset = 0;

		public MemoryImportInfo() { }
	}

	public VulkanImageBase(IVulkanPlatformGraphicsContext context,
		VulkanCommandBufferPool commandBufferPool,
		VkFormat format, SKSizeI size, uint mipLevels = 0)
	{
		Format = format;
		Size = size;
		MipLevels = mipLevels;
		_context = context;
		_commandBufferPool = commandBufferPool;
		UsageFlags = VkImageUsageFlags.VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT
		                   | VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_DST_BIT
		                   | VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_SRC_BIT
		                   | VkImageUsageFlags.VK_IMAGE_USAGE_SAMPLED_BIT;
	}

	protected virtual VkDeviceMemory CreateMemory(VkImage image, ulong size, uint memoryTypeBits)
	{
		var memoryAllocateInfo = new VkMemoryAllocateInfo
		{
			sType = VkStructureType.VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO,
			allocationSize = size,
			memoryTypeIndex = (uint)VulkanMemoryHelper.FindSuitableMemoryTypeIndex(_context,
				memoryTypeBits,
				VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT),
		};

		_context.DeviceApi.AllocateMemory(_context.DeviceHandle, ref memoryAllocateInfo, IntPtr.Zero,
			out var imageMemory).ThrowOnError("vkAllocateMemory");
		return imageMemory;
	}

	public unsafe void Initialize(void* pNext)
	{
		if (Handle.Handle != 0)
			return;
		MipLevels = MipLevels != 0 ? MipLevels : (uint)Math.Floor(Math.Log(Math.Max(Size.Width, Size.Height), 2));
		var createInfo = new VkImageCreateInfo
		{
			pNext = pNext,
			sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_CREATE_INFO,
			imageType = VkImageType.VK_IMAGE_TYPE_2D,
			format = Format,
			extent = new VkExtent3D
			{
				depth = 1,
				width = (uint)Size.Width,
				height = (uint)Size.Height
			},
			mipLevels = MipLevels,
			arrayLayers = 1,
			samples = (VkSampleCountFlags)0x00000001, // VK_SAMPLE_COUNT_1_BIT
			tiling = Tiling,
			usage = UsageFlags,
			sharingMode = VkSharingMode.VK_SHARING_MODE_EXCLUSIVE,
			initialLayout = VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED,
			flags = VkImageCreateFlags.VK_IMAGE_CREATE_MUTABLE_FORMAT_BIT
		};

		_context.DeviceApi.CreateImage(_context.DeviceHandle, ref createInfo, IntPtr.Zero, out _handle)
			.ThrowOnError("vkCreateImage");

		_context.DeviceApi.GetImageMemoryRequirements(_context.DeviceHandle, _handle, out var memoryRequirements);
		try
		{
			_imageMemory = CreateMemory(_handle, memoryRequirements.size, memoryRequirements.memoryTypeBits);
		}
		catch
		{
			_context.DeviceApi.DestroyImage(_context.DeviceHandle, _handle, IntPtr.Zero);
			throw;
		}

		_context.DeviceApi.BindImageMemory(_context.DeviceHandle, _handle, _imageMemory, 0)
			.ThrowOnError("vkBindImageMemory");

		MemorySize = memoryRequirements.size;
		AspectFlags = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT;

		var imageViewCreateInfo = new VkImageViewCreateInfo
		{
			sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_VIEW_CREATE_INFO,
			components = new(),
			subresourceRange = new()
			{
				aspectMask = AspectFlags,
				levelCount = MipLevels,
				layerCount = 1
			},
			format = Format,
			image = _handle,
			viewType = VkImageViewType.VK_IMAGE_VIEW_TYPE_2D
		};
		_context.DeviceApi.CreateImageView(_context.DeviceHandle, ref imageViewCreateInfo,
			IntPtr.Zero, out _imageView).ThrowOnError("vkCreateImageView");
		CurrentLayout = VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED;
	}

	internal void TransitionLayout(VkImageLayout destinationLayout, VkAccessFlags destinationAccessFlags)
	{
		var commandBuffer = _commandBufferPool!.CreateCommandBuffer();
		commandBuffer.BeginRecording();
		VulkanMemoryHelper.TransitionLayout(_context, commandBuffer, Handle,
			CurrentLayout, _currentAccessFlags, destinationLayout, destinationAccessFlags,
			MipLevels);
		commandBuffer.EndRecording();
		commandBuffer.Submit();
		CurrentLayout = destinationLayout;
		_currentAccessFlags = destinationAccessFlags;
	}

	public void TransitionLayout(uint destinationLayout, uint destinationAccessFlags)
	{
		TransitionLayout((VkImageLayout)destinationLayout, (VkAccessFlags)destinationAccessFlags);
	}

	public void Dispose()
	{
		var api = _context.DeviceApi;
		var d = _context.DeviceHandle;
		if (_imageView.Handle != 0)
		{
			api.DestroyImageView(d, _imageView, IntPtr.Zero);
			_imageView = default;
		}
		if (_handle.Handle != 0)
		{
			api.DestroyImage(d, _handle, IntPtr.Zero);
			_handle = default;
		}
		if (_imageMemory.Handle != 0)
		{
			api.FreeMemory(d, _imageMemory, IntPtr.Zero);
			_imageMemory = default;
		}
	}
}

internal unsafe class VulkanImage : VulkanImageBase
{
	public VulkanImage(IVulkanPlatformGraphicsContext context, VulkanCommandBufferPool commandBufferPool,
		VkFormat format, SKSizeI size, uint mipLevels = 0) : base(context, commandBufferPool, format, size, mipLevels)
	{
		Initialize(null);
		TransitionLayout(VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL, VkAccessFlags.VK_ACCESS_NONE_KHR);
	}
}
