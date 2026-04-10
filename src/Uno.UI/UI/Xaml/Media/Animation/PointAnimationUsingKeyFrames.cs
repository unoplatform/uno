using System;
using System.Collections;
using System.Collections.Generic;
using Uno.Disposables;
using System.Text;
using System.Linq;
using Microsoft.UI.Xaml.Markup;
using Uno.Extensions;
using Windows.Foundation;
using Windows.UI.Core;
using Uno.Foundation.Logging;
using System.Diagnostics;

namespace Microsoft.UI.Xaml.Media.Animation
{
	[ContentProperty(Name = "KeyFrames")]
	public partial class PointAnimationUsingKeyFrames : Timeline, ITimeline, IKeyFramesProvider
	{
		private readonly Stopwatch _activeDuration = new Stopwatch();
		private int _replayCount = 1;
		private Point? _startingValue;
		private Point _finalValue;

		private List<IValueAnimator> _animators;
		private IValueAnimator _currentAnimator;

		private readonly CompositeDisposable _subscriptions = new CompositeDisposable();

		public bool EnableDependentAnimation
		{
			get => (bool)this.GetValue(EnableDependentAnimationProperty);
			set => this.SetValue(EnableDependentAnimationProperty, value);
		}
		public static DependencyProperty EnableDependentAnimationProperty { get; } =
			DependencyProperty.Register("EnableDependentAnimation", typeof(bool), typeof(PointAnimationUsingKeyFrames), new FrameworkPropertyMetadata(false));

		public PointAnimationUsingKeyFrames()
		{
			KeyFrames = new PointKeyFrameCollection(this, isAutoPropertyInheritanceEnabled: false);
		}

		public PointKeyFrameCollection KeyFrames { get; }

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

		void ITimeline.Begin()
		{
			if (KeyFrames.Count < 1)
			{
				return;
			}

			_activeDuration.Restart();
			_replayCount = 1;

			Play();
		}

		void ITimeline.Pause()
		{
			if (State == TimelineState.Paused)
			{
				return;
			}

#if __SKIA__
			if (_isTimeManagerDriven)
			{
				State = TimelineState.Paused;
				return;
			}
#endif

			_currentAnimator.Pause();

			State = TimelineState.Paused;
		}

		void ITimeline.Resume()
		{
			if (State != TimelineState.Paused)
			{
				return;
			}

#if __SKIA__
			if (_isTimeManagerDriven)
			{
				State = TimelineState.Active;
				return;
			}
#endif

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
				_currentAnimator.Cancel();
				_currentAnimator = targetAnimator;
			}

			_currentAnimator.CurrentPlayTime = (long)offset.TotalMilliseconds; //Offset is CurrentPlayTime (starting point for animation)

			if (State == TimelineState.Active || State == TimelineState.Paused)
			{
				_ = CoreDispatcher.Main.RunAsync(
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
#if __SKIA__
			CancelDeferredPlay();
			StopTimeManagerDriven();
#endif
			if (_currentAnimator is { IsRunning: true })
			{
				_currentAnimator.Cancel();//Stop the animator if it is running
				_startingValue = null;
			}

			// Read the final value directly from the last keyframe (not cached).
			// This matches WinUI's CAnimation::UpdateAnimationUsingKeyFrames which reads
			// keyframe values at tick time via pKeyFrame->GetValue().
			SetValue(KeyFrames.OrderBy(k => k.KeyTime.TimeSpan).LastOrDefault()?.Value ?? default(Point));

			OnEnd();
		}

		void ITimeline.Deactivate()
		{
#if __SKIA__
			CancelDeferredPlay();
			StopTimeManagerDriven();
#endif
			if (_currentAnimator is { IsRunning: true })
			{
				_currentAnimator.Cancel();//Stop the animator if it is running
				_startingValue = null;
			}

			State = TimelineState.Stopped;
		}

		void ITimeline.Stop()
		{
#if __SKIA__
			CancelDeferredPlay();
			StopTimeManagerDriven();
#endif
			_currentAnimator?.Cancel(); // stop could be called before the initialization
			_startingValue = null;
			ClearValue();

			State = TimelineState.Stopped;
		}

		/// <summary>
		/// Starts the animation. On Skia, defers animator initialization to the first
		/// rendering tick so keyframe binding values are read after layout (matching WinUI
		/// where keyframe values are read at tick time, not at Begin time).
		/// </summary>
		private void Play()
		{
#if __SKIA__
			PlayDeferred();
#else
			PlayImmediate();
#endif
		}

		/// <summary>
		/// Creates animators and starts the animation immediately.
		/// </summary>
		private void PlayImmediate()
		{
			_subscriptions.Clear(); // Dispose all current animators
			InitializeAnimators(); // Create the animator

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
			Point toValue;
			var previousKeyTime = TimeSpan.Zero;

			// Build the animators
			_animators = new List<IValueAnimator>(KeyFrames.Count);

			var index = 0;
			foreach (var keyFrame in KeyFrames.OrderBy(k => k.KeyTime.TimeSpan))
			{
				toValue = keyFrame.Value;
				if (index + 1 == KeyFrames.Count)
				{
					_finalValue = toValue;
				}
				var duration = keyFrame.KeyTime.TimeSpan - previousKeyTime;
				var animator = AnimatorFactory.Create(this, fromValue, toValue, duration);
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

#if __ANDROID__
				if (ABuild.VERSION.SdkInt >= ABuildVersionCodes.Kitkat)
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

			// if it's the last animation part, in the end of the PointAnimationUsingKeyFrames
			if (nextAnimatorIndex == KeyFrames.Count)
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().Debug("PointAnimationUsingKeyFrames has ended.");
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
		private Point ComputeFromValue()
		{
			var value = GetDefaultTargetValue();
			return value ?? default(Point);
		}

		private Point? GetDefaultTargetValue()
		{
			if (_startingValue.HasValue)
			{
				return _startingValue.Value;
			}

			var value = GetValue();
			if (value is Point p)
			{
				return p;
			}

			return null;
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
			_subscriptions.Clear(); // Dispose all current animators

			// If the animation was GPU based, remove the animated value
			if (NeedsRepeat(_activeDuration, _replayCount))
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
		/// Dispose the Point animation.
		/// </summary>
		private protected override void Dispose(bool disposing)
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

#if IS_UNIT_TESTS
		private bool ReportEachFrame() => true;
#endif

		IEnumerable IKeyFramesProvider.GetKeyFrames() => KeyFrames;
	}
}
