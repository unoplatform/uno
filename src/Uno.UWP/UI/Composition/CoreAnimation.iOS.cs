#if __IOS__
using CoreAnimation;
using Foundation;
using Uno.Extensions;
using Uno.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UIKit;

namespace Windows.UI.Composition
{
	internal class UnoCoreAnimation : IDisposable
	{
		private const double __millisecondsPerSecond = 1000d;

		private static int _id = 0;

		private WeakReference<CALayer> _layer;
		private string _property;
		private string _key;
		private float _from;
		private float _to;
		private float _delayMilliseconds;
		private float _durationMilliseconds;
		private CAMediaTimingFunction _timingFunction;
		private long? _pausedTime;
		private float? _pausedValue;
		private Func<float, NSValue> _nsValueConversion;
		private Action _onFinish;		
		private bool _isDiscrete; // No interpolation
		private readonly Action _prepare;
		private readonly Action _cleanup;

		CAAnimation _animation = null;
		EventHandler _onAnimationStarted = null;
		EventHandler<CAAnimationStateEventArgs> _onAnimationStopped = null;

		public UnoCoreAnimation(
			CALayer layer,
			string property,
			float from,
			float to,
			float delayMilliseconds,
			float durationMilliseconds,
			CAMediaTimingFunction timingFunction,
			Func<float, NSValue> nsValueConversion,
			Action onFinish,
			bool isDiscrete,
			Action prepare = null,
			Action cleanup = null
		)
		{
			_layer = new WeakReference<CALayer>(layer);
			_property = property;
			_key = property + Interlocked.Increment(ref _id).ToStringInvariant();
			_from = from;
			_to = to;
			_delayMilliseconds = delayMilliseconds;
			_durationMilliseconds = durationMilliseconds;
			_timingFunction = timingFunction;
			_nsValueConversion = nsValueConversion;
			_onFinish = onFinish;
			_isDiscrete = isDiscrete;
			_prepare = prepare;
			_cleanup = cleanup;
		}

		public void Start()
		{
			_pausedTime = null;
			_pausedValue = null;

			ExecuteIfLayer(view => Animate(view, _from, _to, _delayMilliseconds, _durationMilliseconds));
		}

		public void Pause(long? pausedTime, float? pausedValue)
		{
			_pausedTime = pausedTime;
			_pausedValue = pausedValue;

			ExecuteIfLayer(view => StopAnimation(view));
		}

		public void Resume()
		{
			var startDelay = _delayMilliseconds;
			var duration = _durationMilliseconds;

			if (_pausedTime.HasValue)
			{
				startDelay = _pausedTime.Value > 0 ? 0 : _delayMilliseconds + _pausedTime.Value;
				duration = _pausedTime.Value > 0 ? _durationMilliseconds - _pausedTime.Value : _durationMilliseconds;
			}

			ExecuteIfLayer(view => Animate(
				view,
				_pausedValue.HasValue ? _pausedValue.Value : _from,
				_to,
				startDelay,
				duration
			));

			_pausedTime = null;
			_pausedValue = null;
		}

		public void Cancel()
		{
			_pausedTime = null;
			_pausedValue = null;

			ExecuteIfLayer(view => StopAnimation(view));
		}

		private void ExecuteIfLayer(Action<CALayer> action)
		{
			if (_layer.TryGetTarget(out var layer))
			{
				action(layer);
			}
		}

		private void Animate(CALayer layer, float from, float to, float delayMilliseconds, float durationMilliseconds)
		{
			if (_isDiscrete)
			{
				var animation = CAKeyFrameAnimation.FromKeyPath(_property);
				animation.KeyTimes = new NSNumber[] { new NSNumber(0.0), new NSNumber(1.0) };
				animation.Values = new NSObject[] { _nsValueConversion(to) };
				animation.CalculationMode = CAKeyFrameAnimation.AnimationDescrete;
				_animation = animation;
			}
			else
			{
				var animation = CABasicAnimation.FromKeyPath(_property);
				animation.From = _nsValueConversion(from);
				animation.To = _nsValueConversion(to);
				animation.TimingFunction = _timingFunction;
				_animation = animation;
			}

			if (delayMilliseconds > 0)
			{
				// Note: We must make sure to use the time relative to the 'layer', otherwise we might introduce a random delay and the animations
				//		 will run twice (once "managed" while updating the DP, and a second "native" using this animator)
				_animation.BeginTime = layer.ConvertTimeFromLayer(CAAnimation.CurrentMediaTime() + delayMilliseconds / __millisecondsPerSecond, null);
			}
			_animation.Duration = durationMilliseconds / __millisecondsPerSecond;
			_animation.FillMode = CAFillMode.Forwards;
			_animation.RemovedOnCompletion = false;

			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().DebugFormat("CoreAnimation on property {0} from {1} to {2} is starting.", _property, from, to);
			}

			_onAnimationStarted = (s, e) =>
			{
				// This will disable the transform while the native animation handles it
				// It must be the first thing we do when the animation starts
				// (However we have to wait for the first frame in order to not remove the transform while waiting for the BeginTime)
				_prepare?.Invoke();

				var anim = s as CAAnimation;

				if (anim == null)
				{
					return;
				}

				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().DebugFormat("CoreAnimation on property {0} from {1} to {2} started.", _property, from, to);
				}

				anim.AnimationStarted -= _onAnimationStarted;
				anim.AnimationStopped += _onAnimationStopped;
			};

			_onAnimationStopped = (s, e) =>
			{
				var anim = s as CAAnimation;

				if (anim == null)
				{
					return;
				}

				CATransaction.Begin();
				CATransaction.DisableActions = true;
				layer.SetValueForKeyPath(_nsValueConversion(to), new NSString(_property));
				CATransaction.Commit();
				_cleanup?.Invoke();

				anim.AnimationStopped -= _onAnimationStopped;

				if (e.Finished)
				{
					if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
					{
						this.Log().DebugFormat("CoreAnimation on property {0} from {1} to {2} finished.", _property, from, to);
					}
					
					_onFinish();

					ExecuteIfLayer(l => l.RemoveAnimation(_key));

				}
				else
				{
					if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
					{
						this.Log().DebugFormat("CoreAnimation on property {0} from {1} to {2} stopped before finishing.", _property, from, to);
					}

					anim.AnimationStarted += _onAnimationStarted;
				}
			};

			_animation.AnimationStarted += _onAnimationStarted;

			layer.AddAnimation(_animation, _key);
		}

		private void StopAnimation(CALayer layer)
		{
			layer.RemoveAnimation(_key);
			
			if (_animation == null || _onAnimationStarted == null || _onAnimationStopped == null)
			{
				return;
			}

			this.Log().DebugFormat("CoreAnimation on property {0} was forcefully stopped.", _property);

			_animation.AnimationStarted -= _onAnimationStarted;
			_animation.AnimationStopped -= _onAnimationStopped;

			_animation = null;
			_onAnimationStarted = null;
			_onAnimationStopped = null;
		}

		public void Dispose()
		{
			if (_animation != null)
			{
				ExecuteIfLayer(view => StopAnimation(view));
			}
		}
	}
}
#endif
