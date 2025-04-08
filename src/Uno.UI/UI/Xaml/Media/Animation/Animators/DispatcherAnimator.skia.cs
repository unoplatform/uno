using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Authentication.ExtendedProtection;
using Microsoft.UI.Dispatching;
using Windows.UI.Core;

namespace Microsoft.UI.Xaml.Media.Animation
{
	internal abstract class DispatcherAnimator<T> : CPUBoundAnimator<T> where T : struct
	{
		public const int DefaultFrameRate = 60;

		private Stopwatch _watch = new Stopwatch();
		private TimeSpan? _delay;

		public DispatcherAnimator(T from, T to, int frameRate = 0)
			: base(from, to)
		{
		}

		protected override void EnableFrameReporting() => CompositionTarget.Rendering += OnTargetFrame;
		protected override void DisableFrameReporting() => CompositionTarget.Rendering -= OnTargetFrame;

		private void OnTargetFrame(object sender, object args)
		{
			if (_delay != null)
			{
				if (_watch.Elapsed < _delay)
				{
					return;
				}
				else
				{
					_delay = null;
					_watch.Stop();
					_watch.Reset();
				}
			}

			OnFrame(sender, args);
		}

		protected override void SetStartFrameDelay(long delayMs)
		{
			_delay = TimeSpan.FromMicroseconds(delayMs);
			_watch.Restart();
		}

		protected abstract override T GetUpdatedValue(long frame, T from, T to);
	}
}
