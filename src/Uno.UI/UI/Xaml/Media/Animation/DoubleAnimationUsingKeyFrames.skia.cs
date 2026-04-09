using System;
using Windows.UI.Core;

namespace Microsoft.UI.Xaml.Media.Animation
{
	public partial class DoubleAnimationUsingKeyFrames
	{
		private bool ReportEachFrame() => true;

		// Tracks whether animator initialization has been deferred to the next dispatcher tick.
		// This matches WinUI behavior where keyframe values are read at tick time (after layout),
		// not at Begin() time. See CAnimation::UpdateAnimationUsingKeyFrames in animation.cpp.
		private bool _deferredPlayPending;

		partial void OnFrame(IValueAnimator currentAnimator)
		{
			SetValue(currentAnimator.AnimatedValue);
		}

		/// <summary>
		/// On Skia, defers animator initialization to the next dispatcher tick.
		/// This ensures keyframe binding values (e.g., TemplateSettings.MinimalVerticalDelta)
		/// are read after layout has completed, matching WinUI's tick-based value reading.
		/// </summary>
		private void PlayDeferred()
		{
			if (_deferredPlayPending)
			{
				return; // Already waiting for tick
			}

			_deferredPlayPending = true;
			State = TimelineState.Active;

			_ = Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
			{
				_deferredPlayPending = false;

				if (State != TimelineState.Active)
				{
					// Animation was stopped/deactivated before the tick
					return;
				}

				// Now initialize and start animators — keyframe bindings have settled after layout
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
