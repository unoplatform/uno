using System;
using System.Drawing;
using System.Timers;
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
	private readonly Timer _renderTimer;
	private int _renderCount;
	private SKSurface? _surface;
	private bool _rendering;

	public event EventHandler<SKPath>? RenderingNegativePathReevaluated; // not necessarily changed

	private Timer CreateRenderTimer()
	{
		var timer = new Timer { AutoReset = false, Interval = TimeSpan.FromSeconds(1.0 / _refreshRate).TotalMilliseconds };
		timer.Elapsed += (_, _) => NativeDispatcher.Main.Enqueue(Render, NativeDispatcherPriority.High);
		return timer;
	}

	unsafe void IXamlRootHost.InvalidateRender()
	{
		var success = PInvoke.InvalidateRect(_hwnd, default(RECT*), true);
		if (!success) { this.LogError()?.Error($"{nameof(PInvoke.InvalidateRect)} failed: {Win32Helper.GetErrorMessage()}"); }
		_renderTimer.Enabled = true;
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
				_renderer.Reset();
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
