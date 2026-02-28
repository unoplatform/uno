using System;
using System.Collections;
using System.Collections.Generic;
using Uno.Disposables;
using System.Text;
using System.Linq;
using Microsoft.UI.Xaml.Markup;
using Uno.Extensions;
using Windows.UI.Core;
using Uno.Foundation.Logging;
using System.Diagnostics;

namespace Microsoft.UI.Xaml.Media.Animation
{
	[ContentProperty(Name = "KeyFrames")]
	public partial class DoubleAnimationUsingKeyFrames : Timeline, ITimeline, IKeyFramesProvider
	{
		private readonly Stopwatch _activeDuration = new Stopwatch();
		private bool _wasBeginScheduled;
		private bool _wasRequestedToStop;
		private int _replayCount = 1;
		private double? _startingValue;
		private double _finalValue;
		private bool _isReversing;

		private List<IValueAnimator> _animators;
		private IValueAnimator _currentAnimator;

		private readonly CompositeDisposable _subscriptions = new CompositeDisposable();

		public bool EnableDependentAnimation
		{
			get => (bool)this.GetValue(EnableDependentAnimationProperty);
			set => this.SetValue(EnableDependentAnimationProperty, value);
		}
		public static DependencyProperty EnableDependentAnimationProperty { get; } =
			DependencyProperty.Register("EnableDependentAnimation", typeof(bool), typeof(DoubleAnimationUsingKeyFrames), new FrameworkPropertyMetadata(false));

		public DoubleAnimationUsingKeyFrames()
		{
			KeyFrames = new DoubleKeyFrameCollection(this, isAutoPropertyInheritanceEnabled: false);
		}

		public DoubleKeyFrameCollection KeyFrames { get; }

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
			// It's important to keep this line here, and not
			// inside the if (!_wasBeginScheduled)
			// If Begin(), Stop(), Begin() are called successively in sequence,
			// we want _wasRequestedToStop to be false.
			_wasRequestedToStop = false;

			if (!_wasBeginScheduled)
			{
				// We dispatch the begin so that we can use bindings on DoubleKeyFrame.Value from RelativeParent.
				// This works because the template bindings are executed just after the constructor.
				// WARNING: This does not allow us to bind DoubleKeyFrame.Value with ViewModel properties.

				_wasBeginScheduled = true;

#if !IS_UNIT_TESTS
#if __ANDROID__
				_ = Dispatcher.RunAnimation(() =>
#else
				_ = Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
#endif
#endif
				{
					_wasBeginScheduled = false;

					if (KeyFrames.Count < 1 || // nothing to do
						_wasRequestedToStop // was requested to stop, between Begin() and dispatched here
					)
					{
						return;
					}

					_activeDuration.Restart();
					_replayCount = 1;
					_isReversing = false; // Reset reversing state on Begin

					//Start the animation
					Play();
				}
#if !IS_UNIT_TESTS
				);
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
			if (_currentAnimator is { IsRunning: true })
			{
				_currentAnimator.Cancel();//Stop the animator if it is running
			}

			// With AutoReverse, the final value is the starting value (after reversing back)
			if (AutoReverse)
			{
				SetValue(ComputeFromValue());
			}
			else
			{
				SetValue(KeyFrames.OrderBy(k => k.KeyTime.TimeSpan).LastOrDefault()?.Value);
			}

			State = FillBehavior == FillBehavior.HoldEnd ? TimelineState.Filling : TimelineState.Stopped;
			OnCompleted();
			_startingValue = null;
		}

		/// <summary>
		/// Begins the animation in reverse, playing from the end value back to the start value.
		/// Used by Storyboard-level AutoReverse to signal child animations to play in reverse.
		/// </summary>
		void ITimeline.BeginReversed()
		{
			_wasRequestedToStop = false;

			if (!_wasBeginScheduled)
			{
				_wasBeginScheduled = true;

#if !IS_UNIT_TESTS
#if __ANDROID__
				_ = Dispatcher.RunAnimation(() =>
#else
				_ = Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
#endif
#endif
				{
					_wasBeginScheduled = false;

					if (KeyFrames.Count < 1 || _wasRequestedToStop)
					{
						return;
					}

					_activeDuration.Restart();
					_replayCount = 1;

					// CRITICAL: Cache the starting value BEFORE setting _isReversing
					// This ensures InitializeAnimators() knows where to reverse back to.
					// Without this, ComputeFromValue() would return the current property value
					// (e.g., 100 after forward animation) instead of the original start (e.g., 0).
					if (!_startingValue.HasValue)
					{
						_startingValue = ComputeFromValue();
					}

					// Compute the final value so we know where to start reversing from
					_finalValue = KeyFrames.OrderByDescending(k => k.KeyTime.TimeSpan).FirstOrDefault()?.Value ?? 0;

					_isReversing = true; // Start in reverse mode

					Play();
				}
#if !IS_UNIT_TESTS
				);
#endif
			}
		}

