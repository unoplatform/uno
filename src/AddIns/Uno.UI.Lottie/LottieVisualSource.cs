using System;
using System.Buffers.Text;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Uno;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.Extensions.Specialized;
using Uno.Logging;
using Uno.UI;
using Windows.UI.Composition;

namespace Microsoft.Toolkit.Uwp.UI.Lottie
{
	public partial class LottieVisualSource : DependencyObject, IAnimatedVisualSource
	{
		private AnimatedVisualPlayer _player;

		public static DependencyProperty UriSourceProperty { get ; } = DependencyProperty.Register(
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

		public static DependencyProperty OptionsProperty { get ; } = DependencyProperty.Register(
			"Options", typeof(LottieVisualOptions), typeof(LottieVisualSource), new FrameworkPropertyMetadata(LottieVisualOptions.None));

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

#if HAS_UNO_WINUI
		[NotImplemented]
		public IAnimatedVisual TryCreateAnimatedVisual(Compositor compositor, out object diagnostics)
		{
			throw new NotImplementedException();
		}
#endif

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
			if (compositionSize == default)
			{
				return default;
			}

			var stretch = _player.Stretch;

			if (stretch == Stretch.None)
			{
				return compositionSize;
			}

			var availableWidth = availableSize.Width;
			var availableHeight = availableSize.Height;

			var resultSize = availableSize;

			if (double.IsInfinity(availableWidth))
			{
				if (double.IsInfinity(availableHeight))
				{
					return compositionSize;
				}

				resultSize = new Size(availableHeight * compositionSize.Width / compositionSize.Height, availableHeight);
			}

			if (double.IsInfinity(availableHeight))
			{
				resultSize = new Size(availableWidth, availableWidth * compositionSize.Height / compositionSize.Width);
			}

			InnerMeasure(resultSize);

			return resultSize;
		}

		partial void InnerMeasure(Size size);

		private bool TryLoadEmbeddedJson(Uri uri, out string json)
		{
			if (uri.Scheme != "embedded")
			{
				json = null;
				return false;
			}

			var assemblyName = uri.Host;

			Assembly assembly;
			if (assemblyName == ".")
			{
				assembly = Application.Current.GetType().Assembly;
			}
			else
			{
				assembly = Assembly.Load(assemblyName);
			}

			if (assembly == null)
			{
				json = null;
				return false;
			}

			var resourceName = uri.AbsolutePath.Substring(1).Replace("(assembly)", assembly.GetName().Name);
			using var stream = assembly.GetManifestResourceStream(resourceName);
			if (stream == null)
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn($"Unable to find embedded resource named '{resourceName}' to load.");
				}
				json = null;
				return false;
			}

			var bytes = new byte[(int)stream.Length];
			stream.Read(bytes, 0, bytes.Length);
			json = Encoding.UTF8.GetString(bytes);
			return true;
		}
	}
}
