namespace Windows.ApplicationModel.DataTransfer
{
	public  partial class DataRequest 
	{
		#if false
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.DataTransfer.DataPackage Data
		{
			get
			{
				throw new global::System.NotImplementedException("The member DataPackage DataRequest.Data is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.DataRequest", "DataPackage DataRequest.Data");
			}
		}
		#endif
		#if false
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.DateTimeOffset Deadline
		{
			get
			{
				throw new global::System.NotImplementedException("The member DateTimeOffset DataRequest.Deadline is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.DataRequest.Data.get
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.DataRequest.Data.set
		// Forced skipping of method Windows.ApplicationModel.DataTransfer.DataRequest.Deadline.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void FailWithDisplayText( string value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.DataRequest", "void DataRequest.FailWithDisplayText(string value)");
		}
		#endif
		#if false
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.DataTransfer.DataRequestDeferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member DataRequestDeferral DataRequest.GetDeferral() is not implemented in Uno.");
		}
		#endif
	}
}
