// #define PRINT_FRAME_TIMES
#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SkiaSharp;
using Uno.Foundation.Logging;
using Uno.UI.Composition;
using Uno.UI.Dispatching;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.Composition;

namespace Microsoft.UI.Composition;

public partial class Compositor
{
	private Dictionary<CompositionAnimation, ICompositionTarget> _runningAnimations = new();
	private Dictionary<ICompositionTarget, int> _runningTargets = new();
	private LinkedList<ColorBrushTransitionState> _backgroundTransitions = new();
#if PRINT_FRAME_TIMES
	private int _frameNumber;
#endif

	static partial void Initialize()
	{
		UnoSkiaApi.Initialize();
	}

	/// <summary>
	/// Whether the scene is rasterized on the CPU rather than by a GPU-backed surface.
	/// Set by the active render backend once its renderer is selected; null until then.
	/// Consulted while recording (e.g. by effect brushes to generate filters the target
	/// surface can rasterize) and temporarily overridden by RenderTargetBitmap.
	/// </summary>
	internal bool? IsSoftwareRenderer { get; set; }

	internal static bool SkipVisualTreePainting { get; set; }

	// Frame drivers (e.g. the wheel decay) are motion too, so "wait until animations settle" must cover them.
	internal bool IsAnimating => _runningAnimations.Count > 0 || FrameStarting is not null;

	internal void RegisterAnimation(CompositionAnimation animation, CompositionObject host)
	{
		// Feed the animation into the innermost active scoped batch so its Completed event waits
		// for the animation to actually stop instead of firing synchronously when batch.End() is
		// called.
		if (animation is KeyFrameAnimation keyFrameAnimation && _scopedBatchStack.Count > 0)
		{
			_scopedBatchStack.Peek().TrackAnimation(keyFrameAnimation);
		}

		if (!animation.IsTrackedByCompositor)
		{
			return;
		}

		// Resolve the CompositionTarget that needs invalidation. For Visuals it's the visual's
		// own target; for a CompositionPropertySet it's the owning Visual's target so animations
		// on `someVisual.Properties.Foo` still get ticked. A property set created standalone via
		// Compositor.CreatePropertySet (e.g. AnimatedIcon's progress property set) must therefore
		// have its Owner set to a Visual — AnimatedIcon does this before starting its animations.
		// Without an owning Visual there is no target and the animation never ticks.
		ICompositionTarget? target = host switch
		{
			Visual visual => visual.CompositionTarget,
			CompositionPropertySet { Owner: Visual ownerVisual } => ownerVisual.CompositionTarget,
			_ => null,
		};

		if (target is null)
		{
			return;
		}

		_runningAnimations.Add(animation, target);

		if (_runningTargets.TryGetValue(target, out int count))
		{
			_runningTargets[target] = count + 1;
		}
		else
		{
			_runningTargets[target] = 1;
			target.RequestNewFrame();
		}

		if (this.Log().IsTraceEnabled())
		{
			this.Log().Trace($"Register running targets {target.GetHashCode():X8}={count} Animations={_runningAnimations.Count}");
		}
	}

	internal void UnregisterAnimation(CompositionAnimation animation, CompositionObject visual)
	{
		if (animation.IsTrackedByCompositor)
		{
			if (_runningAnimations.TryGetValue(animation, out var target))
			{
				_runningAnimations.Remove(animation);

				if (_runningTargets.TryGetValue(target, out int count))
				{
					if (this.Log().IsTraceEnabled())
					{
						this.Log().Trace($"Unregister running targets {target.GetHashCode():X8}={count - 1} Animations={_runningAnimations.Count}");
					}

					if (count == 1)
					{
						_runningTargets.Remove(target);
					}
					else
					{
						_runningTargets[target] = count - 1;
					}
				}
			}
			else
			{
				if (this.Log().IsDebugEnabled())
				{
					this.Log().Debug($"Cannot unregister unknown animation");
				}
			}
		}
	}

	internal void DeactivateBackgroundTransition(BorderVisual visual)
	{
		for (var current = _backgroundTransitions.First; current != null; current = current.Next)
		{
			var transition = current.Value;
			var transitionVisual = transition.Visual;

			if (transitionVisual == visual)
			{
				current.Value = transition with { IsActive = false };
				break;
			}
		}
	}

