#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media.Imaging
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class BitmapImage : global::Windows.UI.Xaml.Media.Imaging.BitmapSource
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::System.Uri UriSource
		{
			get
			{
				return (global::System.Uri)this.GetValue(UriSourceProperty);
			}
			set
			{
				this.SetValue(UriSourceProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  int DecodePixelWidth
		{
			get
			{
				return (int)this.GetValue(DecodePixelWidthProperty);
			}
			set
			{
				this.SetValue(DecodePixelWidthProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  int DecodePixelHeight
		{
			get
			{
				return (int)this.GetValue(DecodePixelHeightProperty);
			}
			set
			{
				this.SetValue(DecodePixelHeightProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Media.Imaging.BitmapCreateOptions CreateOptions
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Imaging.BitmapCreateOptions)this.GetValue(CreateOptionsProperty);
			}
			set
			{
				this.SetValue(CreateOptionsProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Media.Imaging.DecodePixelType DecodePixelType
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Imaging.DecodePixelType)this.GetValue(DecodePixelTypeProperty);
			}
			set
			{
				this.SetValue(DecodePixelTypeProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
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
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  bool IsAnimatedBitmap
		{
			get
			{
				return (bool)this.GetValue(IsAnimatedBitmapProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  bool IsPlaying
		{
			get
			{
				return (bool)this.GetValue(IsPlayingProperty);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty CreateOptionsProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"CreateOptions", typeof(global::Windows.UI.Xaml.Media.Imaging.BitmapCreateOptions), 
			typeof(global::Windows.UI.Xaml.Media.Imaging.BitmapImage), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Imaging.BitmapCreateOptions)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty DecodePixelHeightProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"DecodePixelHeight", typeof(int), 
			typeof(global::Windows.UI.Xaml.Media.Imaging.BitmapImage), 
			new FrameworkPropertyMetadata(default(int)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty DecodePixelWidthProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"DecodePixelWidth", typeof(int), 
			typeof(global::Windows.UI.Xaml.Media.Imaging.BitmapImage), 
			new FrameworkPropertyMetadata(default(int)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty UriSourceProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"UriSource", typeof(global::System.Uri), 
			typeof(global::Windows.UI.Xaml.Media.Imaging.BitmapImage), 
			new FrameworkPropertyMetadata(default(global::System.Uri)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty DecodePixelTypeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"DecodePixelType", typeof(global::Windows.UI.Xaml.Media.Imaging.DecodePixelType), 
			typeof(global::Windows.UI.Xaml.Media.Imaging.BitmapImage), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Imaging.DecodePixelType)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty AutoPlayProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"AutoPlay", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Media.Imaging.BitmapImage), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsAnimatedBitmapProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsAnimatedBitmap", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Media.Imaging.BitmapImage), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsPlayingProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsPlaying", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Media.Imaging.BitmapImage), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public BitmapImage( global::System.Uri uriSource) : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Imaging.BitmapImage", "BitmapImage.BitmapImage(Uri uriSource)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.BitmapImage.BitmapImage(System.Uri)
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public BitmapImage() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Imaging.BitmapImage", "BitmapImage.BitmapImage()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.BitmapImage.BitmapImage()
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.BitmapImage.CreateOptions.get
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.BitmapImage.CreateOptions.set
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.BitmapImage.UriSource.get
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.BitmapImage.UriSource.set
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.BitmapImage.DecodePixelWidth.get
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.BitmapImage.DecodePixelWidth.set
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.BitmapImage.DecodePixelHeight.get
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.BitmapImage.DecodePixelHeight.set
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.BitmapImage.DownloadProgress.add
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.BitmapImage.DownloadProgress.remove
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.BitmapImage.ImageOpened.add
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.BitmapImage.ImageOpened.remove
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.BitmapImage.ImageFailed.add
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.BitmapImage.ImageFailed.remove
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.BitmapImage.DecodePixelType.get
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.BitmapImage.DecodePixelType.set
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.BitmapImage.IsAnimatedBitmap.get
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.BitmapImage.IsPlaying.get
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.BitmapImage.AutoPlay.get
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.BitmapImage.AutoPlay.set
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  void Play()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Imaging.BitmapImage", "void BitmapImage.Play()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  void Stop()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Imaging.BitmapImage", "void BitmapImage.Stop()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.BitmapImage.IsAnimatedBitmapProperty.get
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.BitmapImage.IsPlayingProperty.get
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.BitmapImage.AutoPlayProperty.get
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.BitmapImage.DecodePixelTypeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.BitmapImage.CreateOptionsProperty.get
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.BitmapImage.UriSourceProperty.get
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.BitmapImage.DecodePixelWidthProperty.get
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.BitmapImage.DecodePixelHeightProperty.get
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  event global::Windows.UI.Xaml.Media.Imaging.DownloadProgressEventHandler DownloadProgress
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Imaging.BitmapImage", "event DownloadProgressEventHandler BitmapImage.DownloadProgress");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Imaging.BitmapImage", "event DownloadProgressEventHandler BitmapImage.DownloadProgress");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  event global::Windows.UI.Xaml.ExceptionRoutedEventHandler ImageFailed
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Imaging.BitmapImage", "event ExceptionRoutedEventHandler BitmapImage.ImageFailed");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Imaging.BitmapImage", "event ExceptionRoutedEventHandler BitmapImage.ImageFailed");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  event global::Windows.UI.Xaml.RoutedEventHandler ImageOpened
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Imaging.BitmapImage", "event RoutedEventHandler BitmapImage.ImageOpened");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Imaging.BitmapImage", "event RoutedEventHandler BitmapImage.ImageOpened");
			}
		}
		#endif
	}
}
