#nullable enable

using CoreAnimation;
using Foundation;
using Uno.Extensions;
using Uno.Foundation.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
#if __IOS__
using UIKit;
#else
using AppKit;
#endif

namespace Windows.UI.Composition
{
	internal class UnoCoreAnimation : IDisposable
	{
		private const double __millisecondsPerSecond = 1000d;

		private static int _id;

		private readonly CALayer _layer;
		private readonly string _property;
		private readonly string _key;
		private readonly float _from;
		private readonly float _to;
		private readonly float _delayMilliseconds;
		private readonly float _durationMilliseconds;
		private readonly CAMediaTimingFunction _timingFunction;
		private readonly Func<float, NSValue> _nsValueConversion;
		private readonly bool _isDiscrete; // No interpolation
		private readonly Action<CompletedInfo> _onCompleted;
		private readonly Action? _prepare;
		private readonly Action? _cleanup;

		private (CAAnimation animation, float from, float to) _current;
		private (StopReason reason, long? time, float? value) _stop;

		private enum StopReason
		{
			Completed = 0, // The animation stopped it self because it has reached the end of its timeline
			Paused, // The animation was paused, so we have to prepare for a resume, and hold the value
			Canceled, // The animation was canceled, we have to rollback the value
		}

		internal enum CompletedInfo
		{
			/// <summary>
			/// The animation stopped successfully
			/// </summary>
			Success = 0,

			/// <summary>
			/// The animation got stopped (e.g. the animated layer is not in the visual tree)
			/// </summary>
			Error,
		}

		public UnoCoreAnimation(
			CALayer layer,
			string property,
			float from,
			float to,
			float delayMilliseconds,
			float durationMilliseconds,
			CAMediaTimingFunction timingFunction,
			Func<float, NSValue> nsValueConversion,
			Action<CompletedInfo> onCompleted,
			bool isDiscrete,
			Action? prepare = null,
			Action? cleanup = null)
		{
			_layer = layer;
			_property = property;
			_key = property + Interlocked.Increment(ref _id).ToStringInvariant();
			_from = from;
			_to = to;
			_delayMilliseconds = delayMilliseconds;
			_durationMilliseconds = durationMilliseconds;
			_timingFunction = timingFunction;
			_nsValueConversion = nsValueConversion;
			_onCompleted = onCompleted;
			_isDiscrete = isDiscrete;
			_prepare = prepare;
			_cleanup = cleanup;
		}

		public void Start()
		{
			StartAnimation(_from, _to, _delayMilliseconds, _durationMilliseconds);
		}

		public void Pause(long? pausedTime, float? pausedValue)
		{
			StopAnimation(StopReason.Paused, pausedTime, pausedValue);
		}

		public void Resume()
		{
			var startDelay = _delayMilliseconds;
			var duration = _durationMilliseconds;
			var from = _from;

			// If we were paused, we try to effectively resume animation from the its last known state
			if (_stop.reason == StopReason.Paused)
			{
				if (_stop.time.HasValue)
				{
					if (_stop.time.Value > 0) // We were stopped while animating
					{
						startDelay = 0;
						duration = _durationMilliseconds + _stop.time.Value;
					}
					else // We were stopped while waiting for the start delay
					{
						startDelay = _delayMilliseconds + _stop.time.Value;
						duration = _durationMilliseconds;
					}
				}

				if (_stop.value.HasValue)
				{
					from = _stop.value.Value;
				}
			}

			StartAnimation(from, _to, startDelay, duration);
		}

