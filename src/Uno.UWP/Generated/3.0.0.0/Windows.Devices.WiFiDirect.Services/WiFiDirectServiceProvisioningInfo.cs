#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.WiFiDirect.Services
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WiFiDirectServiceProvisioningInfo 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsGroupFormationNeeded
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool WiFiDirectServiceProvisioningInfo.IsGroupFormationNeeded is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.WiFiDirect.Services.WiFiDirectServiceConfigurationMethod SelectedConfigurationMethod
		{
			get
			{
				throw new global::System.NotImplementedException("The member WiFiDirectServiceConfigurationMethod WiFiDirectServiceProvisioningInfo.SelectedConfigurationMethod is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.WiFiDirect.Services.WiFiDirectServiceProvisioningInfo.SelectedConfigurationMethod.get
		// Forced skipping of method Windows.Devices.WiFiDirect.Services.WiFiDirectServiceProvisioningInfo.IsGroupFormationNeeded.get
	}
}
