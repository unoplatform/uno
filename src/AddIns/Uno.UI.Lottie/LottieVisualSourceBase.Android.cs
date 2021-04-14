#if !NET6_0
using System;
using System.Threading;
using Android.Animation;
using Android.Widget;
using Com.Airbnb.Lottie;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Android.Views;
using Uno.Disposables;
using Uno.UI;
using ViewHelper = Uno.UI.ViewHelper;
using System.Threading.Tasks;

namespace Microsoft.Toolkit.Uwp.UI.Lottie
{
	partial class LottieVisualSourceBase
	{
		private LottieAnimationView? _animation;

		private LottieListener? _listener;

		private class LottieListener : AnimatorListenerAdapter
		{
			private readonly LottieVisualSourceBase _lottieVisualSource;

			public LottieListener(LottieVisualSourceBase lottieVisualSource)
			{
				_lottieVisualSource = lottieVisualSource;
			}

			public override void OnAnimationCancel(Animator? animation) => _lottieVisualSource.SetIsPlaying(false);

			public override void OnAnimationEnd(Animator? animation) => _lottieVisualSource.SetIsPlaying(false);

			public override void OnAnimationPause(Animator? animation) => _lottieVisualSource.SetIsPlaying(false);

			public override void OnAnimationResume(Animator? animation) => _lottieVisualSource.SetIsPlaying(true);

			public override void OnAnimationStart(Animator? animation) => _lottieVisualSource.SetIsPlaying(true);
		}

		public bool UseHardwareAcceleration { get; set; } = true;

		private Uri? _lastSource;
		private (double fromProgress, double toProgress, bool looped)? _playState;

		private readonly SerialDisposable _animationDataSubscription = new SerialDisposable();

		async Task InnerUpdate(CancellationToken ct)
		{
			if (!(_player is { } player))
			{
				return;
			}

			if (_animation == null)
			{
				_listener = new LottieListener(this);

				_animation = new LottieAnimationView(Android.App.Application.Context);
				_animation.EnableMergePathsForKitKatAndAbove(true);

				_animation.AddAnimatorListener(_listener);

				await SetProperties();

				// Add the player after
				player.AddView(_animation);
			}
			else
			{
				await SetProperties();
			}

			async Task SetProperties()
			{
				var sourceUri = UriSource;
				if(_lastSource == null || !_lastSource.Equals(sourceUri))
				{
					_lastSource = sourceUri;

					if ((await TryLoadDownloadJson(sourceUri, ct)) is { } jsonStream)
					{
						var first = true;

						var cacheKey = sourceUri.OriginalString;
						_animationDataSubscription.Disposable = null;
						_animationDataSubscription.Disposable =
							LoadAndObserveAnimationData(jsonStream, cacheKey, OnJsonChanged);

						void OnJsonChanged(string updatedJson, string updatedCacheKey)
						{
							_animation.SetAnimationFromJson(updatedJson, updatedCacheKey);

							if (_playState != null)
							{
								var (fromProgress, toProgress, looped) = _playState.Value;
								Play(fromProgress, toProgress, looped);
							}
							else if (player.AutoPlay && first)
							{
								Play(0, 1, true);
							}

							first = false;
							SetDuration();
						}
					}
					else
					{
						var path = sourceUri?.PathAndQuery ?? "";
						if (path.StartsWith("/"))
						{
							path = path.Substring(1);
						}

						_animation.SetAnimation(path);

						if (_playState != null)
						{
							var (fromProgress, toProgress, looped) = _playState.Value;
							Play(fromProgress, toProgress, looped);
						}
						else if (player.AutoPlay)
						{
							Play(0, 1, true);
						}

						SetDuration();
					}
				}

				switch (player.Stretch)
				{
					case Windows.UI.Xaml.Media.Stretch.None:
						_animation.SetScaleType(ImageView.ScaleType.Center);
						break;
					case Windows.UI.Xaml.Media.Stretch.Uniform:
						_animation.SetScaleType(ImageView.ScaleType.CenterInside);
						break;
					case Windows.UI.Xaml.Media.Stretch.Fill:
						_animation.SetScaleType(ImageView.ScaleType.FitCenter);
						break;
					case Windows.UI.Xaml.Media.Stretch.UniformToFill:
						_animation.SetScaleType(ImageView.ScaleType.CenterCrop);
						break;
				}

				_animation.Speed = (float)player.PlaybackRate;
			}
		}

