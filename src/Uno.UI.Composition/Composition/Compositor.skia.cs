// #define PRINT_FRAME_TIMES
#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using SkiaSharp;
using Windows.ApplicationModel.Core;
using Windows.UI;

namespace Microsoft.UI.Composition;

public partial class Compositor
{
	private List<CompositionAnimation> _runningAnimations = new();
	private LinkedList<ColorBrushTransitionState> _backgroundTransitions = new();
#if PRINT_FRAME_TIMES
	private int _frameNumber;
#endif

	static partial void Initialize()
	{
		UnoSkiaApi.Initialize();
	}

	internal bool? IsSoftwareRenderer { get; set; }

	internal void RegisterAnimation(CompositionAnimation animation, CompositionObject visual)
	{
		if (animation.IsTrackedByCompositor)
		{
			_runningAnimations.Add(animation);
			if (visual is Visual { CompositionTarget: { } target })
			{
				CoreApplication.QueueInvalidateRender(target);
			}
		}
	}

	internal void UnregisterAnimation(CompositionAnimation animation)
	{
		if (animation.IsTrackedByCompositor)
		{
			_runningAnimations.Remove(animation);
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

	internal void RenderRootVisual(SKCanvas canvas, ContainerVisual rootVisual)
	{
		if (rootVisual is null)
		{
			throw new ArgumentNullException(nameof(rootVisual));
		}

		foreach (var animation in _runningAnimations.ToArray())
		{
			animation.RaiseAnimationFrame();
		}

#if PRINT_FRAME_TIMES
		var start = Stopwatch.GetTimestamp();
#endif
		rootVisual.RenderRootVisual(canvas, null);
#if PRINT_FRAME_TIMES
		var span = Stopwatch.GetElapsedTime(start);
		Console.WriteLine($"Rendered frame {_frameNumber++} in {span.TotalMilliseconds}ms");
#endif

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

		if (_runningAnimations.Count > 0)
		{
			CoreApplication.QueueInvalidateRender(rootVisual.CompositionTarget);
		}
	}

	partial void InvalidateRenderPartial(Visual visual)
	{
		visual.SetMatrixDirty(); // TODO: only invalidate matrix when specific properties are changed
		visual.InvalidatePaint(); // TODO: only repaint when "dependent" properties are changed
		CoreApplication.QueueInvalidateRender(visual.CompositionTarget);
	}
}
