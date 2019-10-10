using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Windows.UI.Xaml.Media.Animation
{
	internal abstract class CPUBoundFloatAnimator : IValueAnimator
    {
		private readonly float _from;
		private readonly float _to;
		private readonly Stopwatch _elapsed;

		private float _currentValue;
		private IEasingFunction _easing = LinearEase.Instance;
		private bool _isDisposed;
		private bool _isDelaying;

		protected CPUBoundFloatAnimator(float from, float to)
		{
			_from = from;
			_to = to;

			_elapsed = new Stopwatch();
		}

		/// <inheritdoc />
		public event EventHandler Update;

		/// <inheritdoc />
		public event EventHandler AnimationEnd;

		/// <inheritdoc />
		public event EventHandler AnimationPause;

		/// <inheritdoc />
		public event EventHandler AnimationCancel;


		/// <inheritdoc />
		public object AnimatedValue => _currentValue;

		/// <inheritdoc />
		public long CurrentPlayTime { get; set; }

		/// <inheritdoc />
		public bool IsRunning { get; private set; }

		/// <inheritdoc />
		public long StartDelay { get; set; }

		/// <inheritdoc />
		public long Duration { get; private set; }
		/// <inheritdoc />
		public void SetDuration(long duration) => Duration = duration;

		/// <inheritdoc />
		public void SetEasingFunction(IEasingFunction easingFunction) => _easing = easingFunction ?? LinearEase.Instance;

		/// <inheritdoc />
		public void Start()
		{
			CheckDisposed();

			ConfigureStartInterval(elapsed: 0);

			IsRunning = true;
			_elapsed.Restart();
			EnableFrameReporting();
		}

		/// <inheritdoc />
		public void Pause()
		{
			CheckDisposed();

			IsRunning = false;
			_elapsed.Stop();
			DisableFrameReporting();

			AnimationPause?.Invoke(this, EventArgs.Empty);
		}

		/// <inheritdoc />
		public void Resume()
		{
			CheckDisposed();

			ConfigureStartInterval(_elapsed.ElapsedMilliseconds);

			IsRunning = true;
			_elapsed.Start();
			EnableFrameReporting();
		}

		/// <inheritdoc />
		public void Cancel()
		{
			CheckDisposed();

			IsRunning = false;
			_elapsed.Stop();
			DisableFrameReporting();

			AnimationCancel?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>
		/// Requests to implementors to start ticking
		/// </summary>
		protected abstract void EnableFrameReporting();

		/// <summary>
		/// Requests to implementors to stop ticking
		/// </summary>
		protected abstract void DisableFrameReporting();

		/// <summary>
		/// Request to implementors to delay the next frame by <paramref name="delayMs"/> milliseconds since we are witing for the <see cref="StartDelay"/>.
		/// </summary>
		/// <remarks>You can still report freams using the <see cref="OnFrame"/>, they will be ignored.</remarks>
		/// <param name="delayMs">The delay to wait before</param>
		protected virtual void SetStartFrameDelay(long delayMs)
		{
		}

		/// <summary>
		/// Request to implementors to configure its interval between frame for animations
		/// </summary>
		protected virtual void SetAnimationFramesInterval()
		{
		}

		protected void OnFrame(object sender, object e)
		{
			var elapsed = _elapsed.ElapsedMilliseconds;

			if (elapsed < StartDelay)
			{
				// We got an invalid tick ... handle it gracefully

				// Reconfigure the start interval to tick only at the end of the start delay
				ConfigureStartInterval(elapsed);

				CurrentPlayTime = 0;
				_currentValue = _from;
			}
			else if (elapsed >= Duration)
			{
				IsRunning = false;
				DisableFrameReporting();
				_elapsed.Stop();

				CurrentPlayTime = Duration;
				_currentValue = _to;

				Update?.Invoke(this, EventArgs.Empty);
				AnimationEnd?.Invoke(this, EventArgs.Empty);
			}
			else
			{
				if (_isDelaying)
				{
					ConfigureAnimationInterval();
				}

				var frame = elapsed - StartDelay;
				var value = (float)_easing.Ease(frame, _from, _to, Duration);

				CurrentPlayTime = elapsed;
				_currentValue = value;

				Update?.Invoke(this, EventArgs.Empty);
			}
		}

		private void ConfigureStartInterval(long elapsed)
		{
			if (StartDelay > 0)
			{
				_isDelaying = true;
				SetStartFrameDelay(StartDelay - elapsed);
			}
			else
			{
				ConfigureAnimationInterval();
			}
		}

		private void ConfigureAnimationInterval()
		{
			_isDelaying = false;
			SetAnimationFramesInterval();
		}

		private void CheckDisposed()
		{
			if (_isDisposed)
			{
				throw new ObjectDisposedException(nameof(CPUBoundFloatAnimator));
			}
		}

		/// <inheritdoc />
		public void Dispose()
		{
			_isDisposed = true;

			IsRunning = false;
			DisableFrameReporting();
			_elapsed.Stop();
		}
	}
}
