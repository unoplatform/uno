using System;
using System.Drawing;
using Windows.Win32;
using Windows.Win32.Foundation;
using SkiaSharp;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Helpers;
using Uno.UI.Hosting;

namespace Uno.UI.Runtime.Skia.Win32;

internal partial class Win32WindowWrapper
{
	private int _renderCount;
	private SKSurface? _surface;
	private bool _rendering;

	public event EventHandler<SKPath>? RenderingNegativePathReevaluated; // not necessarily changed

	private void Paint()
	{
		if (_rendererDisposed || _rendering)
		{
			return;
		}

		this.LogTrace()?.Trace($"Render {this._renderCount++}");

		_renderer.StartPaint();
		using var paintDisposable = new DisposableStruct<IRenderer>(static r => r.EndPaint(), _renderer);

		if (!PInvoke.GetClientRect(_hwnd, out RECT clientRect))
		{
			this.LogError()?.Error($"{nameof(PInvoke.GetClientRect)} failed: {Win32Helper.GetErrorMessage()}");
			return;
		}

		if (clientRect.IsEmpty)
		{
			return;
		}

		// In some cases, if a call to a synchronization method such as Monitor.Enter or Task.Wait()
		// happens inside Paint(), the dotnet runtime can itself call WndProc, which can lead to
		// Paint() becoming reentrant which can cause crashes.
		_rendering = true;
		try
		{
			var nativeElementClipPath = XamlRoot!.OnNativePlatformFrameRequested(_surface?.Canvas, size =>
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


		// this may call WM_ERASEBKGND
		_renderer.CopyPixels(clientRect.Width, clientRect.Height);
	}
}
