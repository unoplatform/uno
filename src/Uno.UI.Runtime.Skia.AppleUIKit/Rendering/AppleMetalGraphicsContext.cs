#nullable enable

using System;
using CoreAnimation;
using Metal;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.Runtime.Skia.AppleUIKit;

/// <summary>
/// Neutral Metal <see cref="ISwapChain"/> for AppleUIKit. The frame composes into a texture this context owns and
/// keeps, and <see cref="Present"/> blits that onto the view's drawable.
///
/// The drawable is deliberately acquired inside <see cref="Present"/> rather than before the frame: a CAMetalLayer
/// vends only two or three, so holding one across the frame's CPU work starves the pool and throttles rendering
/// (<c>[CAMetalLayer nextDrawable] returning nil because allocation failed</c>). Owning the target is also what lets
/// this report <see cref="PreservesContents"/>, so the compositor can repaint only the damaged region — the Vulkan
/// and software hosts do the same.
/// </summary>
internal sealed class AppleMetalGraphicsContext : ISwapChain, IMetalDeviceContext
{
	private readonly IMTLDevice _device;
	private readonly IMTLCommandQueue _queue;
	private readonly Func<ICAMetalDrawable?> _acquireDrawable;

	private IMTLTexture? _offscreen;
	private AppleMetalRenderTarget? _target;
	private int _width;
	private int _height;

	public AppleMetalGraphicsContext(IMTLDevice device, IMTLCommandQueue queue, Func<ICAMetalDrawable?> acquireDrawable)
	{
		_device = device;
		_queue = queue;
		_acquireDrawable = acquireDrawable;
	}

	public GraphicsContextKind Kind => GraphicsContextKind.Metal;

	public nint Device => _device.Handle;
	public nint Queue => _queue.Handle;

	/// <summary>The frame composes into a texture kept across frames, so last frame's pixels are still there.</summary>
	public bool PreservesContents => true;

	public IRenderTarget AcquireRenderTarget(int width, int height)
	{
		width = Math.Max(1, width);
		height = Math.Max(1, height);

		if (_offscreen is null || width != _width || height != _height)
		{
			_offscreen?.Dispose();

			var descriptor = MTLTextureDescriptor.CreateTexture2DDescriptor(MTLPixelFormat.BGRA8Unorm, (nuint)width, (nuint)height, false);
			// RenderTarget for Skia to draw into, ShaderRead so the blit can source it; Private keeps it GPU-only.
			descriptor.Usage = MTLTextureUsage.RenderTarget | MTLTextureUsage.ShaderRead;
			descriptor.StorageMode = MTLStorageMode.Private;

			_offscreen = _device.CreateTexture(descriptor);
			_width = width;
			_height = height;
			_target = _offscreen is null ? null : new AppleMetalRenderTarget(this, width, height);
		}

		return _target ?? throw new InvalidOperationException("Failed to allocate the Metal render texture.");
	}

	public void Present()
	{
		if (_offscreen is null)
		{
			return;
		}

		// Acquired here, not before the frame: the drawable is held only for the blit and present.
		var drawable = _acquireDrawable();
		if (drawable is null)
		{
			return;
		}

		try
		{
			var destination = drawable.Texture;
			// A mismatch means the layer resized under us; that frame's blit is skipped and the next acquire resizes.
			var copyWidth = (nuint)Math.Min(_width, (int)destination.Width);
			var copyHeight = (nuint)Math.Min(_height, (int)destination.Height);

			using var commandBuffer = _queue.CommandBuffer()!;
			using (var blit = commandBuffer.BlitCommandEncoder!)
			{
				blit.CopyFromTexture(
					_offscreen, 0, 0, new MTLOrigin(0, 0, 0), new MTLSize((nint)copyWidth, (nint)copyHeight, 1),
					destination, 0, 0, new MTLOrigin(0, 0, 0));
				blit.EndEncoding();
			}

			commandBuffer.PresentDrawable(drawable);
			commandBuffer.Commit();
		}
		finally
		{
			// Hand the drawable back immediately; the pool is small.
			drawable.Dispose();
		}
	}

	public void Dispose()
	{
		_offscreen?.Dispose();
		_offscreen = null;
		_target = null;
	}

	private sealed class AppleMetalRenderTarget(AppleMetalGraphicsContext context, int width, int height) : IMetalRenderTarget
	{
		public nint Texture => context._offscreen?.Handle ?? 0;
		public int Width => width;
		public int Height => height;
		public GraphicsColorFormat ColorFormat => GraphicsColorFormat.Bgra8888;
		public void Dispose() { }
	}
}
