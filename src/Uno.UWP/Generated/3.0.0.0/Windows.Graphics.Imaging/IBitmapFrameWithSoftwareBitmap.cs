#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Imaging
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IBitmapFrameWithSoftwareBitmap : global::Windows.Graphics.Imaging.IBitmapFrame
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.IAsyncOperation<global::Windows.Graphics.Imaging.SoftwareBitmap> GetSoftwareBitmapAsync();
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.IAsyncOperation<global::Windows.Graphics.Imaging.SoftwareBitmap> GetSoftwareBitmapAsync( global::Windows.Graphics.Imaging.BitmapPixelFormat pixelFormat,  global::Windows.Graphics.Imaging.BitmapAlphaMode alphaMode);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.IAsyncOperation<global::Windows.Graphics.Imaging.SoftwareBitmap> GetSoftwareBitmapAsync( global::Windows.Graphics.Imaging.BitmapPixelFormat pixelFormat,  global::Windows.Graphics.Imaging.BitmapAlphaMode alphaMode,  global::Windows.Graphics.Imaging.BitmapTransform transform,  global::Windows.Graphics.Imaging.ExifOrientationMode exifOrientationMode,  global::Windows.Graphics.Imaging.ColorManagementMode colorManagementMode);
		#endif
	}
}
