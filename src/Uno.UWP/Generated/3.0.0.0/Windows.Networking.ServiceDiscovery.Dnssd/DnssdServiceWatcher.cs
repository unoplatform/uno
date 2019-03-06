#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.ServiceDiscovery.Dnssd
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DnssdServiceWatcher 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Networking.ServiceDiscovery.Dnssd.DnssdServiceWatcherStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member DnssdServiceWatcherStatus DnssdServiceWatcher.Status is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.ServiceDiscovery.Dnssd.DnssdServiceWatcher.Added.add
		// Forced skipping of method Windows.Networking.ServiceDiscovery.Dnssd.DnssdServiceWatcher.Added.remove
		// Forced skipping of method Windows.Networking.ServiceDiscovery.Dnssd.DnssdServiceWatcher.EnumerationCompleted.add
		// Forced skipping of method Windows.Networking.ServiceDiscovery.Dnssd.DnssdServiceWatcher.EnumerationCompleted.remove
		// Forced skipping of method Windows.Networking.ServiceDiscovery.Dnssd.DnssdServiceWatcher.Stopped.add
		// Forced skipping of method Windows.Networking.ServiceDiscovery.Dnssd.DnssdServiceWatcher.Stopped.remove
		// Forced skipping of method Windows.Networking.ServiceDiscovery.Dnssd.DnssdServiceWatcher.Status.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void Start()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.ServiceDiscovery.Dnssd.DnssdServiceWatcher", "void DnssdServiceWatcher.Start()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void Stop()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.ServiceDiscovery.Dnssd.DnssdServiceWatcher", "void DnssdServiceWatcher.Stop()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Networking.ServiceDiscovery.Dnssd.DnssdServiceWatcher, global::Windows.Networking.ServiceDiscovery.Dnssd.DnssdServiceInstance> Added
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.ServiceDiscovery.Dnssd.DnssdServiceWatcher", "event TypedEventHandler<DnssdServiceWatcher, DnssdServiceInstance> DnssdServiceWatcher.Added");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.ServiceDiscovery.Dnssd.DnssdServiceWatcher", "event TypedEventHandler<DnssdServiceWatcher, DnssdServiceInstance> DnssdServiceWatcher.Added");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Networking.ServiceDiscovery.Dnssd.DnssdServiceWatcher, object> EnumerationCompleted
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.ServiceDiscovery.Dnssd.DnssdServiceWatcher", "event TypedEventHandler<DnssdServiceWatcher, object> DnssdServiceWatcher.EnumerationCompleted");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.ServiceDiscovery.Dnssd.DnssdServiceWatcher", "event TypedEventHandler<DnssdServiceWatcher, object> DnssdServiceWatcher.EnumerationCompleted");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Networking.ServiceDiscovery.Dnssd.DnssdServiceWatcher, object> Stopped
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.ServiceDiscovery.Dnssd.DnssdServiceWatcher", "event TypedEventHandler<DnssdServiceWatcher, object> DnssdServiceWatcher.Stopped");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.ServiceDiscovery.Dnssd.DnssdServiceWatcher", "event TypedEventHandler<DnssdServiceWatcher, object> DnssdServiceWatcher.Stopped");
			}
		}
		#endif
	}
}
