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

	// Per-frame dirty region, accumulated from invalidations on the UI thread (via AddDamage) and
	// snapshotted into the rendered frame in Render(). Only touched on the UI thread.
	private readonly DirtyRegion _pendingDamage = new();

	// Diagnostic escape hatch: when UNO_DIRTY_RECTANGLES=false, present the whole frame every time instead
	// of clipping to the dirty region. Dirty-rectangles rendering is otherwise always on for retaining
	// renderers. This exists so the validation harness can compare dirty output against a full-frame
	// baseline in the same binary; both must be pixel-identical. Read once to keep the present path hot.
	private static readonly bool _forceFullFramePresent =
		Environment.GetEnvironmentVariable("UNO_DIRTY_RECTANGLES") is "false";

	// Snapshot of the dirty region attached to a recorded frame, consumed by Draw() on the render thread.
	private readonly record struct DamageSnapshot(bool IsFullFrame, bool IsEmpty, SKRect Bounds);

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

		var (picture, path, nativeVisualsInZOrder) = SkiaRenderHelper.RecordPictureAndReturnPath(
			(float)bounds.Width,
			(float)bounds.Height,
			rootElement.Visual,
			invertPath: FrameRenderingOptions.invertNativeElementClipPath);

		// Snapshot and reset the accumulated dirty region for this frame. The picture above is always
		// the full tree; the snapshot tells Draw() which region actually needs to be re-presented.
		var damageSnapshot = new DamageSnapshot(_pendingDamage.IsFullFrame, _pendingDamage.IsEmpty, _pendingDamage.Bounds);
		_pendingDamage.Reset();

		// Clamp the damage to the visual-tree bounds. Nothing outside the frame is ever presented, and an
		// unbounded contributor (a visual that falls back to an infinite clip) would otherwise pin the damage
		// — and, with carry-forward, every later frame — to the infinite rect, repainting the whole window.
		if (!damageSnapshot.IsFullFrame && !damageSnapshot.IsEmpty)
		{
			var frameRect = new SKRect(0, 0, (float)bounds.Width, (float)bounds.Height);
			var clampedBounds = SKRect.Intersect(damageSnapshot.Bounds, frameRect);
			damageSnapshot = clampedBounds.IsEmpty
				? new DamageSnapshot(IsFullFrame: false, IsEmpty: true, default)
				: new DamageSnapshot(IsFullFrame: false, IsEmpty: false, clampedBounds);
		}

		var previousFrame = default((IntPtr frame, SKPath path, DamageSnapshot damage)?);
		lock (_frameGate)
		{
			previousFrame = _lastRenderedFrame;

			// If a previously recorded frame was never presented (it's still here because Draw didn't
			// borrow it), its dirty region was never painted to the surface. Carry that damage forward
			// into this frame so those regions are still repainted; otherwise dirty-rectangles would
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

	void ICompositionTarget.AddFullFrameDamage() => _pendingDamage.SetFullFrame();

	// Unions two per-frame damage snapshots (used to carry an un-presented frame's damage forward).
	private static DamageSnapshot MergeDamage(DamageSnapshot current, DamageSnapshot carried)
	{
		if (current.IsFullFrame || carried.IsFullFrame)
		{
			return new DamageSnapshot(IsFullFrame: true, IsEmpty: false, default);
		}

		if (carried.IsEmpty)
		{
			return current;
		}

		if (current.IsEmpty)
		{
			return carried;
		}

		var union = new SKRect(
			Math.Min(current.Bounds.Left, carried.Bounds.Left),
			Math.Min(current.Bounds.Top, carried.Bounds.Top),
			Math.Max(current.Bounds.Right, carried.Bounds.Right),
			Math.Max(current.Bounds.Bottom, carried.Bounds.Bottom));
		return new DamageSnapshot(IsFullFrame: false, IsEmpty: false, union);
	}

	private static void DrawDirtyRectanglesOverlay(SKCanvas canvas, SKRect bounds)
	{
		// Diagnostic only: tint + outline the region repainted this frame.
		using var fill = new SKPaint { Color = new SKColor(0xFF, 0x00, 0x00, 0x30), Style = SKPaintStyle.Fill };
		using var stroke = new SKPaint { Color = new SKColor(0xFF, 0x00, 0x00, 0xB0), Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
		canvas.DrawRect(bounds, fill);
		canvas.DrawRect(bounds, stroke);
	}

	private SKPath Draw(SKCanvas? canvas, Func<Size, SKCanvas> resizeFunc, bool surfaceRetainsContents)
	{
		this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: {nameof(Draw)}");

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
			// repainted this frame regardless of the tracked dirty region.
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

			// Dirty-rectangles: when the surface retains the previous frame's contents and the feature
			// is enabled, clip the present to the changed region so only that area is cleared and
			// repainted; the rest is preserved from the previous frame. Output is identical to a full
			// repaint. Falls back to full-frame on canvas recreation/resize or when the frame is marked
			// full. The clip is set in the post-scale (root/logical) coordinate space the picture uses.
			var damage = lastRenderedFrame.damage;
			var useDirtyRectangles =
				surfaceRetainsContents
				&& !canvasRecreated
				&& !damage.IsFullFrame
				&& !_forceFullFramePresent;
			var overlayEnabled = global::Uno.UI.FeatureConfiguration.Rendering.DirtyRectanglesOverlay;

			// The overlay tint is drawn into the retained surface, so a clipped present would let the marks
			// accumulate and never clear. When the overlay is on we therefore present full-frame (the full
			// redraw wipes the previous frame's tint) and only highlight the region that WOULD have been the
			// dirty clip. This is a diagnostic aid; it intentionally forgoes the clipped present so the marks
			// flash on the current dirty region instead of piling up.
			if (useDirtyRectangles && !overlayEnabled)
			{
				targetCanvas.ClipRect(damage.IsEmpty ? SKRect.Empty : damage.Bounds);
			}

			using var fpsHelperDisposable = _fpsHelper.BeginFrame();
			SkiaRenderHelper.RenderPicture(
				targetCanvas,
				lastRenderedFrame.frame,
				SKColors.Transparent,
				_fpsHelper.DrawFps);

			if (overlayEnabled && useDirtyRectangles && !damage.IsEmpty)
			{
				DrawDirtyRectanglesOverlay(targetCanvas, damage.Bounds);
			}

			targetCanvas.Restore();

			// The frame has now been presented, so its dirty region is consumed. ReturnFrame puts the frame
			// back as _lastRenderedFrame (the platform may re-present the same frame), and the next Render would
			// otherwise carry this already-presented damage forward via MergeDamage — accumulating it forever
			// under a continuous render loop and pinning the dirty region (and the overlay) to the whole frame.
			// Return it with an empty dirty region so a re-present paints nothing and the carry stays clean.
			ReturnFrame((lastRenderedFrame.frame, lastRenderedFrame.nativeElementClipPath, new DamageSnapshot(IsFullFrame: false, IsEmpty: true, default)));

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
