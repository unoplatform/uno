using System;
using Android.Animation;
using Android.Widget;
using Com.Airbnb.Lottie;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Android.Views;
using Uno.UI;
using ViewHelper = Uno.UI.ViewHelper;

namespace Microsoft.Toolkit.Uwp.UI.Lottie
{
	partial class LottieVisualSource
	{
		private LottieAnimationView _animation;

		private LottieListener _listener;

		private class LottieListener : AnimatorListenerAdapter
		{
			private readonly LottieVisualSource _lottieVisualSource;

			public LottieListener(LottieVisualSource lottieVisualSource)
			{
				_lottieVisualSource = lottieVisualSource;
			}

			public override void OnAnimationCancel(Animator animation)
			{
				_lottieVisualSource.SetIsPlaying(false);
			}

			public override void OnAnimationEnd(Animator animation)
			{
				_lottieVisualSource.SetIsPlaying(false);
			}

			public override void OnAnimationPause(Animator animation)
			{
				_lottieVisualSource.SetIsPlaying(false);
			}

			public override void OnAnimationResume(Animator animation)
			{
				_lottieVisualSource.SetIsPlaying(true);
			}

			public override void OnAnimationStart(Animator animation)
			{
				_lottieVisualSource.SetIsPlaying(true);
			}
		}

		public bool UseHardwareAcceleration { get; set; } = true;

		private Uri _lastSource;
		private (double fromProgress, double toProgress, bool looped)? _playState;

		partial void InnerUpdate()
		{
			var player = _player;

			if (_animation == null)
			{
				_listener = new LottieListener(this);

				_animation = new LottieAnimationView(Android.App.Application.Context);
				_animation.EnableMergePathsForKitKatAndAbove(true);

				_animation.AddAnimatorListener(_listener);

				SetProperties();

				player.AddView(_animation);
			}
			else
			{
				SetProperties();
			}

			void SetProperties()
			{
				var source = UriSource;
				if(_lastSource == null || !_lastSource.Equals(source))
				{
					_lastSource = source;

					if (TryLoadEmbeddedJson(source, out var json))
					{
						_animation.SetAnimationFromJson(json, source.OriginalString);
					}
					else
					{
						var path = source?.PathAndQuery ?? "";
						if (path.StartsWith("/"))
						{
							path = path.Substring(1);
						}

						_animation.SetAnimation(path);
					}

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

			_animation.Measure(
				ViewHelper.MakeMeasureSpec((int)physicalSize.Width, MeasureSpecMode.AtMost),
				ViewHelper.MakeMeasureSpec((int)physicalSize.Height, MeasureSpecMode.AtMost)
			);
		}

		private void SetDuration()
		{
			var duration = TimeSpan.FromMilliseconds(_animation.Duration);
			_player?.SetValue(AnimatedVisualPlayer.DurationProperty, duration);
			_player?.SetValue(AnimatedVisualPlayer.IsAnimatedVisualLoadedProperty, duration > TimeSpan.Zero);
		}

		public void Play(double fromProgress, double toProgress, bool looped)
		{
			_playState = (fromProgress, toProgress, looped);
			if (_player == null)
			{
				return;
			}
			SetIsPlaying(true);
#if __ANDROID_26__
			_animation.RepeatCount = looped ? ValueAnimator.Infinite : 0; // Repeat count doesn't include first time.
#else
			_animation.Loop(looped);
#endif
			_animation.SetMinProgress((float)fromProgress);
			_animation.SetMaxProgress((float)toProgress);
			_animation.PlayAnimation();
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
