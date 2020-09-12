#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class EndpointPair 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string RemoteServiceName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string EndpointPair.RemoteServiceName is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.EndpointPair", "string EndpointPair.RemoteServiceName");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.HostName RemoteHostName
		{
			get
			{
				throw new global::System.NotImplementedException("The member HostName EndpointPair.RemoteHostName is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.EndpointPair", "HostName EndpointPair.RemoteHostName");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string LocalServiceName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string EndpointPair.LocalServiceName is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.EndpointPair", "string EndpointPair.LocalServiceName");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.HostName LocalHostName
		{
			get
			{
				throw new global::System.NotImplementedException("The member HostName EndpointPair.LocalHostName is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.EndpointPair", "HostName EndpointPair.LocalHostName");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public EndpointPair( global::Windows.Networking.HostName localHostName,  string localServiceName,  global::Windows.Networking.HostName remoteHostName,  string remoteServiceName) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.EndpointPair", "EndpointPair.EndpointPair(HostName localHostName, string localServiceName, HostName remoteHostName, string remoteServiceName)");
		}
		#endif
		// Forced skipping of method Windows.Networking.EndpointPair.EndpointPair(Windows.Networking.HostName, string, Windows.Networking.HostName, string)
		// Forced skipping of method Windows.Networking.EndpointPair.LocalHostName.get
		// Forced skipping of method Windows.Networking.EndpointPair.LocalHostName.set
		// Forced skipping of method Windows.Networking.EndpointPair.LocalServiceName.get
		// Forced skipping of method Windows.Networking.EndpointPair.LocalServiceName.set
		// Forced skipping of method Windows.Networking.EndpointPair.RemoteHostName.get
		// Forced skipping of method Windows.Networking.EndpointPair.RemoteHostName.set
		// Forced skipping of method Windows.Networking.EndpointPair.RemoteServiceName.get
		// Forced skipping of method Windows.Networking.EndpointPair.RemoteServiceName.set
	}
}
