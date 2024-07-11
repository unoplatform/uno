#nullable enable

using System;
using System.Collections.Generic;
using SkiaSharp;
using Uno.UI.Dispatching;
using Windows.ApplicationModel.Core;
using Windows.UI;

namespace Microsoft.UI.Composition;

internal sealed class ColorBrushTransitionState
{
	internal ColorBrushTransitionState(BorderVisual visual, Color fromColor, Color toColor, long startTimestamp, long endTimestamp)
	{
		Visual = visual;
		FromColor = fromColor;
		ToColor = toColor;
		StartTimestamp = startTimestamp;
		EndTimestamp = endTimestamp;
	}

	internal BorderVisual Visual { get; }
	internal Color FromColor { get; }
	internal Color ToColor { get; }
	internal long StartTimestamp { get; }
	internal long EndTimestamp { get; }

	internal Color CurrentColor
	{
		get
		{
			var progress = (Visual.Compositor.TimestampInTicks - StartTimestamp) / (double)(EndTimestamp - StartTimestamp);

			var a = Lerp(FromColor.A, ToColor.A, progress);
			var r = Lerp(FromColor.R, ToColor.R, progress);
			var g = Lerp(FromColor.G, ToColor.G, progress);
			var b = Lerp(FromColor.B, ToColor.B, progress);

			return new Color(a, r, g, b);

			byte Lerp(byte start, byte end, double progress)
				=> (byte)(((end - start) * progress) + start);
		}
	}
}

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

	internal void RegisterBackgroundTransition(BorderVisual visual, Color fromColor, Color toColor, TimeSpan duration)
	{
		var start = TimestampInTicks;
		var end = start + duration.Ticks;
		_backgroundTransitions.Add(new ColorBrushTransitionState(visual, fromColor, toColor, start, end));
	}

	internal bool TryGetEffectiveBackgroundColor(CompositionSpriteShape shape, out Color color)
	{
		foreach (var transition in _backgroundTransitions)
		{
			if (transition.Visual.BackgroundShape == shape)
			{
				color = transition.CurrentColor;
				return true;
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

		_backgroundTransitions.RemoveAll(transition => TimestampInTicks >= transition.EndTimestamp);

		if (_backgroundTransitions.Count > 0)
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
