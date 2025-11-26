#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Windows.Foundation;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using SkiaSharp;
using Uno.Foundation.Logging;
using Uno.UI.Composition;
using Uno.UI.Dispatching;
using Uno.UI.Helpers;
using Uno.UI.Hosting;

namespace Microsoft.UI.Xaml.Media;

public partial class CompositionTarget
{
	internal static (bool invertNativeElementClipPath, bool applyScalingToNativeElementClipPath) FrameRenderingOptions { get; set; } = (false, true);

	private static readonly long _start = Stopwatch.GetTimestamp();
	// We're using this table as a set with weakref keys. values are always null
	private static readonly ConditionalWeakTable<CompositionTarget, object> _targets = new();
	private static bool _isRenderingActive;

	private readonly SkiaRenderHelper.FpsHelper _fpsHelper = new();
	private readonly Lock _frameGate = new();
	private readonly Lock _xamlRootBoundsGate = new();

	// Only read and set from the native rendering thread in OnNativePlatformFrameRequested
	private Size _lastCanvasSize = Size.Empty;
	private static SKPath? _lastNativeClipPath;
	private float _lastRasterizationScale = 1;
	private static SKPath? _lastScaledNativeClipPath;

	// only set on the UI thread and under _frameGate, only read under _frameGate
	private (IntPtr frame, SKPath nativeElementClipPath)? _lastRenderedFrame;
	// only set and read under _xamlRootBoundsGate
	private Size _xamlRootBounds;
	// only set and read under _xamlRootBoundsGate
	private float _xamlRootRasterizationScale;
	// only set and read on the UI thread
	private List<Visual> _nativeVisualsInZOrder = new();

	internal event Action? FrameRendered;

	private static event EventHandler<object>? _rendering;

	public static event EventHandler<object>? Rendering
	{
		add
		{
			NativeDispatcher.CheckThreadAccess();
			_rendering += value;
			if (!_isRenderingActive)
			{
				_isRenderingActive = true;
				foreach (var (target, _) in _targets)
				{
					((ICompositionTarget)target).RequestNewFrame();
				}
			}
		}
		remove
		{
			NativeDispatcher.CheckThreadAccess();
			_rendering -= value;
			if (_rendering == null)
			{
				_isRenderingActive = false;
			}
		}
	}

	private void Render()
	{
		this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: {nameof(Render)} begins with timestamp {Stopwatch.GetTimestamp()}");

		NativeDispatcher.CheckThreadAccess();

		var rootElement = ContentRoot.VisualTree.RootElement;
		var bounds = ContentRoot.VisualTree.Size;

		var (picture, path, nativeVisualsInZOrder) = SkiaRenderHelper.RecordPictureAndReturnPath(
			(float)bounds.Width,
			(float)bounds.Height,
			rootElement.Visual,
			invertPath: FrameRenderingOptions.invertNativeElementClipPath);
		var renderedFrame = (picture, path);
		var previousFrame = default((IntPtr frame, SKPath path)?);
		lock (_frameGate)
		{
			previousFrame = _lastRenderedFrame;

			_lastRenderedFrame = renderedFrame;
		}

		// Delete previous SKPicture now since we are swapping it
		if (previousFrame != null)
		{
			UnoSkiaApi.sk_refcnt_safe_unref(previousFrame.Value.frame);
		}

		if (_isRenderingActive)
		{
			((ICompositionTarget)this).RequestNewFrame();
		}

		InvalidateNativeRender();

		var nativeVisualsZOrderChanged = _nativeVisualsInZOrder.Count != nativeVisualsInZOrder.Count;
		if (!nativeVisualsZOrderChanged)
		{
			for (int i = 0; i < nativeVisualsInZOrder.Count; i++)
			{
				if (nativeVisualsInZOrder[i] != _nativeVisualsInZOrder[i])
				{
					nativeVisualsZOrderChanged = true;
					break;
				}
			}
		}

		if (nativeVisualsZOrderChanged)
		{
			_nativeVisualsInZOrder = nativeVisualsInZOrder;
			ContentPresenter.OnNativeHostsRenderOrderChanged(nativeVisualsInZOrder);
		}

		FrameRendered?.Invoke();
		this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: {nameof(Render)} ends");
	}

