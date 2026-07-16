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

	// Enqueued from the UI thread, drained on the rendering thread during Draw.
	private readonly ConcurrentQueue<RenderJob> _renderJobs = new();

	static CompositionTarget()
	{
		XamlRootMap.Unregistered += (_, xamlRoot) =>
		{
			var target = xamlRoot.VisualTree.ContentRoot.CompositionTarget;
			// A closing window stops calling Draw; fail its pending render jobs so awaiters fall
			// back to software rendering instead of hanging.
			target.FailPendingRenderJobs();
			OnTargetUnregistered(target);
		};
	}

	// Latest recorded frame per live target. Only touched on the UI thread. Entries are retained
	// until replaced by a newer frame or until the target unregisters, so a Rendering raise can
	// resend the last picture of a target that hasn't re-rendered since (e.g. a window minimized
	// on hosts that stop servicing invalidations, like macOS).
	private static readonly Dictionary<CompositionTarget, FramePicture> _latestFrames = new();

	// Only touched on the UI thread.
	private static bool _renderingRaiseScheduled;

	private readonly SkiaRenderHelper.FpsHelper _fpsHelper = new();
	private readonly Lock _frameGate = new();
	private readonly Lock _xamlRootBoundsGate = new();

	// Only read and set from the native rendering thread in OnNativePlatformFrameRequested
	private Size _lastCanvasSize = Size.Empty;
	private static SKPath? _lastNativeClipPath;
	private float _lastRasterizationScale = 1;
	private static SKPath? _lastScaledNativeClipPath;

	private readonly SKPath _pendingDamage = new();

	// Recycled per-frame damage snapshot paths. At most a couple of frames are ever in flight, so reusing
	// their SKPaths avoids allocating (and finalizing) a native path every frame. Only touched under
	// _frameGate, like the frame slot itself.
	private readonly Stack<SKPath> _damageSnapshotPool = new();

	// only set on the UI thread and under _frameGate, only read under _frameGate
	private (FramePicture picture, SKPath nativeElementClipPath, SKPath damage)? _lastRenderedFrame;
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
			invertPath: FrameRenderingOptions.invertNativeElementClipPath,
			damage: _pendingDamage);

		if (_fpsHelper.TryGetDamageBounds(out var fpsBounds))
		{
			_pendingDamage.UnionRect(fpsBounds);
		}

		var framePicture = new FramePicture(picture);
		var frameRect = new SKRect(0, 0, (float)bounds.Width, (float)bounds.Height);
		var previousFrame = default((FramePicture picture, SKPath path, SKPath damage)?);
		SKPath damageSnapshot;
		lock (_frameGate)
		{
			previousFrame = _lastRenderedFrame;

			if (previousFrame is { damage: var carried } && !carried.IsEmpty)
			{
				_pendingDamage.Union(carried);
			}

			_pendingDamage.ClampTo(frameRect);

			damageSnapshot = _damageSnapshotPool.Count > 0 ? _damageSnapshotPool.Pop() : new SKPath();
			_pendingDamage.Transform(SKMatrix.Identity, damageSnapshot);
			_pendingDamage.Reset();

			_lastRenderedFrame = (framePicture, path, damageSnapshot);

			// The previous frame is being superseded in place (it wasn't borrowed for present, since the
			// slot was non-null); its snapshot is no longer referenced, so recycle it for the next frame.
			if (previousFrame is { damage: var superseded })
			{
				_damageSnapshotPool.Push(superseded);
			}
		}

		_fpsHelper.OnFrameRecorded();

		if (previousFrame is { } prev)
		{
			prev.picture.OnPipelineReleased();
		}

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

		OnFramePictureRecorded(this, framePicture);

		this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: {nameof(Render)} ends");
	}

	void ICompositionTarget.AddDamage(SKRect bounds)
	{
		NativeDispatcher.CheckThreadAccess();
		_pendingDamage.UnionRect(bounds);
	}

	void ICompositionTarget.AddDamage(SKPath region)
	{
		NativeDispatcher.CheckThreadAccess();
		_pendingDamage.Union(region);
	}

	private static readonly SKPaint _damageOverlayFill = new() { Color = new SKColor(0xFF, 0x00, 0x00, 0x30), Style = SKPaintStyle.Fill };
	private static readonly SKPaint _damageOverlayStroke = new() { Color = new SKColor(0xFF, 0x00, 0x00, 0xB0), Style = SKPaintStyle.Stroke, StrokeWidth = 1 };

	private static void DrawDamageRegionOverlay(SKCanvas canvas, SKPath damage)
	{
		canvas.DrawPath(damage, _damageOverlayFill);
		canvas.DrawPath(damage, _damageOverlayStroke);
	}

	private SKPath Draw(SKCanvas? canvas, Func<Size, SKCanvas> resizeFunc)
	{
		this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: {nameof(Draw)}");

		// Run pending render jobs even when there's no frame to present. When the canvas
		// doesn't exist yet, jobs stay queued for the next pass (the one that will create it).
		if (canvas is not null && !_renderJobs.IsEmpty)
		{
			RunRenderJobs(canvas.Context as GRContext);
		}

		(FramePicture picture, SKPath nativeElementClipPath, SKPath damage)? lastRenderedFrameNullable;
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
			var canvasRecreated = canvas is null || _lastCanvasSize != xamlRootBounds || _lastRasterizationScale != rasterizationScale;
			if (canvasRecreated)
			{
				canvas = resizeFunc(new Size(Math.Round(xamlRootBounds.Width * rasterizationScale), Math.Round(xamlRootBounds.Height * rasterizationScale)));
				_lastCanvasSize = xamlRootBounds;
				_lastRasterizationScale = rasterizationScale;
				_lastScaledNativeClipPath = null;

				// Jobs that couldn't run at method entry because the canvas didn't exist yet.
				if (!_renderJobs.IsEmpty)
				{
					RunRenderJobs(canvas.Context as GRContext);
				}
			}

			canvas!.Save();
			if (rasterizationScale != 1)
			{
				canvas.Scale(rasterizationScale, rasterizationScale);
			}

			var damage = lastRenderedFrame.damage;
			var useDamageRegion = !canvasRecreated;
			var overlayEnabled = global::Uno.UI.FeatureConfiguration.Rendering.DamageRegionOverlay;

			if (useDamageRegion && !overlayEnabled)
			{
				canvas.ClipPath(damage, antialias: false);
			}

			using var fpsHelperDisposable = _fpsHelper.BeginFrame();
			SkiaRenderHelper.RenderPicture(
				canvas,
				lastRenderedFrame.picture.Picture,
				SKColors.Transparent,
				_fpsHelper.DrawFps);

			if (overlayEnabled && useDamageRegion && !damage.IsEmpty)
			{
				DrawDamageRegionOverlay(canvas, damage);
			}

			canvas.Restore();

			// This frame's damage is now presented; clear it so Render's carry-forward doesn't re-damage it next frame.
			lastRenderedFrame.damage.Reset();
			ReturnFrame(lastRenderedFrame);

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

	private void ReturnFrame((FramePicture picture, SKPath path, SKPath damage) frame)
	{
		var releasedPicture = default(FramePicture);

		lock (_frameGate)
		{
			// Put the frame back unless it has changed
			if (_lastRenderedFrame == null)
			{
				_lastRenderedFrame = frame;
			}
			else
			{
				releasedPicture = frame.picture;
				// This presented frame is superseded by a newer one; its snapshot (already rewound in Draw)
				// is done — recycle it.
				_damageSnapshotPool.Push(frame.damage);
			}
		}

		// Delete it then
		releasedPicture?.OnPipelineReleased();
	}

	private static void OnFramePictureRecorded(CompositionTarget target, FramePicture picture)
	{
		picture.Retain();
		if (_latestFrames.TryGetValue(target, out var replaced))
		{
			replaced.Release(pictureAccessed: false);
		}
		_latestFrames[target] = picture;

		if (_isRenderingActive && !_renderingRaiseScheduled)
		{
			_renderingRaiseScheduled = true;
			NativeDispatcher.Main.Enqueue(RaiseRendering, NativeDispatcherPriority.High);
		}
	}

	// Raises Rendering once per batch of recorded frames, with the latest picture of every live
	// target — including targets that haven't re-rendered since the last raise, whose previous
	// picture is resent. Synchronous callout to app code.
	private static void RaiseRendering()
	{
		_renderingRaiseScheduled = false;

		var pictures = new FramePicture[_latestFrames.Count];
		var frameData = new List<(Window Window, object Data)>(_latestFrames.Count);
		var i = 0;
		foreach (var (target, picture) in _latestFrames)
		{
			picture.Retain();
			pictures[i++] = picture;
			if (target.ContentRoot.GetOwnerWindow() is { } window)
			{
				frameData.Add((window, picture.Picture));
			}
		}

		var args = new RenderingEventArgs(Stopwatch.GetElapsedTime(_start), frameData);
		try
		{
			_rendering?.Invoke(null, args);
		}
		finally
		{
			foreach (var picture in pictures)
			{
				picture.Release(args.FrameDataAccessed);
			}
		}
	}

	private static void OnTargetUnregistered(CompositionTarget target)
	{
		_targets.Remove(target);
		if (_latestFrames.Remove(target, out var picture))
		{
			picture.Release(pictureAccessed: false);
		}
	}

	/// <summary>
	/// Owns a frame's recorded picture. The picture is disposed deterministically once the
	/// pipeline released it and all retentions (the latest-frame cache, in-flight Rendering
	/// raises) are gone — unless a Rendering subscriber actually read it from the event args,
	/// in which case user code may still hold it and the GC reclaims it instead.
	/// </summary>
	internal sealed class FramePicture(SKPicture picture)
	{
		private readonly Lock _gate = new();
		private int _retainCount;
		private bool _publicized;
		private bool _releasedByPipeline;
		private bool _disposed;

		public SKPicture Picture { get; } = picture;

		public void Retain()
		{
			lock (_gate)
			{
				_retainCount++;
			}
		}

		public void Release(bool pictureAccessed)
		{
			bool dispose;
			lock (_gate)
			{
				_retainCount--;
				_publicized |= pictureAccessed;
				dispose = ShouldDispose();
				_disposed |= dispose;
			}

			if (dispose)
			{
				Picture.Dispose();
			}
		}

		public void OnPipelineReleased()
		{
			bool dispose;
			lock (_gate)
			{
				_releasedByPipeline = true;
				dispose = ShouldDispose();
				_disposed |= dispose;
			}

			if (dispose)
			{
				Picture.Dispose();
			}
		}

		// only call under _gate
		private bool ShouldDispose() => !_disposed && !_publicized && _releasedByPipeline && _retainCount == 0;
	}
}
