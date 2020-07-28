#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.WiFiDirect.Services
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WiFiDirectService 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IBuffer SessionInfo
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer WiFiDirectService.SessionInfo is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.WiFiDirect.Services.WiFiDirectService", "IBuffer WiFiDirectService.SessionInfo");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool PreferGroupOwnerMode
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool WiFiDirectService.PreferGroupOwnerMode is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.WiFiDirect.Services.WiFiDirectService", "bool WiFiDirectService.PreferGroupOwnerMode");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IBuffer RemoteServiceInfo
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer WiFiDirectService.RemoteServiceInfo is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.WiFiDirect.Services.WiFiDirectServiceError ServiceError
		{
			get
			{
				throw new global::System.NotImplementedException("The member WiFiDirectServiceError WiFiDirectService.ServiceError is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.WiFiDirect.Services.WiFiDirectServiceConfigurationMethod> SupportedConfigurationMethods
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<WiFiDirectServiceConfigurationMethod> WiFiDirectService.SupportedConfigurationMethods is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.WiFiDirect.Services.WiFiDirectService.RemoteServiceInfo.get
		// Forced skipping of method Windows.Devices.WiFiDirect.Services.WiFiDirectService.SupportedConfigurationMethods.get
		// Forced skipping of method Windows.Devices.WiFiDirect.Services.WiFiDirectService.PreferGroupOwnerMode.get
		// Forced skipping of method Windows.Devices.WiFiDirect.Services.WiFiDirectService.PreferGroupOwnerMode.set
		// Forced skipping of method Windows.Devices.WiFiDirect.Services.WiFiDirectService.SessionInfo.get
		// Forced skipping of method Windows.Devices.WiFiDirect.Services.WiFiDirectService.SessionInfo.set
		// Forced skipping of method Windows.Devices.WiFiDirect.Services.WiFiDirectService.ServiceError.get
		// Forced skipping of method Windows.Devices.WiFiDirect.Services.WiFiDirectService.SessionDeferred.add
		// Forced skipping of method Windows.Devices.WiFiDirect.Services.WiFiDirectService.SessionDeferred.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.WiFiDirect.Services.WiFiDirectServiceProvisioningInfo> GetProvisioningInfoAsync( global::Windows.Devices.WiFiDirect.Services.WiFiDirectServiceConfigurationMethod selectedConfigurationMethod)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<WiFiDirectServiceProvisioningInfo> WiFiDirectService.GetProvisioningInfoAsync(WiFiDirectServiceConfigurationMethod selectedConfigurationMethod) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.WiFiDirect.Services.WiFiDirectServiceSession> ConnectAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<WiFiDirectServiceSession> WiFiDirectService.ConnectAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.WiFiDirect.Services.WiFiDirectServiceSession> ConnectAsync( string pin)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<WiFiDirectServiceSession> WiFiDirectService.ConnectAsync(string pin) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string GetSelector( string serviceName)
		{
			throw new global::System.NotImplementedException("The member string WiFiDirectService.GetSelector(string serviceName) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string GetSelector( string serviceName,  global::Windows.Storage.Streams.IBuffer serviceInfoFilter)
		{
			throw new global::System.NotImplementedException("The member string WiFiDirectService.GetSelector(string serviceName, IBuffer serviceInfoFilter) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.WiFiDirect.Services.WiFiDirectService> FromIdAsync( string deviceId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<WiFiDirectService> WiFiDirectService.FromIdAsync(string deviceId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.WiFiDirect.Services.WiFiDirectService, global::Windows.Devices.WiFiDirect.Services.WiFiDirectServiceSessionDeferredEventArgs> SessionDeferred
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.WiFiDirect.Services.WiFiDirectService", "event TypedEventHandler<WiFiDirectService, WiFiDirectServiceSessionDeferredEventArgs> WiFiDirectService.SessionDeferred");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.WiFiDirect.Services.WiFiDirectService", "event TypedEventHandler<WiFiDirectService, WiFiDirectServiceSessionDeferredEventArgs> WiFiDirectService.SessionDeferred");
			}
		}
		#endif
	}
}
