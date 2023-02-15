#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Connectivity
{
	#if false || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented("__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	#endif
	public  partial class IPInformation 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Connectivity.NetworkAdapter NetworkAdapter
		{
			get
			{
				throw new global::System.NotImplementedException("The member NetworkAdapter IPInformation.NetworkAdapter is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=NetworkAdapter%20IPInformation.NetworkAdapter");
			}
		}
		#endif
		#if false || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  byte? PrefixLength
		{
			get
			{
				throw new global::System.NotImplementedException("The member byte? IPInformation.PrefixLength is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=byte%3F%20IPInformation.PrefixLength");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.Connectivity.IPInformation.NetworkAdapter.get
		// Forced skipping of method Windows.Networking.Connectivity.IPInformation.PrefixLength.get
	}
}
