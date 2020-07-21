#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Gaming.Input.Custom
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IGipGameControllerInputSink : global::Windows.Gaming.Input.Custom.IGameControllerInputSink
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void OnKeyReceived( ulong timestamp,  byte keyCode,  bool isPressed);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void OnMessageReceived( ulong timestamp,  global::Windows.Gaming.Input.Custom.GipMessageClass messageClass,  byte messageId,  byte sequenceId,  byte[] messageBuffer);
		#endif
	}
}
