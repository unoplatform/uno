using Windows.UI.Core;

namespace Microsoft.UI.Xaml.Media.Animation
{
	public partial class ObjectAnimationUsingKeyFrames
	{
		// Tracks whether play has been deferred to the next dispatcher tick.
		// This matches WinUI behavior where keyframe values are read at tick time
		// (after layout), not at Begin() time.
		private bool _deferredPlayPending;

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

			_ = Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
			{
				_deferredPlayPending = false;

				if (State != TimelineState.Active)
				{
					// Animation was stopped/deactivated before the tick
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
		}
	}
}
