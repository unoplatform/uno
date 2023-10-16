using System;
using System.Collections.Generic;
using System.Diagnostics;
using Uno.Disposables;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;

namespace Windows.UI.Xaml.Media.Animation
{
	internal class DiscreteFloatValueAnimator : IValueAnimator
	{
		private readonly SerialDisposable _scheduledFrame = new SerialDisposable();
		private readonly Stopwatch _watch = new Stopwatch();

		private float _from;
		private float _to;

		public object AnimatedValue { get; private set; }

		public long CurrentPlayTime { get; set; }

		public long Duration { get; private set; }

		public bool IsRunning { get; private set; }

		public long StartDelay { get; set; }

		public event EventHandler AnimationCancel;
		public event EventHandler AnimationEnd;
		public event EventHandler AnimationPause;
#pragma warning disable 67
		public event EventHandler AnimationFailed;
#pragma warning restore 67
		public event EventHandler Update;

		public DiscreteFloatValueAnimator(float from, float to)
		{
			_from = from;
			_to = to;

			AnimatedValue = from;
		}

		public void Cancel()
		{
			IsRunning = false;

			_scheduledFrame.Disposable = null;
			_watch.Reset();

			AnimationCancel?.Invoke(this, EventArgs.Empty);
		}

		public void Dispose()
		{
			IsRunning = false;

			_scheduledFrame.Dispose();
			_watch.Reset();
		}

		public void Pause()
		{
			IsRunning = false;

			_scheduledFrame.Disposable = null;
			_watch.Stop();

			AnimationPause?.Invoke(this, EventArgs.Empty);
		}

		public void Resume()
		{
			var elapsed = _watch.ElapsedMilliseconds;
			ScheduleCompleted(elapsed);
			_watch.Start();

			IsRunning = true;
		}

		private void ScheduleCompleted(long elapsed)
		{
			_scheduledFrame.Disposable = Uno.UI.Dispatching.NativeDispatcher.Main.EnqueueOperation(
				async () =>
				{
					await Task.Delay(TimeSpan.FromMilliseconds(Duration - elapsed));
					Complete();
				}
			);
		}

		public void SetDuration(long duration)
		{
			Duration = duration;
		}

		public void SetEasingFunction(IEasingFunction easingFunction)
		{
			// We don't use any easing function
		}

		public void Start()
		{
			Update?.Invoke(this, EventArgs.Empty);
			ScheduleCompleted(Duration);
			_watch.Start();

			IsRunning = true;
		}

		private void Complete()
		{
			AnimatedValue = _to;

			Update?.Invoke(this, EventArgs.Empty);
			AnimationEnd?.Invoke(this, EventArgs.Empty);
		}
	}
}
