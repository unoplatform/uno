#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.PointOfService.Provider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class BarcodeScannerVideoFrame : global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Imaging.BitmapPixelFormat Format
		{
			get
			{
				throw new global::System.NotImplementedException("The member BitmapPixelFormat BarcodeScannerVideoFrame.Format is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint Height
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint BarcodeScannerVideoFrame.Height is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IBuffer PixelData
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer BarcodeScannerVideoFrame.PixelData is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint Width
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint BarcodeScannerVideoFrame.Width is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.PointOfService.Provider.BarcodeScannerVideoFrame.Format.get
		// Forced skipping of method Windows.Devices.PointOfService.Provider.BarcodeScannerVideoFrame.Width.get
		// Forced skipping of method Windows.Devices.PointOfService.Provider.BarcodeScannerVideoFrame.Height.get
		// Forced skipping of method Windows.Devices.PointOfService.Provider.BarcodeScannerVideoFrame.PixelData.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.Provider.BarcodeScannerVideoFrame", "void BarcodeScannerVideoFrame.Dispose()");
		}
		#endif
		// Processing: System.IDisposable
	}
}
