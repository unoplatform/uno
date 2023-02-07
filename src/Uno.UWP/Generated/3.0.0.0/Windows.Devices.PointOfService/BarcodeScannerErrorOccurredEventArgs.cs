#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.PointOfService
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class BarcodeScannerErrorOccurredEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.PointOfService.UnifiedPosErrorData ErrorData
		{
			get
			{
				throw new global::System.NotImplementedException("The member UnifiedPosErrorData BarcodeScannerErrorOccurredEventArgs.ErrorData is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=UnifiedPosErrorData%20BarcodeScannerErrorOccurredEventArgs.ErrorData");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsRetriable
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool BarcodeScannerErrorOccurredEventArgs.IsRetriable is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20BarcodeScannerErrorOccurredEventArgs.IsRetriable");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.PointOfService.BarcodeScannerReport PartialInputData
		{
			get
			{
				throw new global::System.NotImplementedException("The member BarcodeScannerReport BarcodeScannerErrorOccurredEventArgs.PartialInputData is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=BarcodeScannerReport%20BarcodeScannerErrorOccurredEventArgs.PartialInputData");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.PointOfService.BarcodeScannerErrorOccurredEventArgs.PartialInputData.get
		// Forced skipping of method Windows.Devices.PointOfService.BarcodeScannerErrorOccurredEventArgs.IsRetriable.get
		// Forced skipping of method Windows.Devices.PointOfService.BarcodeScannerErrorOccurredEventArgs.ErrorData.get
	}
}
