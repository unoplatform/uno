#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media.Imaging
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SvgImageSource : global::Windows.UI.Xaml.Media.ImageSource
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
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
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double RasterizePixelWidth
		{
			get
			{
				return (double)this.GetValue(RasterizePixelWidthProperty);
			}
			set
			{
				this.SetValue(RasterizePixelWidthProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double RasterizePixelHeight
		{
			get
			{
				return (double)this.GetValue(RasterizePixelHeightProperty);
			}
			set
			{
				this.SetValue(RasterizePixelHeightProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty RasterizePixelHeightProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(RasterizePixelHeight), typeof(double), 
			typeof(global::Windows.UI.Xaml.Media.Imaging.SvgImageSource), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty RasterizePixelWidthProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(RasterizePixelWidth), typeof(double), 
			typeof(global::Windows.UI.Xaml.Media.Imaging.SvgImageSource), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty UriSourceProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(UriSource), typeof(global::System.Uri), 
			typeof(global::Windows.UI.Xaml.Media.Imaging.SvgImageSource), 
			new FrameworkPropertyMetadata(default(global::System.Uri)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public SvgImageSource() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Imaging.SvgImageSource", "SvgImageSource.SvgImageSource()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.SvgImageSource.SvgImageSource()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public SvgImageSource( global::System.Uri uriSource) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Imaging.SvgImageSource", "SvgImageSource.SvgImageSource(Uri uriSource)");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.SvgImageSource.SvgImageSource(System.Uri)
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.SvgImageSource.UriSource.get
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.SvgImageSource.UriSource.set
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.SvgImageSource.RasterizePixelWidth.get
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.SvgImageSource.RasterizePixelWidth.set
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.SvgImageSource.RasterizePixelHeight.get
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.SvgImageSource.RasterizePixelHeight.set
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.SvgImageSource.Opened.add
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.SvgImageSource.Opened.remove
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.SvgImageSource.OpenFailed.add
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.SvgImageSource.OpenFailed.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.UI.Xaml.Media.Imaging.SvgImageSourceLoadStatus> SetSourceAsync( global::Windows.Storage.Streams.IRandomAccessStream streamSource)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<SvgImageSourceLoadStatus> SvgImageSource.SetSourceAsync(IRandomAccessStream streamSource) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.SvgImageSource.UriSourceProperty.get
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.SvgImageSource.RasterizePixelWidthProperty.get
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.SvgImageSource.RasterizePixelHeightProperty.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Media.Imaging.SvgImageSource, global::Windows.UI.Xaml.Media.Imaging.SvgImageSourceFailedEventArgs> OpenFailed
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Imaging.SvgImageSource", "event TypedEventHandler<SvgImageSource, SvgImageSourceFailedEventArgs> SvgImageSource.OpenFailed");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Imaging.SvgImageSource", "event TypedEventHandler<SvgImageSource, SvgImageSourceFailedEventArgs> SvgImageSource.OpenFailed");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Media.Imaging.SvgImageSource, global::Windows.UI.Xaml.Media.Imaging.SvgImageSourceOpenedEventArgs> Opened
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Imaging.SvgImageSource", "event TypedEventHandler<SvgImageSource, SvgImageSourceOpenedEventArgs> SvgImageSource.Opened");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Imaging.SvgImageSource", "event TypedEventHandler<SvgImageSource, SvgImageSourceOpenedEventArgs> SvgImageSource.Opened");
			}
		}
		#endif
	}
}
