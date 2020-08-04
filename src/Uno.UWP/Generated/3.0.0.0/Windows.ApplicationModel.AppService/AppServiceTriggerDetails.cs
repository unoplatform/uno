#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.AppService
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AppServiceTriggerDetails 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.AppService.AppServiceConnection AppServiceConnection
		{
			get
			{
				throw new global::System.NotImplementedException("The member AppServiceConnection AppServiceTriggerDetails.AppServiceConnection is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string CallerPackageFamilyName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AppServiceTriggerDetails.CallerPackageFamilyName is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Name
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AppServiceTriggerDetails.Name is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsRemoteSystemConnection
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool AppServiceTriggerDetails.IsRemoteSystemConnection is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string CallerRemoteConnectionToken
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AppServiceTriggerDetails.CallerRemoteConnectionToken is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.AppService.AppServiceTriggerDetails.Name.get
		// Forced skipping of method Windows.ApplicationModel.AppService.AppServiceTriggerDetails.CallerPackageFamilyName.get
		// Forced skipping of method Windows.ApplicationModel.AppService.AppServiceTriggerDetails.AppServiceConnection.get
		// Forced skipping of method Windows.ApplicationModel.AppService.AppServiceTriggerDetails.IsRemoteSystemConnection.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> CheckCallerForCapabilityAsync( string capabilityName)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> AppServiceTriggerDetails.CheckCallerForCapabilityAsync(string capabilityName) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.AppService.AppServiceTriggerDetails.CallerRemoteConnectionToken.get
	}
}
