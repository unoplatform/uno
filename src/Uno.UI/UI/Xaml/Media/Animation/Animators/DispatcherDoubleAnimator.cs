using System;
using System.Linq;

namespace Windows.UI.Xaml.Media.Animation
{
	internal sealed class DispatcherDoubleAnimator : CPUBoundAnimator<double>
	{
		public const int DefaultFrameRate = 30;

		private readonly int _frameRate;
		private readonly DispatcherTimer _timer;

		public DispatcherDoubleAnimator(double from, double to, int frameRate = DefaultFrameRate)
			: base(from, to)
		{
			_frameRate = frameRate;
			_timer = new DispatcherTimer();
			_timer.Tick += OnFrame;
		}

		protected override void EnableFrameReporting() => _timer.Start();
		protected override void DisableFrameReporting() => _timer.Stop();

		protected override void SetStartFrameDelay(long delayMs) => _timer.Interval = TimeSpan.FromMilliseconds(delayMs);
		protected override void SetAnimationFramesInterval() => _timer.Interval = TimeSpan.FromSeconds(1d / _frameRate);

		protected override double GetUpdatedValue(long frame, double from, double to) => (float)_easing.Ease(frame, from, to, Duration);
	}
}
