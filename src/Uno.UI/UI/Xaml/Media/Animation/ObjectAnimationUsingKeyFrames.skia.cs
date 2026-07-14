using Windows.UI.Core;

namespace Microsoft.UI.Xaml.Media.Animation
{
	public partial class ObjectAnimationUsingKeyFrames
	{
		// Tracks whether play has been deferred to the next dispatcher tick.
		// This matches WinUI behavior where keyframe values are read at tick time
		// (after layout), not at Begin() time.
		private bool _deferredPlayPending;

		// Invalidates callbacks already queued on the dispatcher: a Begin/Stop/Begin sequence within
		// a single tick would otherwise let the stale callback run PlayImmediate() a second time.
		private int _deferredPlayGeneration;

		/// <summary>
		/// On Skia, defers scheduler creation to the next dispatcher tick.
		/// This ensures keyframe binding values are read after layout has completed,
		/// matching WinUI's tick-based value reading.
		/// </summary>
		private void PlayDeferred()
		{
			if (_deferredPlayPending)
			{
				return;
			}

			_deferredPlayPending = true;

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
					// Paused before the tick: nothing has played yet, so leave the scheduler
					// uncreated. Resume() re-schedules the play.
					return;
				}

				PlayImmediate();
			});
		}

		/// <summary>
		/// Cancels a pending deferred play if one is scheduled.
		/// </summary>
		private void CancelDeferredPlay()
		{
			_deferredPlayPending = false;
			_deferredPlayGeneration++;
		}
	}
}
