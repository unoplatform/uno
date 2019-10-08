using Uno.Extensions;
using System;
using System.Collections.Generic;
using Uno.Disposables;
using System.Text;
using System.Linq;
using Uno.Diagnostics.Eventing;
using Windows.UI.Core;
using Uno.Logging;

namespace Windows.UI.Xaml.Media.Animation
{
	public partial class DoubleAnimation : Timeline, ITimeline
	{
		private readonly static IEventProvider _trace = Tracing.Get(TraceProvider.Id);
		private EventActivity _traceActivity;

		public static class TraceProvider
		{
			public readonly static Guid Id = Guid.Parse("{CC14F7B2-D92B-429D-81A4-E1E7A1B13D3D}");

			public const int Start = 1;
			public const int Stop = 2;
			public const int Pause = 3;
			public const int Resume = 4;
		}

		private DateTimeOffset _lastBeginTime;
		private int _replayCount = 1;
		private float? _startingValue = null;
		private float? _endValue = null;

		// Initialize the field with zero capacity, as it may stay empty more often than it is being used.
		private CompositeDisposable _subscriptions = new CompositeDisposable(0);

		private IValueAnimator _animator;

		public double? By
		{
			get => (double?)GetValue(ByProperty);
			set => SetValue(ByProperty, value);
		}

		public static readonly DependencyProperty ByProperty =
			DependencyProperty.Register("By", typeof(double?), typeof(DoubleAnimation), new PropertyMetadata(null));

		public double? From
		{
			get => (double?)GetValue(FromProperty);
			set => SetValue(FromProperty, value);
		}

		public static readonly DependencyProperty FromProperty =
			DependencyProperty.Register("From", typeof(double?), typeof(DoubleAnimation), new PropertyMetadata(null));

		public double? To
		{
			get => (double?)GetValue(ToProperty);
			set => SetValue(ToProperty, value);
		}

		public static readonly DependencyProperty ToProperty =
			DependencyProperty.Register("To", typeof(double?), typeof(DoubleAnimation), new PropertyMetadata(null));

		public bool EnableDependentAnimation
		{
			get => (bool)GetValue(EnableDependentAnimationProperty);
			set => SetValue(EnableDependentAnimationProperty, value);
		}

		public static readonly DependencyProperty EnableDependentAnimationProperty =
			DependencyProperty.Register("EnableDependentAnimation", typeof(bool), typeof(DoubleAnimation), new PropertyMetadata(false));

		public IEasingFunction EasingFunction
		{
			get => (IEasingFunction)GetValue(EasingFunctionProperty);
			set => SetValue(EasingFunctionProperty, value);
		}

		public static readonly DependencyProperty EasingFunctionProperty =
			DependencyProperty.Register("EasingFunction", typeof(IEasingFunction), typeof(DoubleAnimation), new PropertyMetadata(null));


		void ITimeline.Begin()
		{
			if (_trace.IsEnabled)
			{
				_traceActivity = _trace.WriteEventActivity(
					TraceProvider.Start,
					EventOpcode.Start,
					payload: GetTraceProperties()
				);
			}

			_subscriptions.Clear(); //Dispose all and start a new

			_lastBeginTime = DateTimeOffset.Now;
			_replayCount = 1;

			//Start the animation
			Play();
		}

		void ITimeline.Stop()
		{
			if (_trace.IsEnabled)
			{
				_trace.WriteEventActivity(
					TraceProvider.Stop,
					EventOpcode.Stop,
					_traceActivity,
					payload: GetTraceProperties()
				);
			}

			_animator?.Cancel(); // stop could be called before the initialization
			_startingValue = null;
			ClearValue();
			State = TimelineState.Stopped;
		}

		void ITimeline.Resume()
		{
			if (State != TimelineState.Paused)
			{
				return;
			}

			if (_trace.IsEnabled)
			{
				_trace.WriteEventActivity(
					TraceProvider.Resume,
					EventOpcode.Send,
					_traceActivity,
					payload: GetTraceProperties()
				);
			}

			_animator.Resume();

			State = TimelineState.Active;
		}

