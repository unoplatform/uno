#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Imaging
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IBitmapFrame 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Graphics.Imaging.BitmapAlphaMode BitmapAlphaMode
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Graphics.Imaging.BitmapPixelFormat BitmapPixelFormat
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Graphics.Imaging.BitmapPropertiesView BitmapProperties
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		double DpiX
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		double DpiY
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		uint OrientedPixelHeight
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		uint OrientedPixelWidth
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		uint PixelHeight
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		uint PixelWidth
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.IAsyncOperation<global::Windows.Graphics.Imaging.ImageStream> GetThumbnailAsync();
		#endif
		// Forced skipping of method Windows.Graphics.Imaging.IBitmapFrame.BitmapProperties.get
		// Forced skipping of method Windows.Graphics.Imaging.IBitmapFrame.BitmapPixelFormat.get
		// Forced skipping of method Windows.Graphics.Imaging.IBitmapFrame.BitmapAlphaMode.get
		// Forced skipping of method Windows.Graphics.Imaging.IBitmapFrame.DpiX.get
		// Forced skipping of method Windows.Graphics.Imaging.IBitmapFrame.DpiY.get
		// Forced skipping of method Windows.Graphics.Imaging.IBitmapFrame.PixelWidth.get
		// Forced skipping of method Windows.Graphics.Imaging.IBitmapFrame.PixelHeight.get
		// Forced skipping of method Windows.Graphics.Imaging.IBitmapFrame.OrientedPixelWidth.get
		// Forced skipping of method Windows.Graphics.Imaging.IBitmapFrame.OrientedPixelHeight.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.IAsyncOperation<global::Windows.Graphics.Imaging.PixelDataProvider> GetPixelDataAsync();
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.IAsyncOperation<global::Windows.Graphics.Imaging.PixelDataProvider> GetPixelDataAsync( global::Windows.Graphics.Imaging.BitmapPixelFormat pixelFormat,  global::Windows.Graphics.Imaging.BitmapAlphaMode alphaMode,  global::Windows.Graphics.Imaging.BitmapTransform transform,  global::Windows.Graphics.Imaging.ExifOrientationMode exifOrientationMode,  global::Windows.Graphics.Imaging.ColorManagementMode colorManagementMode);
		#endif
	}
}
