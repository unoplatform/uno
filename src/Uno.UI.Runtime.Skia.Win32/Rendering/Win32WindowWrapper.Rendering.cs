using System;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.Hosting;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace Uno.UI.Runtime.Skia.Win32;

internal partial class Win32WindowWrapper
{
	private readonly FramePacer _framePacer;
	private int _renderCount;
	private SKSurface? _surface;
	private bool _rendering;

	public event EventHandler<SKPath>? RenderingNegativePathReevaluated; // not necessarily changed

	private FramePacer CreateFramePacer()
	{
		return new FramePacer(
			_refreshRate,
			() => NativeDispatcher.Main.Enqueue(Render, NativeDispatcherPriority.High));
	}

	unsafe void IXamlRootHost.InvalidateRender()
	{
		var success = PInvoke.InvalidateRect(_hwnd, default(RECT*), true);
		if (!success) { this.LogError()?.Error($"{nameof(PInvoke.InvalidateRect)} failed: {Win32Helper.GetErrorMessage()}"); }

		_framePacer.RequestFrame();
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

		_framePacer.OnFrameStart();

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
