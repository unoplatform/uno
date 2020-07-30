#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls.Maps
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MapTileUriRequest 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Uri Uri
		{
			get
			{
				throw new global::System.NotImplementedException("The member Uri MapTileUriRequest.Uri is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapTileUriRequest", "Uri MapTileUriRequest.Uri");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public MapTileUriRequest() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapTileUriRequest", "MapTileUriRequest.MapTileUriRequest()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapTileUriRequest.MapTileUriRequest()
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapTileUriRequest.Uri.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapTileUriRequest.Uri.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Controls.Maps.MapTileUriRequestDeferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member MapTileUriRequestDeferral MapTileUriRequest.GetDeferral() is not implemented in Uno.");
		}
		#endif
	}
}
