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

		public bool UseHardwareAcceleration { get; set; } = true;

		private bool _isPlaying = false;
		private string _lastPath = "";

		private AnimatedVisualPlayer _player;

		private void Update()
		{
			if (_player != null)
			{
				Update(_player);
			}
		}

		public void Update(AnimatedVisualPlayer player)
		{
			if (_animation == null)
			{
				_animation = new LottieAnimationView(Android.App.Application.Context);
				_animation.EnableMergePathsForKitKatAndAbove(true);
				_animation.UseHardwareAcceleration(UseHardwareAcceleration);

				//_animation.Scale = (float)Scale;
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

				if (player.AutoPlay && !_isPlaying)
				{
					Play(true);
				}
			}
			
			_player = player;
		}

		public void Play(bool looped)
		{
			_isPlaying = true;
#if __ANDROID_26__
			_animation.RepeatCount = ValueAnimator.Infinite;
#else
			_animation.Loop(looped);
#endif
			_animation.PlayAnimation();
		}

		public void Stop()
		{
			_isPlaying = false;
			_animation.CancelAnimation();
		}

		public void Pause()
		{
			_animation.PauseAnimation();
			_isPlaying = false;
		}

		public void Resume()
		{
			_animation.ResumeAnimation();
			_isPlaying = true;
		}

		public void SetProgress(double progress)
		{
			// TODO
		}

		public void Load()
		{
			if (_isPlaying)
			{
				_animation.ResumeAnimation();
			}
		}

		public void Unload()
		{
			if (_isPlaying)
			{
				_animation.PauseAnimation();
			}
		}

		Size IAnimatedVisualSource.Measure(Size availableSize)
		{
			return availableSize;
		}
	}
}
