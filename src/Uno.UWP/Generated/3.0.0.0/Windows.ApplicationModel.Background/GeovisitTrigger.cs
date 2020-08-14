#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Background
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class GeovisitTrigger : global::Windows.ApplicationModel.Background.IBackgroundTrigger
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Geolocation.VisitMonitoringScope MonitoringScope
		{
			get
			{
				throw new global::System.NotImplementedException("The member VisitMonitoringScope GeovisitTrigger.MonitoringScope is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Background.GeovisitTrigger", "VisitMonitoringScope GeovisitTrigger.MonitoringScope");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public GeovisitTrigger() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Background.GeovisitTrigger", "GeovisitTrigger.GeovisitTrigger()");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Background.GeovisitTrigger.GeovisitTrigger()
		// Forced skipping of method Windows.ApplicationModel.Background.GeovisitTrigger.MonitoringScope.get
		// Forced skipping of method Windows.ApplicationModel.Background.GeovisitTrigger.MonitoringScope.set
		// Processing: Windows.ApplicationModel.Background.IBackgroundTrigger
	}
}
