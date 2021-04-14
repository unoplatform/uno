#if !NET6_0_IOS
using System;
using System.Threading;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Airbnb.Lottie;
using Foundation;
using System.Threading.Tasks;
using Uno.Disposables;
#if __IOS__
using _ViewContentMode = UIKit.UIViewContentMode;
#else
using _ViewContentMode = Airbnb.Lottie.LOTViewContentMode;
#endif

namespace Microsoft.Toolkit.Uwp.UI.Lottie
{
	partial class LottieVisualSourceBase
	{
		private LOTAnimationView? _animation;

		public bool UseHardwareAcceleration { get; set; } = true;

		private Uri? _lastSource;
		private (double fromProgress, double toProgress, bool looped)? _playState;

		private readonly SerialDisposable _animationDataSubscription = new SerialDisposable();

		async Task InnerUpdate(CancellationToken ct)
		{
			var player = _player;

			if (player == null)
			{
				return;
			}

			await SetProperties();

			async Task SetProperties()
			{
				var sourceUri = UriSource;
				if (_lastSource == null || !_lastSource.Equals(sourceUri))
				{
					_lastSource = sourceUri;
					if ((await TryLoadDownloadJson(sourceUri, ct)) is { } jsonStream)
					{
						var cacheKey = sourceUri.OriginalString;
						_animationDataSubscription.Disposable = null;
						_animationDataSubscription.Disposable =
							LoadAndObserveAnimationData(jsonStream, cacheKey, OnJsonChanged);

						void OnJsonChanged(string updatedJson, string updatedCacheKey)
						{
							var jsonData = NSJsonSerialization.Deserialize(NSData.FromString(updatedJson), default, out var _) as NSDictionary;
							var animation = LOTAnimationView.AnimationFromJSON(jsonData);
							SetAnimation(animation);

							if (_playState != null)
							{
								var (fromProgress, toProgress, looped) = _playState.Value;
								Play(fromProgress, toProgress, looped);
							}
						}
					}
					else
					{
						var path = sourceUri?.PathAndQuery ?? "";
						if (path.StartsWith("/"))
						{
							path = path.Substring(1);
						}

						if (_animation == null)
						{
							var animation = new LOTAnimationView();
							_animation = SetAnimation(animation);
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

				if (_animation == null)
				{
					return;
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

		private LOTAnimationView SetAnimation(LOTAnimationView animation)
		{
			if (!ReferenceEquals(_animation, animation))
			{
				_animation?.RemoveFromSuperview();
			}
#if __IOS__
			_player?.Add(animation);
#else
			_player?.AddSubview(animation);
#endif
			_animation = animation;
			return animation;
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
			if (_player?.IsPlaying ?? false)
			{
				_animation?.Play();
			}
		}

		public void Unload()
		{
			if (_player?.IsPlaying ?? false)
			{
				_animation?.Pause();
			}
		}

		private Size CompositionSize => _animation?.IntrinsicContentSize ?? default;
	}
}
#else
using System;
using System.Threading;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Foundation;
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