		partial void InnerMeasure(Size size)
		{
			var physicalSize = size.LogicalToPhysicalPixels();

			_animation?.Measure(
				ViewHelper.MakeMeasureSpec((int)physicalSize.Width, MeasureSpecMode.AtMost),
				ViewHelper.MakeMeasureSpec((int)physicalSize.Height, MeasureSpecMode.AtMost)
			);
		}

		private void SetDuration()
		{
			if (_animation is { } animation)
			{
				var duration = TimeSpan.FromMilliseconds(animation.Duration);
				_player?.SetValue(AnimatedVisualPlayer.DurationProperty, duration);
				_player?.SetValue(AnimatedVisualPlayer.IsAnimatedVisualLoadedProperty, duration > TimeSpan.Zero);
			}
		}

		public void Play(double fromProgress, double toProgress, bool looped)
		{
			_playState = (fromProgress, toProgress, looped);
			if (_player == null)
			{
				return;
			}
			SetIsPlaying(true);
			if (_animation is { } animation)
			{
#if __ANDROID_26__
				animation.RepeatCount =
					looped ? ValueAnimator.Infinite : 0; // Repeat count doesn't include first time.
#else
				animation.Loop(looped);
#endif
				animation.SetMinProgress((float)fromProgress);
				animation.SetMaxProgress((float)toProgress);
				animation.PlayAnimation();
			}
		}

		public void Stop()
		{
			_playState = null;
			if (_player == null)
			{
				return;
			}
			_animation?.CancelAnimation();
		}

		public void Pause()
		{
			_animation?.PauseAnimation();
		}

		public void Resume()
		{
			_animation?.ResumeAnimation();
		}

		public void SetProgress(double progress)
		{
			if (_animation == null)
			{
				return;
			}

			//If setting Progress directly, we should have the full range to choose from
			//Will be overridden in the Play method
			_animation.SetMinAndMaxProgress(0f, 1f);
			_animation.Progress = (float)progress;
		}

		public void Load()
		{
			if (_player?.IsPlaying ?? false)
			{
				_animation?.ResumeAnimation();
			}
		}

		public void Unload()
		{
			if (_player?.IsPlaying ?? false)
			{
				_animation?.PauseAnimation();
			}
		}

		private Size CompositionSize
		{
			get
			{
				var composition = _animation?.Composition;
				if (composition != null)
				{
					SetDuration();

					var bounds = composition.Bounds;

					return new Size(bounds.Width(), bounds.Height());
				}

				return default;
			}
		}
	}
}
#else
using System;
using System.Threading;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using System.Threading.Tasks;
using Uno.Disposables;

namespace Microsoft.Toolkit.Uwp.UI.Lottie
{
	partial class LottieVisualSourceBase
	{
		public bool UseHardwareAcceleration { get; set; } = true;

		async Task InnerUpdate(CancellationToken ct)
		{

		}

		public void Play(double fromProgress, double toProgress, bool looped)
		{
			throw new NotImplementedException();
		}

		public void Stop()
		{
			throw new NotImplementedException();
		}

		public void Pause()
		{
			throw new NotImplementedException();
		}

		public void Resume()
		{
			throw new NotImplementedException();
		}

		public void SetProgress(double progress)
		{
			throw new NotImplementedException();
		}

		public void Load()
		{
			throw new NotImplementedException();
		}

		public void Unload()
		{
			throw new NotImplementedException();
		}

		private Size CompositionSize => default;
	}
}
#endif

