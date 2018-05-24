using System;
using System.Collections.Generic;
using System.Text;
using Android.Animation;
using Android.Views.Animations;

namespace Windows.UI.Xaml.Media.Animation
{
    internal sealed class NativeValueAnimatorAdapter : IValueAnimator
    {
		private readonly Dictionary<EventHandler, EventHandler<ValueAnimator.AnimatorUpdateEventArgs>> _updateHandlers = new Dictionary<EventHandler, EventHandler<ValueAnimator.AnimatorUpdateEventArgs>>();
		private readonly Dictionary<EventHandler, EventHandler<Animator.AnimationPauseEventArgs>> _pauseHandlers = new Dictionary<EventHandler, EventHandler<Animator.AnimationPauseEventArgs>>();

		private readonly ValueAnimator _adaptee;

		public NativeValueAnimatorAdapter(ValueAnimator adaptee)
		{
			_adaptee = adaptee;
		}

		/// <inheritdoc />
		public void Dispose() => _adaptee.Dispose();

		/// <inheritdoc />
		public event EventHandler Update
		{
			add
			{
				EventHandler<ValueAnimator.AnimatorUpdateEventArgs> handler = (snd, e) => value(snd, e);
				if (_updateHandlers.TryAdd(value, handler))
				{
					_adaptee.Update += handler;
				}
			}
			remove
			{
				if (_updateHandlers.TryGetValue(value, out var handler))
				{
					_adaptee.Update -= handler;
					_updateHandlers.Remove(value);
				}
			}
		}

		/// <inheritdoc />
		public event EventHandler AnimationPause
		{
			add
			{
				EventHandler<Animator.AnimationPauseEventArgs> handler = (snd, e) => value(snd, e);
				if (_pauseHandlers.TryAdd(value, handler))
				{
					_adaptee.AnimationPause += handler;
				}
			}
			remove
			{
				if (_pauseHandlers.TryGetValue(value, out var handler))
				{
					_adaptee.AnimationPause -= handler;
					_updateHandlers.Remove(value);
				}
			}
		}

		/// <inheritdoc />
		public event EventHandler AnimationEnd
		{
			add => _adaptee.AnimationEnd += value;
			remove => _adaptee.AnimationEnd -= value;
		}

		/// <inheritdoc />
		public event EventHandler AnimationCancel
		{
			add => _adaptee.AnimationCancel += value;
			remove => _adaptee.AnimationCancel -= value;
		}

		/// <inheritdoc />
		public object AnimatedValue => _adaptee.AnimatedValue;

		public float AnimatedFraction => _adaptee.AnimatedFraction;

		/// <inheritdoc />
		public long CurrentPlayTime
		{
			get => _adaptee.CurrentPlayTime;
			set => _adaptee.CurrentPlayTime = value;
		} 

		/// <inheritdoc />
		public bool IsRunning => _adaptee.IsRunning;

		/// <inheritdoc />
		public long StartDelay
		{
			get => _adaptee.StartDelay;
			set => _adaptee.StartDelay = value;
		}

		/// <inheritdoc />
		public long Duration => _adaptee.Duration;

		/// <inheritdoc />
		public void Start() => _adaptee.Start();

		/// <inheritdoc />
		public void Pause()
		{
#if __ANDROID_19__
			if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Kitkat)
			{
				_adaptee.Pause();
			}
			else
#endif
			{
				_adaptee.Cancel();
			}
		}

		/// <inheritdoc />
		public void Resume()
		{
#if __ANDROID_19__
			if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Kitkat)
			{
				_adaptee.Pause();
			}
			else
#endif
			{
				_adaptee.Start();
			}
		}

		/// <inheritdoc />
		public void Cancel() => _adaptee.Cancel();

		/// <inheritdoc />
		public void SetDuration(long duration) => _adaptee.SetDuration(duration);

		/// <inheritdoc />
		public void SetEasingFunction(IEasingFunction function)
		{
			//Sets different interpolators for Easingfunctions types and different easing modes

			if (function is EasingFunctionBase easing)
			{
				_adaptee.SetInterpolator(easing.CreateTimeInterpolator());
			}
			else
			{
				_adaptee.SetInterpolator(new LinearInterpolator());
			}
		}
	}
}
