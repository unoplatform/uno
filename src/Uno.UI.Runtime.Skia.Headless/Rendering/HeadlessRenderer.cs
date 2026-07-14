#nullable enable

using System;
using System.Threading;
using SkiaSharp;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.Hosting;
using Uno.WinUI.Runtime.Skia.Headless.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Media;

namespace Uno.UI.Runtime.Skia.Headless;

/// <summary>
/// Drives the Skia two-phase render cycle for the headless host. The cycle always runs so the
/// app lifecycle behaves like a real target, but rasterization is optional: when the caller
/// supplies a target buffer, frames are drawn straight into it (zero-copy) and the
/// <c>onFrameRendered</c> callback is invoked; otherwise the visual-tree paint walk is skipped
/// entirely (via <see cref="FeatureConfiguration.Rendering.SkipVisualTreePainting"/>) so the
/// pipeline stays live without producing pixels.
/// </summary>
internal sealed class HeadlessRenderer : IDisposable
{
	private readonly IXamlRootHost _host;

	private readonly bool _hasBuffer;
	private readonly IntPtr _buffer;
	private readonly int _rowBytes;
	private readonly SKColorType _colorType;
	private readonly Action? _onFrameRendered;

	private readonly AutoResetEvent _renderInvalidationEvent = new(false);
	private readonly Thread _renderThread;
	private volatile bool _disposed;

	private SKSurface? _surface;
	private bool _surfaceTargetsBuffer;

	public HeadlessRenderer(IXamlRootHost host, IntPtr buffer, int rowBytes, SKColorType colorType, Action? onFrameRendered)
	{
		_host = host;
		_hasBuffer = buffer != IntPtr.Zero && onFrameRendered is not null;
		_buffer = buffer;
		_rowBytes = rowBytes;
		_colorType = colorType;
		_onFrameRendered = onFrameRendered;

		// With no target buffer the visual output is of no interest, so skip the paint walk entirely
		// (frame scheduling, rendering events and composition animations keep running). Only enabled
		// here to avoid overriding an explicit opt-out; the buffer path leaves the flag untouched.
		if (!_hasBuffer)
		{
			FeatureConfiguration.Rendering.SkipVisualTreePainting = true;
		}

		// Publish the configured size/scale so layout and DisplayInformation resolve correctly.
		HeadlessWindowWrapper.Instance.ApplySize();

		_renderThread = new Thread(_ =>
		{
			while (!_disposed)
			{
				try
				{
					_renderInvalidationEvent.WaitOne();
					if (_disposed)
					{
						break;
					}
					Render();
				}
				catch (Exception ex)
				{
					this.LogError()?.Error("Error during headless rendering", ex);
				}
			}
		})
		{
			IsBackground = true,
			Name = "Headless rendering thread"
		};
		_renderThread.Start();
	}

	public void InvalidateRender() => _renderInvalidationEvent.Set();

	private void Render()
	{
		// The visual tree may not be available yet on the first invalidation(s); the cycle will
		// be driven again once content is loaded.
		if (_host.RootElement?.Visual.CompositionTarget is not CompositionTarget ct)
		{
			return;
		}

		ct.OnNativePlatformFrameRequested(_surface?.Canvas, size =>
		{
			_surface?.Dispose();
			_surface = CreateSurface((int)size.Width, (int)size.Height);
			return _surface.Canvas;
		});

		_surface?.Flush();

		if (_surfaceTargetsBuffer && _surface is not null)
		{
			_onFrameRendered!.Invoke();
		}
	}

	private SKSurface CreateSurface(int width, int height)
	{
		if (_hasBuffer)
		{
			var wrapper = HeadlessWindowWrapper.Instance;
			if (width == wrapper.RawWidth && height == wrapper.RawHeight)
			{
				_surfaceTargetsBuffer = true;
				return SKSurface.Create(new SKImageInfo(width, height, _colorType, SKAlphaType.Premul), _buffer, _rowBytes);
			}

			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning(
					$"Requested render size {width}x{height} does not match the configured headless buffer size " +
					$"{wrapper.RawWidth}x{wrapper.RawHeight}; drawing nothing for this frame.");
			}
		}

		_surfaceTargetsBuffer = false;
		return SKSurface.CreateNull(width, height);
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;

		// Wake the render thread so it can observe _disposed and exit before we dispose the surface.
		_renderInvalidationEvent.Set();
		try
		{
			_renderThread.Join(TimeSpan.FromSeconds(1));
		}
		catch (Exception e)
		{
			this.LogDebug()?.Debug($"Failed to join the headless rendering thread on exit: {e.Message}");
		}

		_surface?.Dispose();
		_renderInvalidationEvent.Dispose();
	}
}
