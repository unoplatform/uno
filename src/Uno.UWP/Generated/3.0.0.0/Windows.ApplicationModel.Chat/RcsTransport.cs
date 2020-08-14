#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Chat
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RcsTransport 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Chat.RcsTransportConfiguration Configuration
		{
			get
			{
				throw new global::System.NotImplementedException("The member RcsTransportConfiguration RcsTransport.Configuration is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyDictionary<string, object> ExtendedProperties
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyDictionary<string, object> RcsTransport.ExtendedProperties is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsActive
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool RcsTransport.IsActive is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string TransportFriendlyName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string RcsTransport.TransportFriendlyName is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string TransportId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string RcsTransport.TransportId is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Chat.RcsTransport.ExtendedProperties.get
		// Forced skipping of method Windows.ApplicationModel.Chat.RcsTransport.IsActive.get
		// Forced skipping of method Windows.ApplicationModel.Chat.RcsTransport.TransportFriendlyName.get
		// Forced skipping of method Windows.ApplicationModel.Chat.RcsTransport.TransportId.get
		// Forced skipping of method Windows.ApplicationModel.Chat.RcsTransport.Configuration.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsStoreAndForwardEnabled( global::Windows.ApplicationModel.Chat.RcsServiceKind serviceKind)
		{
			throw new global::System.NotImplementedException("The member bool RcsTransport.IsStoreAndForwardEnabled(RcsServiceKind serviceKind) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsServiceKindSupported( global::Windows.ApplicationModel.Chat.RcsServiceKind serviceKind)
		{
			throw new global::System.NotImplementedException("The member bool RcsTransport.IsServiceKindSupported(RcsServiceKind serviceKind) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Chat.RcsTransport.ServiceKindSupportedChanged.add
		// Forced skipping of method Windows.ApplicationModel.Chat.RcsTransport.ServiceKindSupportedChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.ApplicationModel.Chat.RcsTransport, global::Windows.ApplicationModel.Chat.RcsServiceKindSupportedChangedEventArgs> ServiceKindSupportedChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Chat.RcsTransport", "event TypedEventHandler<RcsTransport, RcsServiceKindSupportedChangedEventArgs> RcsTransport.ServiceKindSupportedChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Chat.RcsTransport", "event TypedEventHandler<RcsTransport, RcsServiceKindSupportedChangedEventArgs> RcsTransport.ServiceKindSupportedChanged");
			}
		}
		#endif
	}
}
