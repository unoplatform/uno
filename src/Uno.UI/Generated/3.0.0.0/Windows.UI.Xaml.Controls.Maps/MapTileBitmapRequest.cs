#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls.Maps
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MapTileBitmapRequest 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IRandomAccessStreamReference PixelData
		{
			get
			{
				throw new global::System.NotImplementedException("The member IRandomAccessStreamReference MapTileBitmapRequest.PixelData is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapTileBitmapRequest", "IRandomAccessStreamReference MapTileBitmapRequest.PixelData");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public MapTileBitmapRequest() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapTileBitmapRequest", "MapTileBitmapRequest.MapTileBitmapRequest()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapTileBitmapRequest.MapTileBitmapRequest()
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapTileBitmapRequest.PixelData.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapTileBitmapRequest.PixelData.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Controls.Maps.MapTileBitmapRequestDeferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member MapTileBitmapRequestDeferral MapTileBitmapRequest.GetDeferral() is not implemented in Uno.");
		}
		#endif
	}
}
