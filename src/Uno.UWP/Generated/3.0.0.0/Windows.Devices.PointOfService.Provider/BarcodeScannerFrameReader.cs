#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.PointOfService.Provider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class BarcodeScannerFrameReader : global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.PointOfService.Provider.BarcodeScannerProviderConnection Connection
		{
			get
			{
				throw new global::System.NotImplementedException("The member BarcodeScannerProviderConnection BarcodeScannerFrameReader.Connection is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> StartAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> BarcodeScannerFrameReader.StartAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction StopAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction BarcodeScannerFrameReader.StopAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.PointOfService.Provider.BarcodeScannerVideoFrame> TryAcquireLatestFrameAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<BarcodeScannerVideoFrame> BarcodeScannerFrameReader.TryAcquireLatestFrameAsync() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Devices.PointOfService.Provider.BarcodeScannerFrameReader.Connection.get
		// Forced skipping of method Windows.Devices.PointOfService.Provider.BarcodeScannerFrameReader.FrameArrived.add
		// Forced skipping of method Windows.Devices.PointOfService.Provider.BarcodeScannerFrameReader.FrameArrived.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.Provider.BarcodeScannerFrameReader", "void BarcodeScannerFrameReader.Dispose()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.PointOfService.Provider.BarcodeScannerFrameReader, global::Windows.Devices.PointOfService.Provider.BarcodeScannerFrameReaderFrameArrivedEventArgs> FrameArrived
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.Provider.BarcodeScannerFrameReader", "event TypedEventHandler<BarcodeScannerFrameReader, BarcodeScannerFrameReaderFrameArrivedEventArgs> BarcodeScannerFrameReader.FrameArrived");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.PointOfService.Provider.BarcodeScannerFrameReader", "event TypedEventHandler<BarcodeScannerFrameReader, BarcodeScannerFrameReaderFrameArrivedEventArgs> BarcodeScannerFrameReader.FrameArrived");
			}
		}
		#endif
		// Processing: System.IDisposable
	}
}
