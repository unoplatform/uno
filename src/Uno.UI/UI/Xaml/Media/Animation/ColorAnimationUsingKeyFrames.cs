using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml.Markup;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Logging;

namespace Windows.UI.Xaml.Media.Animation
{
	[ContentProperty(Name = "KeyFrames")]
	partial class ColorAnimationUsingKeyFrames : Timeline, ITimeline
	{
		private DateTimeOffset _lastBeginTime;
		private int _replayCount = 1;
		private ColorOffset? _startingValue = null;
		private ColorOffset _finalValue;

		private List<IValueAnimator> _animators;
		private IValueAnimator _currentAnimator;

		private readonly CompositeDisposable _subscriptions = new CompositeDisposable();

		public static DependencyProperty EnableDependentAnimationProperty { get; } = DependencyProperty.Register(
			"EnableDependentAnimation",
			typeof(bool),
			typeof(ColorAnimationUsingKeyFrames),
			new FrameworkPropertyMetadata(false));
		public bool EnableDependentAnimation
		{
			get => (bool)GetValue(EnableDependentAnimationProperty);
			set => SetValue(EnableDependentAnimationProperty, value);
		}

		public static DependencyProperty KeyFramesProperty { get; } = DependencyProperty.Register(
			"KeyFrames",
			typeof(ColorKeyFrameCollection),
			typeof(ColorAnimationUsingKeyFrames),
			new FrameworkPropertyMetadata(default(ColorKeyFrameCollection)));
		public ColorKeyFrameCollection KeyFrames
		{
			get => (ColorKeyFrameCollection)GetValue(KeyFramesProperty);
			set => SetValue(KeyFramesProperty, value);
		}

		public ColorAnimationUsingKeyFrames()
		{
			KeyFrames = new ColorKeyFrameCollection(this, isAutoPropertyInheritanceEnabled: false);
		}

		internal override TimeSpan GetCalculatedDuration()
		{
			var duration = Duration;
			if (duration != Duration.Automatic)
			{
				return base.GetCalculatedDuration();
			}

			if (KeyFrames.Any())
			{
				var lastKeyTime = KeyFrames.Max(kf => kf.KeyTime);
				return lastKeyTime.TimeSpan;
			}

			return base.GetCalculatedDuration();
		}

		bool _wasBeginScheduled = false;
		void ITimeline.Begin()
		{
			if (!_wasBeginScheduled)
			{
				// We dispatch the begin so that we can use bindings on ColorKeyFrame.Value from RelativeParent.
				// This works because the template bindings are executed just after the constructor.
				// WARNING: This does not allow us to bind ColorKeyFrame.Value with ViewModel properties.
				_wasBeginScheduled = true;

#if !NET461
#if __ANDROID__
				Dispatcher.RunAnimation(() =>
#else
				Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
#endif
				{
#endif
					if (KeyFrames.Count < 1)
					{
						return; // nothing to do
					}

					PropertyInfo?.CloneShareableObjectsInPath();

					_wasBeginScheduled = false;
					_subscriptions.Clear(); //Dispose all and start a new

					_lastBeginTime = DateTimeOffset.Now;
					_replayCount = 1;

					//Start the animation
					Play();
#if !NET461
				});
#endif
			}
		}

		void ITimeline.Pause()
		{
			if (State == TimelineState.Paused)
			{
				return;
			}

			_currentAnimator.Pause();

			State = TimelineState.Paused;
		}

		void ITimeline.Resume()
		{
			if (State != TimelineState.Paused)
			{
				return;
			}

			_currentAnimator.Resume();

			State = TimelineState.Active;
		}

