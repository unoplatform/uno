#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.UI.Xaml.Controls
{
	#if false || false || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || false
	[global::Uno.NotImplemented("NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
	#endif
	public  partial class MediaPlayerPresenter 
	{
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
		public  global::Windows.Media.Playback.MediaPlayer MediaPlayer
		{
			get
			{
				return (global::Windows.Media.Playback.MediaPlayer)this.GetValue(MediaPlayerProperty);
			}
			set
			{
				this.SetValue(MediaPlayerProperty, value);
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
		public static global::Microsoft.UI.Xaml.DependencyProperty IsFullWindowProperty { get; } = 
		Microsoft.UI.Xaml.DependencyProperty.Register(
			nameof(IsFullWindow), typeof(bool), 
			typeof(global::Microsoft.UI.Xaml.Controls.MediaPlayerPresenter), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
		public static global::Microsoft.UI.Xaml.DependencyProperty MediaPlayerProperty { get; } = 
		Microsoft.UI.Xaml.DependencyProperty.Register(
			nameof(MediaPlayer), typeof(global::Windows.Media.Playback.MediaPlayer), 
			typeof(global::Microsoft.UI.Xaml.Controls.MediaPlayerPresenter), 
			new FrameworkPropertyMetadata(default(global::Windows.Media.Playback.MediaPlayer)));
		#endif
		#if false || false || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
		public static global::Microsoft.UI.Xaml.DependencyProperty StretchProperty { get; } = 
		Microsoft.UI.Xaml.DependencyProperty.Register(
			nameof(Stretch), typeof(global::Microsoft.UI.Xaml.Media.Stretch), 
			typeof(global::Microsoft.UI.Xaml.Controls.MediaPlayerPresenter), 
			new FrameworkPropertyMetadata(default(global::Microsoft.UI.Xaml.Media.Stretch)));
		#endif
		#if false || false || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
		public MediaPlayerPresenter() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.MediaPlayerPresenter", "MediaPlayerPresenter.MediaPlayerPresenter()");
		}
		#endif
		// Forced skipping of method Microsoft.UI.Xaml.Controls.MediaPlayerPresenter.MediaPlayerPresenter()
		// Forced skipping of method Microsoft.UI.Xaml.Controls.MediaPlayerPresenter.MediaPlayer.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.MediaPlayerPresenter.MediaPlayer.set
		// Forced skipping of method Microsoft.UI.Xaml.Controls.MediaPlayerPresenter.Stretch.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.MediaPlayerPresenter.Stretch.set
		// Forced skipping of method Microsoft.UI.Xaml.Controls.MediaPlayerPresenter.IsFullWindow.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.MediaPlayerPresenter.IsFullWindow.set
		// Forced skipping of method Microsoft.UI.Xaml.Controls.MediaPlayerPresenter.MediaPlayerProperty.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.MediaPlayerPresenter.StretchProperty.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.MediaPlayerPresenter.IsFullWindowProperty.get
	}
}
