#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Services.Maps
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MapRouteLeg 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Geolocation.GeoboundingBox BoundingBox
		{
			get
			{
				throw new global::System.NotImplementedException("The member GeoboundingBox MapRouteLeg.BoundingBox is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan EstimatedDuration
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan MapRouteLeg.EstimatedDuration is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double LengthInMeters
		{
			get
			{
				throw new global::System.NotImplementedException("The member double MapRouteLeg.LengthInMeters is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Services.Maps.MapRouteManeuver> Maneuvers
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<MapRouteManeuver> MapRouteLeg.Maneuvers is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Geolocation.Geopath Path
		{
			get
			{
				throw new global::System.NotImplementedException("The member Geopath MapRouteLeg.Path is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan DurationWithoutTraffic
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan MapRouteLeg.DurationWithoutTraffic is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Services.Maps.TrafficCongestion TrafficCongestion
		{
			get
			{
				throw new global::System.NotImplementedException("The member TrafficCongestion MapRouteLeg.TrafficCongestion is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Services.Maps.MapRouteLeg.BoundingBox.get
		// Forced skipping of method Windows.Services.Maps.MapRouteLeg.Path.get
		// Forced skipping of method Windows.Services.Maps.MapRouteLeg.LengthInMeters.get
		// Forced skipping of method Windows.Services.Maps.MapRouteLeg.EstimatedDuration.get
		// Forced skipping of method Windows.Services.Maps.MapRouteLeg.Maneuvers.get
		// Forced skipping of method Windows.Services.Maps.MapRouteLeg.DurationWithoutTraffic.get
		// Forced skipping of method Windows.Services.Maps.MapRouteLeg.TrafficCongestion.get
	}
}
