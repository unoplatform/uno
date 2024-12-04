using System;
using System.Linq;

namespace Microsoft.UI.Xaml.Media.Animation
{
	internal abstract class DispatcherAnimator<T> : CPUBoundAnimator<T> where T : struct
	{
		public const int DefaultFrameRate = 30;

		private readonly int _frameRate;
		private readonly DispatcherTimer _timer;

		public DispatcherAnimator(T from, T to, int frameRate = DefaultFrameRate)
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

		protected abstract override T GetUpdatedValue(long frame, T from, T to);
	}
}