		void ITimeline.Pause()
		{
			if (State == TimelineState.Paused)
			{
				return;
			}

			if (_trace.IsEnabled)
			{
				_trace.WriteEventActivity(
					TraceProvider.Pause,
					EventOpcode.Send,
					_traceActivity,
					payload: GetTraceProperties()
				);
			}

			_animator.Pause();

			State = TimelineState.Paused;
		}

		void ITimeline.Seek(TimeSpan offset)
		{
			_animator.CurrentPlayTime = (long)offset.TotalMilliseconds; //Offset is CurrentPlayTime (starting point for animation)

			if (State == TimelineState.Active || State == TimelineState.Paused)
			{
				CoreDispatcher.Main.RunAsync(
					CoreDispatcherPriority.Normal,
					() =>
					{
						OnFrame();
						_animator.Pause();
					});
			}
		}

		void ITimeline.SeekAlignedToLastTick(TimeSpan offset)
		{
			// Same as Seek
			((ITimeline)this).Seek(offset);
		}

		void ITimeline.SkipToFill()
		{
			if (_animator != null && _animator.IsRunning)
			{
				_animator.Cancel();//Stop the animator if it is running
				_startingValue = null;
			}

			SetValue(ComputeToValue());//Set property to its final value

			OnEnd();
		}

		void ITimeline.Deactivate()
		{
			_animator?.Cancel();//Stop the animator if it is running
			_startingValue = null;

			State = TimelineState.Stopped;
		}

		/// <summary>
		/// Replay this animation.
		/// </summary>
		private void Replay()
		{
			_replayCount++;

			Play();
		}

		/// <summary>
		/// Initializes the animator and Events
		/// </summary>
		private void InitializeAnimator()
		{
			_startingValue = ComputeFromValue();

			_endValue = ComputeToValue();

			_animator = AnimatorFactory.Create(this, _startingValue.Value, _endValue.Value);

			_animator.SetEasingFunction(this.EasingFunction); //Set the Easing Function of the animator

			SetAnimatorDuration();//Set the duration of the animator

			_animator.DisposeWith(_subscriptions);

			if (ReportEachFrame())
			{
				//Called each frame
				_animator.Update += OnAnimatorUpdate;
			}
			else
			{
#if __ANDROID_19__
				if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Kitkat)
				{
					_animator.AnimationPause += OnAnimatorAnimationPause;
				}
#endif

				_animator.AnimationEnd += OnAnimatorAnimationEndFrame;
			}

			//Called at the end
			_animator.AnimationEnd += OnAnimatorAnimationEnd;

			_animator.AnimationCancel += OnAnimatorCancelled;
		}

