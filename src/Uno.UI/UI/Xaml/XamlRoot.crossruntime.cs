using System;
using Microsoft.UI.Xaml.Media;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;
using Uno.UI.Xaml.Core;

namespace Microsoft.UI.Xaml;

public sealed partial class XamlRoot
{
	private bool _renderQueued;

	internal event Action InvalidateRender = () => { };

	internal void InvalidateMeasure()
	{
		VisualTree.RootElement.InvalidateMeasure();
#if UNO_HAS_ENHANCED_LIFECYCLE
		CoreServices.RequestAdditionalFrame();
#endif
	}

	internal void InvalidateArrange()
	{
		VisualTree.RootElement.InvalidateArrange();
#if UNO_HAS_ENHANCED_LIFECYCLE
		CoreServices.RequestAdditionalFrame();
#endif
	}

	internal void RaiseInvalidateRender()
	{
		InvalidateRender();
	}

	internal void QueueInvalidateRender()
	{
		if (
			// Don't requeue if there's already a render request queued
			!_renderQueued
			&&
			(
				// If NativeDispatcher.Rendering is not called
				// synchronously, then we can queue the render
				!NativeDispatcher.Main.UseSynchronousDispatchRendering

				// Don't queue if continuous rendering is enabled
				|| !CompositionTarget.IsRenderingActive
			)
		)
		{
			_renderQueued = true;

			DispatchQueueRender();
		}
	}

	private void DispatchQueueRender()
	{
		NativeDispatcher.Main.Enqueue(() =>
		{
			if (_renderQueued)
			{
				_renderQueued = false;

				InvalidateRender();
			}
		}, NativeDispatcherPriority.Idle); // Idle is necessary to avoid starving the Normal queue on some platforms (specifically skia/android), otherwise When_Child_Empty_List times out
	}

	/// <summary>
	/// This is used to adjust the sizing of managed vs. native elements on GTK, as it does not have built-in support for fractional scaling
	/// which is available on Windows. We can still emulate this by up-scaling native GTK controls by the ratio between the actual scale 
	/// and the emulated scale.
	/// </summary>
	internal double FractionalScaleAdjustment => RasterizationScale / Math.Truncate(RasterizationScale);
}
