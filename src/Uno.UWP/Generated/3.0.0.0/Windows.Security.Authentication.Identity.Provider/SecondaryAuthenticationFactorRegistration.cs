#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Authentication.Identity.Provider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SecondaryAuthenticationFactorRegistration 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction FinishRegisteringDeviceAsync( global::Windows.Storage.Streams.IBuffer deviceConfigurationData)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction SecondaryAuthenticationFactorRegistration.FinishRegisteringDeviceAsync(IBuffer deviceConfigurationData) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction AbortRegisteringDeviceAsync( string errorLogMessage)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction SecondaryAuthenticationFactorRegistration.AbortRegisteringDeviceAsync(string errorLogMessage) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Authentication.Identity.Provider.SecondaryAuthenticationFactorDevicePresenceMonitoringRegistrationStatus> RegisterDevicePresenceMonitoringAsync( string deviceId,  string deviceInstancePath,  global::Windows.Security.Authentication.Identity.Provider.SecondaryAuthenticationFactorDevicePresenceMonitoringMode monitoringMode)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<SecondaryAuthenticationFactorDevicePresenceMonitoringRegistrationStatus> SecondaryAuthenticationFactorRegistration.RegisterDevicePresenceMonitoringAsync(string deviceId, string deviceInstancePath, SecondaryAuthenticationFactorDevicePresenceMonitoringMode monitoringMode) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Authentication.Identity.Provider.SecondaryAuthenticationFactorDevicePresenceMonitoringRegistrationStatus> RegisterDevicePresenceMonitoringAsync( string deviceId,  string deviceInstancePath,  global::Windows.Security.Authentication.Identity.Provider.SecondaryAuthenticationFactorDevicePresenceMonitoringMode monitoringMode,  string deviceFriendlyName,  string deviceModelNumber,  global::Windows.Storage.Streams.IBuffer deviceConfigurationData)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<SecondaryAuthenticationFactorDevicePresenceMonitoringRegistrationStatus> SecondaryAuthenticationFactorRegistration.RegisterDevicePresenceMonitoringAsync(string deviceId, string deviceInstancePath, SecondaryAuthenticationFactorDevicePresenceMonitoringMode monitoringMode, string deviceFriendlyName, string deviceModelNumber, IBuffer deviceConfigurationData) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction UnregisterDevicePresenceMonitoringAsync( string deviceId)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction SecondaryAuthenticationFactorRegistration.UnregisterDevicePresenceMonitoringAsync(string deviceId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool IsDevicePresenceMonitoringSupported()
		{
			throw new global::System.NotImplementedException("The member bool SecondaryAuthenticationFactorRegistration.IsDevicePresenceMonitoringSupported() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Authentication.Identity.Provider.SecondaryAuthenticationFactorRegistrationResult> RequestStartRegisteringDeviceAsync( string deviceId,  global::Windows.Security.Authentication.Identity.Provider.SecondaryAuthenticationFactorDeviceCapabilities capabilities,  string deviceFriendlyName,  string deviceModelNumber,  global::Windows.Storage.Streams.IBuffer deviceKey,  global::Windows.Storage.Streams.IBuffer mutualAuthenticationKey)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<SecondaryAuthenticationFactorRegistrationResult> SecondaryAuthenticationFactorRegistration.RequestStartRegisteringDeviceAsync(string deviceId, SecondaryAuthenticationFactorDeviceCapabilities capabilities, string deviceFriendlyName, string deviceModelNumber, IBuffer deviceKey, IBuffer mutualAuthenticationKey) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Security.Authentication.Identity.Provider.SecondaryAuthenticationFactorInfo>> FindAllRegisteredDeviceInfoAsync( global::Windows.Security.Authentication.Identity.Provider.SecondaryAuthenticationFactorDeviceFindScope queryType)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<SecondaryAuthenticationFactorInfo>> SecondaryAuthenticationFactorRegistration.FindAllRegisteredDeviceInfoAsync(SecondaryAuthenticationFactorDeviceFindScope queryType) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction UnregisterDeviceAsync( string deviceId)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction SecondaryAuthenticationFactorRegistration.UnregisterDeviceAsync(string deviceId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction UpdateDeviceConfigurationDataAsync( string deviceId,  global::Windows.Storage.Streams.IBuffer deviceConfigurationData)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction SecondaryAuthenticationFactorRegistration.UpdateDeviceConfigurationDataAsync(string deviceId, IBuffer deviceConfigurationData) is not implemented in Uno.");
		}
		#endif
	}
}
