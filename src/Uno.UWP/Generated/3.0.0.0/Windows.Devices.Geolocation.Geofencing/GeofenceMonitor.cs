#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Geolocation.Geofencing
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class GeofenceMonitor 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<global::Windows.Devices.Geolocation.Geofencing.Geofence> Geofences
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<Geofence> GeofenceMonitor.Geofences is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Geolocation.Geoposition LastKnownGeoposition
		{
			get
			{
				throw new global::System.NotImplementedException("The member Geoposition GeofenceMonitor.LastKnownGeoposition is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Geolocation.Geofencing.GeofenceMonitorStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member GeofenceMonitorStatus GeofenceMonitor.Status is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Devices.Geolocation.Geofencing.GeofenceMonitor Current
		{
			get
			{
				throw new global::System.NotImplementedException("The member GeofenceMonitor GeofenceMonitor.Current is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Geolocation.Geofencing.GeofenceMonitor.Status.get
		// Forced skipping of method Windows.Devices.Geolocation.Geofencing.GeofenceMonitor.Geofences.get
		// Forced skipping of method Windows.Devices.Geolocation.Geofencing.GeofenceMonitor.LastKnownGeoposition.get
		// Forced skipping of method Windows.Devices.Geolocation.Geofencing.GeofenceMonitor.GeofenceStateChanged.add
		// Forced skipping of method Windows.Devices.Geolocation.Geofencing.GeofenceMonitor.GeofenceStateChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.Geolocation.Geofencing.GeofenceStateChangeReport> ReadReports()
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<GeofenceStateChangeReport> GeofenceMonitor.ReadReports() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Devices.Geolocation.Geofencing.GeofenceMonitor.StatusChanged.add
		// Forced skipping of method Windows.Devices.Geolocation.Geofencing.GeofenceMonitor.StatusChanged.remove
		// Forced skipping of method Windows.Devices.Geolocation.Geofencing.GeofenceMonitor.Current.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Geolocation.Geofencing.GeofenceMonitor, object> GeofenceStateChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Geolocation.Geofencing.GeofenceMonitor", "event TypedEventHandler<GeofenceMonitor, object> GeofenceMonitor.GeofenceStateChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Geolocation.Geofencing.GeofenceMonitor", "event TypedEventHandler<GeofenceMonitor, object> GeofenceMonitor.GeofenceStateChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Geolocation.Geofencing.GeofenceMonitor, object> StatusChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Geolocation.Geofencing.GeofenceMonitor", "event TypedEventHandler<GeofenceMonitor, object> GeofenceMonitor.StatusChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Geolocation.Geofencing.GeofenceMonitor", "event TypedEventHandler<GeofenceMonitor, object> GeofenceMonitor.StatusChanged");
			}
		}
		#endif
	}
}
