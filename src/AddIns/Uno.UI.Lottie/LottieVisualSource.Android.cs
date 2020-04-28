using System;
using Android.Animation;
using Android.Widget;
using Com.Airbnb.Lottie;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;

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

		private string _lastPath = "";

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
				var path = UriSource?.PathAndQuery ?? "";
				if (path.StartsWith("/"))
				{
					path = path.Substring(1);
				}
				if (_lastPath != path)
				{
					_animation.SetAnimation(path);
					_lastPath = path;

					if (player.AutoPlay)
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
						_animation.SetScaleType(ImageView.ScaleType.FitXy);
						break;
					case Windows.UI.Xaml.Media.Stretch.UniformToFill:
						_animation.SetScaleType(ImageView.ScaleType.CenterCrop);
						break;
				}

				_animation.Speed = (float)player.PlaybackRate;
			}
		}

		private void SetDuration()
		{
			var duration = TimeSpan.FromMilliseconds(_animation.Duration);
			_player?.SetValue(AnimatedVisualPlayer.DurationProperty, duration);
			_player?.SetValue(AnimatedVisualPlayer.IsAnimatedVisualLoadedProperty, duration > TimeSpan.Zero);
		}

		public void Play(double fromProgress, double toProgress, bool looped)
		{
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
			_animation.CancelAnimation();
		}

		public void Pause()
		{
			_animation.PauseAnimation();
		}

		public void Resume()
		{
			_animation.ResumeAnimation();
		}

		public void SetProgress(double progress)
		{
			_animation.Progress = (float)progress;
		}

		public void Load()
		{
			if (_player.IsPlaying)
			{
				_animation.ResumeAnimation();
			}
		}

		public void Unload()
		{
			if (_player.IsPlaying)
			{
				_animation.PauseAnimation();
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
