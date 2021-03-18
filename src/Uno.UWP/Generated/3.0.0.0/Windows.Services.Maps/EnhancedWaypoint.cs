#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Services.Maps
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class EnhancedWaypoint 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Services.Maps.WaypointKind Kind
		{
			get
			{
				throw new global::System.NotImplementedException("The member WaypointKind EnhancedWaypoint.Kind is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Geolocation.Geopoint Point
		{
			get
			{
				throw new global::System.NotImplementedException("The member Geopoint EnhancedWaypoint.Point is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public EnhancedWaypoint( global::Windows.Devices.Geolocation.Geopoint point,  global::Windows.Services.Maps.WaypointKind kind) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Services.Maps.EnhancedWaypoint", "EnhancedWaypoint.EnhancedWaypoint(Geopoint point, WaypointKind kind)");
		}
		#endif
		// Forced skipping of method Windows.Services.Maps.EnhancedWaypoint.EnhancedWaypoint(Windows.Devices.Geolocation.Geopoint, Windows.Services.Maps.WaypointKind)
		// Forced skipping of method Windows.Services.Maps.EnhancedWaypoint.Point.get
		// Forced skipping of method Windows.Services.Maps.EnhancedWaypoint.Kind.get
	}
}
