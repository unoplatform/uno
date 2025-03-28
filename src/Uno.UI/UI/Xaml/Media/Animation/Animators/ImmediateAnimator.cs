using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Media.Animation
{
	internal sealed class ImmediateAnimator<T> : IValueAnimator, IDisposable where T : struct
	{
		private T _to;
		private long _duration;

		public ImmediateAnimator(T to)
		{
			_to = to;

			StartDelay = 0;
			CurrentPlayTime = 0;
		}

		public void Start()
		{
			AnimatedValue = _to;
			Update?.Invoke(this, EventArgs.Empty);
			AnimationEnd(this, EventArgs.Empty);
			CurrentPlayTime = _duration;
		}

		public void Resume()
		{
			Start();
		}

		public void Pause()
		{
			AnimationPause?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>
		/// Cancel this instance.
		/// </summary>
		public void Cancel()
		{
			AnimationCancel?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>
		/// Sets the duration.
		/// </summary>
		/// <param name="duration">Duration.</param>
		public void SetDuration(long duration)
		{
			_duration = duration;
		}

		/// <summary>
		/// Sets the duration.
		/// </summary>
		/// <param name="duration">Duration.</param>
		public void SetEasingFunction(IEasingFunction easingFunction)
		{
		}

		public event EventHandler AnimationEnd;

		public event EventHandler AnimationPause;

		public event EventHandler AnimationCancel;

#pragma warning disable 67
		public event EventHandler AnimationFailed;
#pragma warning restore 67

		public event EventHandler Update;

		public long StartDelay { get; set; }
		public bool IsRunning { get; set; }
		public long CurrentPlayTime { get; set; }

		public object AnimatedValue { get; set; }
		public long Duration { get; internal set; }

		public void Dispose()
		{
			Update = null;
			AnimationEnd = null;
			AnimationCancel = null;
		}
	}
}

