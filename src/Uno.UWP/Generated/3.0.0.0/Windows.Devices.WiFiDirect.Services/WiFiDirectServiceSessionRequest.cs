#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.WiFiDirect.Services
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WiFiDirectServiceSessionRequest : global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Enumeration.DeviceInformation DeviceInformation
		{
			get
			{
				throw new global::System.NotImplementedException("The member DeviceInformation WiFiDirectServiceSessionRequest.DeviceInformation is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.WiFiDirect.Services.WiFiDirectServiceProvisioningInfo ProvisioningInfo
		{
			get
			{
				throw new global::System.NotImplementedException("The member WiFiDirectServiceProvisioningInfo WiFiDirectServiceSessionRequest.ProvisioningInfo is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IBuffer SessionInfo
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer WiFiDirectServiceSessionRequest.SessionInfo is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.WiFiDirect.Services.WiFiDirectServiceSessionRequest.DeviceInformation.get
		// Forced skipping of method Windows.Devices.WiFiDirect.Services.WiFiDirectServiceSessionRequest.ProvisioningInfo.get
		// Forced skipping of method Windows.Devices.WiFiDirect.Services.WiFiDirectServiceSessionRequest.SessionInfo.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.WiFiDirect.Services.WiFiDirectServiceSessionRequest", "void WiFiDirectServiceSessionRequest.Dispose()");
		}
		#endif
		// Processing: System.IDisposable
	}
}
