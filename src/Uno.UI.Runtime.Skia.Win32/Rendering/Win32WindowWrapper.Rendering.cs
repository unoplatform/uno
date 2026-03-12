using System;
using System.Drawing;
using System.Threading;
using Windows.Win32;
using Windows.Win32.Foundation;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;
using Uno.UI.Hosting;

namespace Uno.UI.Runtime.Skia.Win32;

internal partial class Win32WindowWrapper
{
	private int _renderCount;
	private SKSurface? _surface;
	private bool _rendering;
	private bool _blitScheduled;
	private bool _inSizeMove;

	public event EventHandler<SKPath>? RenderingNegativePathReevaluated; // not necessarily changed

	unsafe void IXamlRootHost.InvalidateRender()
	{
		// Mark the window dirty for WM_PAINT. This handles external repaints (window
		// uncovering, restore from minimized) where Windows generates WM_PAINT.
		// Like WPF (InvalidateRect in HwndTarget) and Avalonia (InvalidateRect in WindowImpl).
		PInvoke.InvalidateRect(_hwnd, default(RECT*), false);

		// During the modal resize/move loop, rendering is driven synchronously from
		// WM_SIZE. Don't schedule dispatcher-based blits — they would flood the modal
		// loop's message queue and starve resize messages (each FrameTick posts the
		// next via PostMessage, and GL's SwapBuffers blocks for VSync per blit).
		if (_inSizeMove)
		{
			return;
		}

		// Schedule a single coalesced blit at Render priority. Neither WPF nor Avalonia
		// rely on WM_PAINT for the primary blit — WM_PAINT is too low priority in the
		// Windows message queue (starved by PostMessage'd dispatcher items during
		// continuous animation). Like Avalonia's compositor-driven render loop, we
		// schedule the blit as part of the render pass via the dispatcher.
		if (!Interlocked.Exchange(ref _blitScheduled, true))
		{
			NativeDispatcher.Main.Enqueue(() =>
			{
				Interlocked.Exchange(ref _blitScheduled, false);
				Render();
			}, NativeDispatcherPriority.Render);
		}
	}

	private void ReinitializeRenderer()
	{
		_renderer.Reinitialize();
		_surface?.Dispose();
		_surface = null;
	}

	private void Render()
	{
		if (_rendererDisposed || _rendering)
		{
			return;
		}

		this.LogTrace()?.Trace($"Render {this._renderCount++}");

		_renderer.StartPaint();
		using var paintDisposable = new DisposableStruct<IRenderer>(static r => r.EndPaint(), _renderer);

		// In some cases, if a call to a synchronization method such as Monitor.Enter or Task.Wait()
		// happens inside Paint(), the dotnet runtime can itself call WndProc, which can lead to
		// Paint() becoming reentrant which can cause crashes.
		_rendering = true;
		try
		{
			var nativeElementClipPath = ((CompositionTarget)((IXamlRootHost)this).RootElement!.Visual.CompositionTarget!).OnNativePlatformFrameRequested(_surface?.Canvas, size =>
			{
				_surface?.Dispose();
				_surface = _renderer.UpdateSize((int)size.Width, (int)size.Height);
				return _surface.Canvas;
			});
			RenderingNegativePathReevaluated?.Invoke(this, nativeElementClipPath);
		}
		finally
		{
			_rendering = false;
		}

		if (!PInvoke.GetClientRect(_hwnd, out RECT clientRect))
		{
			this.LogError()?.Error($"{nameof(PInvoke.GetClientRect)} failed: {Win32Helper.GetErrorMessage()}");
			return;
		}
		// this may call WM_ERASEBKGND
		_renderer.CopyPixels(clientRect.Width, clientRect.Height);
	}
}
