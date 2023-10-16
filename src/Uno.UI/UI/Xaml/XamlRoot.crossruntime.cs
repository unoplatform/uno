using System;
using System.Diagnostics;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;

namespace Microsoft.UI.Xaml;

public sealed partial class XamlRoot
{
	private bool _isMeasureWaiting;
	private bool _isArrangeWaiting;
	private bool _isMeasuringOrArranging;
	private bool _renderQueued;

	internal event Action InvalidateRender = () => { };

	internal void InvalidateMeasure()
	{
#if !__WASM__ // TODO: Can we use the same approach on WASM? #8978
		ScheduleInvalidateMeasureOrArrange(invalidateMeasure: true);
#else
		InnerInvalidateMeasure();
#endif
	}

	internal void InvalidateArrange()
	{
#if !__WASM__ // TODO: Can we use the same approach on WASM? #8978
		ScheduleInvalidateMeasureOrArrange(invalidateMeasure: false);
#else
		// We are invalidating both arrange and measure the same way on WASM.
		InnerInvalidateMeasure();
#endif
	}

	internal void QueueInvalidateRender()
	{
		if (!_isMeasuringOrArranging && !_renderQueued)
		{
			_renderQueued = true;

			DispatchQueueRender();
		}
	}
	private void DispatchQueueRender()
	{
		NativeDispatcher.Main.Enqueue(() =>
		{
			if (_isMeasureWaiting || _isArrangeWaiting)
			{
				// If the Render request happends during a UI update pass, and later in the same
				// dispatched iteration a measure is requested, visuals may be in an
				// inconstistent state. We need to skip this request to be rescheduled
				// to run after the pending measure/arrange so the visuals are drawn properly.
				DispatchQueueRender();

				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Trace))
				{
					this.Log().Trace("Delaying Render request");
				}
			}
			else if (_renderQueued)
			{
				_renderQueued = false;
				InvalidateRender();
			}
		});
	}


	internal void ScheduleInvalidateMeasureOrArrange(bool invalidateMeasure)
	{
		if (VisualTree.RootElement is not UIElement rootElement || !(rootElement.IsLoading || rootElement.IsLoaded))
		{
			// The root element is not loaded, no need to schedule anything.
			return;
		}

		if (invalidateMeasure)
		{
			if (_isMeasureWaiting)
			{
				// A measure is already queued
				return;
			}

			_isMeasureWaiting = true;

			if (_isArrangeWaiting)
			{
				// Since an arrange is already queued, no need to
				// schedule something on the dispatcher
				return;
			}
		}
		else
		{
			if (_isArrangeWaiting)
			{
				// An arrange is already queued
				return;
			}

			_isArrangeWaiting = true;

			if (_isMeasureWaiting)
			{
				// Since a measure is already queued, no need to
				// schedule something on the dispatcher
				return;
			}
		}

		NativeDispatcher.Main.Enqueue(RunMeasureAndArrange);
	}

	private void RunMeasureAndArrange()
	{
		if (_isMeasuringOrArranging || VisualTree.RootElement is not UIElement rootElement)
		{
			return; // weird case
		}

		var forMeasure = _isMeasureWaiting;
		var forArrange = _isArrangeWaiting;

		_isMeasureWaiting = false;
		_isArrangeWaiting = false;
		try
		{
			_isMeasuringOrArranging = true;

			var sw = Stopwatch.StartNew();

			if (forMeasure)
			{
				rootElement.Measure(Bounds.Size);
			}

			if (forArrange)
			{
				rootElement.Arrange(Bounds);
				InvalidateRender();
			}

			sw.Stop();

			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"DispatchInvalidateMeasure: {sw.Elapsed}");
			}
		}
		finally
		{
			_isMeasuringOrArranging = false;
		}
	}
}
