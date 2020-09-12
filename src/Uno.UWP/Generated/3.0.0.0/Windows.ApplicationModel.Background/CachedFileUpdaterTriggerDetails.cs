#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Background
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CachedFileUpdaterTriggerDetails 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool CanRequestUserInput
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CachedFileUpdaterTriggerDetails.CanRequestUserInput is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Provider.FileUpdateRequest UpdateRequest
		{
			get
			{
				throw new global::System.NotImplementedException("The member FileUpdateRequest CachedFileUpdaterTriggerDetails.UpdateRequest is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Provider.CachedFileTarget UpdateTarget
		{
			get
			{
				throw new global::System.NotImplementedException("The member CachedFileTarget CachedFileUpdaterTriggerDetails.UpdateTarget is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Background.CachedFileUpdaterTriggerDetails.UpdateTarget.get
		// Forced skipping of method Windows.ApplicationModel.Background.CachedFileUpdaterTriggerDetails.UpdateRequest.get
		// Forced skipping of method Windows.ApplicationModel.Background.CachedFileUpdaterTriggerDetails.CanRequestUserInput.get
	}
}