	private SKPath Draw(SKCanvas? canvas, Func<Size, SKCanvas> resizeFunc)
	{
		this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: {nameof(Draw)}");

		(IntPtr frame, SKPath nativeElementClipPath)? lastRenderedFrameNullable;
		lock (_frameGate)
		{
			lastRenderedFrameNullable = _lastRenderedFrame;

			// Borrow frame temporarily
			_lastRenderedFrame = null;
		}

		if (lastRenderedFrameNullable is not { } lastRenderedFrame)
		{
			return new SKPath();
		}
		else
		{
			Size xamlRootBounds;
			float rasterizationScale;
			lock (_xamlRootBoundsGate)
			{
				xamlRootBounds = _xamlRootBounds;
				rasterizationScale = _xamlRootRasterizationScale;
			}
			if (xamlRootBounds.Width <= 0 || xamlRootBounds.Height <= 0)
			{
				ReturnFrame(lastRenderedFrame);

				// Besides being an optimization step, returning early here also avoids resizing
				// the canvas to 0x0 which may crash on some targets
				return lastRenderedFrame.nativeElementClipPath;
			}
			if (canvas is null || _lastCanvasSize != xamlRootBounds || _lastRasterizationScale != rasterizationScale)
			{
				canvas = resizeFunc(new Size(Math.Round(xamlRootBounds.Width * rasterizationScale), Math.Round(xamlRootBounds.Height * rasterizationScale)));
				_lastCanvasSize = xamlRootBounds;
				_lastRasterizationScale = rasterizationScale;
				_lastScaledNativeClipPath = null;
			}

			canvas.Save();
			if (rasterizationScale != 1)
			{
				canvas.Scale(rasterizationScale, rasterizationScale);
			}
			SkiaRenderHelper.RenderPicture(
				canvas,
				lastRenderedFrame.frame,
				SKColors.Transparent,
				_fpsHelper);
			canvas.Restore();

			ReturnFrame(lastRenderedFrame);

			InvokeRendering();

			if (FrameRenderingOptions.applyScalingToNativeElementClipPath && rasterizationScale != 1)
			{
				if (_lastNativeClipPath != lastRenderedFrame.nativeElementClipPath || _lastScaledNativeClipPath == null)
				{
					_lastScaledNativeClipPath = new();

					lastRenderedFrame
						.nativeElementClipPath
						.Transform(SKMatrix.CreateScale(rasterizationScale, rasterizationScale), _lastScaledNativeClipPath);

					_lastNativeClipPath = lastRenderedFrame.nativeElementClipPath;
				}

				return _lastScaledNativeClipPath;
			}

			return lastRenderedFrame.nativeElementClipPath;
		}
	}

	private void ReturnFrame((IntPtr picture, SKPath path) frame)
	{
		var pictureToDelete = IntPtr.Zero;

		lock (_frameGate)
		{
			// Put the frame back unless it has changed
			if (_lastRenderedFrame == null)
			{
				_lastRenderedFrame = frame;
			}
			else
			{
				pictureToDelete = frame.picture;
			}
		}

		// Delete it then
		if (pictureToDelete != IntPtr.Zero)
		{
			UnoSkiaApi.sk_refcnt_safe_unref(pictureToDelete);
		}
	}

	internal static void InvokeRendering()
	{
		if (NativeDispatcher.Main.HasThreadAccess)
		{
			_rendering?.Invoke(null, new RenderingEventArgs(Stopwatch.GetElapsedTime(_start)));
		}
		else
		{
			NativeDispatcher.Main.Enqueue(() =>
			{
				_rendering?.Invoke(null, new RenderingEventArgs(Stopwatch.GetElapsedTime(_start)));
			}, NativeDispatcherPriority.High);
		}
	}
}
