#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.WiFiDirect
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WiFiDirectConnectionParameters : global::Windows.Devices.Enumeration.IDevicePairingSettings
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  short GroupOwnerIntent
		{
			get
			{
				throw new global::System.NotImplementedException("The member short WiFiDirectConnectionParameters.GroupOwnerIntent is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.WiFiDirect.WiFiDirectConnectionParameters", "short WiFiDirectConnectionParameters.GroupOwnerIntent");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.WiFiDirect.WiFiDirectPairingProcedure PreferredPairingProcedure
		{
			get
			{
				throw new global::System.NotImplementedException("The member WiFiDirectPairingProcedure WiFiDirectConnectionParameters.PreferredPairingProcedure is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.WiFiDirect.WiFiDirectConnectionParameters", "WiFiDirectPairingProcedure WiFiDirectConnectionParameters.PreferredPairingProcedure");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<global::Windows.Devices.WiFiDirect.WiFiDirectConfigurationMethod> PreferenceOrderedConfigurationMethods
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<WiFiDirectConfigurationMethod> WiFiDirectConnectionParameters.PreferenceOrderedConfigurationMethods is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public WiFiDirectConnectionParameters() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.WiFiDirect.WiFiDirectConnectionParameters", "WiFiDirectConnectionParameters.WiFiDirectConnectionParameters()");
		}
		#endif
		// Forced skipping of method Windows.Devices.WiFiDirect.WiFiDirectConnectionParameters.WiFiDirectConnectionParameters()
		// Forced skipping of method Windows.Devices.WiFiDirect.WiFiDirectConnectionParameters.GroupOwnerIntent.get
		// Forced skipping of method Windows.Devices.WiFiDirect.WiFiDirectConnectionParameters.GroupOwnerIntent.set
		// Forced skipping of method Windows.Devices.WiFiDirect.WiFiDirectConnectionParameters.PreferenceOrderedConfigurationMethods.get
		// Forced skipping of method Windows.Devices.WiFiDirect.WiFiDirectConnectionParameters.PreferredPairingProcedure.get
		// Forced skipping of method Windows.Devices.WiFiDirect.WiFiDirectConnectionParameters.PreferredPairingProcedure.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Devices.Enumeration.DevicePairingKinds GetDevicePairingKinds( global::Windows.Devices.WiFiDirect.WiFiDirectConfigurationMethod configurationMethod)
		{
			throw new global::System.NotImplementedException("The member DevicePairingKinds WiFiDirectConnectionParameters.GetDevicePairingKinds(WiFiDirectConfigurationMethod configurationMethod) is not implemented in Uno.");
		}
		#endif
		// Processing: Windows.Devices.Enumeration.IDevicePairingSettings
	}
}
