#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls.Maps
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MapTargetCameraChangedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Controls.Maps.MapCamera Camera
		{
			get
			{
				throw new global::System.NotImplementedException("The member MapCamera MapTargetCameraChangedEventArgs.Camera is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Controls.Maps.MapCameraChangeReason ChangeReason
		{
			get
			{
				throw new global::System.NotImplementedException("The member MapCameraChangeReason MapTargetCameraChangedEventArgs.ChangeReason is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public MapTargetCameraChangedEventArgs() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Maps.MapTargetCameraChangedEventArgs", "MapTargetCameraChangedEventArgs.MapTargetCameraChangedEventArgs()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapTargetCameraChangedEventArgs.MapTargetCameraChangedEventArgs()
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapTargetCameraChangedEventArgs.Camera.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Maps.MapTargetCameraChangedEventArgs.ChangeReason.get
	}
}
