#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Geolocation
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class GeovisitMonitor 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Geolocation.VisitMonitoringScope MonitoringScope
		{
			get
			{
				throw new global::System.NotImplementedException("The member VisitMonitoringScope GeovisitMonitor.MonitoringScope is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public GeovisitMonitor() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Geolocation.GeovisitMonitor", "GeovisitMonitor.GeovisitMonitor()");
		}
		#endif
		// Forced skipping of method Windows.Devices.Geolocation.GeovisitMonitor.GeovisitMonitor()
		// Forced skipping of method Windows.Devices.Geolocation.GeovisitMonitor.MonitoringScope.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Start( global::Windows.Devices.Geolocation.VisitMonitoringScope value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Geolocation.GeovisitMonitor", "void GeovisitMonitor.Start(VisitMonitoringScope value)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Stop()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Geolocation.GeovisitMonitor", "void GeovisitMonitor.Stop()");
		}
		#endif
		// Forced skipping of method Windows.Devices.Geolocation.GeovisitMonitor.VisitStateChanged.add
		// Forced skipping of method Windows.Devices.Geolocation.GeovisitMonitor.VisitStateChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Geolocation.Geovisit> GetLastReportAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<Geovisit> GeovisitMonitor.GetLastReportAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Geolocation.GeovisitMonitor, global::Windows.Devices.Geolocation.GeovisitStateChangedEventArgs> VisitStateChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Geolocation.GeovisitMonitor", "event TypedEventHandler<GeovisitMonitor, GeovisitStateChangedEventArgs> GeovisitMonitor.VisitStateChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Geolocation.GeovisitMonitor", "event TypedEventHandler<GeovisitMonitor, GeovisitStateChangedEventArgs> GeovisitMonitor.VisitStateChanged");
			}
		}
		#endif
	}
}
