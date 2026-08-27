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
	/// The active rendering backend that owns the frame record/present lifecycle. A head may install its own
	/// (e.g. WebGPU); otherwise it falls back to the registered backend's default, throwing if none is registered.
	/// </summary>
	// Per-window backend factory: each CompositionTarget presents through the factory bound to its OWN window's
	// graphics context. This must be per-window (not a process-wide static) because a GRContext is bound to the
	// context it was created on (e.g. one GL/Vulkan context per X11 window), so a single renderer cannot be shared
	// across windows — doing so crashes when one window's context is torn down. Each head installs it per frame.
	private IDrawingFactory? _renderer;

	internal IDrawingFactory Renderer
	{
		get => _renderer
			?? DrawingRegistration.DefaultRenderer
			?? throw new global::System.InvalidOperationException(
				"No graphics backend registered. Register one through the host builder (.GraphicsBackend) and/or the head must set CompositionTarget.Renderer before the first frame.");
		set
		{
			// Invalidate on ANY change, including the first assignment from null: the getter already falls back to a
			// default renderer, so frames may have been recorded before a head assigns its own (async on WASM/WebGPU).
			var changed = !ReferenceEquals(_renderer, value);
			_renderer = value;

			// The retained frame and cached per-visual recordings belong to the previous backend and can't be replayed
			// by the new one; discard them and request a fresh frame so the tree re-records under the new renderer.
			if (changed)
			{
				InvalidateAllRecordings();
			}
		}
	}

	// Non-throwing peek at renderer availability. False while a declared backend initializes asynchronously (WASM
	// WebGPU device import): Render() must SKIP the frame rather than force the throwing renderer getter.
	private bool HasRenderer => _renderer is not null || DrawingRegistration.DefaultRenderer is not null;

	// Neutral→typed narrowing for phase-2 present: downcast the target to its bound kind and dispatch to the
	// backend's typed IDrawingFactory<TTarget>.BeginPresent, keeping the single cast Uno-side.
	private static IPresentSession BeginPresent(IDrawingFactory backend, IRenderTarget target)
		=> target switch
		{
			IGLRenderTarget gl when backend is IDrawingFactory<IGLRenderTarget> b => b.BeginPresent(gl),
			ISoftwareRenderTarget sw when backend is IDrawingFactory<ISoftwareRenderTarget> b => b.BeginPresent(sw),
			IMetalRenderTarget m when backend is IDrawingFactory<IMetalRenderTarget> b => b.BeginPresent(m),
			IVulkanRenderTarget vk when backend is IDrawingFactory<IVulkanRenderTarget> b => b.BeginPresent(vk),
			IWebGpuRenderTarget w when backend is IDrawingFactory<IWebGpuRenderTarget> b => b.BeginPresent(w),
			_ => throw new global::System.NotSupportedException(
				$"The active backend cannot present onto a render target of type {target.GetType().Name}."),
		};

	private static void InvalidateAllRecordings()
	{
		foreach (var kvp in _targets)
		{
			var target = kvp.Key;

			(IRenderRecord frame, IGeometry nativeElementClipPath, IGeometry? damage)? staleFrame;
			lock (target._frameGate)
			{
				staleFrame = target._lastRenderedFrame;
				target._lastRenderedFrame = null;
			}
			if (staleFrame is { } sf)
			{
				sf.frame.Dispose();
				sf.damage?.Dispose();
			}

			if (target.ContentRoot?.VisualTree?.RootElement?.Visual is { } rootVisual)
			{
				rootVisual.InvalidatePaintRecursive();
			}

			((ICompositionTarget)target).RequestNewFrame();
		}
	}

	private static readonly long _start = Stopwatch.GetTimestamp();
	// We're using this table as a set with weakref keys. values are always null
	private static readonly ConditionalWeakTable<CompositionTarget, object> _targets = new();
	private static bool _isRenderingActive;

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
	// UNO_FORCE_FULL_REPAINT=1 disables damage-clipped partial repaints (benchmarking: measures true full-frame cost).
	private static readonly bool _forceFullRepaint =
		Environment.GetEnvironmentVariable("UNO_FORCE_FULL_REPAINT") is "1" or "true";

	// UNO_LOG_FRAME_PHASES=1 prints per-phase frame timing averages every 60 frames (benchmarking).
	private static readonly bool _logFramePhases =
		Environment.GetEnvironmentVariable("UNO_LOG_FRAME_PHASES") is "1" or "true";

	// UNO_FORCE_CONTINUOUS_RENDER=1 re-requests a frame after every render (benchmarking: saturates the pipeline).
	private static readonly bool _forceContinuousRender =
		Environment.GetEnvironmentVariable("UNO_FORCE_CONTINUOUS_RENDER") is "1" or "true";
	private static long _phaseRecordTicks, _phaseFinishTicks, _phaseDrawTicks, _phaseGapTicks, _phaseLastRenderEnd;
	private static int _phaseRenderFrames, _phaseDrawFrames;
	// Itemization of the between-render "gap": Rendering-event handlers (the app's per-frame tick), the layout
	// pass (CoreServices.OnTick's UpdateLayout), GC activity, and a frame-interval histogram for vsync misses.
	private static long _phaseTickTicks, _phaseLayoutTicks, _phaseLastRenderStart, _phaseMaxIntervalTicks;
	private static int _phaseLayoutRuns, _phaseOver20, _phaseOver33;
	private static int _phaseGc0, _phaseGc1, _phaseGc2;
	private static TimeSpan _phaseGcPause;

	internal static bool IsFramePhaseLoggingEnabled => _logFramePhases;

	/// <summary>Accumulates the duration of one layout pass (see CoreServices.OnTick) into the frame-phase log.</summary>
	internal static void PhaseAddLayout(long ticks)
	{
		_phaseLayoutTicks += ticks;
		_phaseLayoutRuns++;
	}

	private (IRenderRecord frame, IGeometry nativeElementClipPath, IGeometry? damage)? _lastRenderedFrame;
	// Damage (dirty region) accumulated between frames from AddDamage + carried-forward unpresented damage;
	// folded into each frame's own damage during Render. Guarded by _frameGate.
	private readonly DamageRegion _pendingDamage = new();
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

		if (!HasRenderer)
		{
			// Declared backend still initializing (async WebGPU device import on WASM); skip the frame rather than
			// fall back to another backend — a fresh frame is requested once the head installs CompositionTarget.Renderer.
			return;
		}

		var rootElement = ContentRoot.VisualTree.RootElement;
		var bounds = ContentRoot.VisualTree.Size;

		// Phase 1 (UI thread): record the visual tree into a backend session, finishing to an opaque frame. The
		// per-frame damage accumulator (seeded with carried-over damage) is clamped to drive a partial repaint at present.
		var frameDamage = new DamageRegion();
		var frameRect = new Rect(0, 0, bounds.Width, bounds.Height);
		lock (_frameGate)
		{
			frameDamage.Union(_pendingDamage);
			_pendingDamage.Reset();
		}

		var phaseT0 = _logFramePhases ? Stopwatch.GetTimestamp() : 0;
		if (_logFramePhases && _phaseLastRenderEnd != 0)
		{
			_phaseGapTicks += phaseT0 - _phaseLastRenderEnd;
		}
		if (_logFramePhases)
		{
			if (_phaseLastRenderStart != 0)
			{
				var interval = phaseT0 - _phaseLastRenderStart;
				if (interval > _phaseMaxIntervalTicks) { _phaseMaxIntervalTicks = interval; }
				var intervalMs = interval * 1000.0 / Stopwatch.Frequency;
				if (intervalMs > 20) { _phaseOver20++; }
				if (intervalMs > 33.4) { _phaseOver33++; }
			}
			_phaseLastRenderStart = phaseT0;
		}
		var recording = Renderer.CreateRecording();
		var (path, nativeVisualsInZOrder) = SkiaRenderHelper.RecordFrame(
			recording,
			(float)bounds.Width,
			(float)bounds.Height,
			rootElement.Visual,
			FrameRenderingOptions.invertNativeElementClipPath,
			frameDamage);
		var phaseT1 = _logFramePhases ? Stopwatch.GetTimestamp() : 0;
		var frame = recording.Finish();
		if (_logFramePhases)
		{
			var phaseT2 = Stopwatch.GetTimestamp();
			_phaseRecordTicks += phaseT1 - phaseT0;
			_phaseFinishTicks += phaseT2 - phaseT1;
		}
		frameDamage.ClampTo(frameRect);
		var renderedFrame = (frame, path, frameDamage.Detach());
		var previousFrame = default((IRenderRecord frame, IGeometry nativeElementClipPath, IGeometry? damage)?);
		lock (_frameGate)
		{
			previousFrame = _lastRenderedFrame;

			_lastRenderedFrame = renderedFrame;

			// A previous frame that was recorded but never presented (its slot was still occupied) is being
			// dropped; carry its damage forward so the area it would have repainted isn't lost.
			if (previousFrame is { damage: { } carried })
			{
				_pendingDamage.Union(carried);
			}
		}

		_fpsHelper.OnFrameRecorded();

		previousFrame?.frame.Dispose();
		previousFrame?.damage?.Dispose();

		if (_isRenderingActive || _forceContinuousRender)
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

		if (_logFramePhases)
		{
			_phaseLastRenderEnd = Stopwatch.GetTimestamp();
			if (++_phaseRenderFrames >= 60)
			{
				double Ms(long t, int c) => c == 0 ? 0 : t * 1000.0 / Stopwatch.Frequency / c;
				var gc0 = GC.CollectionCount(0);
				var gc1 = GC.CollectionCount(1);
				var gc2 = GC.CollectionCount(2);
				var pause = TimeSpan.Zero;
				try
				{
					var total = GC.GetTotalPauseDuration();
					pause = _phaseGcPause == TimeSpan.Zero ? TimeSpan.Zero : total - _phaseGcPause;
					_phaseGcPause = total;
				}
				catch (Exception) { /* not available on every runtime */ }
				Console.WriteLine(
					$"[frame-phases] record={Ms(_phaseRecordTicks, _phaseRenderFrames):F1}ms finish={Ms(_phaseFinishTicks, _phaseRenderFrames):F1}ms draw={Ms(_phaseDrawTicks, Math.Max(_phaseDrawFrames, 1)):F1}ms tick={Ms(_phaseTickTicks, _phaseRenderFrames):F1}ms layout={Ms(_phaseLayoutTicks, Math.Max(_phaseLayoutRuns, 1)):F1}ms({_phaseLayoutRuns}) gap={Ms(_phaseGapTicks, _phaseRenderFrames):F1}ms"
					+ $" | >20ms={_phaseOver20} >33ms={_phaseOver33} max={_phaseMaxIntervalTicks * 1000.0 / Stopwatch.Frequency:F1}ms"
					+ $" | gc0=+{gc0 - _phaseGc0} gc1=+{gc1 - _phaseGc1} gc2=+{gc2 - _phaseGc2} pause={pause.TotalMilliseconds:F1}ms"
					+ $" (avg/frame, {_phaseRenderFrames} renders, {_phaseDrawFrames} draws)");
				_phaseGc0 = gc0;
				_phaseGc1 = gc1;
				_phaseGc2 = gc2;
				_phaseRecordTicks = _phaseFinishTicks = _phaseDrawTicks = _phaseGapTicks = _phaseTickTicks = _phaseLayoutTicks = _phaseMaxIntervalTicks = 0;
				_phaseRenderFrames = _phaseDrawFrames = _phaseLayoutRuns = _phaseOver20 = _phaseOver33 = 0;
			}
		}
		this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: {nameof(Render)} ends");
	}

	private IGeometry Draw(ISwapChain swapChain, Matrix4x4? rootTransform = null, Action<IDrawingSession>? overlay = null)
	{
		this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: {nameof(Draw)}");
		var phaseDrawT0 = _logFramePhases ? Stopwatch.GetTimestamp() : 0;

		(IRenderRecord frame, IGeometry nativeElementClipPath, IGeometry? damage)? lastRenderedFrameNullable;
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
			// The swapchain owns sizing/caching: acquire every frame at the DPI-scaled bounds; it returns the cached
			// target while the size is unchanged and recreates it on resize.
			var target = swapChain.AcquireRenderTarget(
				(int)Math.Round(xamlRootBounds.Width * rasterizationScale),
				(int)Math.Round(xamlRootBounds.Height * rasterizationScale));
			var resized = _lastCanvasSize != xamlRootBounds || _lastRasterizationScale != rasterizationScale;
			if (resized)
			{
				_lastCanvasSize = xamlRootBounds;
				_lastRasterizationScale = rasterizationScale;
				_lastScaledNativeClipPath = null;
			}

			using var fpsHelperDisposable = _fpsHelper.BeginFrame();
			using (var present = BeginPresent(Renderer, target))
			{
				// Partial repaint: when unresized and the host preserves the swapchain's pixels, clip the clear+replay
				// to the damage region so only the changed area is repainted; otherwise repaint the whole frame.
				var hasDamage = !resized && lastRenderedFrame.damage is { } dmg && !dmg.IsEmpty;
				var damageEligible = hasDamage && swapChain.PreservesContents;
				// Debug overlay paints the would-be damage region on a full repaint; deliberately not gated on
				// PreservesContents so the viz works on full-repaint targets too.
				var overlayEnabled = global::Uno.UI.FeatureConfiguration.Rendering.DamageRegionOverlay;
				var useDamage = damageEligible && !overlayEnabled && !_forceFullRepaint;

				// Scaling (DPI) is applied through the neutral session so it works for any backend.
				present.Save();
				// A host may impose an outermost transform (e.g. framebuffer display orientation) that must wrap
				// the whole composition, applied before the DPI scale so content and scale rotate together.
				if (rootTransform is { } rt)
				{
					present.Concat(rt);
				}
				if (rasterizationScale != 1)
				{
					present.Scale(rasterizationScale, rasterizationScale);
				}
				// Clip clear+replay to the damage region so only the damaged area is repainted; FPS/overlay draw
				// outside this scope so they aren't restricted to it.
				present.Save();
				if (useDamage)
				{
					present.ClipPath(lastRenderedFrame.damage!, ClipOperation.Intersect, antialias: false);
				}
				present.Clear(global::Windows.UI.Colors.Transparent);
				lastRenderedFrame.frame.Replay(present);
				present.Restore();
				if (overlayEnabled && hasDamage)
				{
					DrawDamageRegionOverlay(present, lastRenderedFrame.damage!);
				}
				_fpsHelper.DrawFps(present);
				// A host overlay (e.g. the framebuffer software cursor) draws on top of the frame, under the same
				// orientation + DPI transform as the content.
				overlay?.Invoke(present);
				present.Restore();
			}

			ReturnFrame(lastRenderedFrame);

			if (_logFramePhases)
			{
				_phaseDrawTicks += Stopwatch.GetTimestamp() - phaseDrawT0;
				_phaseDrawFrames++;
			}

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


	private void ReturnFrame((IRenderRecord frame, IGeometry nativeElementClipPath, IGeometry? damage) frame)
	{
		IRenderRecord? frameToDelete = null;
		IGeometry? damageToDelete = null;

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
				damageToDelete = frame.damage;
			}
		}

		frameToDelete?.Dispose();
		damageToDelete?.Dispose();
	}

	void ICompositionTarget.AddDamage(Rect bounds)
	{
		NativeDispatcher.CheckThreadAccess();
		lock (_frameGate)
		{
			_pendingDamage.UnionRect(bounds);
		}
	}

	void ICompositionTarget.AddDamage(IGeometry region)
	{
		NativeDispatcher.CheckThreadAccess();
		lock (_frameGate)
		{
			_pendingDamage.Union(region);
		}
	}

	// Debug viz (FeatureConfiguration.Rendering.DamageRegionOverlay): paints the damage region as a translucent
	// red fill + outline over the fully-repainted frame.
	private static void DrawDamageRegionOverlay(IPresentSession present, IGeometry damage)
	{
		present.DrawPath(damage, global::Windows.UI.Color.FromArgb(0x30, 0xFF, 0x00, 0x00), antialias: false);
		using var outline = damage.GetStrokeFillGeometry(new StrokeStyle { Thickness = 1f });
		present.DrawPath(outline, global::Windows.UI.Color.FromArgb(0xB0, 0xFF, 0x00, 0x00), antialias: false);
	}

	internal static void InvokeRendering()
	{
		if (NativeDispatcher.Main.HasThreadAccess)
		{
			InvokeRenderingCore();
		}
		else
		{
			NativeDispatcher.Main.Enqueue(InvokeRenderingCore, NativeDispatcherPriority.High);
		}

		static void InvokeRenderingCore()
		{
			var t0 = _logFramePhases ? Stopwatch.GetTimestamp() : 0;
			_rendering?.Invoke(null, new RenderingEventArgs(Stopwatch.GetElapsedTime(_start)));
			if (_logFramePhases)
			{
				_phaseTickTicks += Stopwatch.GetTimestamp() - t0;
			}
		}
	}
}
