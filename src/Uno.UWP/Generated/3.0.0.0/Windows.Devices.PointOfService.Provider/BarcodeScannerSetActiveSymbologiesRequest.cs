#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.PointOfService.Provider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class BarcodeScannerSetActiveSymbologiesRequest 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<uint> Symbologies
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<uint> BarcodeScannerSetActiveSymbologiesRequest.Symbologies is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3Cuint%3E%20BarcodeScannerSetActiveSymbologiesRequest.Symbologies");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.PointOfService.Provider.BarcodeScannerSetActiveSymbologiesRequest.Symbologies.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ReportCompletedAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction BarcodeScannerSetActiveSymbologiesRequest.ReportCompletedAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20BarcodeScannerSetActiveSymbologiesRequest.ReportCompletedAsync%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ReportFailedAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction BarcodeScannerSetActiveSymbologiesRequest.ReportFailedAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20BarcodeScannerSetActiveSymbologiesRequest.ReportFailedAsync%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ReportFailedAsync( int reason)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction BarcodeScannerSetActiveSymbologiesRequest.ReportFailedAsync(int reason) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20BarcodeScannerSetActiveSymbologiesRequest.ReportFailedAsync%28int%20reason%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ReportFailedAsync( int reason,  string failedReasonDescription)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction BarcodeScannerSetActiveSymbologiesRequest.ReportFailedAsync(int reason, string failedReasonDescription) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20BarcodeScannerSetActiveSymbologiesRequest.ReportFailedAsync%28int%20reason%2C%20string%20failedReasonDescription%29");
		}
		#endif
	}
}
