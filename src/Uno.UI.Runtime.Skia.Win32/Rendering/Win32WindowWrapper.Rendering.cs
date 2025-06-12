using System;
using System.Drawing;
using Windows.Win32;
using Windows.Win32.Foundation;
using SkiaSharp;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Helpers;

namespace Uno.UI.Runtime.Skia.Win32;

internal partial class Win32WindowWrapper
{
	private int _renderCount;

	private Size? _lastSize;
	private SKSurface? _surface;
	private readonly SkiaRenderHelper.FpsHelper _fpsHelper = new();

	public event EventHandler<SKPath>? RenderingNegativePathReevaluated; // not necessarily changed

	private void Paint()
	{
		if (_rendererDisposed)
		{
			return;
		}

		using var _ = _fpsHelper.BeginFrame();

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

		if (_surface is null || _lastSize != clientRect.Size)
		{
			_lastSize = clientRect.Size;
			_renderer.Reset();
			_surface?.Dispose();
			_surface = _renderer.UpdateSize(clientRect.Width, clientRect.Height);
		}

		var canvas = _surface!.Canvas;

		var count = canvas.Save();
		try
		{
			canvas.Clear(_background);
			var scale = XamlRoot!.RasterizationScale;
			canvas.Scale((float)scale);
			if (XamlRoot.VisualTree.RootElement.Visual is { } rootVisual)
			{
				var isSoftwareRenderer = rootVisual.Compositor.IsSoftwareRenderer;
				try
				{
					rootVisual.Compositor.IsSoftwareRenderer = _renderer.IsSoftware();
					var path = SkiaRenderHelper.RenderRootVisualAndReturnNegativePath(clientRect.Width, clientRect.Height, rootVisual, _surface.Canvas);
					_fpsHelper.DrawFps(canvas);
					RenderingNegativePathReevaluated?.Invoke(this, path);
				}
				finally
				{
					rootVisual.Compositor.IsSoftwareRenderer = isSoftwareRenderer;
				}
			}
		}
		finally
		{
			canvas.RestoreToCount(count);
		}

		_surface.Flush();
		// this may call WM_ERASEBKGND
		_renderer.CopyPixels(clientRect.Width, clientRect.Height);
	}
}
