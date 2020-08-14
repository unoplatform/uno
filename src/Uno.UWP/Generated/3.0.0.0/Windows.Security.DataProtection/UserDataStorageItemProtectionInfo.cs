#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.DataProtection
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class UserDataStorageItemProtectionInfo 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.DataProtection.UserDataAvailability Availability
		{
			get
			{
				throw new global::System.NotImplementedException("The member UserDataAvailability UserDataStorageItemProtectionInfo.Availability is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Security.DataProtection.UserDataStorageItemProtectionInfo.Availability.get
	}
}
