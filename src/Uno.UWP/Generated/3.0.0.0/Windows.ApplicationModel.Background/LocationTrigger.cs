#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Background
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class LocationTrigger : global::Windows.ApplicationModel.Background.IBackgroundTrigger
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Background.LocationTriggerType TriggerType
		{
			get
			{
				throw new global::System.NotImplementedException("The member LocationTriggerType LocationTrigger.TriggerType is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public LocationTrigger( global::Windows.ApplicationModel.Background.LocationTriggerType triggerType) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Background.LocationTrigger", "LocationTrigger.LocationTrigger(LocationTriggerType triggerType)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Background.LocationTrigger.LocationTrigger(Windows.ApplicationModel.Background.LocationTriggerType)
		// Forced skipping of method Windows.ApplicationModel.Background.LocationTrigger.TriggerType.get
		// Processing: Windows.ApplicationModel.Background.IBackgroundTrigger
	}
}
