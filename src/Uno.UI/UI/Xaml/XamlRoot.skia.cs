#nullable enable

using System;
using Windows.Foundation;
using Uno.UI.Xaml.Core;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using Uno.UI.Dispatching;
using Uno.UI.Helpers;
using Uno.UI.Hosting;

namespace Microsoft.UI.Xaml;

partial class XamlRoot
{
	private readonly SkiaRenderHelper.FpsHelper _fpsHelper = new();
	// TODO: prevent this from being bottlenecked by the dispatcher queue, and read the native refresh rate instead of hardcoding it
	private readonly DispatcherTimer _renderTimer = new() { Interval = TimeSpan.FromMilliseconds(1 / 60.0) };
	private bool _renderQueued;
	private FocusManager? _focusManager;
	private (SKPicture frame, SKPath nativeElementClipPath, Size size)? _lastRenderedFrame;
	private Size _lastCanvasSize = Size.Empty;
	private readonly object _frameGate = new();
	internal (SKPicture frame, SKPath nativeElementClipPath, Size size)? LastRenderedFrame
	{
		get
		{
			lock (_frameGate)
			{
				return _lastRenderedFrame;
			}
		}
	}

	internal static (bool invertNativeElementClipPath, bool applyScalingToNativeElementClipPath) FrameRenderingOptions { get; set; } = (false, true);

	internal event Action? FramePainted;

	internal void InvalidateOverlays()
	{
		_focusManager ??= VisualTree.GetFocusManagerForElement(Content);
		_focusManager?.FocusRectManager?.RedrawFocusVisual();
		if (_focusManager?.FocusedElement is TextBox textBox)
		{
			textBox.TextBoxView?.Extension?.InvalidateLayout();
		}
	}

	private void PaintFrame()
	{
		NativeDispatcher.CheckThreadAccess();

		var rootElement = VisualTree.RootElement;
		// TODO
		// if (!SkiaRenderHelper.CanRecordPicture(rootElement))
		// {
		// 	// Try again next tick
		// 	return;
		// }

		lock (_frameGate)
		{
			var lastRenderedFrame = SkiaRenderHelper.RecordPictureAndReturnPath(
				(int)(Bounds.Width * RasterizationScale),
				(int)(Bounds.Height * RasterizationScale),
				rootElement,
				invertPath: FrameRenderingOptions.invertNativeElementClipPath,
				applyScaling: FrameRenderingOptions.applyScalingToNativeElementClipPath);
			_lastRenderedFrame = (lastRenderedFrame.Item1, lastRenderedFrame.Item2, Bounds.Size);
		}

		FramePainted?.Invoke();
		XamlRootMap.GetHostForRoot(this)!.InvalidateRender();
	}

	internal SKPath OnNativePlatformFrameRequested(SKCanvas? canvas, Func<Size, SKCanvas> resizeFunc)
	{
		CompositionTarget.InvokeRendering();

		if (LastRenderedFrame is not { } lastRenderedFrame)
		{
			return new SKPath();
		}
		else
		{
			if (canvas is null || _lastCanvasSize != lastRenderedFrame.size)
			{
				canvas = resizeFunc(lastRenderedFrame.size);
				_lastCanvasSize = lastRenderedFrame.size;
			}

			SkiaRenderHelper.RenderPicture(
				canvas,
				lastRenderedFrame.frame,
				SKColors.Transparent,
				_fpsHelper);

			return lastRenderedFrame.nativeElementClipPath;
		}
	}

	internal void RequestNewFrame() => _renderQueued = true;

	private void OnRenderTimerTick()
	{
		if (_renderQueued)
		{
			PaintFrame();
			_renderQueued = false;
		}
	}
}
