#nullable enable

using System;
using System.Collections.Generic;
using SkiaSharp;
using Uno.UI.Dispatching;
using Windows.ApplicationModel.Core;
using Windows.UI;

namespace Windows.UI.Composition;

public partial class Compositor
{
	private List<CompositionAnimation> _runningAnimations = new();
	private List<ColorBrushTransitionState> _backgroundTransitions = new();

	internal bool? IsSoftwareRenderer { get; set; }

	internal void RegisterAnimation(CompositionAnimation animation)
	{
		if (animation.IsTrackedByCompositor)
		{
			_runningAnimations.Add(animation);
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
		for (int i = 0; i < _backgroundTransitions.Count; i++)
		{
			if (_backgroundTransitions[i].Visual == visual)
			{
				_backgroundTransitions[i] = _backgroundTransitions[i] with { IsActive = false };
				break;
			}
		}
	}

	internal void RegisterBackgroundTransition(BorderVisual visual, Color fromColor, Color toColor, TimeSpan duration)
	{
		var start = TimestampInTicks;
		var end = start + duration.Ticks;

		for (int i = 0; i < _backgroundTransitions.Count; i++)
		{
			var transition = _backgroundTransitions[i];
			if (transition.Visual == visual)
			{
				// when the background changes when already in a transition, the new transition
				// picks up from where the preexisting transition stopped UNLESS the preexisting
				// transition was inactive (i.e. an animation started during the transition.
				// In that case, just reactivate the preexisting transition.

				if (!transition.IsActive)
				{
					_backgroundTransitions[i] = transition with { IsActive = true };
					return;
				}

				fromColor = transition.CurrentColor;
				_backgroundTransitions.RemoveAt(i);
				break;
			}
		}

		_backgroundTransitions.Add(new ColorBrushTransitionState(visual, fromColor, toColor, start, end, true));
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

	internal void RenderRootVisual(SKSurface surface, ContainerVisual rootVisual, Action<Visual.PaintingSession, Visual>? postRenderAction)
	{
		if (rootVisual is null)
		{
			throw new ArgumentNullException(nameof(rootVisual));
		}

		foreach (var animation in _runningAnimations.ToArray())
		{
			animation.RaiseAnimationFrame();
		}

		rootVisual.RenderRootVisual(surface, null, postRenderAction);

		RecursiveDispatchAnimationFrames();

		var removedCount = _backgroundTransitions.RemoveAll(transition => TimestampInTicks >= transition.EndTimestamp);

		if (removedCount > 0 || _backgroundTransitions.Count > 0)
		{
			NativeDispatcher.Main.Enqueue(() => CoreApplication.QueueInvalidateRender(rootVisual.CompositionTarget), NativeDispatcherPriority.Idle);
		}
	}

	private void RecursiveDispatchAnimationFrames()
	{
		if (_runningAnimations.Count > 0)
		{
			foreach (var animation in _runningAnimations.ToArray())
			{
				animation.RaiseAnimationFrame();
			}

			if (_runningAnimations.Count > 0)
			{
				NativeDispatcher.Main.Enqueue(RecursiveDispatchAnimationFrames, NativeDispatcherPriority.Idle);
			}
		}
	}

	partial void InvalidateRenderPartial(Visual visual)
	{
		visual.SetMatrixDirty();
		CoreApplication.QueueInvalidateRender(visual.CompositionTarget);
	}
}
