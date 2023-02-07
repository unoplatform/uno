#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.PointOfService.Provider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class BarcodeScannerGetSymbologyAttributesRequest 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint Symbology
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint BarcodeScannerGetSymbologyAttributesRequest.Symbology is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=uint%20BarcodeScannerGetSymbologyAttributesRequest.Symbology");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.PointOfService.Provider.BarcodeScannerGetSymbologyAttributesRequest.Symbology.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ReportCompletedAsync( global::Windows.Devices.PointOfService.BarcodeSymbologyAttributes attributes)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction BarcodeScannerGetSymbologyAttributesRequest.ReportCompletedAsync(BarcodeSymbologyAttributes attributes) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20BarcodeScannerGetSymbologyAttributesRequest.ReportCompletedAsync%28BarcodeSymbologyAttributes%20attributes%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ReportFailedAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction BarcodeScannerGetSymbologyAttributesRequest.ReportFailedAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20BarcodeScannerGetSymbologyAttributesRequest.ReportFailedAsync%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ReportFailedAsync( int reason)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction BarcodeScannerGetSymbologyAttributesRequest.ReportFailedAsync(int reason) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20BarcodeScannerGetSymbologyAttributesRequest.ReportFailedAsync%28int%20reason%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ReportFailedAsync( int reason,  string failedReasonDescription)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction BarcodeScannerGetSymbologyAttributesRequest.ReportFailedAsync(int reason, string failedReasonDescription) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20BarcodeScannerGetSymbologyAttributesRequest.ReportFailedAsync%28int%20reason%2C%20string%20failedReasonDescription%29");
		}
		#endif
	}
}
