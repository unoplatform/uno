#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Protection.PlayReady
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PlayReadyLicenseManagement 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction DeleteLicenses( global::Windows.Media.Protection.PlayReady.PlayReadyContentHeader contentHeader)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction PlayReadyLicenseManagement.DeleteLicenses(PlayReadyContentHeader contentHeader) is not implemented in Uno.");
		}
		#endif
	}
}
