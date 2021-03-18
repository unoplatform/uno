#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Input.Preview
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class GazeInputSourcePreview 
	{
		// Forced skipping of method Windows.Devices.Input.Preview.GazeInputSourcePreview.GazeMoved.add
		// Forced skipping of method Windows.Devices.Input.Preview.GazeInputSourcePreview.GazeMoved.remove
		// Forced skipping of method Windows.Devices.Input.Preview.GazeInputSourcePreview.GazeEntered.add
		// Forced skipping of method Windows.Devices.Input.Preview.GazeInputSourcePreview.GazeEntered.remove
		// Forced skipping of method Windows.Devices.Input.Preview.GazeInputSourcePreview.GazeExited.add
		// Forced skipping of method Windows.Devices.Input.Preview.GazeInputSourcePreview.GazeExited.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Devices.Input.Preview.GazeInputSourcePreview GetForCurrentView()
		{
			throw new global::System.NotImplementedException("The member GazeInputSourcePreview GazeInputSourcePreview.GetForCurrentView() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Devices.Input.Preview.GazeDeviceWatcherPreview CreateWatcher()
		{
			throw new global::System.NotImplementedException("The member GazeDeviceWatcherPreview GazeInputSourcePreview.CreateWatcher() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Input.Preview.GazeInputSourcePreview, global::Windows.Devices.Input.Preview.GazeEnteredPreviewEventArgs> GazeEntered
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Input.Preview.GazeInputSourcePreview", "event TypedEventHandler<GazeInputSourcePreview, GazeEnteredPreviewEventArgs> GazeInputSourcePreview.GazeEntered");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Input.Preview.GazeInputSourcePreview", "event TypedEventHandler<GazeInputSourcePreview, GazeEnteredPreviewEventArgs> GazeInputSourcePreview.GazeEntered");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Input.Preview.GazeInputSourcePreview, global::Windows.Devices.Input.Preview.GazeExitedPreviewEventArgs> GazeExited
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Input.Preview.GazeInputSourcePreview", "event TypedEventHandler<GazeInputSourcePreview, GazeExitedPreviewEventArgs> GazeInputSourcePreview.GazeExited");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Input.Preview.GazeInputSourcePreview", "event TypedEventHandler<GazeInputSourcePreview, GazeExitedPreviewEventArgs> GazeInputSourcePreview.GazeExited");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Input.Preview.GazeInputSourcePreview, global::Windows.Devices.Input.Preview.GazeMovedPreviewEventArgs> GazeMoved
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Input.Preview.GazeInputSourcePreview", "event TypedEventHandler<GazeInputSourcePreview, GazeMovedPreviewEventArgs> GazeInputSourcePreview.GazeMoved");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Input.Preview.GazeInputSourcePreview", "event TypedEventHandler<GazeInputSourcePreview, GazeMovedPreviewEventArgs> GazeInputSourcePreview.GazeMoved");
			}
		}
		#endif
	}
}
