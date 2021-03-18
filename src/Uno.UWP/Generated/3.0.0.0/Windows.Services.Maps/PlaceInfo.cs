#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Services.Maps
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PlaceInfo 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string DisplayAddress
		{
			get
			{
				throw new global::System.NotImplementedException("The member string PlaceInfo.DisplayAddress is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string DisplayName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string PlaceInfo.DisplayName is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Geolocation.IGeoshape Geoshape
		{
			get
			{
				throw new global::System.NotImplementedException("The member IGeoshape PlaceInfo.Geoshape is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Identifier
		{
			get
			{
				throw new global::System.NotImplementedException("The member string PlaceInfo.Identifier is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool IsShowSupported
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool PlaceInfo.IsShowSupported is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Show( global::Windows.Foundation.Rect selection)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Services.Maps.PlaceInfo", "void PlaceInfo.Show(Rect selection)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Show( global::Windows.Foundation.Rect selection,  global::Windows.UI.Popups.Placement preferredPlacement)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Services.Maps.PlaceInfo", "void PlaceInfo.Show(Rect selection, Placement preferredPlacement)");
		}
		#endif
		// Forced skipping of method Windows.Services.Maps.PlaceInfo.Identifier.get
		// Forced skipping of method Windows.Services.Maps.PlaceInfo.DisplayName.get
		// Forced skipping of method Windows.Services.Maps.PlaceInfo.DisplayAddress.get
		// Forced skipping of method Windows.Services.Maps.PlaceInfo.Geoshape.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Services.Maps.PlaceInfo CreateFromAddress( string displayAddress)
		{
			throw new global::System.NotImplementedException("The member PlaceInfo PlaceInfo.CreateFromAddress(string displayAddress) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Services.Maps.PlaceInfo CreateFromAddress( string displayAddress,  string displayName)
		{
			throw new global::System.NotImplementedException("The member PlaceInfo PlaceInfo.CreateFromAddress(string displayAddress, string displayName) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Services.Maps.PlaceInfo Create( global::Windows.Devices.Geolocation.Geopoint referencePoint)
		{
			throw new global::System.NotImplementedException("The member PlaceInfo PlaceInfo.Create(Geopoint referencePoint) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Services.Maps.PlaceInfo Create( global::Windows.Devices.Geolocation.Geopoint referencePoint,  global::Windows.Services.Maps.PlaceInfoCreateOptions options)
		{
			throw new global::System.NotImplementedException("The member PlaceInfo PlaceInfo.Create(Geopoint referencePoint, PlaceInfoCreateOptions options) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Services.Maps.PlaceInfo CreateFromIdentifier( string identifier)
		{
			throw new global::System.NotImplementedException("The member PlaceInfo PlaceInfo.CreateFromIdentifier(string identifier) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Services.Maps.PlaceInfo CreateFromIdentifier( string identifier,  global::Windows.Devices.Geolocation.Geopoint defaultPoint,  global::Windows.Services.Maps.PlaceInfoCreateOptions options)
		{
			throw new global::System.NotImplementedException("The member PlaceInfo PlaceInfo.CreateFromIdentifier(string identifier, Geopoint defaultPoint, PlaceInfoCreateOptions options) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Services.Maps.PlaceInfo CreateFromMapLocation( global::Windows.Services.Maps.MapLocation location)
		{
			throw new global::System.NotImplementedException("The member PlaceInfo PlaceInfo.CreateFromMapLocation(MapLocation location) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Services.Maps.PlaceInfo.IsShowSupported.get
	}
}
