#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media.Imaging
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RenderTargetBitmap : global::Windows.UI.Xaml.Media.ImageSource
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int PixelHeight
		{
			get
			{
				return (int)this.GetValue(PixelHeightProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int PixelWidth
		{
			get
			{
				return (int)this.GetValue(PixelWidthProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty PixelHeightProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(PixelHeight), typeof(int), 
			typeof(global::Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap), 
			new FrameworkPropertyMetadata(default(int)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty PixelWidthProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(PixelWidth), typeof(int), 
			typeof(global::Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap), 
			new FrameworkPropertyMetadata(default(int)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public RenderTargetBitmap() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap", "RenderTargetBitmap.RenderTargetBitmap()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap.RenderTargetBitmap()
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap.PixelWidth.get
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap.PixelHeight.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction RenderAsync( global::Windows.UI.Xaml.UIElement element)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction RenderTargetBitmap.RenderAsync(UIElement element) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction RenderAsync( global::Windows.UI.Xaml.UIElement element,  int scaledWidth,  int scaledHeight)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction RenderTargetBitmap.RenderAsync(UIElement element, int scaledWidth, int scaledHeight) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.Streams.IBuffer> GetPixelsAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IBuffer> RenderTargetBitmap.GetPixelsAsync() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap.PixelWidthProperty.get
		// Forced skipping of method Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap.PixelHeightProperty.get
	}
}
