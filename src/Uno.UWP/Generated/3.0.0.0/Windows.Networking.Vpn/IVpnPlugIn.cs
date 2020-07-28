#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Vpn
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IVpnPlugIn 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void Connect( global::Windows.Networking.Vpn.VpnChannel channel);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void Disconnect( global::Windows.Networking.Vpn.VpnChannel channel);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void GetKeepAlivePayload( global::Windows.Networking.Vpn.VpnChannel channel, out global::Windows.Networking.Vpn.VpnPacketBuffer keepAlivePacket);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void Encapsulate( global::Windows.Networking.Vpn.VpnChannel channel,  global::Windows.Networking.Vpn.VpnPacketBufferList packets,  global::Windows.Networking.Vpn.VpnPacketBufferList encapulatedPackets);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void Decapsulate( global::Windows.Networking.Vpn.VpnChannel channel,  global::Windows.Networking.Vpn.VpnPacketBuffer encapBuffer,  global::Windows.Networking.Vpn.VpnPacketBufferList decapsulatedPackets,  global::Windows.Networking.Vpn.VpnPacketBufferList controlPacketsToSend);
		#endif
	}
}