		private void OnAnimatorAnimationEnd(object sender, EventArgs e)
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug("DoubleAnimation has ended.");
			}

			OnEnd();
			_startingValue = null;
		}

		private void OnAnimatorAnimationEndFrame(object sender, EventArgs e) => OnFrame();

		private void OnAnimatorAnimationPause(object sender, EventArgs e) => OnFrame();

		private void OnAnimatorUpdate(object sender, EventArgs e) => OnFrame();

		/// <summary>
		/// Creates a new animator and animates the view
		/// </summary>
		private void Play()
		{
			InitializeAnimator();//Create the animator

			if (!EnableDependentAnimation && this.GetIsDependantAnimation())
			{ // Don't start the animator its a dependent animation
				return;
			}

			UseHardware();//Ensure that the GPU is used for animations

			if (BeginTime.HasValue)
			{ // Set the start delay
				_animator.StartDelay = (long)BeginTime.Value.TotalMilliseconds;
			}

			_animator.Start();
			State = TimelineState.Active;

#if XAMARIN_IOS
			// On iOS, animations started while the app is in the background will lead to properties in an incoherent state (native static 
			// values out of syc with native presented values and Xaml values). As a workaround we fast-forward the animation to its final
			// state. (The ideal would probably be to restart the animation when the app resumes.)
			if (Application.Current?.IsSuspended ?? false)
			{
				((ITimeline)this).SkipToFill();
			} 
#endif
		}

		/// <summary>
		/// Sets the duration of the animator.
		/// </summary>
		private void SetAnimatorDuration()
		{
			switch (Duration.Type)
			{
				case DurationType.Automatic:
					_animator.SetDuration(1000);
					//Default 1 sec animations
					break;
				case DurationType.Forever:
					_animator.SetDuration(long.MaxValue);
					//Nothing is forever, but this is pretty close
					break;
				case DurationType.TimeSpan:
					//If the duration is a timespan use it
					_animator.SetDuration((long)Duration.TimeSpan.TotalMilliseconds);
					break;
			}
		}

		/// <summary>
		/// Replays the Animation if required, Sets the final state, Raises the Completed event. 
		/// </summary>
		private void OnEnd()
		{
			// If the animation was GPU based, remove the animated value
			if (NeedsRepeat(_lastBeginTime, _replayCount))
			{
				Replay(); // replay the animation
				return;
			}

			if (FillBehavior == FillBehavior.HoldEnd)//Two types of fill behaviors : HoldEnd - Keep displaying the last frame
			{
#if __IOS__
				// iOS: Here we make sure that the final frame is applied properly (it may have been skipped by animator)
				// Note: The value is applied using the "Animations" precedence, which means that the user won't be able to alter
				//		 it from application code. Instead we should set the value using a lower precedence
				//		 (possibly "Local" with PropertyInfo.SetLocalValue(ComputeToValue())) but we must keep the
				//		 original "Local" value, so will be able to rollback the animation when
				//		 going to another VisualState (if the storyboard ran in that context).
				//		 In that case we should also do "ClearValue();" to remove the "Animations" value, even if using "HoldEnd"
				//		 cf. https://github.com/unoplatform/uno/issues/631
				PropertyInfo.Value = ComputeToValue();
#endif

				State = TimelineState.Filling;
			}
			else // Stop -Put back the initial state
			{
				State = TimelineState.Stopped;

				ClearValue();
			}

			OnCompleted();
		}

		/// <summary>
		/// Sets the final state
		/// </summary>
		private void OnAnimatorCancelled(object sender, EventArgs e)
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug("DoubleAnimation was cancelled.");
			}

			// Means the animation is stopped because the animator was cancelled.
			// This may happen either if someone stops the animation beforehand or
			// if the animator is stopped at deactivation.  This means that even though
			// we should consider the animation to be stopped, we shouldn't clear any animated
			// value in order to support deactivation scenarios.
			State = TimelineState.Stopped;

#if XAMARIN_IOS
			_startingValue = null;

			// On Android, AnimationEnd is always called after AnimationCancel. We don't unset _startingValue yet to be able to calculate 
			// the final value correctly.
#endif
		}

		/// <summary>
		/// Calculates the From value of the animation
		/// For simplicity, animations are based on to and from values
		/// </summary>
		private float ComputeFromValue()
		{
			if (From.HasValue)
			{
				return (float)From.Value;
			}

			if (By.HasValue && To.HasValue)
			{
				return (float)(To.Value - By.Value);
			}

			return GetDefaultTargetValue() ?? 0f;
		}

		private float? GetDefaultTargetValue()
		{
			if (_startingValue != null)
			{
				return _startingValue;
			}
			else
			{
				var value = GetValue();

				if (value != null)
				{
					return Convert.ToSingle(value);
				}
			}

			return null;
		}

		/// <summary>
		/// Calculates the To value of the animation
		/// For simplicity, animations are based on to and from values
		/// </summary>
		private float ComputeToValue()
		{
			if (To.HasValue)
			{
				return (float)To.Value;
			}

			if (By.HasValue)
			{
				if (From.HasValue)
				{
					return (float)(From.Value + By.Value);
				}
				else
				{
					return (GetDefaultTargetValue() ?? 0f) + (float)By.Value;
				}
			}

			return GetDefaultTargetValue() ?? 0f;
		}

		/// <summary>
		/// Dispose the Double animation.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_subscriptions.Dispose();

				DisposePartial();
			}

			base.Dispose(disposing);
		}

		partial void DisposePartial();

		partial void OnFrame();
		partial void HoldValue();
		partial void UseHardware();
	}
}

