using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Windows.UI;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Markup;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Foundation.Logging;
using System.Diagnostics;

namespace Microsoft.UI.Xaml.Media.Animation
{
	[ContentProperty(Name = nameof(KeyFrames))]
	partial class ColorAnimationUsingKeyFrames : Timeline, ITimeline, IKeyFramesProvider
	{
		private readonly Stopwatch _activeDuration = new Stopwatch();
		private int _replayCount = 1;
		private ColorOffset? _startingValue;
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
			new FrameworkPropertyMetadata(default(ColorKeyFrameCollection), OnKeyFramesChanged));
		public ColorKeyFrameCollection KeyFrames
		{
			get => (ColorKeyFrameCollection)GetValue(KeyFramesProperty);
			set => SetValue(KeyFramesProperty, value);
		}
		private static void OnKeyFramesChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			if (sender is ColorAnimationUsingKeyFrames owner)
			{
				(e.OldValue as ColorKeyFrameCollection)?.SetParent(null);

				// The parent must always be set, so that items in the collection can use that to walk up the visual tree.
				// This is needed when updating the resource bindings that point to local resources (for example, from Page.Resources).
				(e.NewValue as ColorKeyFrameCollection)?.SetParent(owner);
			}
		}

		public ColorAnimationUsingKeyFrames()
		{
			KeyFrames = new ColorKeyFrameCollection();
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

		void ITimeline.Begin()
		{
			if (KeyFrames.Count < 1)
			{
				// A key-frame-less animation has a zero duration, so it completes right away.
				// Reporting completion is required for a parent Storyboard to decrement its
				// running-children counter and raise Completed.
				State = TimelineState.Stopped;
				OnCompleted();
				return;
			}

			PropertyInfo?.CloneShareableObjectsInPath();

			_activeDuration.Restart();
			_replayCount = 1;

			Play();
		}

		void ITimeline.Pause()
		{
			if (State is TimelineState.Paused or TimelineState.Stopped)
			{
				return;
			}

			// The animators do not exist yet while the play is deferred to the next tick, nor when the
			// animation is dependent and was never started. Resume() picks the play back up in that case.
			_currentAnimator?.Pause();

			State = TimelineState.Paused;
		}

		void ITimeline.Resume()
		{
			if (State != TimelineState.Paused)
			{
				return;
			}

			State = TimelineState.Active;

			if (_currentAnimator is null)
			{
				// Paused before the deferred play created the animators: nothing has been played yet,
				// so resuming means (re)starting the play. Play() is a no-op if one is already pending.
				Play();
				return;
			}

			_currentAnimator.Resume();
		}

		void ITimeline.Seek(TimeSpan offset)
		{
			if (_animators is null)
			{
				// Play is still deferred to the next tick: there is no animator to seek yet.
				return;
			}

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

			if (_currentAnimator == null)
			{
				return;
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
			CancelDeferredPlay();
			if (_currentAnimator is { IsRunning: true })
			{
				_currentAnimator.Cancel();//Stop the animator if it is running
				_startingValue = null;
			}

			// Read the final value directly from the last keyframe (not from _finalValue
			// which may be stale if deferred play hasn't initialized animators yet).
			// This matches WinUI's CAnimation::UpdateAnimationUsingKeyFrames which reads
			// keyframe values at tick time via pKeyFrame->GetValue().
			var fillValue = FindFinalValue() ?? default;
			SetValue(fillValue);

			OnEnd();
		}

		void ITimeline.Deactivate()
		{
			CancelDeferredPlay();
			if (_currentAnimator is { IsRunning: true })
			{
				_currentAnimator.Cancel();//Stop the animator if it is running
				_startingValue = null;
			}

			State = TimelineState.Stopped;
		}

		void ITimeline.Stop()
		{
			CancelDeferredPlay();
			_currentAnimator?.Cancel(); // stop could be called before the initialization
			_startingValue = null;
			ClearValue();

			State = TimelineState.Stopped;
		}

		/// <summary>
		/// Starts the animation. On Skia, defers animator initialization to the first
		/// rendering tick so keyframe binding values are read after layout.
		/// </summary>
		private void Play()
		{
			PlayDeferred();
		}

		/// <summary>
		/// Creates animators and starts the animation immediately.
		/// </summary>
		private void PlayImmediate()
		{
			_subscriptions.Clear(); // Dispose all and start a new
			InitializeAnimators(); // Create the animator

			if (!EnableDependentAnimation && this.GetIsDependantAnimation())
			{
				// A dependent animation that was not opted in never runs, so it never reports completion
				// either (pre-existing behavior on every platform). Do not add OnCompleted() here without
				// checking the Storyboard running-children accounting in Storyboard.ChildCompleted.
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
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
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

		private ColorOffset? FindFinalValue()
		{
			return (ColorOffset?)KeyFrames.OrderBy(x => x.KeyTime)?.LastOrDefault()?.Value;
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
			_subscriptions.Clear();

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
		/// Dispose the animation.
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

		private protected override void OnThemeChanged()
		{
			// Value may have changed
			_finalValue = FindFinalValue() ?? default;
		}

		partial void OnFrame(IValueAnimator currentAnimator);
		partial void DisposePartial();
		partial void UseHardware();
		partial void HoldValue();

#if IS_UNIT_TESTS
		private bool ReportEachFrame() => true;
#endif

		IEnumerable IKeyFramesProvider.GetKeyFrames() => KeyFrames;

		private bool ReportEachFrame() => true;

		// Tracks whether animator initialization has been deferred to the next dispatcher tick.
		// This matches WinUI behavior where keyframe values are read at tick time (after layout),
		// not at Begin() time. See CAnimation::UpdateAnimationUsingKeyFrames in animation.cpp.
		private bool _deferredPlayPending;

		// Invalidates callbacks already queued on the dispatcher: a Begin/Stop/Begin sequence within
		// a single tick would otherwise let the stale callback run PlayImmediate() a second time.
		private int _deferredPlayGeneration;

		partial void OnFrame(IValueAnimator currentAnimator)
		{
			SetValue(currentAnimator.AnimatedValue);
		}

		/// <summary>
		/// On Skia, defers animator initialization to the next dispatcher tick.
		/// This ensures keyframe binding values are read after layout has completed,
		/// matching WinUI's tick-based value reading.
		/// </summary>
		private void PlayDeferred()
		{
			if (_deferredPlayPending)
			{
				return;
			}

			_deferredPlayPending = true;

			// Active has to be set now, not in PlayImmediate(): the parent Storyboard must see this child
			// as running, and Pause() ignores a Stopped timeline, so a Begin(); Pause(); pair would
			// otherwise silently start the animation anyway on the tick. Pause/Resume/Seek all tolerate
			// the null animators of that window.
			State = TimelineState.Active;

			var generation = ++_deferredPlayGeneration;

			_ = Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
			{
				if (_deferredPlayGeneration != generation)
				{
					// A Stop/Deactivate/SkipToFill cycle invalidated this callback
					return;
				}

				_deferredPlayPending = false;

				if (State != TimelineState.Active)
				{
					// Paused before the tick: nothing has played yet, so leave the animators
					// uninitialized. Resume() re-schedules the play.
					return;
				}

				PlayImmediate();
			});
		}

		/// <summary>
		/// Cancels a pending deferred play if one is scheduled.
		/// </summary>
		private void CancelDeferredPlay()
		{
			_deferredPlayPending = false;
			_deferredPlayGeneration++;
		}
	}
}
