using System;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Airbnb.Lottie;
using Foundation;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.Logging;
using Uno.UI;
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

		private Uri _lastSource;
		private (double fromProgress, double toProgress, bool looped)? _playState;

		partial void InnerUpdate()
		{
			var player = _player;
			SetProperties();

			void SetProperties()
			{
				var source = UriSource;
				if (_lastSource == null || !_lastSource.Equals(source))
				{
					_lastSource = source;

					if (TryLoadEmbeddedJson(source, out var json))
					{
						var jsonData = NSJsonSerialization.Deserialize(NSData.FromString(json), default, out var _) as NSDictionary;
						if (jsonData != null)
						{
							var animation = LOTAnimationView.AnimationFromJSON(jsonData);
							SetAnimation(animation);
						}
					}
					else
					{
						var path = source?.PathAndQuery ?? "";
						if (path.StartsWith("/"))
						{
							path = path.Substring(1);
						}

						if (_animation == null)
						{
							var animation = new LOTAnimationView();
							SetAnimation(animation);
						}

						_animation.SetAnimationNamed(path);
					}

					// Force layout to recalculate
					player.InvalidateMeasure();
					player.InvalidateArrange();

					if (_playState != null)
					{
						var (fromProgress, toProgress, looped) = _playState.Value;
						Play(fromProgress, toProgress, looped);
					}
					else if (player.AutoPlay)
					{
						Play(0, 1, true);
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
		}

		private void SetAnimation(LOTAnimationView animation)
		{
			if (!ReferenceEquals(_animation, animation))
			{
				_animation?.RemoveFromSuperview();
			}
#if __IOS__
			_player.Add(animation);
#else
			_player.AddSubview(animation);
#endif
			_animation = animation;
		}

		public void Play(double fromProgress, double toProgress, bool looped)
		{
			_playState = (fromProgress, toProgress, looped);
			if (_animation != null)
			{
				if (_animation.IsAnimationPlaying)
				{
					_animation.Stop();
				}

				_animation.LoopAnimation = looped;

				void Start()
				{
					_animation.PlayFromProgress((nfloat)fromProgress, (nfloat)toProgress, isFinished =>
					{
						if (looped && isFinished)
						{
							Start();
						}
					});
				}

				Start();
				SetIsPlaying(true);
			}
		}

		public void Stop()
		{
			_playState = null;
			SetIsPlaying(false);
			_animation?.Stop();
		}

		public void Pause()
		{
			SetIsPlaying(false);
			_animation?.Pause();
		}

		public void Resume()
		{
			_animation?.Play();
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
				_animation?.Play();
			}
		}

		public void Unload()
		{
			if (_player.IsPlaying)
			{
				_animation?.Pause();
			}
		}

		private Size CompositionSize => _animation?.IntrinsicContentSize ?? default;
	}
}
