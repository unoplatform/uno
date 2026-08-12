#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using Uno.Foundation.Logging;
using Uno.UI.Composition;
using Uno.UI.Composition.Drawing;
using Uno.UI.Dispatching;
using Uno.UI.Helpers;
using Uno.UI.Hosting;

namespace Microsoft.UI.Xaml.Media;

public partial class CompositionTarget
{
	internal static (bool invertNativeElementClipPath, bool applyScalingToNativeElementClipPath) FrameRenderingOptions { get; set; } = (false, true);

	/// <summary>
	/// The active rendering backend that owns the frame record/present lifecycle. Defaults to the Skia
	/// two-phase backend; a host/experiment can replace it before the first frame.
	/// </summary>
	// Backend-agnostic: the framework doesn't reference a concrete renderer. A head that installs its own renderer
	// (e.g. WebGPU) sets this; otherwise it falls back to the registered backend's default (DrawingRegistration set
	// by SkiaBackend.Register). Accessing it before any backend is registered throws a diagnosable error.
	private static IRenderer? _renderer;
	internal static IRenderer Renderer
	{
		get => _renderer
			?? DrawingRegistration.DefaultRenderer
			?? throw new global::System.InvalidOperationException(
				"No IRenderer registered. The app entry must register a drawing backend (SkiaBackend.Register / ManagedBackend.Register) and/or the head must set CompositionTarget.Renderer before the first frame.");
		set => _renderer = value;
	}

	private static readonly long _start = Stopwatch.GetTimestamp();
	// We're using this table as a set with weakref keys. values are always null
	private static readonly ConditionalWeakTable<CompositionTarget, object> _targets = new();
	private static bool _isRenderingActive;

	// Enqueued from the UI thread, drained on the rendering thread during Draw.

	static CompositionTarget()
	{
		// A closing window stops calling Draw; fail its pending render jobs so awaiters fall
		// back to software rendering instead of hanging.
	}

	private readonly SkiaRenderHelper.FpsHelper _fpsHelper = new();
	private readonly Lock _frameGate = new();
	private readonly Lock _xamlRootBoundsGate = new();

	// Only read and set from the native rendering thread in OnNativePlatformFrameRequested
	private Size _lastCanvasSize = Size.Empty;
	private static IGeometry? _lastNativeClipPath;
	private float _lastRasterizationScale = 1;
	private static IGeometry? _lastScaledNativeClipPath;

	// only set on the UI thread and under _frameGate, only read under _frameGate
	private (IRenderData frame, IGeometry nativeElementClipPath)? _lastRenderedFrame;
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

		// Phase 1 (UI thread): the backend hands us a recording session, the agnostic cycle walks the
		// visual tree into it, then we finish recording to get the opaque frame.
		var recording = Renderer.BeginFrame();
		var (path, nativeVisualsInZOrder) = SkiaRenderHelper.RecordFrame(
			recording,
			(float)bounds.Width,
			(float)bounds.Height,
			rootElement.Visual,
			FrameRenderingOptions.invertNativeElementClipPath);
		var frame = recording.Finish();
		var renderedFrame = (frame, path);
		var previousFrame = default((IRenderData frame, IGeometry path)?);
		lock (_frameGate)
		{
			previousFrame = _lastRenderedFrame;

			_lastRenderedFrame = renderedFrame;
		}

		_fpsHelper.OnFrameRecorded();

		// Release the previous frame now since we are swapping it
		previousFrame?.frame.Dispose();

		if (_isRenderingActive)
		{
			((ICompositionTarget)this).RequestNewFrame();
		}

		if (rootElement.XamlRoot is not null)
		{
			XamlRootMap.GetHostForRoot(rootElement.XamlRoot)?.InvalidateRender();
		}

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

	private IGeometry Draw(IRenderTarget? target, Func<Size, IRenderTarget> resizeFunc)
	{
		this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: {nameof(Draw)}");

		(IRenderData frame, IGeometry nativeElementClipPath)? lastRenderedFrameNullable;
		lock (_frameGate)
		{
			lastRenderedFrameNullable = _lastRenderedFrame;

			// Borrow frame temporarily
			_lastRenderedFrame = null;

			_fpsHelper.OnFramePresentRequested();
		}

		if (lastRenderedFrameNullable is not { } lastRenderedFrame)
		{
			return SkiaRenderHelper.EmptyClipPath;
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
			if (target is null || _lastCanvasSize != xamlRootBounds || _lastRasterizationScale != rasterizationScale)
			{
				target = resizeFunc(new Size(Math.Round(xamlRootBounds.Width * rasterizationScale), Math.Round(xamlRootBounds.Height * rasterizationScale)));
				_lastCanvasSize = xamlRootBounds;
				_lastRasterizationScale = rasterizationScale;
				_lastScaledNativeClipPath = null;
			}

			using var fpsHelperDisposable = _fpsHelper.BeginFrame();
			using (var present = Renderer.BeginPresent(target))
			{
				// Scaling (DPI) is applied through the neutral session so it works for any backend.
				present.Save();
				if (rasterizationScale != 1)
				{
					present.Scale(rasterizationScale, rasterizationScale);
				}
				present.Clear(global::Windows.UI.Colors.Transparent);
				present.Replay(lastRenderedFrame.frame);
				_fpsHelper.DrawFps(present);
				present.Restore();
			}

			ReturnFrame(lastRenderedFrame);

			InvokeRendering();

			if (FrameRenderingOptions.applyScalingToNativeElementClipPath && rasterizationScale != 1)
			{
				if (_lastNativeClipPath != lastRenderedFrame.nativeElementClipPath || _lastScaledNativeClipPath == null)
				{
					_lastScaledNativeClipPath = lastRenderedFrame
						.nativeElementClipPath
						.Transform(Matrix3x2.CreateScale(rasterizationScale, rasterizationScale));

					_lastNativeClipPath = lastRenderedFrame.nativeElementClipPath;
				}

				return _lastScaledNativeClipPath;
			}

			return lastRenderedFrame.nativeElementClipPath;
		}
	}


	private void ReturnFrame((IRenderData frame, IGeometry path) frame)
	{
		IRenderData? frameToDelete = null;

		lock (_frameGate)
		{
			// Put the frame back unless it has changed
			if (_lastRenderedFrame == null)
			{
				_lastRenderedFrame = frame;
			}
			else
			{
				frameToDelete = frame.frame;
			}
		}

		// Release it then
		frameToDelete?.Dispose();
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
