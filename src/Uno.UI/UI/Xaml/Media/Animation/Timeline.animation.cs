using Uno.Extensions;
using System;
using System.Collections.Generic;
using Uno.Disposables;
using System.Text;
using System.Linq;
using Uno.Diagnostics.Eventing;
using Windows.UI.Core;
using Uno.Foundation.Logging;
using Uno.UI.DataBinding;
using System.Diagnostics;
using Uno.UI;

namespace Microsoft.UI.Xaml.Media.Animation
{
	public partial class Timeline
	{
		private protected partial class AnimationImplementation<T> : IDisposable where T : struct
		{
			private static readonly IEventProvider _trace = Tracing.Get(TraceProvider.Id);
			private Timeline _owner;
			private IAnimation<T> AnimationOwner => _owner as IAnimation<T>;
			private EventActivity _traceActivity;

			public static class TraceProvider
			{
				public static readonly Guid Id = Guid.Parse("{CC14F7B2-D92B-429D-81A4-E1E7A1B13D3D}");

				public const int Start = 1;
				public const int Stop = 2;
				public const int Pause = 3;
				public const int Resume = 4;
			}

			private readonly Stopwatch _activeDuration = new Stopwatch();
			private int _replayCount = 1;
			private T? _startingValue;
			private T? _endValue;

			// Initialize the field with zero capacity, as it may stay empty more often than it is being used.
			private readonly CompositeDisposable _subscriptions = new CompositeDisposable(0);

			private IValueAnimator _animator;

			public AnimationImplementation(Timeline owner)
			{
				this._owner = owner;
			}

			private TimelineState State
			{
				get => _owner?.State ?? default(TimelineState);
				set
				{
					if (_owner != null)
					{
						_owner.State = value;
					}
				}
			}
			private TimeSpan? BeginTime => _owner?.BeginTime;
			private Duration Duration => _owner?.Duration ?? default(Duration);
			private FillBehavior FillBehavior => _owner?.FillBehavior ?? default(FillBehavior);

			private T? From => AnimationOwner?.From;
			private T? To => AnimationOwner?.To;
			private T? By => AnimationOwner?.By;
			private IEasingFunction EasingFunction => AnimationOwner?.EasingFunction;
			private bool EnableDependentAnimation => AnimationOwner?.EnableDependentAnimation ?? false;
			private BindingPath PropertyInfo => _owner?.PropertyInfo;

			private string[] GetTraceProperties() => _owner?.GetTraceProperties();

			private void ClearValue() => _owner?.ClearValue();
			private void SetValue(object value) => _owner?.SetValue(value);
			private object GetValue() => _owner?.GetValue();
			private object GetNonAnimatedValue() => _owner?.GetNonAnimatedValue();
			private bool NeedsRepeat(Stopwatch activeDuration, int replayCount) => _owner?.NeedsRepeat(activeDuration, replayCount) ?? false;

			public void Begin()
			{
				if (_trace.IsEnabled)
				{
					_traceActivity = _trace.WriteEventActivity(
						TraceProvider.Start,
						EventOpcode.Start,
						payload: GetTraceProperties()
					);
				}

				PropertyInfo?.CloneShareableObjectsInPath();

				_subscriptions.Clear(); //Dispose all and start a new

				_activeDuration.Restart();
				_replayCount = 1;

				//Start the animation
				Play();
			}

			public void Stop()
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

			public void Resume()
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

			public void Pause()
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

			public void Seek(TimeSpan offset)
			{
				_animator.CurrentPlayTime = (long)offset.TotalMilliseconds; //Offset is CurrentPlayTime (starting point for animation)

				if (State == TimelineState.Active || State == TimelineState.Paused)
				{
					_ = CoreDispatcher.Main.RunAsync(
						CoreDispatcherPriority.Normal,
						() =>
						{
							OnFrame();
							_animator.Pause();
						});
				}
			}

			public void SeekAlignedToLastTick(TimeSpan offset)
			{
				// Same as Seek
				Seek(offset);
			}

			public void SkipToFill()
			{
				if (_animator is { IsRunning: true })
				{
					_animator.Cancel(); // Stop the animator if it is running
					_startingValue = null;
				}

				// Set property to its final value
				var value = ComputeToValue();
				SetValue(value);

				OnEnd();
			}

			public void Deactivate()
			{
				_animator?.Cancel(); // Stop the animator if it is running
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

				_animator = AnimatorFactory.Create(_owner, _startingValue.Value, _endValue.Value);

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
#if __ANDROID__
					if (ABuild.VERSION.SdkInt >= ABuildVersionCodes.Kitkat)
					{
						_animator.AnimationPause += OnAnimatorAnimationPause;
					}
#endif

					_animator.AnimationEnd += OnAnimatorAnimationEndFrame;
				}

				//Called at the end
				_animator.AnimationEnd += OnAnimatorAnimationEnd;

				_animator.AnimationCancel += OnAnimatorCancelled;
				_animator.AnimationFailed += OnAnimatorFailed;
			}

			private void OnAnimatorAnimationEnd(object sender, EventArgs e)
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().Debug("TimeLine has ended.");
				}

