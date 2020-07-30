#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.WiFi
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WiFiAdapter 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Connectivity.NetworkAdapter NetworkAdapter
		{
			get
			{
				throw new global::System.NotImplementedException("The member NetworkAdapter WiFiAdapter.NetworkAdapter is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.WiFi.WiFiNetworkReport NetworkReport
		{
			get
			{
				throw new global::System.NotImplementedException("The member WiFiNetworkReport WiFiAdapter.NetworkReport is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.WiFi.WiFiAdapter.NetworkAdapter.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ScanAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction WiFiAdapter.ScanAsync() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Devices.WiFi.WiFiAdapter.NetworkReport.get
		// Forced skipping of method Windows.Devices.WiFi.WiFiAdapter.AvailableNetworksChanged.add
		// Forced skipping of method Windows.Devices.WiFi.WiFiAdapter.AvailableNetworksChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.WiFi.WiFiConnectionResult> ConnectAsync( global::Windows.Devices.WiFi.WiFiAvailableNetwork availableNetwork,  global::Windows.Devices.WiFi.WiFiReconnectionKind reconnectionKind)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<WiFiConnectionResult> WiFiAdapter.ConnectAsync(WiFiAvailableNetwork availableNetwork, WiFiReconnectionKind reconnectionKind) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.WiFi.WiFiConnectionResult> ConnectAsync( global::Windows.Devices.WiFi.WiFiAvailableNetwork availableNetwork,  global::Windows.Devices.WiFi.WiFiReconnectionKind reconnectionKind,  global::Windows.Security.Credentials.PasswordCredential passwordCredential)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<WiFiConnectionResult> WiFiAdapter.ConnectAsync(WiFiAvailableNetwork availableNetwork, WiFiReconnectionKind reconnectionKind, PasswordCredential passwordCredential) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.WiFi.WiFiConnectionResult> ConnectAsync( global::Windows.Devices.WiFi.WiFiAvailableNetwork availableNetwork,  global::Windows.Devices.WiFi.WiFiReconnectionKind reconnectionKind,  global::Windows.Security.Credentials.PasswordCredential passwordCredential,  string ssid)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<WiFiConnectionResult> WiFiAdapter.ConnectAsync(WiFiAvailableNetwork availableNetwork, WiFiReconnectionKind reconnectionKind, PasswordCredential passwordCredential, string ssid) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Disconnect()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.WiFi.WiFiAdapter", "void WiFiAdapter.Disconnect()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.WiFi.WiFiWpsConfigurationResult> GetWpsConfigurationAsync( global::Windows.Devices.WiFi.WiFiAvailableNetwork availableNetwork)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<WiFiWpsConfigurationResult> WiFiAdapter.GetWpsConfigurationAsync(WiFiAvailableNetwork availableNetwork) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.WiFi.WiFiConnectionResult> ConnectAsync( global::Windows.Devices.WiFi.WiFiAvailableNetwork availableNetwork,  global::Windows.Devices.WiFi.WiFiReconnectionKind reconnectionKind,  global::Windows.Security.Credentials.PasswordCredential passwordCredential,  string ssid,  global::Windows.Devices.WiFi.WiFiConnectionMethod connectionMethod)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<WiFiConnectionResult> WiFiAdapter.ConnectAsync(WiFiAvailableNetwork availableNetwork, WiFiReconnectionKind reconnectionKind, PasswordCredential passwordCredential, string ssid, WiFiConnectionMethod connectionMethod) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.WiFi.WiFiAdapter>> FindAllAdaptersAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<WiFiAdapter>> WiFiAdapter.FindAllAdaptersAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string GetDeviceSelector()
		{
			throw new global::System.NotImplementedException("The member string WiFiAdapter.GetDeviceSelector() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.WiFi.WiFiAdapter> FromIdAsync( string deviceId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<WiFiAdapter> WiFiAdapter.FromIdAsync(string deviceId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.WiFi.WiFiAccessStatus> RequestAccessAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<WiFiAccessStatus> WiFiAdapter.RequestAccessAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.WiFi.WiFiAdapter, object> AvailableNetworksChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.WiFi.WiFiAdapter", "event TypedEventHandler<WiFiAdapter, object> WiFiAdapter.AvailableNetworksChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.WiFi.WiFiAdapter", "event TypedEventHandler<WiFiAdapter, object> WiFiAdapter.AvailableNetworksChanged");
			}
		}
		#endif
	}
}