		void ITimeline.Seek(TimeSpan offset)
		{
			var msOffset = (long)offset.TotalMilliseconds;
			IValueAnimator targetAnimator = null;
			foreach (var animator in _animators)
			{
				if (msOffset < animator.Duration)
				{
					targetAnimator = animator;
					break;
				}
				msOffset -= animator.Duration;
			}

			if (targetAnimator != _currentAnimator)
			{
				_currentAnimator?.Cancel();
				_currentAnimator = targetAnimator;
			}

			if(_currentAnimator == null)
			{
				return;
			}

			_currentAnimator.CurrentPlayTime = (long)offset.TotalMilliseconds; //Offset is CurrentPlayTime (starting point for animation)

			if (State == TimelineState.Active || State == TimelineState.Paused)
			{
				CoreDispatcher.Main.RunAsync(
					CoreDispatcherPriority.Normal,
					() =>
					{
						OnFrame(_currentAnimator);

						_currentAnimator.Pause();
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
			if (_currentAnimator != null && _currentAnimator.IsRunning)
			{
				_currentAnimator.Cancel();//Stop the animator if it is running
				_startingValue = null;
			}

			SetValue(_finalValue);//Set property to its final value

			OnEnd();
		}

		void ITimeline.Deactivate()
		{
			if (_currentAnimator != null && _currentAnimator.IsRunning)
			{
				_currentAnimator.Cancel();//Stop the animator if it is running
				_startingValue = null;
			}
			State = TimelineState.Stopped;
		}

		void ITimeline.Stop()
		{
			_currentAnimator?.Cancel(); // stop could be called before the initialization
			_startingValue = null;
			ClearValue();
			State = TimelineState.Stopped;
		}

		/// <summary>
		/// Creates a new animator and animates the view
		/// </summary>
		private void Play()
		{
			InitializeAnimators();//Create the animator

			if (!EnableDependentAnimation && this.GetIsDependantAnimation())
			{ // Don't start the animator its a dependent animation
				return;
			}

			UseHardware();//Ensure that the GPU is used for animations

			_currentAnimator = _animators.First();
			if (BeginTime.HasValue)
			{ // Set the start delay
				_currentAnimator.StartDelay = (long)BeginTime.Value.TotalMilliseconds;
			}

			_currentAnimator.Start();
			State = TimelineState.Active;
		}

		/// <summary>
		/// Initializes the animators and 
		/// </summary>
		private void InitializeAnimators()
		{
			var startingValue = ComputeFromValue();

			var fromValue = startingValue;
			ColorOffset toValue;
			var previousKeyTime = TimeSpan.Zero;

			// Build the animators
			_animators = new List<IValueAnimator>(KeyFrames.Count);

			var index = 0;
			foreach (var keyFrame in KeyFrames.OrderBy(k => k.KeyTime.TimeSpan))
			{
				toValue = (ColorOffset)keyFrame.Value;
				if (index + 1 == KeyFrames.Count)
				{
					_finalValue = toValue;
				}
				var animator = AnimatorFactory.Create(this, fromValue, toValue);
				var duration = keyFrame.KeyTime.TimeSpan - previousKeyTime;
				animator.SetDuration((long)duration.TotalMilliseconds);
				animator.SetEasingFunction(keyFrame.GetEasingFunction());
				animator.DisposeWith(_subscriptions);
				_animators.Add(animator);

				// For next iteration
				fromValue = toValue;
				previousKeyTime = keyFrame.KeyTime.TimeSpan;

				if (ReportEachFrame())
				{
					//Called each frame
					animator.Update += (sender, e) =>
					{
						OnFrame((IValueAnimator)sender);
					};
				}

				var i = index;

#if __ANDROID_19__
				if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Kitkat)
				{
					animator.AnimationPause += (a, _) => OnFrame((IValueAnimator)a);
				}
#endif

				animator.AnimationEnd += (a, _) =>
				{
					OnFrame((IValueAnimator)a);
					OnAnimatorEnd(i);
				};
				++index;
			}
		}

		private void OnAnimatorEnd(int i)
		{
			var nextAnimatorIndex = i + 1;

			// if it's the last animation part, in the end of the ColorAnimationUsingKeyFrames
			if (nextAnimatorIndex == KeyFrames.Count)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug("ColorAnimationUsingKeyFrames has ended.");
				}

				OnEnd();
				_startingValue = null;
			}
			else
			{
				_currentAnimator = _animators[nextAnimatorIndex];
				_currentAnimator.Start();
			}
		}

		/// <summary>
		/// Calculates the From value of the animation
		/// For simplification animations are based on to and from values
		/// </summary>
		private ColorOffset ComputeFromValue() => GetDefaultTargetValue() ?? ColorOffset.Zero;

		private ColorOffset? GetDefaultTargetValue()
		{
			var value = _startingValue;
			if (value != null)
			{
				return value;
			}

			var v = GetValue();
			if (v is Color color)
			{
				return (ColorOffset?)color;
			}
			return default;
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
				HoldValue();
				State = TimelineState.Filling;
			}
			else// HoldEnd -Put back the initial state
			{
				State = TimelineState.Stopped;
				ClearValue();
			}

			OnCompleted();
		}


		/// <summary>
		/// Dispose the animation.
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

		partial void OnFrame(IValueAnimator currentAnimator);
		partial void DisposePartial();
		partial void UseHardware();
		partial void HoldValue();

#if NET461
		private bool ReportEachFrame() => true;
#endif
	}
}
