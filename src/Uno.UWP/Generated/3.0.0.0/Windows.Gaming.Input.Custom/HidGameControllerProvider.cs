#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Gaming.Input.Custom
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class HidGameControllerProvider : global::Windows.Gaming.Input.Custom.IGameControllerProvider
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Gaming.Input.Custom.GameControllerVersionInfo FirmwareVersionInfo
		{
			get
			{
				throw new global::System.NotImplementedException("The member GameControllerVersionInfo HidGameControllerProvider.FirmwareVersionInfo is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ushort HardwareProductId
		{
			get
			{
				throw new global::System.NotImplementedException("The member ushort HidGameControllerProvider.HardwareProductId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ushort HardwareVendorId
		{
			get
			{
				throw new global::System.NotImplementedException("The member ushort HidGameControllerProvider.HardwareVendorId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Gaming.Input.Custom.GameControllerVersionInfo HardwareVersionInfo
		{
			get
			{
				throw new global::System.NotImplementedException("The member GameControllerVersionInfo HidGameControllerProvider.HardwareVersionInfo is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsConnected
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool HidGameControllerProvider.IsConnected is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ushort UsageId
		{
			get
			{
				throw new global::System.NotImplementedException("The member ushort HidGameControllerProvider.UsageId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ushort UsagePage
		{
			get
			{
				throw new global::System.NotImplementedException("The member ushort HidGameControllerProvider.UsagePage is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Gaming.Input.Custom.HidGameControllerProvider.UsageId.get
		// Forced skipping of method Windows.Gaming.Input.Custom.HidGameControllerProvider.UsagePage.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void GetFeatureReport( byte reportId,  byte[] reportBuffer)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.Custom.HidGameControllerProvider", "void HidGameControllerProvider.GetFeatureReport(byte reportId, byte[] reportBuffer)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SendFeatureReport( byte reportId,  byte[] reportBuffer)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.Custom.HidGameControllerProvider", "void HidGameControllerProvider.SendFeatureReport(byte reportId, byte[] reportBuffer)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SendOutputReport( byte reportId,  byte[] reportBuffer)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.Custom.HidGameControllerProvider", "void HidGameControllerProvider.SendOutputReport(byte reportId, byte[] reportBuffer)");
		}
		#endif
		// Forced skipping of method Windows.Gaming.Input.Custom.HidGameControllerProvider.FirmwareVersionInfo.get
		// Forced skipping of method Windows.Gaming.Input.Custom.HidGameControllerProvider.HardwareProductId.get
		// Forced skipping of method Windows.Gaming.Input.Custom.HidGameControllerProvider.HardwareVendorId.get
		// Forced skipping of method Windows.Gaming.Input.Custom.HidGameControllerProvider.HardwareVersionInfo.get
		// Forced skipping of method Windows.Gaming.Input.Custom.HidGameControllerProvider.IsConnected.get
		// Processing: Windows.Gaming.Input.Custom.IGameControllerProvider
	}
}