		/// <summary>
		/// Skips to the fill state as if the animation had played in reverse.
		/// Sets the animated property to its starting value (the "reversed" end state).
		/// </summary>
		void ITimeline.SkipToFillReversed()
		{
			if (_currentAnimator is { IsRunning: true })
			{
				_currentAnimator.Cancel();
			}

			// Set to the starting value (the "reversed" end state)
			SetValue(ComputeFromValue());

			State = TimelineState.Filling;
			OnCompleted();
			_startingValue = null;
		}

		void ITimeline.Deactivate()
		{
			if (_currentAnimator is { IsRunning: true })
			{
				_currentAnimator.Cancel();//Stop the animator if it is running
				_startingValue = null;
			}

			State = TimelineState.Stopped;
			_wasRequestedToStop = true;
		}

		void ITimeline.Stop()
		{
			_currentAnimator?.Cancel(); // stop could be called before the initialization
			_startingValue = null;
			ClearValue();

			State = TimelineState.Stopped;
			_wasRequestedToStop = true;
		}

		/// <summary>
		/// Creates a new animator and animates the view
		/// </summary>
		private void Play()
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

			// Build the animators
			_animators = new List<IValueAnimator>(KeyFrames.Count);

			if (_isReversing)
			{
				// When reversing, play keyframes in reverse order: finalValue -> ... -> startingValue
				var orderedKeyFrames = KeyFrames.OrderByDescending(k => k.KeyTime.TimeSpan).ToList();
				var fromValue = _finalValue;
				var previousKeyTime = orderedKeyFrames.Count > 0 ? orderedKeyFrames[0].KeyTime.TimeSpan : TimeSpan.Zero;

				var index = 0;
				foreach (var keyFrame in orderedKeyFrames)
				{
					double toValue;
					TimeSpan duration;

					if (index + 1 == orderedKeyFrames.Count)
					{
						// Last keyframe in reverse -> go back to starting value
						toValue = startingValue;
						duration = keyFrame.KeyTime.TimeSpan;
					}
					else
					{
						// Go to the next keyframe value (which is the previous one in forward order)
						toValue = orderedKeyFrames[index + 1].Value;
						duration = keyFrame.KeyTime.TimeSpan - orderedKeyFrames[index + 1].KeyTime.TimeSpan;
					}

					var animator = AnimatorFactory.Create(this, fromValue, toValue);
					animator.SetDuration((long)duration.TotalMilliseconds);
					animator.SetEasingFunction(keyFrame.GetEasingFunction());
					animator.DisposeWith(_subscriptions);
					_animators.Add(animator);

					// For next iteration
					fromValue = toValue;

					if (ReportEachFrame())
					{
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
			else
			{
				// Forward playback
				var fromValue = startingValue;
				double toValue;
				var previousKeyTime = TimeSpan.Zero;

				var index = 0;
				foreach (var keyFrame in KeyFrames.OrderBy(k => k.KeyTime.TimeSpan))
				{
					toValue = keyFrame.Value;
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
		}

		private void OnAnimatorEnd(int i)
		{
			var nextAnimatorIndex = i + 1;

			// if it's the last animation part, in the end of the DoubleAnimationUsingKeyFrames
			if (nextAnimatorIndex == KeyFrames.Count)
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().Debug("DoubleAnimationUsingKeyFrames has ended.");
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
		private double ComputeFromValue() => GetDefaultTargetValue() ?? 0f;

		private double? GetDefaultTargetValue() => _startingValue ?? (double?)GetValue();

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

			// Handle AutoReverse: if enabled and we just finished the forward animation, reverse it
			if (AutoReverse && !_isReversing)
			{
				_isReversing = true;
				// Use Play() instead of Replay() to avoid incrementing _replayCount during the reverse phase.
				// This ensures RepeatBehavior counts complete cycles (forward + reverse) as single iterations.
				Play();
				return;
			}

			// If we were reversing, we've now completed both forward and reverse
			if (_isReversing)
			{
				_isReversing = false;
			}

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
		/// Dispose the Double animation.
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
