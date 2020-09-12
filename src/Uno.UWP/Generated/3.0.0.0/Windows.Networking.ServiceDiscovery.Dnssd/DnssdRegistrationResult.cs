#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.ServiceDiscovery.Dnssd
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DnssdRegistrationResult : global::Windows.Foundation.IStringable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool HasInstanceNameChanged
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool DnssdRegistrationResult.HasInstanceNameChanged is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.HostName IPAddress
		{
			get
			{
				throw new global::System.NotImplementedException("The member HostName DnssdRegistrationResult.IPAddress is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.ServiceDiscovery.Dnssd.DnssdRegistrationStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member DnssdRegistrationStatus DnssdRegistrationResult.Status is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public DnssdRegistrationResult() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.ServiceDiscovery.Dnssd.DnssdRegistrationResult", "DnssdRegistrationResult.DnssdRegistrationResult()");
		}
		#endif
		// Forced skipping of method Windows.Networking.ServiceDiscovery.Dnssd.DnssdRegistrationResult.DnssdRegistrationResult()
		// Forced skipping of method Windows.Networking.ServiceDiscovery.Dnssd.DnssdRegistrationResult.Status.get
		// Forced skipping of method Windows.Networking.ServiceDiscovery.Dnssd.DnssdRegistrationResult.IPAddress.get
		// Forced skipping of method Windows.Networking.ServiceDiscovery.Dnssd.DnssdRegistrationResult.HasInstanceNameChanged.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public override string ToString()
		{
			throw new global::System.NotImplementedException("The member string DnssdRegistrationResult.ToString() is not implemented in Uno.");
		}
		#endif
	}
}
