#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Foundation.Diagnostics
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RuntimeBrokerErrorSettings : global::Windows.Foundation.Diagnostics.IErrorReportingSettings
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public RuntimeBrokerErrorSettings() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Diagnostics.RuntimeBrokerErrorSettings", "RuntimeBrokerErrorSettings.RuntimeBrokerErrorSettings()");
		}
		#endif
		// Forced skipping of method Windows.Foundation.Diagnostics.RuntimeBrokerErrorSettings.RuntimeBrokerErrorSettings()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetErrorOptions( global::Windows.Foundation.Diagnostics.ErrorOptions value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Diagnostics.RuntimeBrokerErrorSettings", "void RuntimeBrokerErrorSettings.SetErrorOptions(ErrorOptions value)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Diagnostics.ErrorOptions GetErrorOptions()
		{
			throw new global::System.NotImplementedException("The member ErrorOptions RuntimeBrokerErrorSettings.GetErrorOptions() is not implemented in Uno.");
		}
		#endif
		// Processing: Windows.Foundation.Diagnostics.IErrorReportingSettings
	}
}
