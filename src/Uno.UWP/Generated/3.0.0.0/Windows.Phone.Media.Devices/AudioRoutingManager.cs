#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Phone.Media.Devices
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AudioRoutingManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Phone.Media.Devices.AvailableAudioRoutingEndpoints AvailableAudioEndpoints
		{
			get
			{
				throw new global::System.NotImplementedException("The member AvailableAudioRoutingEndpoints AudioRoutingManager.AvailableAudioEndpoints is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Phone.Media.Devices.AudioRoutingEndpoint GetAudioEndpoint()
		{
			throw new global::System.NotImplementedException("The member AudioRoutingEndpoint AudioRoutingManager.GetAudioEndpoint() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetAudioEndpoint( global::Windows.Phone.Media.Devices.AudioRoutingEndpoint endpoint)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Phone.Media.Devices.AudioRoutingManager", "void AudioRoutingManager.SetAudioEndpoint(AudioRoutingEndpoint endpoint)");
		}
		#endif
		// Forced skipping of method Windows.Phone.Media.Devices.AudioRoutingManager.AudioEndpointChanged.add
		// Forced skipping of method Windows.Phone.Media.Devices.AudioRoutingManager.AudioEndpointChanged.remove
		// Forced skipping of method Windows.Phone.Media.Devices.AudioRoutingManager.AvailableAudioEndpoints.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Phone.Media.Devices.AudioRoutingManager GetDefault()
		{
			throw new global::System.NotImplementedException("The member AudioRoutingManager AudioRoutingManager.GetDefault() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Phone.Media.Devices.AudioRoutingManager, object> AudioEndpointChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Phone.Media.Devices.AudioRoutingManager", "event TypedEventHandler<AudioRoutingManager, object> AudioRoutingManager.AudioEndpointChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Phone.Media.Devices.AudioRoutingManager", "event TypedEventHandler<AudioRoutingManager, object> AudioRoutingManager.AudioEndpointChanged");
			}
		}
		#endif
	}
}
