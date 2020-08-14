#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls.Maps
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MapElementPointerEnteredEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Geolocation.Geopoint Location
		{
			get
			{
				throw new global::System.NotImplementedException("The member Geopoint MapElementPointerEnteredEventArgs.Location is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Controls.Maps.MapElement MapElement
		{
			get
			{
				throw new global::System.NotImplementedException("The member MapElement MapElementPointerEnteredEventArgs.MapElement is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Point Position
		{
			get
			{
				throw new global::System.NotImplementedException("The member Point MapElementPointerEnteredEventArgs.Position is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public MapElementPointerEnteredEventArgs() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapElementPointerEnteredEventArgs", "MapElementPointerEnteredEventArgs.MapElementPointerEnteredEventArgs()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapElementPointerEnteredEventArgs.MapElementPointerEnteredEventArgs()
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapElementPointerEnteredEventArgs.Position.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapElementPointerEnteredEventArgs.Location.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapElementPointerEnteredEventArgs.MapElement.get
	}
}
