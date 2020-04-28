using System;
using System.Threading.Tasks;
using Uno;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Uno.UI;

namespace Microsoft.Toolkit.Uwp.UI.Lottie
{
	public partial class LottieVisualSource : DependencyObject, IAnimatedVisualSource
	{
		private AnimatedVisualPlayer _player;

		public static readonly DependencyProperty UriSourceProperty = DependencyProperty.Register(
			"UriSource",
			typeof(Uri),
			typeof(LottieVisualSource),
			new FrameworkPropertyMetadata(
				default(Uri),
				FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange,
				OnUriSourceChanged));

		public Uri UriSource
		{
			get => (Uri)GetValue(UriSourceProperty);
			set => SetValue(UriSourceProperty, value);
		}

		public static readonly DependencyProperty OptionsProperty = DependencyProperty.Register(
			"Options", typeof(LottieVisualOptions), typeof(LottieVisualSource), new PropertyMetadata(LottieVisualOptions.None));

		[NotImplemented]
		public LottieVisualOptions Options
		{
			get => (LottieVisualOptions)GetValue(OptionsProperty);
			set => SetValue(OptionsProperty, value);
		}

		[NotImplemented]
		public static LottieVisualSource CreateFromString(string uri)
		{
			throw new NotImplementedException();
		}


		private static void OnUriSourceChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			if (sender is LottieVisualSource source)
			{
				source.Update(source._player);
			}
		}

		public Task SetSourceAsync(Uri sourceUri)
		{
			UriSource = sourceUri;

			// TODO: this method should not return before the animation is ready.

			return Task.CompletedTask;
		}


#if !(__WASM__ || __ANDROID__ || __IOS__ || __MACOS__)

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

		public Size Measure(Size availableSize)
		{
			throw new NotImplementedException();
		}

		private readonly Size CompositionSize = default;
#endif
		public void Update(AnimatedVisualPlayer player)
		{
			_player = player;
			if (_player != null)
			{
				InnerUpdate();
			}
		}

		partial void InnerUpdate();

		private void SetIsPlaying(bool isPlaying) => _player?.SetValue(AnimatedVisualPlayer.IsPlayingProperty, isPlaying);

		Size IAnimatedVisualSource.Measure(Size availableSize)
		{
			var compositionSize = CompositionSize;
			var stretch = _player.Stretch;

			if (stretch == Stretch.None)
			{
				return compositionSize;
			}

			var availableWidth = availableSize.Width;
			var availableHeight = availableSize.Height;

			if (double.IsInfinity(availableWidth))
			{
				if (double.IsInfinity(availableHeight))
				{
					return compositionSize;
				}

				return new Size(availableHeight * compositionSize.Width / compositionSize.Height, availableHeight);
			}

			if (double.IsInfinity(availableHeight))
			{
				return new Size(availableWidth, availableWidth * compositionSize.Height / compositionSize.Width);
			}

			return availableSize;
		}
	}
}
