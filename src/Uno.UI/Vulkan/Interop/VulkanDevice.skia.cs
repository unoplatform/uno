#nullable enable
// Based on the Avalonia project (MIT License, Copyright (c) AvaloniaUI OÜ).
// Original source: https://github.com/AvaloniaUI/Avalonia/tree/master/src/Avalonia.Vulkan
using System;
using System.Collections.Generic;
using System.Threading;
using Uno.UI.Runtime.Skia.Vulkan;
using Uno.UI.Runtime.Skia.Vulkan.UnmanagedInterop;

namespace Uno.UI.Runtime.Skia.Vulkan.Interop;

internal partial class VulkanDevice : IVulkanDevice, IDisposable
{
	private readonly VulkanInstanceApi _instanceApi;
	private VkDevice _handle;
	private readonly VkPhysicalDevice _physicalDeviceHandle;
	private readonly VkQueue _mainQueue;
	private readonly uint _graphicsQueueIndex;
	private readonly object _lock = new();
	private Thread? _lockedByThread;
	private int _lockCount;
	private readonly string[] _enabledExtensions;

	private VulkanDevice(VulkanInstanceApi instanceApi, VkDevice handle, VkPhysicalDevice physicalDeviceHandle,
		VkQueue mainQueue, uint graphicsQueueIndex, string[] enabledExtensions)
	{
		_instanceApi = instanceApi;
		_handle = handle;
		_physicalDeviceHandle = physicalDeviceHandle;
		_mainQueue = mainQueue;
		_graphicsQueueIndex = graphicsQueueIndex;
		_enabledExtensions = enabledExtensions;
		Instance = _instanceApi.Instance;
	}

	T CheckAccess<T>(T f)
	{
		if (_lockedByThread != Thread.CurrentThread)
			throw new InvalidOperationException("This class is only usable when locked");
		return f;
	}

	public IDisposable Lock()
	{
		Monitor.Enter(_lock);
		_lockCount++;
		_lockedByThread = Thread.CurrentThread;
		return new DeviceLockDisposable(this);
	}

	private void Unlock()
	{
		_lockCount--;
		if (_lockCount == 0)
			_lockedByThread = null;
		Monitor.Exit(_lock);
	}

	private sealed class DeviceLockDisposable : IDisposable
	{
		private VulkanDevice? _device;

		public DeviceLockDisposable(VulkanDevice device) => _device = device;

		public void Dispose()
		{
			_device?.Unlock();
			_device = null;
		}
	}

	public IReadOnlyList<string> EnabledExtensions => _enabledExtensions;

	public bool IsLost => false;
	public IntPtr Handle => _handle.Handle;
	public IntPtr PhysicalDeviceHandle => _physicalDeviceHandle.Handle;
	public IntPtr MainQueueHandle => CheckAccess(_mainQueue).Handle;
	public uint GraphicsQueueFamilyIndex => _graphicsQueueIndex;
	public IVulkanInstance Instance { get; }

	public void Dispose()
	{
		if (_handle.Handle != IntPtr.Zero)
		{
			_instanceApi.DestroyDevice(_handle, IntPtr.Zero);
			_handle = default;
		}
	}
}
