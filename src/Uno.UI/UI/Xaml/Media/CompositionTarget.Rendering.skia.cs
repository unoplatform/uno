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
	private static IDrawingFactory? _renderer;
	internal static IDrawingFactory Renderer
	{
		get => _renderer
			?? DrawingRegistration.DefaultRenderer
			?? throw new global::System.InvalidOperationException(
				"No graphics backend registered. Register one through the host builder (.GraphicsBackend) and/or the head must set CompositionTarget.Renderer before the first frame.");
		set
		{
			// Invalidate on ANY change of the effective renderer, including the first assignment from the null
			// field (the getter falls back to the default Skia renderer, so frames may already have been recorded
			// with it before a head assigns WebGPU). NOTE the null guard is intentionally absent: on WebAssembly the
			// WebGPU device imports asynchronously and this is the FIRST assignment to the field, yet Skia frames
			// were already recorded via the default — those recordings can't be replayed by WebGPU.
			var changed = !ReferenceEquals(_renderer, value);
			_renderer = value;

			// The retained frame (_lastRenderedFrame) and every visual's cached recording were produced by the
			// previous backend and can't be replayed by the new one — the tree would render blank. Discard them and
			// request a fresh frame so the whole tree re-records under the new renderer.
			if (changed)
			{
				InvalidateAllRecordings();
			}
		}
	}

	// Non-throwing peek at renderer availability. False while a declared graphics backend is still initializing
	// asynchronously (e.g. the WASM/WebGPU device import replacing no default): there is deliberately no implicit
	// Skia fallback renderer, so Render() must SKIP the frame rather than force the throwing Renderer getter.
	private static bool HasRenderer => _renderer is not null || DrawingRegistration.DefaultRenderer is not null;

	// Neutral→typed narrowing for phase-2 present: the fresh target is downcast to the kind it always is (the
	// context bound at negotiation only yields that one type), and dispatched to the backend's typed
	// IDrawingFactory<TTarget>.BeginPresent. The single cast is here, Uno-side; the backend surface stays typed.
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
			// The declared graphics backend hasn't finished initializing (async WebGPU device import on WASM). Skip
			// this frame instead of falling back to another backend; a fresh frame is requested once the head installs
			// CompositionTarget.Renderer (see InitWebGpuAsync / InvalidateAllRecordings), so the tree records under it.
			return;
		}

		var rootElement = ContentRoot.VisualTree.RootElement;
		var bounds = ContentRoot.VisualTree.Size;

		// Phase 1 (UI thread): the backend hands us a recording session, the agnostic cycle walks the
		// visual tree into it, then we finish recording to get the opaque frame. A per-frame damage
		// accumulator collects each changed/moved visual's dirty region during the walk; it is seeded with
		// damage carried over from AddDamage / a superseded-before-present previous frame, then clamped to
		// the frame so it can drive a partial repaint at present time.
		var frameDamage = new DamageRegion();
		var frameRect = new Rect(0, 0, bounds.Width, bounds.Height);
		lock (_frameGate)
		{
			frameDamage.Union(_pendingDamage);
			_pendingDamage.Reset();
		}

		var recording = Renderer.CreateRecording();
		var (path, nativeVisualsInZOrder) = SkiaRenderHelper.RecordFrame(
			recording,
			(float)bounds.Width,
			(float)bounds.Height,
			rootElement.Visual,
			FrameRenderingOptions.invertNativeElementClipPath,
			frameDamage);
		var frame = recording.Finish();
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

		// Release the previous frame now since we are swapping it
		previousFrame?.frame.Dispose();
		previousFrame?.damage?.Dispose();

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

	private IGeometry Draw(IRenderTarget? target, Func<Size, IRenderTarget> resizeFunc, Matrix4x4? rootTransform = null, Action<IDrawingSession>? overlay = null)
	{
		this.LogTrace()?.Trace($"CompositionTarget#{GetHashCode()}: {nameof(Draw)}");

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
			var resized = target is null || _lastCanvasSize != xamlRootBounds || _lastRasterizationScale != rasterizationScale;
			if (resized)
			{
				target = resizeFunc(new Size(Math.Round(xamlRootBounds.Width * rasterizationScale), Math.Round(xamlRootBounds.Height * rasterizationScale)));
				_lastCanvasSize = xamlRootBounds;
				_lastRasterizationScale = rasterizationScale;
				_lastScaledNativeClipPath = null;
			}

			using var fpsHelperDisposable = _fpsHelper.BeginFrame();
			using (var present = BeginPresent(Renderer, target!))
			{
				// Partial repaint: when the host guarantees the target keeps the previous frame's pixels and the frame
				// wasn't resized, clip the clear+replay to the damage region so only the changed area is repainted and
				// the rest survives. The backend is oblivious — this is just an initial clip. Otherwise (fresh/undefined
				// surface) repaint the whole frame.
				var damageEligible = !resized
					&& target!.PreservesContents
					&& lastRenderedFrame.damage is { } dmg && !dmg.IsEmpty;
				// Debug overlay: full-repaint (no damage clip) but paint the would-be damage region so it's visible.
				var overlayEnabled = global::Uno.UI.FeatureConfiguration.Rendering.DamageRegionOverlay;
				var useDamage = damageEligible && !overlayEnabled;

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
				// Clip the content clear+replay to the damage region (in root/logical coords, matching the content);
				// Clear respects the clip, so only the damaged area is cleared and repainted. FPS/overlay draw outside
				// this scope so they aren't restricted to the damage region.
				present.Save();
				if (useDamage)
				{
					present.ClipPath(lastRenderedFrame.damage!, ClipOperation.Intersect, antialias: false);
				}
				present.Clear(global::Windows.UI.Colors.Transparent);
				lastRenderedFrame.frame.Replay(present);
				present.Restore();
				if (overlayEnabled && damageEligible)
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

		// Release it then
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

	// Debug viz (FeatureConfiguration.Rendering.DamageRegionOverlay): paints the frame's damage region as a
	// translucent red fill + outline over the fully-repainted frame, so the areas that would be partially
	// repainted are visible.
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
