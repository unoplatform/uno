#nullable enable

using System;
using System.Threading;
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
	private bool _renderQueued;
	private FocusManager? _focusManager;
	private (SKPicture frame, SKPath nativeElementClipPath)? _lastRenderedFrame;
	private object _frameGate = new();
	internal (SKPicture frame, SKPath nativeElementClipPath)? LastRenderedFrame
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

	internal void InvalidateRender()
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
			_lastRenderedFrame = SkiaRenderHelper.RecordPictureAndReturnPath(
				(int)(Bounds.Width * RasterizationScale),
				(int)(Bounds.Height * RasterizationScale),
				rootElement,
				invertPath: FrameRenderingOptions.invertNativeElementClipPath,
				applyScaling: FrameRenderingOptions.applyScalingToNativeElementClipPath);
		}

		RenderInvalidated?.Invoke();
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
