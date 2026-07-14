using Windows.UI.Core;

namespace Microsoft.UI.Xaml.Media.Animation
{
	partial class ColorAnimationUsingKeyFrames
	{
		private bool ReportEachFrame() => true;

		private bool _deferredPlayPending;

		// Invalidates callbacks already queued on the dispatcher: a Begin/Stop/Begin sequence within
		// a single tick would otherwise let the stale callback run PlayImmediate() a second time.
		private int _deferredPlayGeneration;

		partial void OnFrame(IValueAnimator currentAnimator)
		{
			SetValue(currentAnimator.AnimatedValue);
		}

		private void PlayDeferred()
		{
			if (_deferredPlayPending)
			{
				return;
			}

			_deferredPlayPending = true;
			State = TimelineState.Active;

			var generation = ++_deferredPlayGeneration;

			_ = Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
			{
				if (_deferredPlayGeneration != generation)
				{
					// A Stop/Deactivate/SkipToFill cycle invalidated this callback
					return;
				}

				_deferredPlayPending = false;

				if (State != TimelineState.Active)
				{
					return;
				}

				PlayImmediate();
			});
		}

		private void CancelDeferredPlay()
		{
			_deferredPlayPending = false;
			_deferredPlayGeneration++;
		}
	}
}
