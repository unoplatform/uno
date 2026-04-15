using System;
using Windows.Win32;
using Windows.Win32.Foundation;
using SkiaSharp;
using Uno.Foundation.Logging;
using Uno.UI.Runtime.Skia.Vulkan;
using Uno.UI.Runtime.Skia.Win32.Vulkan;

namespace Uno.UI.Runtime.Skia.Win32;

internal partial class Win32WindowWrapper
{
	/// <summary>
	/// Vulkan-backed IRenderer for Win32. Adapts VulkanContext to the
	/// StartPaint/EndPaint/UpdateSize/CopyPixels lifecycle used by Win32WindowWrapper.Rendering.cs.
	///
	/// The Win32 render loop calls:
	///   StartPaint → OnNativePlatformFrameRequested(canvas, resizeFunc) → CopyPixels → EndPaint
	///
	/// VulkanContext needs the device lock held for the entire frame. We acquire it in StartPaint
	/// and release in EndPaint. UpdateSize provides the Vulkan-backed SKSurface. CopyPixels
	/// flushes Skia, blits to swapchain, and presents.
	/// </summary>
	private class VulkanRenderer : IRenderer
	{
		private readonly HWND _hwnd;
		private VulkanContext _vulkanContext;
		private IDisposable? _deviceLock;
		private bool _skipPresent;

		private VulkanRenderer(HWND hwnd, VulkanContext vulkanContext)
		{
			_hwnd = hwnd;
			_vulkanContext = vulkanContext;
		}

		public static VulkanRenderer? TryCreateVulkanRenderer(HWND hwnd)
		{
			if (!Win32VulkanSurfaceFactory.IsVulkanAvailable())
			{
				typeof(VulkanRenderer).Log().Warn("Vulkan rendering not available: vulkan-1.dll not found. Falling back.");
				return null;
			}

			try
			{
				if (!PInvoke.GetClientRect(hwnd, out var clientRect))
				{
					typeof(VulkanRenderer).Log().Error($"GetClientRect failed: {Win32Helper.GetErrorMessage()}");
					return null;
				}

				var width = Math.Max(clientRect.Width, 1);
				var height = Math.Max(clientRect.Height, 1);

				var factory = new Win32VulkanSurfaceFactory();
				var context = new VulkanContext();
				context.Initialize(factory, hwnd.Value, width, height);

				var (deviceName, driverVersion) = context.GetDeviceInfo();
				typeof(VulkanRenderer).Log().Info($"Vulkan rendering initialized: {deviceName}, {driverVersion}");

				return new VulkanRenderer(hwnd, context);
			}
			catch (Exception ex)
			{
				typeof(VulkanRenderer).Log().Warn($"Vulkan rendering not available: {ex.Message}. Falling back.");
				return null;
			}
		}

		public void StartPaint()
		{
			// Acquire the Vulkan device lock for the duration of the frame.
			// This prevents concurrent Vulkan access between Skia and our presentation code.
			_deviceLock = _vulkanContext.Device?.Lock();
			_vulkanContext.GrContext?.ResetContext();
			_vulkanContext.EnsureCachedSurface();
		}

		public void EndPaint()
		{
			_deviceLock?.Dispose();
			_deviceLock = null;
		}

		public SKSurface UpdateSize(int width, int height)
		{
			_skipPresent = true;

			// The caller (Win32WindowWrapper.Render) disposes _surface before calling
			// UpdateSize. Since _surface IS our cached SKSurface, invalidate the
			// stale reference, resize the render image, and create a fresh surface.
			_vulkanContext.InvalidateCachedSurface();
			_vulkanContext.ResizeRenderImage(Math.Max(width, 1), Math.Max(height, 1));
			_vulkanContext.GrContext?.ResetContext();
			_vulkanContext.EnsureCachedSurface();

			if (_vulkanContext.CachedSkSurface == null)
			{
				typeof(VulkanRenderer).Log().Warn("Vulkan SKSurface creation failed, using software fallback for this frame");
				var info = new SKImageInfo(Math.Max(width, 1), Math.Max(height, 1), SKColorType.Rgba8888, SKAlphaType.Premul);
				return SKSurface.Create(info);
			}

			return _vulkanContext.CachedSkSurface;
		}

		public void CopyPixels(int width, int height)
		{
			if (_vulkanContext.CachedSkSurface == null || _vulkanContext.GrContext == null)
				return;

			// Flush Skia commands to the Vulkan intermediate image
			_vulkanContext.CachedSkSurface.Canvas.Flush();
			_vulkanContext.GrContext.Flush();

			if (!_skipPresent)
			{
				// Blit intermediate image to swapchain and present
				_vulkanContext.BlitAndPresent();
			}
			_skipPresent = false;
		}

		public bool IsSoftware() => false;

		public void Reinitialize()
		{
			try
			{
				if (!PInvoke.GetClientRect(_hwnd, out var clientRect))
					return;

				var width = Math.Max(clientRect.Width, 1);
				var height = Math.Max(clientRect.Height, 1);

				_vulkanContext.Dispose();
				_vulkanContext = new VulkanContext();
				var factory = new Win32VulkanSurfaceFactory();
				_vulkanContext.Initialize(factory, _hwnd.Value, width, height);
			}
			catch (Exception ex)
			{
				typeof(VulkanRenderer).Log().Error($"Vulkan reinitialize failed: {ex.Message}");
			}
		}

		public void Dispose()
		{
			_deviceLock?.Dispose();
			_deviceLock = null;
			_vulkanContext.Dispose();
		}
	}
}
