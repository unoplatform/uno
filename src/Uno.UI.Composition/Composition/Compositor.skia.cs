#nullable enable

using System;
using System.Collections.Generic;
using SkiaSharp;
using Uno.UI.Dispatching;
using Windows.ApplicationModel.Core;

namespace Windows.UI.Composition;

public partial class Compositor
{
	private List<CompositionAnimation> _runningAnimations = new();

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
