#nullable enable

using System;
using System.Threading;
using Windows.Foundation;
using Uno.UI.Xaml.Core;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using Uno.UI.Dispatching;
using Uno.UI.Helpers;

namespace Microsoft.UI.Xaml;

partial class XamlRoot
{
	private readonly SkiaRenderHelper.FpsHelper _fpsHelper = new();
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

	internal event Action? RenderInvalidated;

	// For profiling purposes only. Do not depend on these events.
	internal event Action? FramePainted;
	internal event Action? FrameRendered;

	internal void InvalidateOverlays()
	{
		_focusManager ??= VisualTree.GetFocusManagerForElement(Content);
		_focusManager?.FocusRectManager?.RedrawFocusVisual();
		if (_focusManager?.FocusedElement is TextBox textBox)
		{
			textBox.TextBoxView?.Extension?.InvalidateLayout();
		}
	}

	internal void QueueInvalidateRender()
	{
		if (!_renderQueued)
		{
			_renderQueued = true;

			NativeDispatcher.Main.Enqueue(() =>
			{
				if (_renderQueued)
				{
					_renderQueued = false;

					InvalidateRender();
				}
			}, NativeDispatcherPriority.Idle); // Idle is necessary to avoid starving the Normal queue on some platforms (specifically skia/android), otherwise When_Child_Empty_List times out
		}
	}

	private void InvalidateRender()
	{
		NativeDispatcher.CheckThreadAccess();

		var rootElement = VisualTree.RootElement;
		if (!SkiaRenderHelper.CanRecordPicture(rootElement))
		{
			// Try again next tick
			QueueInvalidateRender();
			return;
		}

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

		RenderInvalidated?.Invoke();
	}

	internal SKPath OnNativePlatformFrameRequested(SKCanvas? canvas, Func<Size, SKCanvas> resizeFunc)
	{
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

	internal void InvokeFramePainted()
	{
		NativeDispatcher.CheckThreadAccess();
		FramePainted?.Invoke();
	}

	internal void InvokeFrameRendered()
	{
		if (NativeDispatcher.Main.HasThreadAccess)
		{
			FrameRendered?.Invoke();
		}
		else
		{
			NativeDispatcher.Main.Enqueue(() => FrameRendered?.Invoke(), NativeDispatcherPriority.High);
		}
	}
}
