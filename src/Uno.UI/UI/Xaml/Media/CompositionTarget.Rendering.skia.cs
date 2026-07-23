#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using SkiaSharp;
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
	internal static IRenderBackend RenderBackend { get; set; } = new SkiaRenderBackend();

	private static readonly long _start = Stopwatch.GetTimestamp();
	// We're using this table as a set with weakref keys. values are always null
	private static readonly ConditionalWeakTable<CompositionTarget, object> _targets = new();
	private static bool _isRenderingActive;

	// Enqueued from the UI thread, drained on the rendering thread during Draw.
	private readonly ConcurrentQueue<RenderJob> _renderJobs = new();

	static CompositionTarget()
	{
		// A closing window stops calling Draw; fail its pending render jobs so awaiters fall
		// back to software rendering instead of hanging.
		XamlRootMap.Unregistered += (_, xamlRoot) => xamlRoot.VisualTree.ContentRoot.CompositionTarget.FailPendingRenderJobs();
	}

	private readonly SkiaRenderHelper.FpsHelper _fpsHelper = new();
	private readonly Lock _frameGate = new();
	private readonly Lock _xamlRootBoundsGate = new();

	// Only read and set from the native rendering thread in OnNativePlatformFrameRequested
	private Size _lastCanvasSize = Size.Empty;
	private static SKPath? _lastNativeClipPath;
	private float _lastRasterizationScale = 1;
	private static SKPath? _lastScaledNativeClipPath;

	// only set on the UI thread and under _frameGate, only read under _frameGate
	private (IRenderData frame, SKPath nativeElementClipPath)? _lastRenderedFrame;
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
		var recording = RenderBackend.BeginFrame();
		var (path, nativeVisualsInZOrder) = SkiaRenderHelper.RecordFrame(
			recording,
			(float)bounds.Width,
			(float)bounds.Height,
			rootElement.Visual,
			FrameRenderingOptions.invertNativeElementClipPath);
		var frame = recording.Finish();
		var renderedFrame = (frame, path);
		var previousFrame = default((IRenderData frame, SKPath path)?);
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

	// Render jobs run on the render thread with the GPU context current; this is a Skia-specific concern
	// (GRContext GPU tasks). Non-Skia targets have no GRContext and simply run jobs with null.
	private static GRContext? GetGRContext(IRenderTarget? target) => (target as SkiaRenderTarget)?.Canvas.Context as GRContext;

	private SKPath Draw(IRenderTarget? target, Func<Size, IRenderTarget> resizeFunc)
	{
		this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: {nameof(Draw)}");

		// Run pending render jobs even when there's no frame to present. When the target
		// doesn't exist yet, jobs stay queued for the next pass (the one that will create it).
		if (target is not null && !_renderJobs.IsEmpty)
		{
			RunRenderJobs(GetGRContext(target));
		}

		(IRenderData frame, SKPath nativeElementClipPath)? lastRenderedFrameNullable;
		lock (_frameGate)
		{
			lastRenderedFrameNullable = _lastRenderedFrame;

			// Borrow frame temporarily
			_lastRenderedFrame = null;

			_fpsHelper.OnFramePresentRequested();
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
			if (target is null || _lastCanvasSize != xamlRootBounds || _lastRasterizationScale != rasterizationScale)
			{
				target = resizeFunc(new Size(Math.Round(xamlRootBounds.Width * rasterizationScale), Math.Round(xamlRootBounds.Height * rasterizationScale)));
				_lastCanvasSize = xamlRootBounds;
				_lastRasterizationScale = rasterizationScale;
				_lastScaledNativeClipPath = null;

				// Jobs that couldn't run at method entry because the target didn't exist yet.
				if (!_renderJobs.IsEmpty)
				{
					RunRenderJobs(GetGRContext(target));
				}
			}

			using var fpsHelperDisposable = _fpsHelper.BeginFrame();
			using (var present = RenderBackend.BeginPresent(target))
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

	/// <summary>
	/// Schedules <paramref name="render"/> to run during the next native render pass — on the
	/// rendering thread, with the GRContext current — and invalidates so that pass happens
	/// promptly. The task completes true once the action has run, or false when it couldn't be
	/// executed (software rendering, the window is shutting down, or the action threw); the
	/// caller should then fall back to rendering in software.
	/// </summary>
	internal Task<bool> TryExecuteOnNextRenderAsync(Action<GRContext> render)
	{
		NativeDispatcher.CheckThreadAccess();

		var job = new RenderJob(render);
		_renderJobs.Enqueue(job);

		if (ContentRoot.XamlRoot is { } xamlRoot && XamlRootMap.GetHostForRoot(xamlRoot) is { } host)
		{
			host.InvalidateRender();
		}
		else
		{
			// No host to render a pass; don't leave the awaiter hanging.
			FailPendingRenderJobs();
		}

		return job.Task;
	}

	private void RunRenderJobs(GRContext? context)
	{
		if (context is null)
		{
			// No GPU context (raster canvas): this target renders in software. Fail the jobs so
			// callers fall back to software rendering instead of waiting for a context that
			// never comes.
			FailPendingRenderJobs();
			return;
		}

		while (_renderJobs.TryDequeue(out var job))
		{
			job.Run(context);
		}
	}

	private void FailPendingRenderJobs()
	{
		while (_renderJobs.TryDequeue(out var job))
		{
			job.Fail();
		}
	}

	private sealed class RenderJob(Action<GRContext> render)
	{
		// RunContinuationsAsynchronously so completing a job never runs the awaiter's
		// continuation inline on the rendering thread, which would stall frame presentation.
		private readonly TaskCompletionSource<bool> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public Task<bool> Task => _tcs.Task;

		public void Run(GRContext context)
		{
			try
			{
				render(context);
				_tcs.TrySetResult(true);
			}
			catch (Exception e)
			{
				if (typeof(CompositionTarget).Log().IsEnabled(LogLevel.Error))
				{
					typeof(CompositionTarget).Log().Error("Render job failed.", e);
				}
				_tcs.TrySetResult(false);
			}
		}

		public void Fail() => _tcs.TrySetResult(false);
	}

	private void ReturnFrame((IRenderData frame, SKPath path) frame)
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
