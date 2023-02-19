#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Calls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public static partial class PhoneCallVideoCapabilitiesManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Calls.PhoneCallVideoCapabilities> GetCapabilitiesAsync( string phoneNumber)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<PhoneCallVideoCapabilities> PhoneCallVideoCapabilitiesManager.GetCapabilitiesAsync(string phoneNumber) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CPhoneCallVideoCapabilities%3E%20PhoneCallVideoCapabilitiesManager.GetCapabilitiesAsync%28string%20phoneNumber%29");
		}
		#endif
	}
}