		public void Cancel()
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().DebugFormat("CoreAnimation '{0}': animation cancelled (.Cancel).", _key);
			}
			StopAnimation(StopReason.Canceled);
		}

		private void StartAnimation(float from, float to, float delayMilliseconds, float durationMilliseconds)
		{
			var layer = _layer;

			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().DebugFormat("CoreAnimation '{0}' is starting from {1} to {2}.", _key, from, to);
			}

			CAAnimation animation;
			if (_isDiscrete)
			{
				var discreteAnim = CAKeyFrameAnimation.FromKeyPath(_property);
				discreteAnim.KeyTimes = new NSNumber[] { new NSNumber(0.0), new NSNumber(1.0) };
				discreteAnim.Values = new NSObject[] { _nsValueConversion(to) };

				discreteAnim.CalculationMode = CAKeyFrameAnimation.AnimationDiscrete;

				animation = discreteAnim;
			}
			else
			{
				var continuousAnim = CABasicAnimation.FromKeyPath(_property);
				continuousAnim.From = _nsValueConversion(from);
				continuousAnim.To = _nsValueConversion(to);
				continuousAnim.TimingFunction = _timingFunction;

				animation = continuousAnim;
			}

			if (delayMilliseconds > 0)
			{
				// Note: We must make sure to use the time relative to the 'layer', otherwise we might introduce a random delay and the animations
				//		 will run twice (once "managed" while updating the DP, and a second "native" using this animator)
				animation.BeginTime = layer.ConvertTimeFromLayer(CAAnimation.CurrentMediaTime() + delayMilliseconds / __millisecondsPerSecond, null);
			}

			animation.Duration = durationMilliseconds / __millisecondsPerSecond;
			animation.FillMode = CAFillMode.Forwards;
			// Let the 'OnAnimationStop' forcefully apply the final value before removing the animation.
			// That's required for Storyboards that animating multiple properties of the same object at once.
			animation.RemovedOnCompletion = false;
			animation.AnimationStarted += OnAnimationStarted(animation);
			animation.AnimationStopped += OnAnimationStopped(animation);

			// Start the animation
			_stop = default; // Cleanup stop reason
			_current = (animation, from, to);
			layer.AddAnimation(animation, _key); // This will effectively start the animation (and invoke OnAnimationStarted after the begin delay)
		}

		private void StopAnimation(StopReason reason, long? time = default, float? value = default)
		{
			if (_current.animation == null)
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().DebugFormat("CoreAnimation '{0}' unable to remove native animation: no running animation. Already disposed?", _key);
				}
				return;
			}

			var layer = _layer;

			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().DebugFormat("CoreAnimation '{0}' is forcefully stopping.", _key);
			}

			_stop = (reason, time, value);
			layer.RemoveAnimation(_key); // This will effectively stop the animation (and invoke OnAnimationStopped)
		}

		private EventHandler OnAnimationStarted(CAAnimation animation)
		{
			EventHandler? handler = default;
			handler = Handler;

			return handler;

			void Handler(object? sender, EventArgs _)
			{
				// Note: The sender is usually not the same managed instance of the started animation
				//		 (even the sender.Handle is not the same). So we cannot rely on it to determine
				//		 if we are still the '_current'. Instead we have to create a new handler
				//		 for each started animation which captures its target 'animation' instance.

				animation.AnimationStarted -= handler; // Prevent leak

				if (_current.animation != animation)
				{
					return; // We are no longer the current animation, do not interfere with the current
				}

				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().DebugFormat("CoreAnimation '{0}' has started.", _property);
				}

				// This will disable the transform while the native animation handles it
				// It must be the first thing we do when the animation starts
				// (However we have to wait for the first frame in order to not remove the transform while waiting for the BeginTime)
				_prepare?.Invoke();
			}
		}

		private EventHandler<CAAnimationStateEventArgs> OnAnimationStopped(CAAnimation animation)
		{
			// This callback will be invoked when the animation is stopped, no matter the reason (completed, paused or canceled)

			EventHandler<CAAnimationStateEventArgs>? handler = default;
			handler = Handler;

			return handler;

			void Handler(object? sender, CAAnimationStateEventArgs args)
			{
				// Note: The sender is usually not the same managed instance of the started animation
				//		 (even the sender.Handle is not the same). So we cannot rely on it to determine
				//		 if we are still the '_current'. Instead we have to create a new handler
				//		 for each started animation which captures its target 'animation' instance.

				animation.AnimationStopped -= handler; // Prevent leak

				var (currentAnim, from, to) = _current;
				if (currentAnim != animation)
				{
					if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
					{
						this.Log().DebugFormat("CoreAnimation '{0}' [{1}]: unable to {2} because another animation is running.", _key, _property, _stop.reason);
					}
					return; // We are no longer the current animation, do not interfere with the current
				}

				_current = default;

				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().DebugFormat("CoreAnimation {0} has been {1}.", _key, _stop.reason);
				}

				// First commit the expected final (end, current or initial) value.
				var layer = _layer;
				{
					var keyPath = new NSString(_property);
					NSObject finalValue;
					switch (_stop.reason)
					{
						case StopReason.Paused:
							finalValue = _stop.value.HasValue
								? _nsValueConversion(_stop.value.Value)
								: layer.ValueForKeyPath(keyPath);
							break;

						case StopReason.Canceled:
							finalValue = _nsValueConversion(from);
							break;

						default:
						case StopReason.Completed:
							finalValue = _nsValueConversion(to);
							break;
					}

					CATransaction.Begin();
					CATransaction.DisableActions = true;
					layer.SetValueForKeyPath(finalValue, keyPath);
					CATransaction.Commit();
				}

				// Then reactivate the managed code handling of transforms that was disabled by the _prepare.
				_cleanup?.Invoke();

				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().DebugFormat("CoreAnimation {0} [{1}] Stopped (reason: {2}, Finished:{3})", _key, _property, _stop.reason, args.Finished);
				}

				// Finally raise callbacks
				if (_stop.reason == StopReason.Completed)
				{
					// We have to remove the animation only in case of 'StopReason.Completed',
					// for other cases it's what we actually did to request the stop.
					layer?.RemoveAnimation(_key);
					_onCompleted(args.Finished ? CompletedInfo.Success : CompletedInfo.Error);
				}
			}
		}

		public void Dispose()
		{
			StopAnimation(StopReason.Canceled);
		}
	}
}
