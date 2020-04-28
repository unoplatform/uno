using System;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Airbnb.Lottie;

#if __IOS__
using _ViewContentMode = UIKit.UIViewContentMode;
#else
using _ViewContentMode = Airbnb.Lottie.LOTViewContentMode;
#endif

namespace Microsoft.Toolkit.Uwp.UI.Lottie
{
	partial class LottieVisualSource
	{
		private LOTAnimationView _animation;

		public bool UseHardwareAcceleration { get; set; } = true;

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
				_animation = new LOTAnimationView();
				SetProperties();
#if __IOS__
				player.Add(_animation);
#else
				player.AddSubview(_animation);
#endif
			}
			else
			{
				SetProperties();
			}

			void SetProperties()
			{
				var path = UriSource?.PathAndQuery ?? "";
				if (_lastPath != path)
				{
					_animation.SetAnimationNamed(path);
					_lastPath = path;

					if (player.AutoPlay)
					{
						Play(true);
					}
				}

				switch (player.Stretch)
				{
					case Windows.UI.Xaml.Media.Stretch.None:
						_animation.ContentMode = _ViewContentMode.Center;
						break;
					case Windows.UI.Xaml.Media.Stretch.Uniform:
						_animation.ContentMode = _ViewContentMode.ScaleAspectFit;
						break;
					case Windows.UI.Xaml.Media.Stretch.Fill:
						_animation.ContentMode = _ViewContentMode.ScaleToFill;
						break;
					case Windows.UI.Xaml.Media.Stretch.UniformToFill:
						_animation.ContentMode = _ViewContentMode.ScaleAspectFill;
						break;
				}

				var duration = TimeSpan.FromSeconds(_animation.AnimationDuration);
				player.SetValue(AnimatedVisualPlayer.DurationProperty, duration);

				var isLoaded = duration > TimeSpan.Zero;
				player.SetValue(AnimatedVisualPlayer.IsAnimatedVisualLoadedProperty, isLoaded);

				_animation.CompletionBlock = isCompleted =>
				{
					SetIsPlaying(_animation.IsAnimationPlaying);
				};

				_animation.AnimationSpeed = (nfloat)player.PlaybackRate;
			}

			_player = player;
		}

		public void Play(bool looped)
		{
			_animation.LoopAnimation = looped;
			_animation.Play();
			SetIsPlaying(true);
		}

		public void Stop()
		{
			SetIsPlaying(false);
			_animation.Stop();
		}

		public void Pause()
		{
			SetIsPlaying(false);
			_animation.Pause();
		}

		public void Resume()
		{
			_animation.Play();
			SetIsPlaying(true);
		}

		public void SetProgress(double progress)
		{
			if (_animation != null)
			{
				_animation.AnimationProgress = (nfloat)progress;
			}
		}

		public void Load()
		{
			if (_player.IsPlaying)
			{
				_animation.Play();
			}
		}

		public void Unload()
		{
			if (_player.IsPlaying)
			{
				_animation.Pause();
			}
		}

		Size IAnimatedVisualSource.Measure(Size availableSize)
		{


			return availableSize;
		}
	}
}
