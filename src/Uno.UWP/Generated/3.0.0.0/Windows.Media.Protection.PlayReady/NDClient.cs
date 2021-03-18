#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Protection.PlayReady
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class NDClient 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public NDClient( global::Windows.Media.Protection.PlayReady.INDDownloadEngine downloadEngine,  global::Windows.Media.Protection.PlayReady.INDStreamParser streamParser,  global::Windows.Media.Protection.PlayReady.INDMessenger pMessenger) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Protection.PlayReady.NDClient", "NDClient.NDClient(INDDownloadEngine downloadEngine, INDStreamParser streamParser, INDMessenger pMessenger)");
		}
		#endif
		// Forced skipping of method Windows.Media.Protection.PlayReady.NDClient.NDClient(Windows.Media.Protection.PlayReady.INDDownloadEngine, Windows.Media.Protection.PlayReady.INDStreamParser, Windows.Media.Protection.PlayReady.INDMessenger)
		// Forced skipping of method Windows.Media.Protection.PlayReady.NDClient.RegistrationCompleted.add
		// Forced skipping of method Windows.Media.Protection.PlayReady.NDClient.RegistrationCompleted.remove
		// Forced skipping of method Windows.Media.Protection.PlayReady.NDClient.ProximityDetectionCompleted.add
		// Forced skipping of method Windows.Media.Protection.PlayReady.NDClient.ProximityDetectionCompleted.remove
		// Forced skipping of method Windows.Media.Protection.PlayReady.NDClient.LicenseFetchCompleted.add
		// Forced skipping of method Windows.Media.Protection.PlayReady.NDClient.LicenseFetchCompleted.remove
		// Forced skipping of method Windows.Media.Protection.PlayReady.NDClient.ReRegistrationNeeded.add
		// Forced skipping of method Windows.Media.Protection.PlayReady.NDClient.ReRegistrationNeeded.remove
		// Forced skipping of method Windows.Media.Protection.PlayReady.NDClient.ClosedCaptionDataReceived.add
		// Forced skipping of method Windows.Media.Protection.PlayReady.NDClient.ClosedCaptionDataReceived.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Protection.PlayReady.INDStartResult> StartAsync( global::System.Uri contentUrl,  uint startAsyncOptions,  global::Windows.Media.Protection.PlayReady.INDCustomData registrationCustomData,  global::Windows.Media.Protection.PlayReady.INDLicenseFetchDescriptor licenseFetchDescriptor)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<INDStartResult> NDClient.StartAsync(Uri contentUrl, uint startAsyncOptions, INDCustomData registrationCustomData, INDLicenseFetchDescriptor licenseFetchDescriptor) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Protection.PlayReady.INDLicenseFetchResult> LicenseFetchAsync( global::Windows.Media.Protection.PlayReady.INDLicenseFetchDescriptor licenseFetchDescriptor)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<INDLicenseFetchResult> NDClient.LicenseFetchAsync(INDLicenseFetchDescriptor licenseFetchDescriptor) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ReRegistrationAsync( global::Windows.Media.Protection.PlayReady.INDCustomData registrationCustomData)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction NDClient.ReRegistrationAsync(INDCustomData registrationCustomData) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Close()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Protection.PlayReady.NDClient", "void NDClient.Close()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Protection.PlayReady.NDClient, global::Windows.Media.Protection.PlayReady.INDClosedCaptionDataReceivedEventArgs> ClosedCaptionDataReceived
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Protection.PlayReady.NDClient", "event TypedEventHandler<NDClient, INDClosedCaptionDataReceivedEventArgs> NDClient.ClosedCaptionDataReceived");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Protection.PlayReady.NDClient", "event TypedEventHandler<NDClient, INDClosedCaptionDataReceivedEventArgs> NDClient.ClosedCaptionDataReceived");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Protection.PlayReady.NDClient, global::Windows.Media.Protection.PlayReady.INDLicenseFetchCompletedEventArgs> LicenseFetchCompleted
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Protection.PlayReady.NDClient", "event TypedEventHandler<NDClient, INDLicenseFetchCompletedEventArgs> NDClient.LicenseFetchCompleted");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Protection.PlayReady.NDClient", "event TypedEventHandler<NDClient, INDLicenseFetchCompletedEventArgs> NDClient.LicenseFetchCompleted");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Protection.PlayReady.NDClient, global::Windows.Media.Protection.PlayReady.INDProximityDetectionCompletedEventArgs> ProximityDetectionCompleted
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Protection.PlayReady.NDClient", "event TypedEventHandler<NDClient, INDProximityDetectionCompletedEventArgs> NDClient.ProximityDetectionCompleted");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Protection.PlayReady.NDClient", "event TypedEventHandler<NDClient, INDProximityDetectionCompletedEventArgs> NDClient.ProximityDetectionCompleted");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Protection.PlayReady.NDClient, object> ReRegistrationNeeded
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Protection.PlayReady.NDClient", "event TypedEventHandler<NDClient, object> NDClient.ReRegistrationNeeded");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Protection.PlayReady.NDClient", "event TypedEventHandler<NDClient, object> NDClient.ReRegistrationNeeded");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Protection.PlayReady.NDClient, global::Windows.Media.Protection.PlayReady.INDRegistrationCompletedEventArgs> RegistrationCompleted
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Protection.PlayReady.NDClient", "event TypedEventHandler<NDClient, INDRegistrationCompletedEventArgs> NDClient.RegistrationCompleted");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Protection.PlayReady.NDClient", "event TypedEventHandler<NDClient, INDRegistrationCompletedEventArgs> NDClient.RegistrationCompleted");
			}
		}
		#endif
	}
}
