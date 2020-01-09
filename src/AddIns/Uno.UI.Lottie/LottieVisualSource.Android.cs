using Android.Animation;
using Com.Airbnb.Lottie;
using System;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;

namespace Microsoft.Toolkit.Uwp.UI.Lottie
{
	partial class LottieVisualSource
	{
		private LottieAnimationView _animation;

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
				_animation = new LottieAnimationView(Android.App.Application.Context);
				_animation.EnableMergePathsForKitKatAndAbove(true);
				_animation.SetRenderMode(RenderMode.Hardware);

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
				_animation.SetAnimation(UriSource?.PathAndQuery ?? "");

				if(player.AutoPlay && !_isPlaying)
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
			throw new NotImplementedException();
		}
	}
}