				OnEnd();
			}

			private void OnAnimatorAnimationEndFrame(object sender, EventArgs e) => OnFrame();

			private void OnAnimatorAnimationPause(object sender, EventArgs e) => OnFrame();

			private void OnAnimatorUpdate(object sender, EventArgs e) => OnFrame();

			/// <summary>
			/// Creates a new animator and animates the view
			/// </summary>
			private void Play()
			{
				_animator?.Dispose();

				InitializeAnimator(); // Create the animator

				if (!EnableDependentAnimation && _owner.GetIsDependantAnimation())
				{ // Don't start the animator its a dependent animation
					// However, we still need to complete the animation to maintain consistency with WinUI
					State = TimelineState.Active;
					_ = CoreDispatcher.Main.RunAsync(CoreDispatcherPriority.Normal, () =>
					{
						if (State == TimelineState.Stopped)
						{
							// If the animation was force-stopped, don't trigger completion
							return;
						}
						OnEnd();
					});
					return;
				}

				UseHardware();//Ensure that the GPU is used for animations

				if (BeginTime.HasValue)
				{ // Set the start delay
					_animator.StartDelay = (long)BeginTime.Value.TotalMilliseconds;
				}

				_animator.Start();
				State = TimelineState.Active;

#if __APPLE_UIKIT__
				// On iOS, animations started while the app is in the background will lead to properties in an incoherent state (native static
				// values out of syc with native presented values and Xaml values). As a workaround we fast-forward the animation to its final
				// state. (The ideal would probably be to restart the animation when the app resumes.)
				if (Application.Current?.IsSuspended ?? false)
				{
					SkipToFill();
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
				_animator?.Dispose();

				// If the animation was GPU based, remove the animated value

				if (NeedsRepeat(_activeDuration, _replayCount))
				{
					Replay(); // replay the animation
					return;
				}

				// There are two types of fill behaviors:
				if (FillBehavior == FillBehavior.HoldEnd) // HoldEnd: Keep displaying the last frame
				{
#if __APPLE_UIKIT__
					// iOS && macOS: Here we make sure that the final frame is applied properly (it may have been skipped by animator)
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
				else // Stop: Put back the initial state
				{
					State = TimelineState.Stopped;

					ClearValue();
				}

				_owner.OnCompleted();
				_startingValue = null;
			}

			/// <summary>
			/// Stops the timeline when an animator failed
			/// </summary>
			private void OnAnimatorFailed(object sender, EventArgs e)
			{
				// Failed - Put back the initial state, and don't try to replay.
				State = TimelineState.Stopped;

				ClearValue();

				_owner.OnFailed();
			}

			/// <summary>
			/// Sets the final state
			/// </summary>
			private void OnAnimatorCancelled(object sender, EventArgs e)
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().Debug("DoubleAnimation was cancelled.");
				}

				// Means the animation is stopped because the animator was cancelled.
				// This may happen either if someone stops the animation beforehand or
				// if the animator is stopped at deactivation.  This means that even though
				// we should consider the animation to be stopped, we shouldn't clear any animated
				// value in order to support deactivation scenarios.
				State = TimelineState.Stopped;

#if __APPLE_UIKIT__
				_startingValue = null;

				// On Android, AnimationEnd is always called after AnimationCancel. We don't unset _startingValue yet to be able to calculate
				// the final value correctly.
#endif
			}

			/// <summary>
			/// Calculates the From value of the animation
			/// For simplicity, animations are based on to and from values
			/// </summary>
			private T ComputeFromValue()
			{
				if (From.HasValue)
				{
					return (T)From.Value;
				}

				if (By.HasValue && To.HasValue)
				{
					return AnimationOwner.Subtract(To.Value, By.Value);
				}

				return GetDefaultTargetValue() ?? default(T);
			}

			private T? GetDefaultTargetValue()
			{
				if (_startingValue != null)
				{
					return _startingValue;
				}
				else
				{
					var value = FeatureConfiguration.Timeline.DefaultsStartingValueFromAnimatedValue
						? GetValueCore()
						: GetNonAnimatedValue();
					if (value != null)
					{
						return AnimationOwner.Convert(value);
					}
				}

				return null;
			}

			private object GetValueCore()
			{
#if !__ANDROID__
				return GetValue();
#else
				// On android, animation may target a native property implementing the behavior instead of the specified dependency property.
				// When starting a new animation midst another, in order to continue from the current animated value,
				// we need to retrieve the value of that native property, as reading the dp value will just give the final value.
				if (AnimatorFactory.TryGetNativeAnimatedValue(_owner, out var value))
				{
					return value;
				}
				else
				{
					return GetValue();
				}
#endif
			}

			/// <summary>
			/// Calculates the To value of the animation
			/// For simplicity, animations are based on to and from values
			/// </summary>
			private T ComputeToValue()
			{
				if (To.HasValue)
				{
					return To.Value;
				}

				if (!By.HasValue)
				{
					return GetDefaultTargetValue() ?? default;
				}

				return From.HasValue
					? AnimationOwner.Add(From.Value, By.Value)
					: AnimationOwner.Add(GetDefaultTargetValue() ?? default, By.Value);

			}

			/// <summary>
			/// Dispose the Double animation.
			/// </summary>
			public void Dispose()
			{
				_subscriptions.Dispose();

				_owner = null;

				DisposePartial();
			}

			partial void DisposePartial();

			partial void OnFrame();
			partial void UseHardware();
		}
	}
}

