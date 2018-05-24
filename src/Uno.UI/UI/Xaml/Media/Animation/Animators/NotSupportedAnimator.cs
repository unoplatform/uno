#pragma warning disable CS0067

using System;
using System.Collections.Generic;
using System.Text;


namespace Windows.UI.Xaml.Media.Animation
{
	internal sealed class NotSupportedAnimator : IValueAnimator, IDisposable
	{
		public NotSupportedAnimator()
		{
		}

		public void Start()
		{
			throw new NotSupportedException();
		}

		public void Resume()
		{
			throw new NotSupportedException();
		}
		public void Pause()
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Cancel this instance.
		/// </summary>
		public void Cancel()
		{
			IsRunning = false;
		}

		/// <summary>
		/// Stop this instance.
		/// </summary>
		private void Stop()
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Sets the duration.
		/// </summary>
		/// <param name="duration">Duration.</param>
		public void SetDuration(long duration)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Sets the duration.
		/// </summary>
		/// <param name="duration">Duration.</param>
		public void SetEasingFunction(IEasingFunction easingFunction)
		{
			throw new NotSupportedException();
		}

		public event EventHandler AnimationEnd;

		public event EventHandler AnimationPause;

		public event EventHandler AnimationCancel;

		public event EventHandler Update;

		public long StartDelay { get; set; }
		public bool IsRunning { get; set; }
		public long CurrentPlayTime { get; set; }

		public object AnimatedValue { get; set; }
		public long Duration { get; internal set; }

		public void Dispose()
		{
			throw new NotSupportedException();
		}

	}

}

