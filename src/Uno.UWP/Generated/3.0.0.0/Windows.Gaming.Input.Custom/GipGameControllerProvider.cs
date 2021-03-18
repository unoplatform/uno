#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Gaming.Input.Custom
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class GipGameControllerProvider : global::Windows.Gaming.Input.Custom.IGameControllerProvider
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Gaming.Input.Custom.GameControllerVersionInfo FirmwareVersionInfo
		{
			get
			{
				throw new global::System.NotImplementedException("The member GameControllerVersionInfo GipGameControllerProvider.FirmwareVersionInfo is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ushort HardwareProductId
		{
			get
			{
				throw new global::System.NotImplementedException("The member ushort GipGameControllerProvider.HardwareProductId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ushort HardwareVendorId
		{
			get
			{
				throw new global::System.NotImplementedException("The member ushort GipGameControllerProvider.HardwareVendorId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Gaming.Input.Custom.GameControllerVersionInfo HardwareVersionInfo
		{
			get
			{
				throw new global::System.NotImplementedException("The member GameControllerVersionInfo GipGameControllerProvider.HardwareVersionInfo is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsConnected
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool GipGameControllerProvider.IsConnected is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SendMessage( global::Windows.Gaming.Input.Custom.GipMessageClass messageClass,  byte messageId,  byte[] messageBuffer)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.Custom.GipGameControllerProvider", "void GipGameControllerProvider.SendMessage(GipMessageClass messageClass, byte messageId, byte[] messageBuffer)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SendReceiveMessage( global::Windows.Gaming.Input.Custom.GipMessageClass messageClass,  byte messageId,  byte[] requestMessageBuffer,  byte[] responseMessageBuffer)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.Custom.GipGameControllerProvider", "void GipGameControllerProvider.SendReceiveMessage(GipMessageClass messageClass, byte messageId, byte[] requestMessageBuffer, byte[] responseMessageBuffer)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Gaming.Input.Custom.GipFirmwareUpdateResult, global::Windows.Gaming.Input.Custom.GipFirmwareUpdateProgress> UpdateFirmwareAsync( global::Windows.Storage.Streams.IInputStream firmwareImage)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<GipFirmwareUpdateResult, GipFirmwareUpdateProgress> GipGameControllerProvider.UpdateFirmwareAsync(IInputStream firmwareImage) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Gaming.Input.Custom.GipGameControllerProvider.FirmwareVersionInfo.get
		// Forced skipping of method Windows.Gaming.Input.Custom.GipGameControllerProvider.HardwareProductId.get
		// Forced skipping of method Windows.Gaming.Input.Custom.GipGameControllerProvider.HardwareVendorId.get
		// Forced skipping of method Windows.Gaming.Input.Custom.GipGameControllerProvider.HardwareVersionInfo.get
		// Forced skipping of method Windows.Gaming.Input.Custom.GipGameControllerProvider.IsConnected.get
		// Processing: Windows.Gaming.Input.Custom.IGameControllerProvider
	}
}
