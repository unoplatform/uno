#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Geolocation.Geofencing
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class GeofenceStateChangeReport 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Geolocation.Geofencing.Geofence Geofence
		{
			get
			{
				throw new global::System.NotImplementedException("The member Geofence GeofenceStateChangeReport.Geofence is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Geolocation.Geoposition Geoposition
		{
			get
			{
				throw new global::System.NotImplementedException("The member Geoposition GeofenceStateChangeReport.Geoposition is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Geolocation.Geofencing.GeofenceState NewState
		{
			get
			{
				throw new global::System.NotImplementedException("The member GeofenceState GeofenceStateChangeReport.NewState is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Geolocation.Geofencing.GeofenceRemovalReason RemovalReason
		{
			get
			{
				throw new global::System.NotImplementedException("The member GeofenceRemovalReason GeofenceStateChangeReport.RemovalReason is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Geolocation.Geofencing.GeofenceStateChangeReport.NewState.get
		// Forced skipping of method Windows.Devices.Geolocation.Geofencing.GeofenceStateChangeReport.Geofence.get
		// Forced skipping of method Windows.Devices.Geolocation.Geofencing.GeofenceStateChangeReport.Geoposition.get
		// Forced skipping of method Windows.Devices.Geolocation.Geofencing.GeofenceStateChangeReport.RemovalReason.get
	}
}
