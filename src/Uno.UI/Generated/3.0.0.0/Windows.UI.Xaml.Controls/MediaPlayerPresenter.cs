#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class MediaPlayerPresenter 
	{
		#if false || false || NET461 || __WASM__ || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Media.Stretch Stretch
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Stretch)this.GetValue(StretchProperty);
			}
			set
			{
				this.SetValue(StretchProperty, value);
			}
		}
		#endif
		#if false || false || NET461 || __WASM__ || false
		[global::Uno.NotImplemented]
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
		#if false || false || NET461 || __WASM__ || false
		[global::Uno.NotImplemented]
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
		#if false || false || NET461 || __WASM__ || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsFullWindowProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(IsFullWindow), typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.MediaPlayerPresenter), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || NET461 || __WASM__ || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty MediaPlayerProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(MediaPlayer), typeof(global::Windows.Media.Playback.MediaPlayer), 
			typeof(global::Windows.UI.Xaml.Controls.MediaPlayerPresenter), 
			new FrameworkPropertyMetadata(default(global::Windows.Media.Playback.MediaPlayer)));
		#endif
		#if false || false || NET461 || __WASM__ || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty StretchProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(Stretch), typeof(global::Windows.UI.Xaml.Media.Stretch), 
			typeof(global::Windows.UI.Xaml.Controls.MediaPlayerPresenter), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Stretch)));
		#endif
		// Skipping already declared method Windows.UI.Xaml.Controls.MediaPlayerPresenter.MediaPlayerPresenter()
		// Forced skipping of method Windows.UI.Xaml.Controls.MediaPlayerPresenter.MediaPlayerPresenter()
		// Forced skipping of method Windows.UI.Xaml.Controls.MediaPlayerPresenter.MediaPlayer.get
		// Forced skipping of method Windows.UI.Xaml.Controls.MediaPlayerPresenter.MediaPlayer.set
		// Forced skipping of method Windows.UI.Xaml.Controls.MediaPlayerPresenter.Stretch.get
		// Forced skipping of method Windows.UI.Xaml.Controls.MediaPlayerPresenter.Stretch.set
		// Forced skipping of method Windows.UI.Xaml.Controls.MediaPlayerPresenter.IsFullWindow.get
		// Forced skipping of method Windows.UI.Xaml.Controls.MediaPlayerPresenter.IsFullWindow.set
		// Forced skipping of method Windows.UI.Xaml.Controls.MediaPlayerPresenter.MediaPlayerProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.MediaPlayerPresenter.StretchProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.MediaPlayerPresenter.IsFullWindowProperty.get
	}
}
