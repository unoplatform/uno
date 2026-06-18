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

	// The per-frame damage region. During Render() it is threaded through the PaintingSession so each visual
	// adds the region it (re)paints as the walk proceeds (see Visual.ContributeDamageOnPaint). It also collects
	// out-of-band damage that occurs with no active walk — a visual hidden or removed between frames isn't
	// walked, so it reports its vacated region here (via ICompositionTarget.AddDamage). Snapshotted into the
	// rendered frame and reset each Render(). Only touched on the UI thread.
	private readonly DamageRegion _pendingDamage = new();

	// Snapshot of the damage region attached to a recorded frame, consumed by Draw() on the render thread.
	// The region may be a possibly-disjoint union of arbitrary shapes. It is transported across the
	// record/present threads as SVG path data (a GC-managed string, so the snapshot needs no native cleanup):
	// RegionSvg is null when the region is a single rectangle (the common case) — Bounds then describes it.
	private readonly record struct DamageSnapshot(bool IsFullFrame, bool IsEmpty, SKRect Bounds, string? RegionSvg);

	private static readonly DamageSnapshot _emptyDamage = new(IsFullFrame: false, IsEmpty: true, default, null);

	private static DamageSnapshot SnapshotFrom(DamageRegion region)
	{
		if (region.IsFullFrame)
		{
			return new DamageSnapshot(IsFullFrame: true, IsEmpty: false, default, null);
		}
		if (region.IsEmpty)
		{
			return _emptyDamage;
		}
		if (region.IsRect(out var rect))
		{
			return new DamageSnapshot(IsFullFrame: false, IsEmpty: false, rect, null);
		}
		return new DamageSnapshot(IsFullFrame: false, IsEmpty: false, region.Bounds, region.Region.ToSvgPathData());
	}

	// Builds an SKPath for a (non-full, non-empty) snapshot's region. Caller owns/disposes it.
	private static SKPath ToRegionPath(DamageSnapshot damage)
	{
		if (damage.RegionSvg is { } svg)
		{
			return SKPath.ParseSvgPathData(svg);
		}
		var path = new SKPath();
		path.AddRect(damage.Bounds);
		return path;
	}

	// Builds a DamageSnapshot from a region path (rectangle fast path when possible). Does not take ownership.
	private static DamageSnapshot SnapshotFromPath(SKPath region)
	{
		if (region.IsEmpty)
		{
			return _emptyDamage;
		}
		if (region.IsRect)
		{
			return new DamageSnapshot(IsFullFrame: false, IsEmpty: false, region.Bounds, null);
		}
		return new DamageSnapshot(IsFullFrame: false, IsEmpty: false, region.Bounds, region.ToSvgPathData());
	}

	// only set on the UI thread and under _frameGate, only read under _frameGate
	private (IntPtr frame, SKPath nativeElementClipPath, DamageSnapshot damage)? _lastRenderedFrame;
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

		// Thread the per-frame damage accumulator through the walk: visuals add the region they (re)paint to
		// it via the PaintingSession as RecordPictureAndReturnPath walks the tree. It already carries any
		// out-of-band damage (visuals hidden/removed since the last frame) accumulated via AddDamage.
		var (picture, path, nativeVisualsInZOrder) = SkiaRenderHelper.RecordPictureAndReturnPath(
			(float)bounds.Width,
			(float)bounds.Height,
			rootElement.Visual,
			invertPath: FrameRenderingOptions.invertNativeElementClipPath,
			damage: _pendingDamage);

		// The frame-rate counter is a present-time overlay (drawn in Draw, clipped to the damage region) that
		// changes every frame, so its panel region must be damaged each frame or the clipped present would
		// leave stale digits. It paints over composition content with a semi-transparent panel, so the content
		// underneath must be repainted too — adding the bounds to the damage covers both.
		if (_fpsHelper.TryGetDamageBounds(out var fpsBounds))
		{
			_pendingDamage.AddRect(fpsBounds);
		}

		// Snapshot and reset the accumulated damage region for this frame. The picture above is always
		// the full tree; the snapshot tells Draw() which region actually needs to be re-presented.
		var damageSnapshot = SnapshotFrom(_pendingDamage);
		_pendingDamage.Reset();

		// Clamp the damage to the visual-tree bounds. Nothing outside the frame is ever presented, and an
		// unbounded contributor (a visual that falls back to an infinite clip) would otherwise pin the damage
		// — and, with carry-forward, every later frame — to the infinite rect, repainting the whole window.
		if (!damageSnapshot.IsFullFrame && !damageSnapshot.IsEmpty)
		{
			var frameRect = new SKRect(0, 0, (float)bounds.Width, (float)bounds.Height);
			if (!frameRect.Contains(damageSnapshot.Bounds))
			{
				if (damageSnapshot.RegionSvg is null)
				{
					var clampedBounds = SKRect.Intersect(damageSnapshot.Bounds, frameRect);
					damageSnapshot = clampedBounds.IsEmpty ? _emptyDamage : new DamageSnapshot(false, false, clampedBounds, null);
				}
				else
				{
					using var region = ToRegionPath(damageSnapshot);
					using var framePath = new SKPath();
					framePath.AddRect(frameRect);
					region.Op(framePath, SKPathOp.Intersect, region);
					damageSnapshot = SnapshotFromPath(region);
				}
			}
		}

		var previousFrame = default((IntPtr frame, SKPath path, DamageSnapshot damage)?);
		lock (_frameGate)
		{
			previousFrame = _lastRenderedFrame;

			// If a previously recorded frame was never presented (it's still here because Draw didn't
			// borrow it), its damage region was never painted to the surface. Carry that damage forward
			// into this frame so those regions are still repainted; otherwise damage-region would
			// drop the changes from the discarded frame, leaving stale pixels.
			if (previousFrame is { } pf)
			{
				damageSnapshot = MergeDamage(damageSnapshot, pf.damage);
			}

			_lastRenderedFrame = (picture, path, damageSnapshot);
		}

		_fpsHelper.OnFrameRecorded();

		// Delete previous SKPicture now since we are swapping it
		if (previousFrame != null)
		{
			UnoSkiaApi.sk_refcnt_safe_unref(previousFrame.Value.frame);
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
		this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: {nameof(Render)} ends");
	}

	void ICompositionTarget.AddDamage(SKRect bounds) => _pendingDamage.AddRect(bounds);

	void ICompositionTarget.AddDamage(SKPath region) => _pendingDamage.AddPath(region);

	void ICompositionTarget.AddFullFrameDamage() => _pendingDamage.SetFullFrame();

	// Unions two per-frame damage snapshots (used to carry an un-presented frame's damage forward). The
	// union is a true geometric union of the two (possibly non-rectangular, possibly disjoint) regions, not
	// their bounding box, so carrying damage forward doesn't fill in the gaps between far-apart changes.
	private static DamageSnapshot MergeDamage(DamageSnapshot current, DamageSnapshot carried)
	{
		if (current.IsFullFrame || carried.IsFullFrame)
		{
			return new DamageSnapshot(IsFullFrame: true, IsEmpty: false, default, null);
		}

		if (carried.IsEmpty)
		{
			return current;
		}

		if (current.IsEmpty)
		{
			return carried;
		}

		using var region = ToRegionPath(current);
		using var carriedRegion = ToRegionPath(carried);
		region.Op(carriedRegion, SKPathOp.Union, region);
		return SnapshotFromPath(region);
	}

	private static void DrawDamageRegionOverlay(SKCanvas canvas, in DamageSnapshot damage)
	{
		// Diagnostic only: tint + outline the region repainted this frame. Drawing the actual region (which
		// may be disjoint / non-rectangular) shows the gaps between changed areas are not being repainted.
		using var fill = new SKPaint { Color = new SKColor(0xFF, 0x00, 0x00, 0x30), Style = SKPaintStyle.Fill };
		using var stroke = new SKPaint { Color = new SKColor(0xFF, 0x00, 0x00, 0xB0), Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
		if (damage.RegionSvg is null)
		{
			canvas.DrawRect(damage.Bounds, fill);
			canvas.DrawRect(damage.Bounds, stroke);
		}
		else
		{
			using var region = ToRegionPath(damage);
			canvas.DrawPath(region, fill);
			canvas.DrawPath(region, stroke);
		}
	}

	private SKPath Draw(SKCanvas? canvas, Func<Size, SKCanvas> resizeFunc, bool surfaceRetainsContents)
	{
		this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: {nameof(Draw)}");

		// Run pending render jobs even when there's no frame to present. When the canvas
		// doesn't exist yet, jobs stay queued for the next pass (the one that will create it).
		if (canvas is not null && !_renderJobs.IsEmpty)
		{
			RunRenderJobs(canvas.Context as GRContext);
		}

		(IntPtr frame, SKPath nativeElementClipPath, DamageSnapshot damage)? lastRenderedFrameNullable;
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
			// A (re)created or resized canvas has undefined contents, so the whole surface must be
			// repainted this frame regardless of the tracked damage region.
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

			// canvas is guaranteed non-null here: it is either the (non-null) argument or freshly
			// created by resizeFunc when canvasRecreated is true.
			var targetCanvas = canvas!;

			targetCanvas.Save();
			if (rasterizationScale != 1)
			{
				targetCanvas.Scale(rasterizationScale, rasterizationScale);
			}

			// Damage-region: when the surface retains the previous frame's contents and the feature
			// is enabled, clip the present to the changed region so only that area is cleared and
			// repainted; the rest is preserved from the previous frame. Output is identical to a full
			// repaint. Falls back to full-frame on canvas recreation/resize or when the frame is marked
			// full. The clip is set in the post-scale (root/logical) coordinate space the picture uses.
			var damage = lastRenderedFrame.damage;
			var useDamageRegion =
				surfaceRetainsContents
				&& !canvasRecreated
				&& !damage.IsFullFrame;
			var overlayEnabled = global::Uno.UI.FeatureConfiguration.Rendering.DamageRegionOverlay;

			// The overlay tint is drawn into the retained surface, so a clipped present would let the marks
			// accumulate and never clear. When the overlay is on we therefore present full-frame (the full
			// redraw wipes the previous frame's tint) and only highlight the region that WOULD have been the
			// damage clip. This is a diagnostic aid; it intentionally forgoes the clipped present so the marks
			// flash on the current damage region instead of piling up.
			SKPath? damageClipPath = null;
			if (useDamageRegion && !overlayEnabled)
			{
				if (damage.IsEmpty || damage.RegionSvg is null)
				{
					targetCanvas.ClipRect(damage.IsEmpty ? SKRect.Empty : damage.Bounds);
				}
				else
				{
					// Non-rectangular / disjoint region: clip to the actual region so the gaps between changed
					// areas (and the parts outside a curved clip) are preserved from the previous frame. No
					// antialiasing on the damage clip — the regions are pixel-snapped, and an AA clip would blend
					// boundary pixels against the retained surface, diverging from a full repaint.
					damageClipPath = ToRegionPath(damage);
					targetCanvas.ClipPath(damageClipPath, antialias: false);
				}
			}

			using var fpsHelperDisposable = _fpsHelper.BeginFrame();
			SkiaRenderHelper.RenderPicture(
				targetCanvas,
				lastRenderedFrame.frame,
				SKColors.Transparent,
				_fpsHelper.DrawFps);

			if (overlayEnabled && useDamageRegion && !damage.IsEmpty)
			{
				DrawDamageRegionOverlay(targetCanvas, damage);
			}

			targetCanvas.Restore();
			damageClipPath?.Dispose();

			// The frame has now been presented, so its damage region is consumed. ReturnFrame puts the frame
			// back as _lastRenderedFrame (the platform may re-present the same frame), and the next Render would
			// otherwise carry this already-presented damage forward via MergeDamage — accumulating it forever
			// under a continuous render loop and pinning the damage region (and the overlay) to the whole frame.
			// Return it with an empty damage region so a re-present paints nothing and the carry stays clean.
			ReturnFrame((lastRenderedFrame.frame, lastRenderedFrame.nativeElementClipPath, _emptyDamage));

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

	private void ReturnFrame((IntPtr frame, SKPath nativeElementClipPath, DamageSnapshot damage) frame)
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
				pictureToDelete = frame.frame;
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
