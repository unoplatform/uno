#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Capture
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AppCapture 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsCapturingAudio
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool AppCapture.IsCapturingAudio is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20AppCapture.IsCapturingAudio");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsCapturingVideo
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool AppCapture.IsCapturingVideo is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20AppCapture.IsCapturingVideo");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Capture.AppCapture.IsCapturingAudio.get
		// Forced skipping of method Windows.Media.Capture.AppCapture.IsCapturingVideo.get
		// Forced skipping of method Windows.Media.Capture.AppCapture.CapturingChanged.add
		// Forced skipping of method Windows.Media.Capture.AppCapture.CapturingChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction SetAllowedAsync( bool allowed)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction AppCapture.SetAllowedAsync(bool allowed) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20AppCapture.SetAllowedAsync%28bool%20allowed%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Media.Capture.AppCapture GetForCurrentView()
		{
			throw new global::System.NotImplementedException("The member AppCapture AppCapture.GetForCurrentView() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=AppCapture%20AppCapture.GetForCurrentView%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Capture.AppCapture, object> CapturingChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.AppCapture", "event TypedEventHandler<AppCapture, object> AppCapture.CapturingChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.AppCapture", "event TypedEventHandler<AppCapture, object> AppCapture.CapturingChanged");
			}
		}
		#endif
	}
}
