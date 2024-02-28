#nullable enable

using System;
using System.Collections.Generic;
using SkiaSharp;
using Windows.ApplicationModel.Core;

namespace Microsoft.UI.Composition;

public partial class Compositor
{
	private List<CompositionAnimation> _runningAnimations = new();

	internal bool? IsSoftwareRenderer { get; set; }

	internal void TrackKeyFrameAnimation(CompositionAnimation animation)
	{
		if (animation.IsTrackedByCompositor)
		{
			_runningAnimations.Add(animation);
		}
	}

	internal void ForgetKeyFrameAnimation(CompositionAnimation animation)
	{
		if (animation.IsTrackedByCompositor)
		{
			_runningAnimations.Remove(animation);
		}
	}

	internal void RenderRootVisual(SKSurface surface, ContainerVisual rootVisual)
	{
		if (rootVisual is null)
		{
			throw new ArgumentNullException(nameof(rootVisual));
		}

		foreach (var animation in _runningAnimations.ToArray())
		{
			animation.RaiseAnimationFrame();
		}

		rootVisual.RenderRootVisual(surface);

		if (_runningAnimations.Count > 0)
		{
			InvalidateRender(rootVisual);
		}
	}

	partial void InvalidateRenderPartial(Visual visual)
	{
		visual.SetMatrixDirty();
		CoreApplication.QueueInvalidateRender(visual.CompositionTarget);
	}
}
