#nullable enable

using System;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.Runtime.Skia.AppleUIKit;

/// <summary>
/// A context that consumes the per-frame <c>MTLTexture</c> the MTKView supplies, rather than a swapchain-owning
/// context that sources its own drawable.
/// </summary>
internal interface IAppleNativeTextureSink
{
	/// <summary>Pushes the texture for the frame about to be acquired.</summary>
	void SetCurrentTexture(nint texture);
}

/// <summary>
/// Neutral native-texture Metal <see cref="ISwapChain"/> for AppleUIKit: holds the MTKView's device/queue and wraps the
/// per-frame drawable <c>MTLTexture</c> as an <see cref="IMetalRenderTarget"/>. The MTKView owns and presents the
/// drawable, so <see cref="Present"/> is a no-op.
/// </summary>
internal sealed class AppleMetalGraphicsContext : ISwapChain, IAppleNativeTextureSink, IMetalDeviceContext
{
	private readonly nint _device;
	private readonly nint _queue;
	private nint _currentTexture;
	private AppleMetalRenderTarget? _target;

	public AppleMetalGraphicsContext(nint device, nint queue)
	{
		_device = device;
		_queue = queue;
	}

	public GraphicsContextKind Kind => GraphicsContextKind.Metal;

	public nint Device => _device;
	public nint Queue => _queue;

	// Metal presents to the MTKView's per-frame drawable with no host-retained surface yet, so the drawable is
	// undefined each frame — the compositor repaints the whole frame.
	public bool PreservesContents => false;

	public void SetCurrentTexture(nint texture) => _currentTexture = texture;

	public IRenderTarget AcquireRenderTarget(int width, int height)
	{
		width = Math.Max(1, width);
		height = Math.Max(1, height);
		// Cache the target across frames while the size is unchanged; the wrapped drawable texture is read live
		// from the context, so a per-frame texture swap is reflected without reallocating the target.
		if (_target is null || _target.Width != width || _target.Height != height)
		{
			_target = new AppleMetalRenderTarget(this, width, height);
		}
		return _target;
	}

	// The MTKView owns the drawable and commits after its Draw returns.
	public void Present() { }

	public void Dispose() { }

	private sealed class AppleMetalRenderTarget(AppleMetalGraphicsContext context, int width, int height) : IMetalRenderTarget
	{
		public nint Texture => context._currentTexture;
		public int Width => width;
		public int Height => height;
		public GraphicsColorFormat ColorFormat => GraphicsColorFormat.Bgra8888;
		public void Dispose() { }
	}
}
