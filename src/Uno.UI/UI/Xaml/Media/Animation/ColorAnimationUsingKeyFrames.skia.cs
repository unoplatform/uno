using System;
using Windows.UI.Core;

namespace Microsoft.UI.Xaml.Media.Animation
{
	partial class ColorAnimationUsingKeyFrames
	{
		private bool ReportEachFrame() => true;

		private bool _deferredPlayPending;

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

			_ = Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
			{
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
		}
	}
}
