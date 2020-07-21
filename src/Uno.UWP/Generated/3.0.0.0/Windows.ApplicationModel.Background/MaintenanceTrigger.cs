#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Background
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MaintenanceTrigger : global::Windows.ApplicationModel.Background.IBackgroundTrigger
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint FreshnessTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint MaintenanceTrigger.FreshnessTime is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool OneShot
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool MaintenanceTrigger.OneShot is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public MaintenanceTrigger( uint freshnessTime,  bool oneShot) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Background.MaintenanceTrigger", "MaintenanceTrigger.MaintenanceTrigger(uint freshnessTime, bool oneShot)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Background.MaintenanceTrigger.MaintenanceTrigger(uint, bool)
		// Forced skipping of method Windows.ApplicationModel.Background.MaintenanceTrigger.FreshnessTime.get
		// Forced skipping of method Windows.ApplicationModel.Background.MaintenanceTrigger.OneShot.get
		// Processing: Windows.ApplicationModel.Background.IBackgroundTrigger
	}
}
