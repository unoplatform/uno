// #define PRINT_FRAME_TIMES
#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Uno.Foundation.Logging;
using Uno.UI.Composition;
using Uno.UI.Dispatching;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.Composition;

namespace Microsoft.UI.Composition;

public partial class Compositor
{
	private Dictionary<CompositionAnimation, RunningAnimation> _runningAnimations = new();
	private Dictionary<ICompositionTarget, int> _runningTargets = new();

	// Animations whose host isn't connected to a composition target: started while detached, or unbounded
	// animations whose visual left the tree. Held weakly so a detached subtree stays collectable; they resume
	// once the host is attached again.
	private List<(WeakReference<CompositionAnimation> Animation, WeakReference<CompositionObject> Host)>? _detachedAnimations;
	private int _visualTreeVersion;
	private int _validatedVisualTreeVersion;
	private LinkedList<ColorBrushTransitionState> _backgroundTransitions = new();
#if PRINT_FRAME_TIMES
	private int _frameNumber;
#endif

	/// <summary>
	/// Whether the scene is rasterized on the CPU rather than by a GPU-backed surface.
	/// Set by the active render backend once its renderer is selected; null until then.
	/// Consulted while recording (e.g. by effect brushes to generate filters the target
	/// surface can rasterize) and temporarily overridden by RenderTargetBitmap.
	/// </summary>
	internal bool? IsSoftwareRenderer { get; set; }

	internal static bool SkipVisualTreePainting { get; set; }

	internal bool IsAnimating => _runningAnimations.Count > 0;

	private readonly record struct RunningAnimation(ICompositionTarget Target, CompositionObject Host);

	/// <summary>Called whenever the composition tree's shape changes, so animation targets get re-validated on the next frame.</summary>
	internal void OnVisualTreeChanged() => _visualTreeVersion++;

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

