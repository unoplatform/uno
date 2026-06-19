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

	// The per-frame damage region. During Render() it is threaded through the PaintingSession so each visual
	// adds the region it (re)paints as the walk proceeds (see Visual.ContributeDamageOnPaint). It also collects
	// out-of-band damage that occurs with no active walk — a visual hidden or removed between frames isn't
	// walked, so it reports its vacated region here (via ICompositionTarget.AddDamage). Snapshotted into the
	// rendered frame and reset each Render(). Only touched on the UI thread.
	private readonly DamageRegion _pendingDamage = new();

	// The per-frame damage region attached to a recorded frame, consumed by Draw() on the render thread, as an
	// SKPath the frame OWNS — a copy of the per-frame accumulator (which is reused/reset each frame), so it must
	// be disposed when the frame is retired (replaced by a newer frame in Render, or superseded in ReturnFrame).
	// An empty path means nothing changed. Full repaints (resize/canvas recreation) are handled in Draw via
	// canvasRecreated, not here.
	private static SKPath SnapshotDamage(DamageRegion region) => new(region.Region);

	// only set on the UI thread and under _frameGate, only read under _frameGate
	private (IntPtr frame, SKPath nativeElementClipPath, SKPath damage)? _lastRenderedFrame;
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

		// The frame-rate counter is a present-time overlay (drawn in Draw, clipped to the damage region) that
		// changes every frame, so its panel region must be damaged each frame or the clipped present would
		// leave stale digits. It paints over composition content with a semi-transparent panel, so the content
		// underneath must be repainted too — adding the bounds to the damage covers both.
		if (_fpsHelper.TryGetDamageBounds(out var fpsBounds))
		{
			_pendingDamage.AddRect(fpsBounds);
		}

		var frameRect = new SKRect(0, 0, (float)bounds.Width, (float)bounds.Height);
		var previousFrame = default((IntPtr frame, SKPath path, SKPath damage)?);
		SKPath damageSnapshot;
		lock (_frameGate)
		{
			previousFrame = _lastRenderedFrame;

			// If a previously recorded frame was never presented (it's still here because Draw didn't borrow
			// it), its damage region was never painted to the surface. Carry it forward into this frame's
			// accumulator so those regions are still repainted; otherwise damage-region would drop the changes
			// from the discarded frame, leaving stale pixels. (Draw empties a presented frame's path, so this
			// no-ops for it.)
			if (previousFrame is { damage: var carried } && !carried.IsEmpty)
			{
				_pendingDamage.AddPath(carried);
			}

			// Clamp to the visual-tree bounds: nothing outside the frame is ever presented, and an unbounded
			// contributor (a visual that falls back to an infinite clip) would otherwise pin the damage — and,
			// with carry-forward, every later frame — to the infinite rect, repainting the whole window.
			_pendingDamage.ClampTo(frameRect);

			// Snapshot (owned copy) and reset the accumulator. The picture above is always the full tree;
			// the snapshot path tells Draw() which region actually needs to be re-presented.
			damageSnapshot = SnapshotDamage(_pendingDamage);
			_pendingDamage.Reset();

			_lastRenderedFrame = (picture, path, damageSnapshot);
		}

		_fpsHelper.OnFrameRecorded();

		// The previous frame is now replaced: free its picture, and dispose its damage path — its region was
		// copied into this frame's snapshot above (or wasn't needed), so it is no longer referenced.
		if (previousFrame is { } prev)
		{
			UnoSkiaApi.sk_refcnt_safe_unref(prev.frame);
			prev.damage.Dispose();
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

	private static void DrawDamageRegionOverlay(SKCanvas canvas, SKPath damage)
	{
		// Diagnostic only: tint + outline the region repainted this frame. Drawing the actual region (which
		// may be disjoint / non-rectangular) shows the gaps between changed areas are not being repainted.
		using var fill = new SKPaint { Color = new SKColor(0xFF, 0x00, 0x00, 0x30), Style = SKPaintStyle.Fill };
		using var stroke = new SKPaint { Color = new SKColor(0xFF, 0x00, 0x00, 0xB0), Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
		canvas.DrawPath(damage, fill);
		canvas.DrawPath(damage, stroke);
	}

	private SKPath Draw(SKCanvas? canvas, Func<Size, SKCanvas> resizeFunc)
	{
		this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: {nameof(Draw)}");

		(IntPtr frame, SKPath nativeElementClipPath, SKPath damage)? lastRenderedFrameNullable;
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
			}

			// canvas is guaranteed non-null here: it is either the (non-null) argument or freshly
			// created by resizeFunc when canvasRecreated is true.
			var targetCanvas = canvas!;

			targetCanvas.Save();
			if (rasterizationScale != 1)
			{
				targetCanvas.Scale(rasterizationScale, rasterizationScale);
			}

			// Damage-region: every renderer presents through a surface that retains the previous frame, so
			// clip the present to the changed region — only that area is cleared and repainted; the rest is
			// preserved from the previous frame. Output is identical to a full repaint. Falls back to full-frame
			// on canvas recreation/resize. The clip is set in the post-scale (root/logical) coordinate space the
			// picture uses. An empty damage path clips out everything (a re-present repaints nothing); a frame-
			// covering path clips to the whole frame (a full repaint).
			var damage = lastRenderedFrame.damage;
			var useDamageRegion = !canvasRecreated;
			var overlayEnabled = global::Uno.UI.FeatureConfiguration.Rendering.DamageRegionOverlay;

			// The overlay tint is drawn into the retained surface, so a clipped present would let the marks
			// accumulate and never clear. When the overlay is on we therefore present full-frame (the full
			// redraw wipes the previous frame's tint) and only highlight the region that WOULD have been the
			// damage clip. This is a diagnostic aid; it intentionally forgoes the clipped present so the marks
			// flash on the current damage region instead of piling up.
			if (useDamageRegion && !overlayEnabled)
			{
				// Clip to the changed region so the gaps between changed areas (and the parts outside a curved
				// clip) are preserved from the previous frame. No antialiasing on the damage clip — the regions
				// are pixel-snapped, and an AA clip would blend boundary pixels against the retained surface,
				// diverging from a full repaint.
				targetCanvas.ClipPath(damage, antialias: false);
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

			// The frame has now been presented, so its damage region is consumed. ReturnFrame puts the frame
			// back as _lastRenderedFrame (the platform may re-present the same frame), and the next Render would
			// otherwise carry this already-presented damage forward — accumulating it forever under a continuous
			// render loop and pinning the damage region to the whole frame. Empty the path (kept, not disposed,
			// since the frame may be re-presented) so a re-present repaints nothing and the carry stays clean.
			lastRenderedFrame.damage.Rewind();
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

	private void ReturnFrame((IntPtr frame, SKPath nativeElementClipPath, SKPath damage) frame)
	{
		var supersededFrame = false;

		lock (_frameGate)
		{
			// Put the frame back unless it has changed
			if (_lastRenderedFrame == null)
			{
				_lastRenderedFrame = frame;
			}
			else
			{
				supersededFrame = true;
			}
		}

		// A newer frame is already in place: this one is discarded, so free its picture and damage path.
		if (supersededFrame)
		{
			UnoSkiaApi.sk_refcnt_safe_unref(frame.frame);
			frame.damage.Dispose();
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
