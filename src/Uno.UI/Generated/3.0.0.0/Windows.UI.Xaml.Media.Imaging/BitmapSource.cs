#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media.Imaging
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class BitmapSource : global::Windows.UI.Xaml.Media.ImageSource
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  int PixelHeight
		{
			get
			{
				return (int)this.GetValue(PixelHeightProperty);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  int PixelWidth
		{
			get
			{
				return (int)this.GetValue(PixelWidthProperty);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty PixelHeightProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"PixelHeight", typeof(int), 
			typeof(global::Windows.UI.Xaml.Media.Imaging.BitmapSource), 
			new FrameworkPropertyMetadata(default(int)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty PixelWidthProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"PixelWidth", typeof(int), 
			typeof(global::Windows.UI.Xaml.Media.Imaging.BitmapSource), 
			new FrameworkPropertyMetadata(default(int)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		protected BitmapSource() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Imaging.BitmapSource", "BitmapSource.BitmapSource()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.BitmapSource.BitmapSource()
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.BitmapSource.PixelWidth.get
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.BitmapSource.PixelHeight.get
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  void SetSource( global::Windows.Storage.Streams.IRandomAccessStream streamSource)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Imaging.BitmapSource", "void BitmapSource.SetSource(IRandomAccessStream streamSource)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncAction SetSourceAsync( global::Windows.Storage.Streams.IRandomAccessStream streamSource)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction BitmapSource.SetSourceAsync(IRandomAccessStream streamSource) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.BitmapSource.PixelWidthProperty.get
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.BitmapSource.PixelHeightProperty.get
	}
}
