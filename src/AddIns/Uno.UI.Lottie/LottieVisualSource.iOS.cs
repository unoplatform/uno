using System;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Airbnb.Lottie;

namespace Microsoft.Toolkit.Uwp.UI.Lottie
{
	partial class LottieVisualSource
	{
		private LOTAnimationView _animation;

		public bool UseHardwareAcceleration { get; set; } = true;

		private bool _isPlaying = false;
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
				//_animation.Scale = (float)Scale;
				SetProperties();

				player.Add(_animation);
			}
			else
			{
				SetProperties();
			}

			void SetProperties()
			{
				_animation.SetAnimationNamed(UriSource?.PathAndQuery ?? "");

				switch (player.Stretch)
				{
					case Windows.UI.Xaml.Media.Stretch.None:
						_animation.ContentMode = UIKit.UIViewContentMode.Center;
						break;
					case Windows.UI.Xaml.Media.Stretch.Uniform:
						_animation.ContentMode = UIKit.UIViewContentMode.ScaleAspectFit;
						break;
					case Windows.UI.Xaml.Media.Stretch.Fill:
						_animation.ContentMode = UIKit.UIViewContentMode.ScaleToFill;
						break;
					case Windows.UI.Xaml.Media.Stretch.UniformToFill:
						_animation.ContentMode = UIKit.UIViewContentMode.ScaleAspectFill;
						break;

				}


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
			_animation.LoopAnimation = looped;
			_animation.Play();
		}

		public void Stop()
		{
			_isPlaying = false;
			_animation.Stop();
		}

		public void Pause()
		{
			_animation.Pause();
			_isPlaying = false;
		}

		public void Resume()
		{
			_animation.Play();
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
				_animation.Play();
			}
		}

		public void Unload()
		{
			if (_isPlaying)
			{
				_animation.Pause();
			}
		}

		Size IAnimatedVisualSource.Measure(Size availableSize)
		{
			throw new NotImplementedException();
		}
	}
}
