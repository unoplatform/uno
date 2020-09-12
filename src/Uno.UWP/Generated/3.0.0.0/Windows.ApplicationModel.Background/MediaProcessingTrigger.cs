#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Background
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MediaProcessingTrigger : global::Windows.ApplicationModel.Background.IBackgroundTrigger
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public MediaProcessingTrigger() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Background.MediaProcessingTrigger", "MediaProcessingTrigger.MediaProcessingTrigger()");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Background.MediaProcessingTrigger.MediaProcessingTrigger()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Background.MediaProcessingTriggerResult> RequestAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<MediaProcessingTriggerResult> MediaProcessingTrigger.RequestAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Background.MediaProcessingTriggerResult> RequestAsync( global::Windows.Foundation.Collections.ValueSet arguments)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<MediaProcessingTriggerResult> MediaProcessingTrigger.RequestAsync(ValueSet arguments) is not implemented in Uno.");
		}
		#endif
		// Processing: Windows.ApplicationModel.Background.IBackgroundTrigger
	}
}
