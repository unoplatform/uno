#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.UI.Xaml.Controls
{
	#if false || false || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || false
	[global::Uno.NotImplemented("NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
	#endif
	public  partial class MediaPlayerElement : global::Microsoft.UI.Xaml.Controls.Control
	{
		#if false || false || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
		public  global::Microsoft.UI.Xaml.Controls.MediaTransportControls TransportControls
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaTransportControls MediaPlayerElement.TransportControls is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=MediaTransportControls%20MediaPlayerElement.TransportControls");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.MediaPlayerElement", "MediaTransportControls MediaPlayerElement.TransportControls");
			}
		}
		#endif
		#if false || false || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
		public  global::Microsoft.UI.Xaml.Media.Stretch Stretch
		{
			get
			{
				return (global::Microsoft.UI.Xaml.Media.Stretch)this.GetValue(StretchProperty);
			}
			set
			{
				this.SetValue(StretchProperty, value);
			}
		}
		#endif
		#if false || false || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
		public  global::Windows.Media.Playback.IMediaPlaybackSource Source
		{
			get
			{
				return (global::Windows.Media.Playback.IMediaPlaybackSource)this.GetValue(SourceProperty);
			}
			set
			{
				this.SetValue(SourceProperty, value);
			}
		}
		#endif
		#if false || false || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
		public  global::Microsoft.UI.Xaml.Media.ImageSource PosterSource
		{
			get
			{
				return (global::Microsoft.UI.Xaml.Media.ImageSource)this.GetValue(PosterSourceProperty);
			}
			set
			{
				this.SetValue(PosterSourceProperty, value);
			}
		}
		#endif
		#if false || false || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
		public  bool IsFullWindow
		{
			get
			{
				return (bool)this.GetValue(IsFullWindowProperty);
			}
			set
			{
				this.SetValue(IsFullWindowProperty, value);
			}
		}
		#endif
		#if false || false || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
		public  bool AutoPlay
		{
			get
			{
				return (bool)this.GetValue(AutoPlayProperty);
			}
			set
			{
				this.SetValue(AutoPlayProperty, value);
			}
		}
		#endif
		#if false || false || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
		public  bool AreTransportControlsEnabled
		{
			get
			{
				return (bool)this.GetValue(AreTransportControlsEnabledProperty);
			}
			set
			{
				this.SetValue(AreTransportControlsEnabledProperty, value);
			}
		}
		#endif
		#if false || false || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
		public  global::Windows.Media.Playback.MediaPlayer MediaPlayer
		{
			get
			{
				return (global::Windows.Media.Playback.MediaPlayer)this.GetValue(MediaPlayerProperty);
			}
		}
		#endif
		#if false || false || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
		public static global::Microsoft.UI.Xaml.DependencyProperty AreTransportControlsEnabledProperty { get; } = 
		Microsoft.UI.Xaml.DependencyProperty.Register(
			nameof(AreTransportControlsEnabled), typeof(bool), 
			typeof(global::Microsoft.UI.Xaml.Controls.MediaPlayerElement), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
		public static global::Microsoft.UI.Xaml.DependencyProperty AutoPlayProperty { get; } = 
		Microsoft.UI.Xaml.DependencyProperty.Register(
			nameof(AutoPlay), typeof(bool), 
			typeof(global::Microsoft.UI.Xaml.Controls.MediaPlayerElement), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
		public static global::Microsoft.UI.Xaml.DependencyProperty IsFullWindowProperty { get; } = 
		Microsoft.UI.Xaml.DependencyProperty.Register(
			nameof(IsFullWindow), typeof(bool), 
			typeof(global::Microsoft.UI.Xaml.Controls.MediaPlayerElement), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
		public static global::Microsoft.UI.Xaml.DependencyProperty MediaPlayerProperty { get; } = 
		Microsoft.UI.Xaml.DependencyProperty.Register(
			nameof(MediaPlayer), typeof(global::Windows.Media.Playback.MediaPlayer), 
			typeof(global::Microsoft.UI.Xaml.Controls.MediaPlayerElement), 
			new FrameworkPropertyMetadata(default(global::Windows.Media.Playback.MediaPlayer)));
		#endif
		#if false || false || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
		public static global::Microsoft.UI.Xaml.DependencyProperty PosterSourceProperty { get; } = 
		Microsoft.UI.Xaml.DependencyProperty.Register(
			nameof(PosterSource), typeof(global::Microsoft.UI.Xaml.Media.ImageSource), 
			typeof(global::Microsoft.UI.Xaml.Controls.MediaPlayerElement), 
			new FrameworkPropertyMetadata(default(global::Microsoft.UI.Xaml.Media.ImageSource)));
		#endif
		#if false || false || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
		public static global::Microsoft.UI.Xaml.DependencyProperty SourceProperty { get; } = 
		Microsoft.UI.Xaml.DependencyProperty.Register(
			nameof(Source), typeof(global::Windows.Media.Playback.IMediaPlaybackSource), 
			typeof(global::Microsoft.UI.Xaml.Controls.MediaPlayerElement), 
			new FrameworkPropertyMetadata(default(global::Windows.Media.Playback.IMediaPlaybackSource)));
		#endif
		#if false || false || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
		public static global::Microsoft.UI.Xaml.DependencyProperty StretchProperty { get; } = 
		Microsoft.UI.Xaml.DependencyProperty.Register(
			nameof(Stretch), typeof(global::Microsoft.UI.Xaml.Media.Stretch), 
			typeof(global::Microsoft.UI.Xaml.Controls.MediaPlayerElement), 
			new FrameworkPropertyMetadata(default(global::Microsoft.UI.Xaml.Media.Stretch)));
		#endif
		#if false || false || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
		public MediaPlayerElement() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.MediaPlayerElement", "MediaPlayerElement.MediaPlayerElement()");
		}
		#endif
		// Forced skipping of method Microsoft.UI.Xaml.Controls.MediaPlayerElement.MediaPlayerElement()
		// Forced skipping of method Microsoft.UI.Xaml.Controls.MediaPlayerElement.Source.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.MediaPlayerElement.Source.set
		// Forced skipping of method Microsoft.UI.Xaml.Controls.MediaPlayerElement.TransportControls.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.MediaPlayerElement.TransportControls.set
		// Forced skipping of method Microsoft.UI.Xaml.Controls.MediaPlayerElement.AreTransportControlsEnabled.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.MediaPlayerElement.AreTransportControlsEnabled.set
		// Forced skipping of method Microsoft.UI.Xaml.Controls.MediaPlayerElement.PosterSource.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.MediaPlayerElement.PosterSource.set
		// Forced skipping of method Microsoft.UI.Xaml.Controls.MediaPlayerElement.Stretch.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.MediaPlayerElement.Stretch.set
		// Forced skipping of method Microsoft.UI.Xaml.Controls.MediaPlayerElement.AutoPlay.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.MediaPlayerElement.AutoPlay.set
		// Forced skipping of method Microsoft.UI.Xaml.Controls.MediaPlayerElement.IsFullWindow.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.MediaPlayerElement.IsFullWindow.set
		// Forced skipping of method Microsoft.UI.Xaml.Controls.MediaPlayerElement.MediaPlayer.get
		#if false || false || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
		public  void SetMediaPlayer( global::Windows.Media.Playback.MediaPlayer mediaPlayer)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.MediaPlayerElement", "void MediaPlayerElement.SetMediaPlayer(MediaPlayer mediaPlayer)");
		}
		#endif
		// Forced skipping of method Microsoft.UI.Xaml.Controls.MediaPlayerElement.SourceProperty.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.MediaPlayerElement.AreTransportControlsEnabledProperty.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.MediaPlayerElement.PosterSourceProperty.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.MediaPlayerElement.StretchProperty.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.MediaPlayerElement.AutoPlayProperty.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.MediaPlayerElement.IsFullWindowProperty.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.MediaPlayerElement.MediaPlayerProperty.get
	}
}
