using System;
using System.Threading.Tasks;
using Uno;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft.Toolkit.Uwp.UI.Lottie
{
	public partial class LottieVisualSource : DependencyObject, IAnimatedVisualSource
	{
		public static readonly DependencyProperty UriSourceProperty = DependencyProperty.Register(
			"UriSource", typeof(Uri), typeof(LottieVisualSource), new PropertyMetadata(default(Uri), OnUriSourceChanged));

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
			(sender as LottieVisualSource)?.Update();
		}

		public async Task SetSourceAsync(Uri sourceUri)
		{
			UriSource = sourceUri;
		}


#if !(__WASM__ || __ANDROID__ || __IOS__ || __MACOS__)

		private void Update()
		{
		}

		public void Update(AnimatedVisualPlayer player)
		{
			throw new NotImplementedException();
		}

		public void Play(bool looped)
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
#endif
	}
}