		if (GetAnimationTarget(host) is { } target)
		{
			AddRunningAnimation(animation, host, target);
		}
		else
		{
			// Not connected yet: start ticking once the host is attached instead of never.
			AddDetachedAnimation(animation, host);
		}
	}

	// Resolve the CompositionTarget that needs invalidation. For Visuals it's the visual's
	// own target; for a CompositionPropertySet it's the owning Visual's target so animations
	// on `someVisual.Properties.Foo` still get ticked. A property set created standalone via
	// Compositor.CreatePropertySet (e.g. AnimatedIcon's progress property set) must therefore
	// have its Owner set to a Visual — AnimatedIcon does this before starting its animations.
	// Without an owning Visual there is no target and the animation never ticks.
	private static ICompositionTarget? GetAnimationTarget(CompositionObject host) => host switch
	{
		Visual visual => visual.CompositionTarget,
		CompositionPropertySet { Owner: Visual ownerVisual } => ownerVisual.CompositionTarget,
		_ => null,
	};

	private void AddRunningAnimation(CompositionAnimation animation, CompositionObject host, ICompositionTarget target)
	{
		_runningAnimations.Add(animation, new(target, host));

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

	private void AddDetachedAnimation(CompositionAnimation animation, CompositionObject host)
		=> (_detachedAnimations ??= new()).Add((new(animation), new(host)));

	internal void UnregisterAnimation(CompositionAnimation animation, CompositionObject visual)
	{
		if (!animation.IsTrackedByCompositor)
		{
			return;
		}

		if (_runningAnimations.TryGetValue(animation, out var running))
		{
			RemoveRunningAnimation(animation, running.Target);
		}
		else if (!RemoveDetachedAnimation(animation))
		{
			if (this.Log().IsDebugEnabled())
			{
				this.Log().Debug($"Cannot unregister unknown animation");
			}
		}
	}

	private void RemoveRunningAnimation(CompositionAnimation animation, ICompositionTarget target)
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

	private bool RemoveDetachedAnimation(CompositionAnimation animation)
	{
		if (_detachedAnimations is not { } detached)
		{
			return false;
		}

		for (var i = 0; i < detached.Count; i++)
		{
			if (detached[i].Animation.TryGetTarget(out var candidate) && ReferenceEquals(candidate, animation))
			{
				detached.RemoveAt(i);
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Re-validates where running animations tick after the tree changed. An unbounded animation whose host left the
	/// tree (e.g. it sits below the root of a removed subtree, whose own animations are stopped on removal) would
	/// otherwise keep requesting frames forever and keep the detached subtree alive through <see cref="_runningAnimations"/>.
	/// Finite animations keep running: they end on their own and may be awaited through a scoped batch.
	/// </summary>
	private void RefreshAnimationTargets()
	{
		List<CompositionAnimation>? retarget = null;
		foreach (var (animation, running) in _runningAnimations)
		{
			var target = GetAnimationTarget(running.Host);
			if (!ReferenceEquals(target, running.Target)
				&& (target is not null || animation is KeyFrameAnimation { IterationBehavior: AnimationIterationBehavior.Forever }))
			{
				(retarget ??= new()).Add(animation);
			}
		}

		if (retarget is not null)
		{
			foreach (var animation in retarget)
			{
				var running = _runningAnimations[animation];
				RemoveRunningAnimation(animation, running.Target);

				if (GetAnimationTarget(running.Host) is { } target)
				{
					AddRunningAnimation(animation, running.Host, target);
				}
				else
				{
					AddDetachedAnimation(animation, running.Host);
				}
			}
		}

		if (_detachedAnimations is { Count: > 0 } detached)
		{
			for (var i = detached.Count - 1; i >= 0; i--)
			{
				if (!detached[i].Animation.TryGetTarget(out var animation) || !detached[i].Host.TryGetTarget(out var host))
				{
					detached.RemoveAt(i);
				}
				else if (GetAnimationTarget(host) is { } target && !_runningAnimations.ContainsKey(animation))
				{
					// An instance already running on another target stays parked (see TODO Uno #24102).
					detached.RemoveAt(i);
					AddRunningAnimation(animation, host, target);
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

	// UNO_LOG_FRAME_PHASES=1 splits the record phase into animation-tick vs visual-walk time (benchmarking);
	// pairs with the frame-level [frame-phases] line in CompositionTarget.
	private static readonly bool _logRecordPhases =
		Environment.GetEnvironmentVariable("UNO_LOG_FRAME_PHASES") is "1" or "true";
	private static long _recAnimationTicks, _recWalkTicks;
	private static int _recPhaseFrames;

	internal void RenderRootVisual(Uno.UI.Composition.Drawing.IDrawingSession drawingSession, ContainerVisual rootVisual, DamageRegion? damage = null)
	{
		if (rootVisual is null)
		{
			throw new ArgumentNullException(nameof(rootVisual));
		}

		if (_validatedVisualTreeVersion != _visualTreeVersion)
		{
			_validatedVisualTreeVersion = _visualTreeVersion;
			RefreshAnimationTargets();
		}

		var recPhaseT0 = _logRecordPhases ? Stopwatch.GetTimestamp() : 0;
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
		var recPhaseT1 = _logRecordPhases ? Stopwatch.GetTimestamp() : 0;
		// Skip only the paint walk: animations above still tick and transitions/frame
		// re-requests below still run, so the scene stays live without producing pixels.
		if (!SkipVisualTreePainting)
		{
			rootVisual.RenderRootVisual(drawingSession, null, damage);
		}
		if (_logRecordPhases)
		{
			var recPhaseT2 = Stopwatch.GetTimestamp();
			_recAnimationTicks += recPhaseT1 - recPhaseT0;
			_recWalkTicks += recPhaseT2 - recPhaseT1;
			if (++_recPhaseFrames >= 60)
			{
				Console.WriteLine($"[record-phases] animations={_recAnimationTicks * 1000.0 / Stopwatch.Frequency / _recPhaseFrames:F1}ms walk={_recWalkTicks * 1000.0 / Stopwatch.Frequency / _recPhaseFrames:F1}ms (avg/frame, {_runningAnimations.Count} running animations)");
				_recAnimationTicks = _recWalkTicks = 0;
				_recPhaseFrames = 0;
			}
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

		if (_runningAnimations.Count > 0 || transitionsCount > 0)
		{
			rootVisual.CompositionTarget?.RequestNewFrame();
		}
	}

	partial void InvalidateRenderPartial(Visual visual, bool translationOnly)
	{
		visual.SetMatrixDirty(); // TODO: only invalidate matrix when specific properties are changed

		// A translation keeps the recording valid: it is made in the visual's own space and replayed under the new
		// matrix, and the paint walk damages both placements of a moved visual. SetMatrixDirty above already dropped
		// the ancestor caches holding the old placement. Scale and rotation do not qualify: brushes and geometry are
		// rasterized at the scale they are drawn under.
		if (!translationOnly)
		{
			visual.InvalidatePaint(); // TODO: only repaint when "dependent" properties are changed
		}

		visual.CompositionTarget?.RequestNewFrame();
	}
}
