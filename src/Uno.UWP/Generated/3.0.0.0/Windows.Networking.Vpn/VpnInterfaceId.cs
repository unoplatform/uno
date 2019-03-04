#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Vpn
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class VpnInterfaceId 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public VpnInterfaceId( byte[] address) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Vpn.VpnInterfaceId", "VpnInterfaceId.VpnInterfaceId(byte[] address)");
		}
		#endif
		// Forced skipping of method Windows.Networking.Vpn.VpnInterfaceId.VpnInterfaceId(byte[])
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void GetAddressInfo(out byte[] id)
		{
			throw new global::System.NotImplementedException("The member void VpnInterfaceId.GetAddressInfo(out byte[] id) is not implemented in Uno.");
		}
		#endif
	}
}
