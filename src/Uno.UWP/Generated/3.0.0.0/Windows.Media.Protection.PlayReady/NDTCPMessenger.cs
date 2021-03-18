#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Protection.PlayReady
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class NDTCPMessenger : global::Windows.Media.Protection.PlayReady.INDMessenger
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public NDTCPMessenger( string remoteHostName,  uint remoteHostPort) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Protection.PlayReady.NDTCPMessenger", "NDTCPMessenger.NDTCPMessenger(string remoteHostName, uint remoteHostPort)");
		}
		#endif
		// Forced skipping of method Windows.Media.Protection.PlayReady.NDTCPMessenger.NDTCPMessenger(string, uint)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Protection.PlayReady.INDSendResult> SendRegistrationRequestAsync( byte[] sessionIDBytes,  byte[] challengeDataBytes)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<INDSendResult> NDTCPMessenger.SendRegistrationRequestAsync(byte[] sessionIDBytes, byte[] challengeDataBytes) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Protection.PlayReady.INDSendResult> SendProximityDetectionStartAsync( global::Windows.Media.Protection.PlayReady.NDProximityDetectionType pdType,  byte[] transmitterChannelBytes,  byte[] sessionIDBytes,  byte[] challengeDataBytes)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<INDSendResult> NDTCPMessenger.SendProximityDetectionStartAsync(NDProximityDetectionType pdType, byte[] transmitterChannelBytes, byte[] sessionIDBytes, byte[] challengeDataBytes) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Protection.PlayReady.INDSendResult> SendProximityDetectionResponseAsync( global::Windows.Media.Protection.PlayReady.NDProximityDetectionType pdType,  byte[] transmitterChannelBytes,  byte[] sessionIDBytes,  byte[] responseDataBytes)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<INDSendResult> NDTCPMessenger.SendProximityDetectionResponseAsync(NDProximityDetectionType pdType, byte[] transmitterChannelBytes, byte[] sessionIDBytes, byte[] responseDataBytes) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Protection.PlayReady.INDSendResult> SendLicenseFetchRequestAsync( byte[] sessionIDBytes,  byte[] challengeDataBytes)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<INDSendResult> NDTCPMessenger.SendLicenseFetchRequestAsync(byte[] sessionIDBytes, byte[] challengeDataBytes) is not implemented in Uno.");
		}
		#endif
		// Processing: Windows.Media.Protection.PlayReady.INDMessenger
	}
}
