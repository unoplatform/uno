#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.Preview
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class TwoPanelHingedDevicePosturePreview 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.System.Preview.TwoPanelHingedDevicePosturePreviewReading> GetCurrentPostureAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<TwoPanelHingedDevicePosturePreviewReading> TwoPanelHingedDevicePosturePreview.GetCurrentPostureAsync() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.System.Preview.TwoPanelHingedDevicePosturePreview.PostureChanged.add
		// Forced skipping of method Windows.System.Preview.TwoPanelHingedDevicePosturePreview.PostureChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.System.Preview.TwoPanelHingedDevicePosturePreview> GetDefaultAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<TwoPanelHingedDevicePosturePreview> TwoPanelHingedDevicePosturePreview.GetDefaultAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.System.Preview.TwoPanelHingedDevicePosturePreview, global::Windows.System.Preview.TwoPanelHingedDevicePosturePreviewReadingChangedEventArgs> PostureChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.Preview.TwoPanelHingedDevicePosturePreview", "event TypedEventHandler<TwoPanelHingedDevicePosturePreview, TwoPanelHingedDevicePosturePreviewReadingChangedEventArgs> TwoPanelHingedDevicePosturePreview.PostureChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.Preview.TwoPanelHingedDevicePosturePreview", "event TypedEventHandler<TwoPanelHingedDevicePosturePreview, TwoPanelHingedDevicePosturePreviewReadingChangedEventArgs> TwoPanelHingedDevicePosturePreview.PostureChanged");
			}
		}
		#endif
	}
}
