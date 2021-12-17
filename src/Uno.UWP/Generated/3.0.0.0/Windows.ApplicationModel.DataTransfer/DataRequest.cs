#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.DataTransfer
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class DataRequest 
	{
		// Skipping already declared property Data
		// Skipping already declared property Deadline
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
		// Skipping already declared method Windows.ApplicationModel.DataTransfer.DataRequest.GetDeferral()
	}
}
