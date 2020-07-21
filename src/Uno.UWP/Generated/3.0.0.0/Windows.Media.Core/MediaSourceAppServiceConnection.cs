#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MediaSourceAppServiceConnection 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public MediaSourceAppServiceConnection( global::Windows.ApplicationModel.AppService.AppServiceConnection appServiceConnection) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MediaSourceAppServiceConnection", "MediaSourceAppServiceConnection.MediaSourceAppServiceConnection(AppServiceConnection appServiceConnection)");
		}
		#endif
		// Forced skipping of method Windows.Media.Core.MediaSourceAppServiceConnection.MediaSourceAppServiceConnection(Windows.ApplicationModel.AppService.AppServiceConnection)
		// Forced skipping of method Windows.Media.Core.MediaSourceAppServiceConnection.InitializeMediaStreamSourceRequested.add
		// Forced skipping of method Windows.Media.Core.MediaSourceAppServiceConnection.InitializeMediaStreamSourceRequested.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Start()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MediaSourceAppServiceConnection", "void MediaSourceAppServiceConnection.Start()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Core.MediaSourceAppServiceConnection, global::Windows.Media.Core.InitializeMediaStreamSourceRequestedEventArgs> InitializeMediaStreamSourceRequested
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MediaSourceAppServiceConnection", "event TypedEventHandler<MediaSourceAppServiceConnection, InitializeMediaStreamSourceRequestedEventArgs> MediaSourceAppServiceConnection.InitializeMediaStreamSourceRequested");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MediaSourceAppServiceConnection", "event TypedEventHandler<MediaSourceAppServiceConnection, InitializeMediaStreamSourceRequestedEventArgs> MediaSourceAppServiceConnection.InitializeMediaStreamSourceRequested");
			}
		}
		#endif
	}
}