	internal void RegisterBackgroundTransition(BorderVisual visual, Color fromColor, Color toColor, TimeSpan duration)
	{
		var start = TimestampInTicks;
		var end = start + duration.Ticks;

		for (var current = _backgroundTransitions.First; current != null; current = current.Next)
		{
			var transition = current.Value;
			var transitionVisual = transition.Visual;

			if (transition.Visual == visual)
			{
				// when the background changes when already in a transition, the new transition
				// picks up from where the preexisting transition stopped UNLESS the preexisting
				// transition was inactive (i.e. an animation started during the transition.
				// In that case, just reactivate the preexisting transition.

				if (!transition.IsActive)
				{
					current.Value = transition with { IsActive = true };
					return;
				}

				fromColor = transition.CurrentColor;
				_backgroundTransitions.Remove(current);
				break;
			}
		}

		_backgroundTransitions.AddLast(new ColorBrushTransitionState(visual, fromColor, toColor, start, end, true));
	}

	internal bool TryGetEffectiveBackgroundColor(CompositionSpriteShape shape, out Color color)
	{
		foreach (var transition in _backgroundTransitions)
		{
			if (transition.Visual.IsMyBackgroundShape(shape))
			{
				if (transition.IsActive)
				{
					color = transition.CurrentColor;
					return true;
				}
				else
				{
					break;
				}
			}
		}

		color = default;
		return false;
	}

	/// <summary>
	/// Raised once per recorded frame, before the paint walk, with the timestamp every driver in that
	/// frame must evaluate against.
	/// </summary>
	/// <remarks>
	/// This is the only pre-record per-frame hook in the Skia pipeline. CompositionTarget.Rendering is
	/// raised from a dispatcher continuation *after* the picture is recorded, so a driver on it lands its
	/// writes in the following frame.
	/// </remarks>
	internal event Action<long>? FrameStarting;

	internal bool HasFrameStartingSubscribers => FrameStarting is not null;

	/// <summary>The timestamp the current frame's drivers were evaluated against.</summary>
	internal long CurrentFrameTimestampInTicks { get; private set; }

	/// <summary>Kicks the render loop so a newly-subscribed frame driver gets its first tick.</summary>
	internal static void RequestFrame(Visual visual) => visual.CompositionTarget?.RequestNewFrame();

	/// <summary>Estimated interval between presented frames, for drivers that need a nominal step.</summary>
	internal long FrameIntervalInTicks => _frameDeltaCount >= FrameClockMinSamples
		? MedianFrameDelta()
		: TimeSpan.TicksPerSecond / 60;

	private const int FrameClockWindow = 32;
	private const int FrameClockMinSamples = 8;

	private readonly long[] _frameDeltas = new long[FrameClockWindow];
	private int _frameDeltaIndex;
	private int _frameDeltaCount;
	private long _lastRawFrameTimestamp;
	private long _frameClock;

	/// <summary>
	/// A uniform frame clock for the drivers to evaluate against.
	/// </summary>
	/// <remarks>
	/// Pictures are presented one per vsync, but they are not <i>recorded</i> on a vsync: a record
	/// carries the measure/arrange cost of the tick that produced it, so the raw clock wobbles by
	/// milliseconds around a cadence that is otherwise exact. A driver whose position is a function of
	/// time turns that wobble into v·Δt of position error, which at scroll speeds is a visible fraction
	/// of a frame step — so the drivers get the grid the frames are actually shown on, recovered from
	/// the median record interval, rather than the instant the UI thread happened to get here.
	/// </remarks>
	internal long GetFrameTimestamp(long raw)
	{
		if (_lastRawFrameTimestamp == 0)
		{
			_lastRawFrameTimestamp = raw;
			return _frameClock = raw;
		}

		var delta = raw - _lastRawFrameTimestamp;
		_lastRawFrameTimestamp = raw;

		var period = _frameDeltaCount >= FrameClockMinSamples ? MedianFrameDelta() : 0;

		// A gap far longer than a frame is the loop having been idle between motions, not an interval the
		// display ever ran at. Admitting it would skew the median, which also sets the interval a fling
		// back-dates its launch by — turning a timing artefact into a position error.
		if (period <= 0 || delta < period * 4)
		{
			_frameDeltas[_frameDeltaIndex] = delta;
			_frameDeltaIndex = (_frameDeltaIndex + 1) % FrameClockWindow;
			if (_frameDeltaCount < FrameClockWindow)
			{
				_frameDeltaCount++;
			}
		}

		if (period <= 0)
		{
			return _frameClock = raw;
		}

		var previous = _frameClock;

		// Advance by whole frames, never fewer than one, then correct the sub-period phase. Rounding
		// unconditionally rather than branching once the error passes a threshold is what keeps a period
		// that is a whole multiple of the record rate from flipping sides on jitter, and it re-anchors
		// after an idle gap without a special case.
		var frames = Math.Max(1, (long)Math.Round((raw - _frameClock) / (double)period, MidpointRounding.AwayFromZero));
		_frameClock += frames * period;
		_frameClock += (raw - _frameClock) / 16;

		// Monotone by construction above, asserted here so it stays that way under any future edit: a
		// backward step makes a fling's elapsed time negative, and the curve reads that as "not started"
		// and snaps the content back to where the flick began.
		return _frameClock = Math.Max(_frameClock, previous);
	}

