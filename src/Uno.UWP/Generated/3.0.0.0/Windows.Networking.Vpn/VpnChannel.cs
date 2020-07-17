#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Vpn
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class VpnChannel 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  object PlugInContext
		{
			get
			{
				throw new global::System.NotImplementedException("The member object VpnChannel.PlugInContext is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Vpn.VpnChannel", "object VpnChannel.PlugInContext");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Vpn.VpnChannelConfiguration Configuration
		{
			get
			{
				throw new global::System.NotImplementedException("The member VpnChannelConfiguration VpnChannel.Configuration is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint Id
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint VpnChannel.Id is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Vpn.VpnSystemHealth SystemHealth
		{
			get
			{
				throw new global::System.NotImplementedException("The member VpnSystemHealth VpnChannel.SystemHealth is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  object CurrentRequestTransportContext
		{
			get
			{
				throw new global::System.NotImplementedException("The member object VpnChannel.CurrentRequestTransportContext is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void AssociateTransport( object mainOuterTunnelTransport,  object optionalOuterTunnelTransport)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Vpn.VpnChannel", "void VpnChannel.AssociateTransport(object mainOuterTunnelTransport, object optionalOuterTunnelTransport)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Start( global::System.Collections.Generic.IReadOnlyList<global::Windows.Networking.HostName> assignedClientIPv4list,  global::System.Collections.Generic.IReadOnlyList<global::Windows.Networking.HostName> assignedClientIPv6list,  global::Windows.Networking.Vpn.VpnInterfaceId vpnInterfaceId,  global::Windows.Networking.Vpn.VpnRouteAssignment routeScope,  global::Windows.Networking.Vpn.VpnNamespaceAssignment namespaceScope,  uint mtuSize,  uint maxFrameSize,  bool optimizeForLowCostNetwork,  object mainOuterTunnelTransport,  object optionalOuterTunnelTransport)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Vpn.VpnChannel", "void VpnChannel.Start(IReadOnlyList<HostName> assignedClientIPv4list, IReadOnlyList<HostName> assignedClientIPv6list, VpnInterfaceId vpnInterfaceId, VpnRouteAssignment routeScope, VpnNamespaceAssignment namespaceScope, uint mtuSize, uint maxFrameSize, bool optimizeForLowCostNetwork, object mainOuterTunnelTransport, object optionalOuterTunnelTransport)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Stop()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Vpn.VpnChannel", "void VpnChannel.Stop()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Vpn.VpnPickedCredential RequestCredentials( global::Windows.Networking.Vpn.VpnCredentialType credType,  bool isRetry,  bool isSingleSignOnCredential,  global::Windows.Security.Cryptography.Certificates.Certificate certificate)
		{
			throw new global::System.NotImplementedException("The member VpnPickedCredential VpnChannel.RequestCredentials(VpnCredentialType credType, bool isRetry, bool isSingleSignOnCredential, Certificate certificate) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void RequestVpnPacketBuffer( global::Windows.Networking.Vpn.VpnDataPathType type, out global::Windows.Networking.Vpn.VpnPacketBuffer vpnPacketBuffer)
		{
			throw new global::System.NotImplementedException("The member void VpnChannel.RequestVpnPacketBuffer(VpnDataPathType type, out VpnPacketBuffer vpnPacketBuffer) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void LogDiagnosticMessage( string message)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Vpn.VpnChannel", "void VpnChannel.LogDiagnosticMessage(string message)");
		}
		#endif
		// Forced skipping of method Windows.Networking.Vpn.VpnChannel.Id.get
		// Forced skipping of method Windows.Networking.Vpn.VpnChannel.Configuration.get
		// Forced skipping of method Windows.Networking.Vpn.VpnChannel.ActivityChange.add
		// Forced skipping of method Windows.Networking.Vpn.VpnChannel.ActivityChange.remove
		// Forced skipping of method Windows.Networking.Vpn.VpnChannel.PlugInContext.set
		// Forced skipping of method Windows.Networking.Vpn.VpnChannel.PlugInContext.get
		// Forced skipping of method Windows.Networking.Vpn.VpnChannel.SystemHealth.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void RequestCustomPrompt( global::System.Collections.Generic.IReadOnlyList<global::Windows.Networking.Vpn.IVpnCustomPrompt> customPrompt)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Vpn.VpnChannel", "void VpnChannel.RequestCustomPrompt(IReadOnlyList<IVpnCustomPrompt> customPrompt)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetErrorMessage( string message)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Vpn.VpnChannel", "void VpnChannel.SetErrorMessage(string message)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetAllowedSslTlsVersions( object tunnelTransport,  bool useTls12)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Vpn.VpnChannel", "void VpnChannel.SetAllowedSslTlsVersions(object tunnelTransport, bool useTls12)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void StartWithMainTransport( global::System.Collections.Generic.IReadOnlyList<global::Windows.Networking.HostName> assignedClientIPv4list,  global::System.Collections.Generic.IReadOnlyList<global::Windows.Networking.HostName> assignedClientIPv6list,  global::Windows.Networking.Vpn.VpnInterfaceId vpnInterfaceId,  global::Windows.Networking.Vpn.VpnRouteAssignment assignedRoutes,  global::Windows.Networking.Vpn.VpnDomainNameAssignment assignedDomainName,  uint mtuSize,  uint maxFrameSize,  bool Reserved,  object mainOuterTunnelTransport)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Vpn.VpnChannel", "void VpnChannel.StartWithMainTransport(IReadOnlyList<HostName> assignedClientIPv4list, IReadOnlyList<HostName> assignedClientIPv6list, VpnInterfaceId vpnInterfaceId, VpnRouteAssignment assignedRoutes, VpnDomainNameAssignment assignedDomainName, uint mtuSize, uint maxFrameSize, bool Reserved, object mainOuterTunnelTransport)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void StartExistingTransports( global::System.Collections.Generic.IReadOnlyList<global::Windows.Networking.HostName> assignedClientIPv4list,  global::System.Collections.Generic.IReadOnlyList<global::Windows.Networking.HostName> assignedClientIPv6list,  global::Windows.Networking.Vpn.VpnInterfaceId vpnInterfaceId,  global::Windows.Networking.Vpn.VpnRouteAssignment assignedRoutes,  global::Windows.Networking.Vpn.VpnDomainNameAssignment assignedDomainName,  uint mtuSize,  uint maxFrameSize,  bool Reserved)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Vpn.VpnChannel", "void VpnChannel.StartExistingTransports(IReadOnlyList<HostName> assignedClientIPv4list, IReadOnlyList<HostName> assignedClientIPv6list, VpnInterfaceId vpnInterfaceId, VpnRouteAssignment assignedRoutes, VpnDomainNameAssignment assignedDomainName, uint mtuSize, uint maxFrameSize, bool Reserved)");
		}
		#endif
		// Forced skipping of method Windows.Networking.Vpn.VpnChannel.ActivityStateChange.add
		// Forced skipping of method Windows.Networking.Vpn.VpnChannel.ActivityStateChange.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Vpn.VpnPacketBuffer GetVpnSendPacketBuffer()
		{
			throw new global::System.NotImplementedException("The member VpnPacketBuffer VpnChannel.GetVpnSendPacketBuffer() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Vpn.VpnPacketBuffer GetVpnReceivePacketBuffer()
		{
			throw new global::System.NotImplementedException("The member VpnPacketBuffer VpnChannel.GetVpnReceivePacketBuffer() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction RequestCustomPromptAsync( global::System.Collections.Generic.IReadOnlyList<global::Windows.Networking.Vpn.IVpnCustomPromptElement> customPromptElement)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction VpnChannel.RequestCustomPromptAsync(IReadOnlyList<IVpnCustomPromptElement> customPromptElement) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Networking.Vpn.VpnCredential> RequestCredentialsAsync( global::Windows.Networking.Vpn.VpnCredentialType credType,  uint credOptions,  global::Windows.Security.Cryptography.Certificates.Certificate certificate)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<VpnCredential> VpnChannel.RequestCredentialsAsync(VpnCredentialType credType, uint credOptions, Certificate certificate) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Networking.Vpn.VpnCredential> RequestCredentialsAsync( global::Windows.Networking.Vpn.VpnCredentialType credType,  uint credOptions)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<VpnCredential> VpnChannel.RequestCredentialsAsync(VpnCredentialType credType, uint credOptions) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Networking.Vpn.VpnCredential> RequestCredentialsAsync( global::Windows.Networking.Vpn.VpnCredentialType credType)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<VpnCredential> VpnChannel.RequestCredentialsAsync(VpnCredentialType credType) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void TerminateConnection( string message)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Vpn.VpnChannel", "void VpnChannel.TerminateConnection(string message)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void StartWithTrafficFilter( global::System.Collections.Generic.IReadOnlyList<global::Windows.Networking.HostName> assignedClientIpv4List,  global::System.Collections.Generic.IReadOnlyList<global::Windows.Networking.HostName> assignedClientIpv6List,  global::Windows.Networking.Vpn.VpnInterfaceId vpnInterfaceId,  global::Windows.Networking.Vpn.VpnRouteAssignment assignedRoutes,  global::Windows.Networking.Vpn.VpnDomainNameAssignment assignedNamespace,  uint mtuSize,  uint maxFrameSize,  bool reserved,  object mainOuterTunnelTransport,  object optionalOuterTunnelTransport,  global::Windows.Networking.Vpn.VpnTrafficFilterAssignment assignedTrafficFilters)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Vpn.VpnChannel", "void VpnChannel.StartWithTrafficFilter(IReadOnlyList<HostName> assignedClientIpv4List, IReadOnlyList<HostName> assignedClientIpv6List, VpnInterfaceId vpnInterfaceId, VpnRouteAssignment assignedRoutes, VpnDomainNameAssignment assignedNamespace, uint mtuSize, uint maxFrameSize, bool reserved, object mainOuterTunnelTransport, object optionalOuterTunnelTransport, VpnTrafficFilterAssignment assignedTrafficFilters)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void AddAndAssociateTransport( object transport,  object context)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Vpn.VpnChannel", "void VpnChannel.AddAndAssociateTransport(object transport, object context)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void StartWithTrafficFilter( global::System.Collections.Generic.IEnumerable<global::Windows.Networking.HostName> assignedClientIpv4Addresses,  global::System.Collections.Generic.IEnumerable<global::Windows.Networking.HostName> assignedClientIpv6Addresses,  global::Windows.Networking.Vpn.VpnInterfaceId vpninterfaceId,  global::Windows.Networking.Vpn.VpnRouteAssignment assignedRoutes,  global::Windows.Networking.Vpn.VpnDomainNameAssignment assignedNamespace,  uint mtuSize,  uint maxFrameSize,  bool reserved,  global::System.Collections.Generic.IEnumerable<object> transports,  global::Windows.Networking.Vpn.VpnTrafficFilterAssignment assignedTrafficFilters)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Vpn.VpnChannel", "void VpnChannel.StartWithTrafficFilter(IEnumerable<HostName> assignedClientIpv4Addresses, IEnumerable<HostName> assignedClientIpv6Addresses, VpnInterfaceId vpninterfaceId, VpnRouteAssignment assignedRoutes, VpnDomainNameAssignment assignedNamespace, uint mtuSize, uint maxFrameSize, bool reserved, IEnumerable<object> transports, VpnTrafficFilterAssignment assignedTrafficFilters)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void ReplaceAndAssociateTransport( object transport,  object context)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Vpn.VpnChannel", "void VpnChannel.ReplaceAndAssociateTransport(object transport, object context)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void StartReconnectingTransport( object transport,  object context)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Vpn.VpnChannel", "void VpnChannel.StartReconnectingTransport(object transport, object context)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Sockets.ControlChannelTriggerStatus GetSlotTypeForTransportContext( object context)
		{
			throw new global::System.NotImplementedException("The member ControlChannelTriggerStatus VpnChannel.GetSlotTypeForTransportContext(object context) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Networking.Vpn.VpnChannel.CurrentRequestTransportContext.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void ProcessEventAsync( object thirdPartyPlugIn,  object @event)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Vpn.VpnChannel", "void VpnChannel.ProcessEventAsync(object thirdPartyPlugIn, object @event)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Networking.Vpn.VpnChannel, global::Windows.Networking.Vpn.VpnChannelActivityEventArgs> ActivityChange
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Vpn.VpnChannel", "event TypedEventHandler<VpnChannel, VpnChannelActivityEventArgs> VpnChannel.ActivityChange");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Vpn.VpnChannel", "event TypedEventHandler<VpnChannel, VpnChannelActivityEventArgs> VpnChannel.ActivityChange");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Networking.Vpn.VpnChannel, global::Windows.Networking.Vpn.VpnChannelActivityStateChangedArgs> ActivityStateChange
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Vpn.VpnChannel", "event TypedEventHandler<VpnChannel, VpnChannelActivityStateChangedArgs> VpnChannel.ActivityStateChange");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Vpn.VpnChannel", "event TypedEventHandler<VpnChannel, VpnChannelActivityStateChangedArgs> VpnChannel.ActivityStateChange");
			}
		}
		#endif
	}
}
