#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.WiFi
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WiFiWpsConfigurationResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.WiFi.WiFiWpsConfigurationStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member WiFiWpsConfigurationStatus WiFiWpsConfigurationResult.Status is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=WiFiWpsConfigurationStatus%20WiFiWpsConfigurationResult.Status");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.WiFi.WiFiWpsKind> SupportedWpsKinds
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<WiFiWpsKind> WiFiWpsConfigurationResult.SupportedWpsKinds is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3CWiFiWpsKind%3E%20WiFiWpsConfigurationResult.SupportedWpsKinds");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.WiFi.WiFiWpsConfigurationResult.Status.get
		// Forced skipping of method Windows.Devices.WiFi.WiFiWpsConfigurationResult.SupportedWpsKinds.get
	}
}
