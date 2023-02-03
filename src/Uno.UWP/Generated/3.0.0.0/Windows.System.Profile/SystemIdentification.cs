#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.Profile
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public static partial class SystemIdentification 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.System.Profile.SystemIdentificationInfo GetSystemIdForPublisher()
		{
			throw new global::System.NotImplementedException("The member SystemIdentificationInfo SystemIdentification.GetSystemIdForPublisher() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=SystemIdentificationInfo%20SystemIdentification.GetSystemIdForPublisher%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.System.Profile.SystemIdentificationInfo GetSystemIdForUser( global::Windows.System.User user)
		{
			throw new global::System.NotImplementedException("The member SystemIdentificationInfo SystemIdentification.GetSystemIdForUser(User user) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=SystemIdentificationInfo%20SystemIdentification.GetSystemIdForUser%28User%20user%29");
		}
		#endif
	}
}
