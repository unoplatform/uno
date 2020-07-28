#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.ServiceDiscovery.Dnssd
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DnssdServiceInstanceCollection : global::System.Collections.Generic.IReadOnlyList<global::Windows.Networking.ServiceDiscovery.Dnssd.DnssdServiceInstance>,global::System.Collections.Generic.IEnumerable<global::Windows.Networking.ServiceDiscovery.Dnssd.DnssdServiceInstance>
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint Size
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint DnssdServiceInstanceCollection.Size is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.ServiceDiscovery.Dnssd.DnssdServiceInstanceCollection.GetAt(uint)
		// Forced skipping of method Windows.Networking.ServiceDiscovery.Dnssd.DnssdServiceInstanceCollection.Size.get
		// Forced skipping of method Windows.Networking.ServiceDiscovery.Dnssd.DnssdServiceInstanceCollection.IndexOf(Windows.Networking.ServiceDiscovery.Dnssd.DnssdServiceInstance, out uint)
		// Forced skipping of method Windows.Networking.ServiceDiscovery.Dnssd.DnssdServiceInstanceCollection.GetMany(uint, Windows.Networking.ServiceDiscovery.Dnssd.DnssdServiceInstance[])
		// Forced skipping of method Windows.Networking.ServiceDiscovery.Dnssd.DnssdServiceInstanceCollection.First()
		// Processing: System.Collections.Generic.IReadOnlyList<Windows.Networking.ServiceDiscovery.Dnssd.DnssdServiceInstance>
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public global::Windows.Networking.ServiceDiscovery.Dnssd.DnssdServiceInstance this[int index]
		{
			get
			{
				throw new global::System.NotSupportedException();
			}
			set
			{
				throw new global::System.NotSupportedException();
			}
		}
		#endif
		// Processing: System.Collections.Generic.IEnumerable<Windows.Networking.ServiceDiscovery.Dnssd.DnssdServiceInstance>
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		// DeclaringType: System.Collections.Generic.IEnumerable<Windows.Networking.ServiceDiscovery.Dnssd.DnssdServiceInstance>
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public global::System.Collections.Generic.IEnumerator<global::Windows.Networking.ServiceDiscovery.Dnssd.DnssdServiceInstance> GetEnumerator()
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		// Processing: System.Collections.IEnumerable
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		// DeclaringType: System.Collections.IEnumerable
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		 global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator()
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		// Processing: System.Collections.Generic.IReadOnlyCollection<Windows.Networking.ServiceDiscovery.Dnssd.DnssdServiceInstance>
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public int Count
		{
			get
			{
				throw new global::System.NotSupportedException();
			}
			set
			{
				throw new global::System.NotSupportedException();
			}
		}
		#endif
	}
}
