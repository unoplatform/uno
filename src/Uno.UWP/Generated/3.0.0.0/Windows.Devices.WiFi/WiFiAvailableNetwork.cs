#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.WiFi
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WiFiAvailableNetwork 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.TimeSpan BeaconInterval
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan WiFiAvailableNetwork.BeaconInterval is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string Bssid
		{
			get
			{
				throw new global::System.NotImplementedException("The member string WiFiAvailableNetwork.Bssid is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  int ChannelCenterFrequencyInKilohertz
		{
			get
			{
				throw new global::System.NotImplementedException("The member int WiFiAvailableNetwork.ChannelCenterFrequencyInKilohertz is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsWiFiDirect
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool WiFiAvailableNetwork.IsWiFiDirect is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Devices.WiFi.WiFiNetworkKind NetworkKind
		{
			get
			{
				throw new global::System.NotImplementedException("The member WiFiNetworkKind WiFiAvailableNetwork.NetworkKind is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  double NetworkRssiInDecibelMilliwatts
		{
			get
			{
				throw new global::System.NotImplementedException("The member double WiFiAvailableNetwork.NetworkRssiInDecibelMilliwatts is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Devices.WiFi.WiFiPhyKind PhyKind
		{
			get
			{
				throw new global::System.NotImplementedException("The member WiFiPhyKind WiFiAvailableNetwork.PhyKind is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Networking.Connectivity.NetworkSecuritySettings SecuritySettings
		{
			get
			{
				throw new global::System.NotImplementedException("The member NetworkSecuritySettings WiFiAvailableNetwork.SecuritySettings is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  byte SignalBars
		{
			get
			{
				throw new global::System.NotImplementedException("The member byte WiFiAvailableNetwork.SignalBars is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string Ssid
		{
			get
			{
				throw new global::System.NotImplementedException("The member string WiFiAvailableNetwork.Ssid is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.TimeSpan Uptime
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan WiFiAvailableNetwork.Uptime is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.WiFi.WiFiAvailableNetwork.Uptime.get
		// Forced skipping of method Windows.Devices.WiFi.WiFiAvailableNetwork.Ssid.get
		// Forced skipping of method Windows.Devices.WiFi.WiFiAvailableNetwork.Bssid.get
		// Forced skipping of method Windows.Devices.WiFi.WiFiAvailableNetwork.ChannelCenterFrequencyInKilohertz.get
		// Forced skipping of method Windows.Devices.WiFi.WiFiAvailableNetwork.NetworkRssiInDecibelMilliwatts.get
		// Forced skipping of method Windows.Devices.WiFi.WiFiAvailableNetwork.SignalBars.get
		// Forced skipping of method Windows.Devices.WiFi.WiFiAvailableNetwork.NetworkKind.get
		// Forced skipping of method Windows.Devices.WiFi.WiFiAvailableNetwork.PhyKind.get
		// Forced skipping of method Windows.Devices.WiFi.WiFiAvailableNetwork.SecuritySettings.get
		// Forced skipping of method Windows.Devices.WiFi.WiFiAvailableNetwork.BeaconInterval.get
		// Forced skipping of method Windows.Devices.WiFi.WiFiAvailableNetwork.IsWiFiDirect.get
	}
}
