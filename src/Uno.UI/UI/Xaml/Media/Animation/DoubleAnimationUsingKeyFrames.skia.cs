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

		// Invalidates callbacks already queued on the dispatcher: a Begin/Stop/Begin sequence within
		// a single tick would otherwise let the stale callback run PlayImmediate() a second time.
		private int _deferredPlayGeneration;

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

			// Active has to be set now, not in PlayImmediate(): the parent Storyboard must see this child
			// as running, and Pause() ignores a Stopped timeline, so a Begin(); Pause(); pair would
			// otherwise silently start the animation anyway on the tick. Pause/Resume/Seek all tolerate
			// the null animators of that window.
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
					// Paused before the tick: nothing has played yet, so leave the animators
					// uninitialized. Resume() re-schedules the play.
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
			_deferredPlayGeneration++;
		}
	}
}
