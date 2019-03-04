#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Streaming.Adaptive
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AdaptiveMediaSourceDiagnostics 
	{
		// Forced skipping of method Windows.Media.Streaming.Adaptive.AdaptiveMediaSourceDiagnostics.DiagnosticAvailable.add
		// Forced skipping of method Windows.Media.Streaming.Adaptive.AdaptiveMediaSourceDiagnostics.DiagnosticAvailable.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Streaming.Adaptive.AdaptiveMediaSourceDiagnostics, global::Windows.Media.Streaming.Adaptive.AdaptiveMediaSourceDiagnosticAvailableEventArgs> DiagnosticAvailable
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Streaming.Adaptive.AdaptiveMediaSourceDiagnostics", "event TypedEventHandler<AdaptiveMediaSourceDiagnostics, AdaptiveMediaSourceDiagnosticAvailableEventArgs> AdaptiveMediaSourceDiagnostics.DiagnosticAvailable");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Streaming.Adaptive.AdaptiveMediaSourceDiagnostics", "event TypedEventHandler<AdaptiveMediaSourceDiagnostics, AdaptiveMediaSourceDiagnosticAvailableEventArgs> AdaptiveMediaSourceDiagnostics.DiagnosticAvailable");
			}
		}
		#endif
	}
}