	private long MedianFrameDelta()
	{
		Span<long> sorted = stackalloc long[FrameClockWindow];
		_frameDeltas.AsSpan(0, _frameDeltaCount).CopyTo(sorted);
		sorted = sorted[.._frameDeltaCount];
		sorted.Sort();
		return sorted[_frameDeltaCount / 2];
	}

	internal void RenderRootVisual(SKCanvas canvas, ContainerVisual rootVisual, DamageRegion? damage = null)
	{
		if (rootVisual is null)
		{
			throw new ArgumentNullException(nameof(rootVisual));
		}

		if (FrameStarting is { } frameStarting)
		{
			// One timestamp for the whole frame: TimestampInTicks re-reads the clock on every access, so
			// sampling per driver would give drivers in the same frame different times.
			var frameTimestamp = GetFrameTimestamp(TimestampInTicks);
			CurrentFrameTimestampInTicks = frameTimestamp;
			try
			{
				frameStarting(frameTimestamp);
			}
			catch (Exception e)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error("A frame driver threw; the frame is still recorded.", e);
				}
			}
		}

		foreach (var animation in _runningAnimations.Keys.ToArray())
		{
			try
			{
				animation.RaiseAnimationFrame();
			}
			catch (Exception e)
			{
				// A single animation's expression must never wedge the render loop. Its failure is
				// deterministic, so stop it rather than throwing every frame and stalling rendering.
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error("Stopping animation after an unhandled evaluation error.", e);
				}
				animation.Stop();
			}
		}

#if PRINT_FRAME_TIMES
		var start = Stopwatch.GetTimestamp();
#endif
		// Skip only the paint walk: animations above still tick and transitions/frame
		// re-requests below still run, so the scene stays live without producing pixels.
		if (!SkipVisualTreePainting)
		{
			rootVisual.RenderRootVisual(canvas, null, damage);
		}
#if PRINT_FRAME_TIMES
		var span = Stopwatch.GetElapsedTime(start);
		Console.WriteLine($"Rendered frame {_frameNumber++} in {span.TotalMilliseconds}ms");
#endif

		var transitionsCount = _backgroundTransitions.Count;
		for (var current = _backgroundTransitions.First; current != null; current = current.Next)
		{
			var transition = current.Value;
			var transitionVisual = transition.Visual;

			transitionVisual.InvalidatePaint();

			if (TimestampInTicks >= transition.EndTimestamp)
			{
				_backgroundTransitions.Remove(current);
			}
		}

		if (_runningAnimations.Count > 0 || transitionsCount > 0 || FrameStarting is not null)
		{
			rootVisual.CompositionTarget?.RequestNewFrame();
		}
	}

	partial void InvalidateRenderPartial(Visual visual)
	{
		visual.SetMatrixDirty(); // TODO: only invalidate matrix when specific properties are changed
		visual.InvalidatePaint(); // TODO: only repaint when "dependent" properties are changed
		visual.CompositionTarget?.RequestNewFrame();
	}
}
