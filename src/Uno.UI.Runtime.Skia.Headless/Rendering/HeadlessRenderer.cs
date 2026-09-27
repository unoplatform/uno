#nullable enable

using System;
using System.Threading;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Media;

namespace Uno.UI.Runtime.Skia.Headless;

/// <summary>
/// Keeps the Skia two-phase render cycle ticking for a single headless window on a dedicated thread, so
/// the app lifecycle, composition animations and <c>RenderTargetBitmap</c> behave like a real target.
/// The window itself produces no pixel output; the paint walk is skipped globally
/// (<c>FeatureConfiguration.Rendering.SkipVisualTreePainting</c>) and frames are drawn to a null surface.
/// </summary>
internal sealed class HeadlessRenderer : IDisposable
{
	private readonly IXamlRootHost _host;
	private readonly AutoResetEvent _renderInvalidationEvent = new(false);
	private readonly Thread _renderThread;
	private volatile bool _disposed;

	private readonly Uno.UI.Composition.Drawing.ISwapChain _swapChain;
	private readonly Uno.UI.Composition.Drawing.IDrawingFactory _rendererFactory;

	public HeadlessRenderer(IXamlRootHost host, Uno.UI.Composition.Drawing.ISwapChain swapChain, Uno.UI.Composition.Drawing.IDrawingFactory rendererFactory)
	{
		_host = host;
		_swapChain = swapChain;
		_rendererFactory = rendererFactory;

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

	/// <summary>Ticks the render cycle (draws nothing, keeping scheduling/animations/RenderTargetBitmap alive).</summary>
	public void Invalidate()
	{
		// A late invalidation can arrive after disposal (the event is disposed once the thread stops).
		if (_disposed)
		{
			return;
		}

		_renderInvalidationEvent.Set();
	}

	private void Render()
	{
		// The visual tree may not be available yet on the first invalidation(s). Don't drop the request:
		// the framework latches RequestNewFrame until the next frame runs, so a dropped invalidation is a
		// lost wakeup that stalls layout forever (this host has no unconditional native frame pump to heal it).
		CompositionTarget? ct;
		while ((ct = _host.RootElement?.Visual.CompositionTarget as CompositionTarget) is null)
		{
			if (_disposed)
			{
				return;
			}

			Thread.Sleep(15);
		}

		ct.Renderer = _rendererFactory;
		ct.OnNativePlatformFrameRequested(_swapChain);
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;

		// Wake the render thread so it can observe _disposed and exit before we dispose shared resources.
		_renderInvalidationEvent.Set();
		var stopped = _renderThread.Join(TimeSpan.FromSeconds(1));

		if (stopped)
		{
			_swapChain.Dispose();
			_renderInvalidationEvent.Dispose();
		}
		else
		{
			// The thread may still be mid-render; leak its buffer/event rather than risk a use-after-dispose race.
			this.LogWarn()?.Warn("The headless rendering thread did not stop within the timeout; its buffer and event are left undisposed to avoid a race.");
		}
	}
}
